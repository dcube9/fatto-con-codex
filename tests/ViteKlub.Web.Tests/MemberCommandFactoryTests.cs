using System.Reflection;
using ViteKlub.Core.Data;
using ViteKlub.Core.Members;
using ViteKlub.Web.Members;
using Xunit;

namespace ViteKlub.Web.Tests;

public sealed class MemberCommandFactoryTests
{
    [Fact]
    public void CreateUsesExactlyTheIdsAndTimestampProvidedByTheClientAbstraction()
    {
        DemoDataset dataset = LoadDataset();
        var values = new FixedValues();
        var factory = new MemberCommandFactory(values);
        var input = new MemberInput(
            "Ada", "Rossi", new(1990, 1, 1), "ada@example.test", "+39 1", new(2026, 9, 1),
            PrivacyConsent: true);

        MemberOperationResult result = factory.Create(dataset, input, dataset.Users[0].Id);

        Assert.Equal(values.MemberId, result.Member.Id);
        Assert.Equal(values.Timestamp, result.Member.CreatedAtUtc);
        Assert.Equal(values.Timestamp, result.Member.UpdatedAtUtc);
        AuditEvent audit = result.Dataset.AuditEvents.Single(item => item.Id == values.AuditId);
        Assert.Equal(values.Timestamp, audit.OccurredAtUtc);
    }

    private static DemoDataset LoadDataset()
    {
        Assembly assembly = typeof(MemberCommandFactoryTests).Assembly;
        string resourceName = assembly.GetManifestResourceNames().Single(
            name => name.EndsWith("initial-dataset.json", StringComparison.Ordinal));
        using Stream stream = assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);
        return DemoDatasetJson.Deserialize(reader.ReadToEnd());
    }

    private sealed class FixedValues : IMemberOperationValues
    {
        public DateTimeOffset Timestamp { get; } = new(2026, 9, 11, 12, 34, 56, TimeSpan.Zero);

        public Guid MemberId { get; } = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        public Guid AuditId { get; } = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

        public DateTimeOffset GetTimestamp() => Timestamp;

        public Guid NewMemberId() => MemberId;

        public Guid NewAuditEventId() => AuditId;
    }
}
