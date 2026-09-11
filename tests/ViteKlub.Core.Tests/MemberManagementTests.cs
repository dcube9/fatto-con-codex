using ViteKlub.Core.Data;
using ViteKlub.Core.Members;
using Xunit;

namespace ViteKlub.Core.Tests;

public sealed class MemberManagementTests
{
    private static readonly DateTimeOffset Timestamp = new(2026, 9, 11, 10, 30, 0, TimeSpan.Zero);

    [Fact]
    public void CreateNormalizesDataUsesExplicitIdsAndTimestampAndDoesNotMutateSource()
    {
        DemoDataset source = Dataset() with { Members = [] };
        Guid memberId = Guid.NewGuid(); Guid auditId = Guid.NewGuid();

        MemberOperationResult result = MemberManagement.Create(source,
            Input() with { FirstName = "  Ada   Maria ", Notes = "  Nota   demo " },
            memberId, auditId, source.Users[0].Id, Timestamp);

        Assert.Empty(source.Members);
        Assert.Equal("VK-00001", result.Member.MemberNumber);
        Assert.Equal("Ada Maria", result.Member.FirstName);
        Assert.Equal("Nota demo", result.Member.Notes);
        Assert.Equal(MemberStatus.Active, result.Member.Status);
        Assert.Equal(Timestamp, result.Member.CreatedAtUtc);
        Assert.Equal(Timestamp, result.Member.UpdatedAtUtc);
        AuditEvent audit = Assert.Single(result.Dataset.AuditEvents);
        Assert.Equal((auditId, memberId, "member.created", Timestamp), (audit.Id, audit.EntityId, audit.Action, audit.OccurredAtUtc));
        Assert.DoesNotContain("ada@example.test", audit.Summary, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void NumberGenerationUsesFirstGapAndIgnoresOtherFormats()
    {
        DemoDataset dataset = Dataset();
        Member template = Member();
        Member[] members = [template with { MemberNumber = "VK-00001" }, template with { Id = Guid.NewGuid(), MemberNumber = "VK-00003" }, template with { Id = Guid.NewGuid(), MemberNumber = "OLD-2" }];
        Assert.Equal("VK-00002", MemberManagement.NextMemberNumber(members));
        Assert.Equal("VK-00001", MemberManagement.NextMemberNumber([]));
    }

    [Theory]
    [InlineData("", "Rossi", "ada@example.test", "+39 1", true, "FirstName")]
    [InlineData("Ada", "", "ada@example.test", "+39 1", true, "LastName")]
    [InlineData("Ada", "Rossi", "non-email", "+39 1", true, "Email")]
    [InlineData("Ada", "Rossi", "ada@example.test", "", true, "Phone")]
    [InlineData("Ada", "Rossi", "ada@example.test", "+39 1", false, "PrivacyConsent")]
    public void CreateValidatesRequiredFields(string first, string last, string email, string phone, bool consent, string field)
    {
        DemoDataset source = Dataset();
        MemberValidationException exception = Assert.Throws<MemberValidationException>(() => MemberManagement.Create(source,
            Input() with { FirstName = first, LastName = last, Email = email, Phone = phone, PrivacyConsent = consent },
            Guid.NewGuid(), Guid.NewGuid(), source.Users[0].Id, Timestamp));
        Assert.Contains(field, exception.Errors.Keys);
    }

    [Fact]
    public void CreateRejectsFutureBirthAndJoinedBeforeBirth()
    {
        DemoDataset source = Dataset();
        MemberValidationException exception = Assert.Throws<MemberValidationException>(() => MemberManagement.Create(source,
            Input() with { DateOfBirth = new(2027, 1, 1), JoinedOn = new(2020, 1, 1) },
            Guid.NewGuid(), Guid.NewGuid(), source.Users[0].Id, Timestamp));
        Assert.Contains("DateOfBirth", exception.Errors.Keys);
        Assert.Contains("JoinedOn", exception.Errors.Keys);
    }

    [Fact]
    public void UpdatePreservesImmutableAndRelatedDataAndChecksVersion()
    {
        DemoDataset source = Dataset(); Member original = source.Members[0];
        MemberOperationResult result = MemberManagement.Update(source, original.Id, original.Version,
            Input() with { FirstName = "Nuovo" }, Guid.NewGuid(), source.Users[0].Id, Timestamp);
        Assert.Equal(original.Id, result.Member.Id); Assert.Equal(original.CreatedAtUtc, result.Member.CreatedAtUtc);
        Assert.Equal(original.MemberNumber, result.Member.MemberNumber); Assert.Equal(original.Version + 1, result.Member.Version);
        Assert.Equal(Timestamp, result.Member.UpdatedAtUtc);
        Assert.Same(source.Subscriptions, result.Dataset.Subscriptions); Assert.Same(source.Accesses, result.Dataset.Accesses); Assert.Same(source.Payments, result.Dataset.Payments);
        Assert.Equal(original.FirstName, source.Members[0].FirstName);
        Assert.Throws<MemberValidationException>(() => MemberManagement.Update(source, original.Id, 99, Input(), Guid.NewGuid(), source.Users[0].Id, Timestamp));
    }

    [Fact]
    public void StateTransitionsAreExplicitAndArchivingIsLogical()
    {
        Assert.True(MemberManagement.CanTransition(MemberStatus.Active, MemberStatus.Suspended));
        Assert.True(MemberManagement.CanTransition(MemberStatus.Suspended, MemberStatus.Active));
        Assert.True(MemberManagement.CanTransition(MemberStatus.Active, MemberStatus.Archived));
        Assert.False(MemberManagement.CanTransition(MemberStatus.Archived, MemberStatus.Active));
        DemoDataset source = Dataset(); Member member = source.Members[0];
        MemberOperationResult archived = MemberManagement.ChangeStatus(source, member.Id, member.Version, MemberStatus.Archived,
            Guid.NewGuid(), source.Users[0].Id, Timestamp);
        Assert.Equal(source.Members.Count, archived.Dataset.Members.Count);
        Assert.Equal(source.Subscriptions.Count, archived.Dataset.Subscriptions.Count);
        Assert.Equal(MemberStatus.Archived, archived.Member.Status);
        Assert.Throws<MemberValidationException>(() => MemberManagement.ChangeStatus(archived.Dataset, member.Id, archived.Member.Version,
            MemberStatus.Active, Guid.NewGuid(), source.Users[0].Id, Timestamp));
    }

    private static MemberInput Input() => new("Ada", "Rossi", new(1990, 1, 1), "ada@example.test", "+39 1", new(2020, 1, 1), PrivacyConsent: true);
    private static DemoDataset Dataset()
    {
        DateTimeOffset old = Timestamp.AddYears(-1);
        var user = new DemoUser { Id = Guid.NewGuid(), Username = "admin", DisplayName = "Admin", Role = DemoRole.Administrator, IsActive = true, CreatedAtUtc = old, UpdatedAtUtc = old };
        return new() { SchemaVersion = 1, DatasetVersion = "test", ReferenceDate = new(2026, 9, 1), Users = [user], Members = [Member()], MembershipPlans = [], Subscriptions = [], Accesses = [], Payments = [], AuditEvents = [] };
    }
    private static Member Member() { DateTimeOffset old = Timestamp.AddYears(-1); return new() { Id = Guid.NewGuid(), MemberNumber = "VK-00001", FirstName = "Ada", LastName = "Rossi", DateOfBirth = new(1990, 1, 1), Email = "ada@example.test", Phone = "+39 1", JoinedOn = new(2020, 1, 1), PrivacyConsent = true, Status = MemberStatus.Active, CreatedAtUtc = old, UpdatedAtUtc = old }; }
}
