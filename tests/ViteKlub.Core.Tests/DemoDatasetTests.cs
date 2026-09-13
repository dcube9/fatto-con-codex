using System.Text.Json;
using System.Text.RegularExpressions;
using ViteKlub.Core.Data;
using Xunit;

namespace ViteKlub.Core.Tests;

public sealed partial class DemoDatasetTests
{
    [Fact]
    public void InitialDatasetIsValidAndHasExpectedSize()
    {
        DemoDataset dataset = LoadInitialDataset();

        IReadOnlyList<DatasetValidationError> errors = DemoDatasetValidator.Validate(dataset);

        Assert.Empty(errors);
        Assert.Equal(4, dataset.Users.Count);
        Assert.Equal(75, dataset.Members.Count);
        Assert.Equal(8, dataset.MembershipPlans.Count);
        Assert.Equal(90, dataset.Subscriptions.Count);
        Assert.Equal(350, dataset.Accesses.Count);
        Assert.Equal(150, dataset.Payments.Count);
        Assert.Equal(25, dataset.AuditEvents.Count);
    }

    [Fact]
    public void ValidatorRejectsDuplicateEntityIdentifiers()
    {
        DemoDataset dataset = LoadInitialDataset();
        Member duplicate = dataset.Members[0] with { MemberNumber = "VK-DUPLICATE" };
        dataset = dataset with { Members = [.. dataset.Members, duplicate] };

        IReadOnlyList<DatasetValidationError> errors = DemoDatasetValidator.Validate(dataset);

        Assert.Contains(errors, error => error.Code == "entity.id.duplicate");
    }

    [Fact]
    public void ValidatorRejectsMissingReferences()
    {
        DemoDataset dataset = LoadInitialDataset();
        MemberSubscription invalid = dataset.Subscriptions[0] with { MemberId = Guid.NewGuid() };
        dataset = dataset with { Subscriptions = [invalid, .. dataset.Subscriptions.Skip(1)] };

        IReadOnlyList<DatasetValidationError> errors = DemoDatasetValidator.Validate(dataset);

        Assert.Contains(errors, error => error.Code == "reference.missing"
            && error.Path == "subscriptions[0].memberId");
    }

    [Fact]
    public void ValidatorAcceptsEnrollmentOnBirthDate()
    {
        DemoDataset source = LoadInitialDataset();
        Member original = source.Members[0];
        Member boundary = original with { JoinedOn = original.DateOfBirth };
        DemoDataset dataset = source with { Members = [boundary, .. source.Members.Skip(1)] };

        IReadOnlyList<DatasetValidationError> errors = DemoDatasetValidator.Validate(dataset);

        Assert.DoesNotContain(errors, error => error.Code == "member.dates.invalid"
            && error.Path == "members[0]");
        Assert.NotEqual(source.Members[0].JoinedOn, source.Members[0].DateOfBirth);
    }

    [Fact]
    public void ValidatorRejectsEnrollmentBeforeBirthDate()
    {
        DemoDataset source = LoadInitialDataset();
        Member original = source.Members[0];
        Member invalid = original with { JoinedOn = original.DateOfBirth.AddDays(-1) };
        DemoDataset dataset = source with { Members = [invalid, .. source.Members.Skip(1)] };

        IReadOnlyList<DatasetValidationError> errors = DemoDatasetValidator.Validate(dataset);

        DatasetValidationError error = Assert.Single(errors,
            error => error.Code == "member.dates.invalid" && error.Path == "members[0]");
        Assert.Equal("member.dates.invalid", error.Code);
        Assert.Equal("members[0]", error.Path);
        Assert.NotEqual(source.Members[0].JoinedOn, invalid.JoinedOn);
    }

    [Fact]
    public void DeserializerRejectsUnknownProperties()
    {
        const string json = """
            {
              "schemaVersion": 1,
              "datasetVersion": "test",
              "referenceDate": "2026-09-01",
              "users": [],
              "members": [],
              "membershipPlans": [],
              "subscriptions": [],
              "accesses": [],
              "payments": [],
              "auditEvents": [],
              "unexpected": true
            }
            """;

        Assert.Throws<JsonException>(() => DemoDatasetJson.Deserialize(json));
    }

    [Fact]
    public void DatasetRoundTripPreservesContent()
    {
        DemoDataset expected = LoadInitialDataset();

        string json = DemoDatasetJson.Serialize(expected);
        DemoDataset actual = DemoDatasetJson.Deserialize(json);

        Assert.Equal(json, DemoDatasetJson.Serialize(actual));
    }

    [Theory]
    [InlineData(2, "2026-09-01T12:30:00.0000000Z")]
    [InlineData(-5, "2026-09-01T19:30:00.0000000Z")]
    public void SerializerAlwaysWritesUtcWithZ(int sourceOffsetHours, string expected)
    {
        DemoDataset source = LoadInitialDataset();
        DateTimeOffset timestamp = new(2026, 9, 1, 14, 30, 0, TimeSpan.FromHours(sourceOffsetHours));
        source = source with { Users = [source.Users[0] with { CreatedAtUtc = timestamp }, .. source.Users.Skip(1)] };

        string json = DemoDatasetJson.Serialize(source);

        Assert.Contains($"\"createdAtUtc\": \"{expected}\"", json, StringComparison.Ordinal);
        Assert.DoesNotMatch("(?:created|updated|occurred)AtUtc\\\": \\\"[^\\\"]*[+-]\\d{2}:\\d{2}", json);
    }

    [Fact]
    public void PreviousSnapshotOffsetRoundTripsAsTheSameUtcInstant()
    {
        DemoDataset source = LoadInitialDataset();
        string json = TimestampValue().Replace(DemoDatasetJson.Serialize(source), "$1\"2026-09-01T14:30:00+02:00\"", 1);

        DemoDataset actual = DemoDatasetJson.Deserialize(json);

        Assert.Equal(new DateTimeOffset(2026, 9, 1, 12, 30, 0, TimeSpan.Zero), actual.Users[0].CreatedAtUtc);
        Assert.Equal(TimeSpan.Zero, actual.Users[0].CreatedAtUtc.Offset);
    }

    [Fact]
    public void DeserializerRejectsTimestampWithoutOffset()
    {
        string json = DemoDatasetJson.Serialize(LoadInitialDataset());
        int timestampStart = json.IndexOf("Z\"", StringComparison.Ordinal);
        json = string.Concat(json.AsSpan(0, timestampStart), json.AsSpan(timestampStart + 1));

        Assert.Throws<JsonException>(() => DemoDatasetJson.Deserialize(json));
    }

    [Fact]
    public void ValidatorRejectsInMemoryTimestampWithNonZeroOffset()
    {
        DemoDataset source = LoadInitialDataset();
        source = source with { Users = [source.Users[0] with { UpdatedAtUtc = source.Users[0].UpdatedAtUtc.ToOffset(TimeSpan.FromHours(2)) }, .. source.Users.Skip(1)] };

        Assert.Contains(DemoDatasetValidator.Validate(source), error => error.Code == "entity.timestamps.notUtc");
    }

    private static DemoDataset LoadInitialDataset()
    {
        System.Reflection.Assembly assembly = typeof(DemoDatasetTests).Assembly;
        string? resourceName = assembly.GetManifestResourceNames().SingleOrDefault(
            name => name.EndsWith("initial-dataset.json", StringComparison.Ordinal));
        if (resourceName is null)
        {
            throw new InvalidOperationException("La risorsa incorporata del dataset iniziale non è disponibile.");
        }

        using Stream stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"La risorsa incorporata '{resourceName}' non è leggibile.");
        using var reader = new StreamReader(stream);
        return DemoDatasetJson.Deserialize(reader.ReadToEnd());
    }

    [GeneratedRegex("(\"createdAtUtc\"\\s*:\\s*)\"[^\"]+\"")]
    private static partial Regex TimestampValue();

}
