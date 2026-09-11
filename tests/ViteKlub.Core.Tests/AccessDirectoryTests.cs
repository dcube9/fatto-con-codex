using ViteKlub.Core.Accesses;
using ViteKlub.Core.Data;
using Xunit;

namespace ViteKlub.Core.Tests;

public sealed class AccessDirectoryTests
{
    private static readonly DateTimeOffset Timestamp = new(2026, 9, 1, 12, 30, 0, TimeSpan.Zero);

    [Fact]
    public void ProjectsAllRelationshipsWithoutChangingSources()
    {
        GymAccess access = Access(1, GuidFrom(11), GuidFrom(21), GuidFrom(31));
        GymAccess originalAccess = access with { };
        Member member = Member(GuidFrom(11), "VK-001", "Anna", "Bianchi");
        MemberSubscription subscription = Subscription(GuidFrom(21), member.Id);
        DemoUser user = User(GuidFrom(31), "Rebecca Reception", "reception.demo");

        AccessDirectoryItem item = Assert.Single(AccessDirectory.Project([access], [member], [subscription], [user]));

        Assert.Equal(("VK-001", "Anna Bianchi", true), (item.MemberNumber, item.MemberFullName, item.HasMember));
        Assert.Equal((subscription.Id, true), (item.SubscriptionId, item.HasSubscription));
        Assert.Equal("Rebecca Reception (reception.demo)", item.RecordedBy);
        Assert.Equal(originalAccess, access);
    }

    [Fact]
    public void ProjectsEmptyDataset() => Assert.Empty(AccessDirectory.Project([], [], [], []));

    [Fact]
    public void DistinguishesAbsentAndMissingReferences()
    {
        AccessDirectoryItem absent = Assert.Single(AccessDirectory.Project([Access(1, GuidFrom(11), null, GuidFrom(31))], [], [], []));
        AccessDirectoryItem missing = Assert.Single(AccessDirectory.Project([Access(2, GuidFrom(11), GuidFrom(99), GuidFrom(31))], [], [], []));

        Assert.Equal(AccessDirectory.MissingMemberLabel, absent.MemberFullName);
        Assert.Equal(AccessDirectory.MissingUserLabel, absent.RecordedBy);
        Assert.False(absent.HasMember);
        Assert.False(absent.HasRecordedByUser);
        Assert.Equal(AccessDirectory.NoSubscriptionLabel, absent.SubscriptionLabel);
        Assert.Equal(AccessDirectory.MissingSubscriptionLabel, missing.SubscriptionLabel);
        Assert.False(missing.HasSubscription);
    }

    [Theory]
    [InlineData("VK-001")]
    [InlineData("anna")]
    [InlineData("bianchi")]
    [InlineData("Anna Bianchi")]
    [InlineData("reception.demo")]
    [InlineData("REBECCA RECEPTION")]
    [InlineData("  Anna    Bianchi  ")]
    public void SearchesRequiredFieldsCaseInsensitivelyAndNormalizesSpaces(string search)
    {
        Assert.Equal(GuidFrom(1), Assert.Single(AccessDirectory.Query(Items(), new(Search: search)).Items).Id);
    }

    [Theory]
    [InlineData(AccessOutcome.Granted)]
    [InlineData(AccessOutcome.Denied)]
    [InlineData(AccessOutcome.Cancelled)]
    public void FiltersEveryOutcome(AccessOutcome outcome) =>
        Assert.All(AccessDirectory.Query(Items(), new(Outcome: outcome)).Items, item => Assert.Equal(outcome, item.Outcome));

    [Theory]
    [InlineData(AccessSource.FrontDesk)]
    [InlineData(AccessSource.Simulator)]
    public void FiltersEverySource(AccessSource source) =>
        Assert.All(AccessDirectory.Query(Items(), new(Source: source)).Items, item => Assert.Equal(source, item.Source));

    [Theory]
    [InlineData(AccessDenialReason.None)]
    [InlineData(AccessDenialReason.MemberUnavailable)]
    [InlineData(AccessDenialReason.NoValidSubscription)]
    [InlineData(AccessDenialReason.SubscriptionSuspended)]
    [InlineData(AccessDenialReason.EntriesExhausted)]
    [InlineData(AccessDenialReason.MedicalCertificateExpired)]
    [InlineData(AccessDenialReason.DuplicateCheckIn)]
    public void FiltersEveryDenialReason(AccessDenialReason reason)
    {
        AccessDirectoryItem[] items = Enum.GetValues<AccessDenialReason>().Select((value, index) => Item(index + 10, denialReason: value)).ToArray();
        Assert.Equal(reason, Assert.Single(AccessDirectory.Query(items, new(DenialReason: reason)).Items).DenialReason);
    }

    [Fact]
    public void CombinesSearchAndFiltersAndReturnsNoResults()
    {
        Assert.Single(AccessDirectory.Query(Items(), new("anna", AccessOutcome.Granted, AccessSource.FrontDesk, AccessDenialReason.None)).Items);
        Assert.Empty(AccessDirectory.Query(Items(), new("anna", AccessOutcome.Denied)).Items);
        Assert.Equal(0, AccessDirectory.Query(Items(), new(Search: "inesistente")).TotalCount);
    }

    [Theory]
    [InlineData(AccessSortField.OccurredAt)]
    [InlineData(AccessSortField.MemberNumber)]
    [InlineData(AccessSortField.MemberName)]
    [InlineData(AccessSortField.Outcome)]
    [InlineData(AccessSortField.Source)]
    public void SortsAscendingAndDescending(AccessSortField field)
    {
        AccessDirectoryResult ascending = AccessDirectory.Query(Items(), new(SortBy: field, Direction: AccessSortDirection.Ascending));
        AccessDirectoryResult descending = AccessDirectory.Query(Items(), new(SortBy: field, Direction: AccessSortDirection.Descending));
        Assert.NotEqual(ascending.Items.Select(item => item.Id), descending.Items.Select(item => item.Id));
    }

    [Fact]
    public void UsesIdAsStableTieBreakerIncludingMissingMembers()
    {
        AccessDirectoryItem second = Item(2, memberNumber: AccessDirectory.MissingMemberLabel);
        AccessDirectoryItem first = Item(1, memberNumber: AccessDirectory.MissingMemberLabel);
        Assert.Equal([first.Id, second.Id], AccessDirectory.Query([second, first], new(SortBy: AccessSortField.MemberNumber)).Items.Select(item => item.Id));
        Assert.Equal([first.Id, second.Id], AccessDirectory.Query([second, first], new(SortBy: AccessSortField.MemberNumber, Direction: AccessSortDirection.Descending)).Items.Select(item => item.Id));
    }

    [Fact]
    public void PaginatesFirstLaterAndPastAvailablePages()
    {
        AccessDirectoryItem[] items = Enumerable.Range(1, 5).Select(index => Item(index)).ToArray();
        Assert.Equal(2, AccessDirectory.Query(items, new(SortBy: AccessSortField.MemberNumber, Direction: AccessSortDirection.Ascending, PageSize: 2)).Items.Count);
        Assert.Equal([GuidFrom(3), GuidFrom(4)], AccessDirectory.Query(items, new(SortBy: AccessSortField.MemberNumber, Direction: AccessSortDirection.Ascending, Page: 2, PageSize: 2)).Items.Select(item => item.Id));
        AccessDirectoryResult past = AccessDirectory.Query(items, new(Page: 9, PageSize: 2));
        Assert.Empty(past.Items);
        Assert.Equal(5, past.TotalCount);
    }

    [Theory]
    [InlineData(AccessOutcome.Granted, "Consentito")]
    [InlineData(AccessOutcome.Denied, "Negato")]
    [InlineData(AccessOutcome.Cancelled, "Annullato")]
    public void TranslatesOutcomes(AccessOutcome outcome, string expected) => Assert.Equal(expected, AccessDirectory.OutcomeLabel(outcome));

    [Theory]
    [InlineData(AccessSource.FrontDesk, "Reception")]
    [InlineData(AccessSource.Simulator, "Simulatore")]
    public void TranslatesSources(AccessSource source, string expected) => Assert.Equal(expected, AccessDirectory.SourceLabel(source));

    [Theory]
    [InlineData(AccessDenialReason.None, "Nessuno")]
    [InlineData(AccessDenialReason.MemberUnavailable, "Iscritto non disponibile")]
    [InlineData(AccessDenialReason.NoValidSubscription, "Nessun abbonamento valido")]
    [InlineData(AccessDenialReason.SubscriptionSuspended, "Abbonamento sospeso")]
    [InlineData(AccessDenialReason.EntriesExhausted, "Ingressi esauriti")]
    [InlineData(AccessDenialReason.MedicalCertificateExpired, "Certificato medico scaduto")]
    [InlineData(AccessDenialReason.DuplicateCheckIn, "Accesso duplicato")]
    public void TranslatesDenialReasons(AccessDenialReason reason, string expected) => Assert.Equal(expected, AccessDirectory.DenialReasonLabel(reason));

    [Fact]
    public void AppliesOutcomeSemanticsToDenialReason()
    {
        Assert.Equal(AccessDirectory.NotApplicableLabel, AccessDirectory.DenialReasonPresentation(AccessOutcome.Granted, AccessDenialReason.None));
        Assert.Equal("Ingressi esauriti", AccessDirectory.DenialReasonPresentation(AccessOutcome.Denied, AccessDenialReason.EntriesExhausted));
        Assert.Equal(AccessDirectory.MissingDenialReasonLabel, AccessDirectory.DenialReasonPresentation(AccessOutcome.Denied, AccessDenialReason.None));
        Assert.Equal(AccessDirectory.NotApplicableLabel, AccessDirectory.DenialReasonPresentation(AccessOutcome.Cancelled, AccessDenialReason.None));
    }

    [Fact]
    public void FindsExistingDetailAndHandlesUnknownId()
    {
        AccessDirectoryItem expected = Items()[0];
        Assert.Same(expected, AccessDirectory.FindById([expected], expected.Id));
        Assert.Null(AccessDirectory.FindById([expected], GuidFrom(999)));
    }

    [Fact]
    public void FormatsTheInstantDeterministicallyInUtc() =>
        Assert.Equal("01/09/2026 12:30 UTC", AccessDirectory.FormatOccurredAt(new(2026, 9, 1, 14, 30, 0, TimeSpan.FromHours(2))));

    private static AccessDirectoryItem[] Items() =>
    [
        Item(1, "VK-001", "Anna", "Bianchi", AccessOutcome.Granted, AccessSource.FrontDesk, AccessDenialReason.None, Timestamp, "Rebecca Reception (reception.demo)"),
        Item(2, "VK-002", "Luca", "Verdi", AccessOutcome.Denied, AccessSource.Simulator, AccessDenialReason.EntriesExhausted, Timestamp.AddHours(1), "Marco Manager (manager.demo)"),
        Item(3, "VK-003", "Sara", "Neri", AccessOutcome.Cancelled, AccessSource.FrontDesk, AccessDenialReason.None, Timestamp.AddHours(2), "Marco Manager (manager.demo)")
    ];

    private static AccessDirectoryItem Item(int id, string? memberNumber = null, string firstName = "Nome", string lastName = "Cognome", AccessOutcome outcome = AccessOutcome.Granted, AccessSource source = AccessSource.FrontDesk, AccessDenialReason denialReason = AccessDenialReason.None, DateTimeOffset? occurredAt = null, string recordedBy = "Utente Demo (demo)") =>
        new(GuidFrom(id), GuidFrom(100 + id), GuidFrom(200 + id), GuidFrom(300 + id), occurredAt ?? Timestamp, outcome, source, denialReason, null, memberNumber ?? $"VK-{id:D3}", firstName, lastName, memberNumber != AccessDirectory.MissingMemberLabel, true, recordedBy, true);

    private static GymAccess Access(int id, Guid memberId, Guid? subscriptionId, Guid userId) => new()
    {
        Id = GuidFrom(id),
        CreatedAtUtc = Timestamp,
        UpdatedAtUtc = Timestamp,
        MemberId = memberId,
        SubscriptionId = subscriptionId,
        OccurredAtUtc = Timestamp,
        Outcome = AccessOutcome.Granted,
        Source = AccessSource.FrontDesk,
        RecordedByUserId = userId
    };

    private static Member Member(Guid id, string number, string firstName, string lastName) => new()
    {
        Id = id,
        CreatedAtUtc = Timestamp,
        UpdatedAtUtc = Timestamp,
        MemberNumber = number,
        FirstName = firstName,
        LastName = lastName,
        DateOfBirth = new(1990, 1, 1),
        Email = "demo@example.invalid",
        Phone = "000",
        JoinedOn = new(2025, 1, 1),
        Status = MemberStatus.Active,
        PrivacyConsent = true
    };

    private static MemberSubscription Subscription(Guid id, Guid memberId) => new()
    {
        Id = id,
        CreatedAtUtc = Timestamp,
        UpdatedAtUtc = Timestamp,
        MemberId = memberId,
        MembershipPlanId = GuidFrom(500),
        StartsOn = new(2026, 1, 1),
        EndsOn = new(2026, 12, 31),
        Status = SubscriptionStatus.Active,
        PurchasePrice = 50m
    };

    private static DemoUser User(Guid id, string displayName, string username) => new()
    {
        Id = id,
        CreatedAtUtc = Timestamp,
        UpdatedAtUtc = Timestamp,
        DisplayName = displayName,
        Username = username,
        Role = DemoRole.Receptionist
    };

    private static Guid GuidFrom(int value) => Guid.Parse($"00000000-0000-0000-0000-{value:D12}");
}
