using System.Text.Json;
using ViteKlub.Core.Data;
using Xunit;

namespace ViteKlub.Core.Tests;

public sealed class DemoDatasetTests
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
}
