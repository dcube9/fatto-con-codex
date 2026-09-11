using ViteKlub.Core.Data;
using ViteKlub.Core.Memberships;
using Xunit;

namespace ViteKlub.Core.Tests;

public sealed class MembershipDirectoryTests
{
    private static readonly DateTimeOffset Timestamp = new(2026, 1, 1, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ProjectAssociatesMemberAndPlanWithoutChangingSources()
    {
        Member member = Member("VK-001", "Anna", "Bianchi");
        MembershipPlan plan = Plan("Mensile", MembershipPlanType.TimeBased);
        MemberSubscription subscription = Subscription(member.Id, plan.Id);

        MembershipDirectoryItem item = Assert.Single(MembershipDirectory.Project([subscription], [member], [plan]));

        Assert.Equal(("VK-001", "Anna", "Bianchi", true), (item.MemberNumber, item.MemberFirstName, item.MemberLastName, item.HasMember));
        Assert.Equal(("Mensile", MembershipPlanType.TimeBased, true), (item.PlanName, item.PlanType, item.HasPlan));
        Assert.Equal(10, subscription.RemainingEntries);
    }

    [Fact]
    public void ProjectHandlesMissingReferencesAndEmptyDataset()
    {
        MembershipDirectoryItem item = Assert.Single(MembershipDirectory.Project([Subscription(Guid.NewGuid(), Guid.NewGuid())], [], []));

        Assert.Equal(MembershipDirectory.MissingMemberLabel, item.MemberNumber);
        Assert.Equal(MembershipDirectory.MissingMemberLabel, item.MemberFullName);
        Assert.Equal(MembershipDirectory.MissingPlanLabel, item.PlanName);
        Assert.Null(item.PlanType);
        Assert.Empty(MembershipDirectory.Project([], [], []));
    }

    [Theory]
    [InlineData("vk-001")]
    [InlineData("ANNA")]
    [InlineData("bianchi")]
    [InlineData("Anna Bianchi")]
    [InlineData("  anna   BIANCHI ")]
    [InlineData("mensile")]
    public void SearchMatchesEverySupportedValueAndNormalizesInput(string search)
    {
        Assert.Equal("VK-001", Assert.Single(MembershipDirectory.Query(Items(), new(Search: search)).Items).MemberNumber);
    }

    [Theory]
    [InlineData(SubscriptionStatus.Scheduled)]
    [InlineData(SubscriptionStatus.Active)]
    [InlineData(SubscriptionStatus.Suspended)]
    [InlineData(SubscriptionStatus.Expired)]
    [InlineData(SubscriptionStatus.Cancelled)]
    public void FiltersEverySubscriptionStatus(SubscriptionStatus status)
    {
        MembershipDirectoryItem[] items = Enum.GetValues<SubscriptionStatus>()
            .Select((value, index) => Item($"VK-{index}", value, MembershipPlanType.TimeBased, GuidFrom(index + 1))).ToArray();

        Assert.Equal(status, Assert.Single(MembershipDirectory.Query(items, new(Status: status)).Items).Status);
    }

    [Theory]
    [InlineData(MembershipPlanType.TimeBased)]
    [InlineData(MembershipPlanType.EntryBased)]
    public void FiltersEveryPlanType(MembershipPlanType type)
    {
        Assert.All(MembershipDirectory.Query(Items(), new(PlanType: type)).Items, item => Assert.Equal(type, item.PlanType));
    }

    [Fact]
    public void CombinesSearchAndFiltersAndReturnsNoMatches()
    {
        Assert.Single(MembershipDirectory.Query(Items(), new("anna", SubscriptionStatus.Active, MembershipPlanType.TimeBased)).Items);
        Assert.Empty(MembershipDirectory.Query(Items(), new("anna", SubscriptionStatus.Cancelled, MembershipPlanType.TimeBased)).Items);
        Assert.Empty(MembershipDirectory.Query(Items(), new(Search: "inesistente")).Items);
    }

    [Theory]
    [InlineData(MembershipSortField.MemberNumber)]
    [InlineData(MembershipSortField.MemberName)]
    [InlineData(MembershipSortField.PlanName)]
    [InlineData(MembershipSortField.StartsOn)]
    [InlineData(MembershipSortField.EndsOn)]
    [InlineData(MembershipSortField.Status)]
    public void SortsAscendingAndDescending(MembershipSortField field)
    {
        MembershipDirectoryResult ascending = MembershipDirectory.Query(Items(), new(SortBy: field));
        MembershipDirectoryResult descending = MembershipDirectory.Query(Items(), new(SortBy: field, Direction: MembershipSortDirection.Descending));

        Assert.Equal(ascending.Items.Select(item => item.Id).Reverse(), descending.Items.Select(item => item.Id));
    }

    [Fact]
    public void EqualAndMissingSortValuesUseIdAsDeterministicTieBreaker()
    {
        MembershipDirectoryItem later = Item(MembershipDirectory.MissingMemberLabel, SubscriptionStatus.Active, null, GuidFrom(2));
        MembershipDirectoryItem earlier = Item(MembershipDirectory.MissingMemberLabel, SubscriptionStatus.Active, null, GuidFrom(1));

        Assert.Equal([earlier.Id, later.Id], MembershipDirectory.Query([later, earlier], new()).Items.Select(item => item.Id));
        Assert.Equal([earlier.Id, later.Id], MembershipDirectory.Query([later, earlier], new(Direction: MembershipSortDirection.Descending)).Items.Select(item => item.Id));
    }

    [Fact]
    public void PaginatesFirstLaterAndPastAvailablePages()
    {
        MembershipDirectoryItem[] items = Enumerable.Range(1, 5).Select(index => Item($"VK-{index:D3}", SubscriptionStatus.Active, MembershipPlanType.TimeBased, GuidFrom(index))).ToArray();

        Assert.Equal(2, MembershipDirectory.Query(items, new(PageSize: 2)).Items.Count);
        Assert.Equal(["VK-003", "VK-004"], MembershipDirectory.Query(items, new(Page: 2, PageSize: 2)).Items.Select(item => item.MemberNumber));
        MembershipDirectoryResult past = MembershipDirectory.Query(items, new(Page: 9, PageSize: 2));
        Assert.Empty(past.Items);
        Assert.Equal(5, past.TotalCount);
        Assert.Equal(3, past.PageCount);
    }

    [Theory]
    [InlineData(SubscriptionStatus.Scheduled, "Programmato")]
    [InlineData(SubscriptionStatus.Active, "Attivo")]
    [InlineData(SubscriptionStatus.Suspended, "Sospeso")]
    [InlineData(SubscriptionStatus.Expired, "Scaduto")]
    [InlineData(SubscriptionStatus.Cancelled, "Annullato")]
    public void TranslatesStatuses(SubscriptionStatus status, string label) => Assert.Equal(label, MembershipDirectory.SubscriptionStatusLabel(status));

    [Theory]
    [InlineData(MembershipPlanType.TimeBased, "A tempo")]
    [InlineData(MembershipPlanType.EntryBased, "A ingressi")]
    public void TranslatesPlanTypes(MembershipPlanType type, string label) => Assert.Equal(label, MembershipDirectory.PlanTypeLabel(type));

    [Fact]
    public void PresentsRemainingEntriesAccordingToPlanSemantics()
    {
        Assert.Equal("8", MembershipDirectory.RemainingEntriesLabel(MembershipPlanType.EntryBased, 8));
        Assert.Equal("Dato non disponibile", MembershipDirectory.RemainingEntriesLabel(MembershipPlanType.EntryBased, null));
        Assert.Equal("Non applicabile", MembershipDirectory.RemainingEntriesLabel(MembershipPlanType.TimeBased, 8));
    }

    [Fact]
    public void FindsExistingDetailAndReturnsNullForUnknownId()
    {
        MembershipDirectoryItem[] items = Items();
        MembershipDirectoryItem expected = items[0];
        Assert.Same(expected, MembershipDirectory.FindById(items, expected.Id));
        Assert.Null(MembershipDirectory.FindById(items, Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff")));
    }

    [Fact]
    public void FormatsAmountsUsingItalianConventions() => Assert.Equal("1.234,50 EUR", MembershipDirectory.FormatAmount(1234.5m, "EUR"));

    private static MembershipDirectoryItem[] Items() =>
    [
        new(GuidFrom(1), GuidFrom(11), GuidFrom(21), "VK-001", "Anna", "Bianchi", true, "Mensile", MembershipPlanType.TimeBased, true, SubscriptionStatus.Active, new(2026, 1, 1), new(2026, 1, 31), null, 50m, "EUR"),
        new(GuidFrom(2), GuidFrom(12), GuidFrom(22), "VK-002", "Luca", "Verdi", true, "Carnet", MembershipPlanType.EntryBased, true, SubscriptionStatus.Cancelled, new(2026, 2, 1), new(2026, 6, 1), 4, 100m, "EUR")
    ];

    private static MembershipDirectoryItem Item(string number, SubscriptionStatus status, MembershipPlanType? type, Guid id) =>
        new(id, GuidFrom(20), GuidFrom(30), number, "Nome", "Cognome", number != MembershipDirectory.MissingMemberLabel, type is null ? MembershipDirectory.MissingPlanLabel : "Piano", type, type is not null, status, new(2026, 1, 1), new(2026, 12, 31), 1, 10m, "EUR");

    private static Member Member(string number, string firstName, string lastName) => new()
    {
        Id = GuidFrom(11),
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

    private static MembershipPlan Plan(string name, MembershipPlanType type) => new()
    {
        Id = GuidFrom(21),
        CreatedAtUtc = Timestamp,
        UpdatedAtUtc = Timestamp,
        Name = name,
        Description = "Demo",
        Type = type,
        DurationDays = 30,
        Price = 50m
    };

    private static MemberSubscription Subscription(Guid memberId, Guid planId) => new()
    {
        Id = GuidFrom(1),
        CreatedAtUtc = Timestamp,
        UpdatedAtUtc = Timestamp,
        MemberId = memberId,
        MembershipPlanId = planId,
        StartsOn = new(2026, 1, 1),
        EndsOn = new(2026, 1, 31),
        Status = SubscriptionStatus.Active,
        RemainingEntries = 10,
        PurchasePrice = 50m
    };

    private static Guid GuidFrom(int value) => Guid.Parse($"00000000-0000-0000-0000-{value:D12}");
}
