using ViteKlub.Core.Data;
using ViteKlub.Core.Payments;
using Xunit;

namespace ViteKlub.Core.Tests;

public sealed class PaymentDirectoryTests
{
    private static readonly DateTimeOffset Timestamp = new(2026, 9, 1, 12, 30, 0, TimeSpan.Zero);

    [Fact]
    public void ProjectsAllRelationshipsWithoutChangingSources()
    {
        Payment payment = Payment(1, GuidFrom(11), GuidFrom(21), GuidFrom(31));
        Payment original = payment with { };
        Member member = Member(GuidFrom(11), "VK-001", "Anna", "Bianchi");
        MemberSubscription subscription = Subscription(GuidFrom(21), member.Id);
        DemoUser user = User(GuidFrom(31), "Rebecca Reception", "reception.demo");

        PaymentDirectoryItem item = Assert.Single(PaymentDirectory.Project([payment], [member], [subscription], [user]));

        Assert.Equal(("VK-001", "Anna Bianchi", true), (item.MemberNumber, item.MemberFullName, item.HasMember));
        Assert.Equal((subscription.Id, true), (item.SubscriptionId, item.HasSubscription));
        Assert.Equal("Rebecca Reception (reception.demo)", item.RecordedBy);
        Assert.Equal((payment.Amount, payment.Currency), (item.Amount, item.Currency));
        Assert.Equal(original, payment);
    }

    [Fact]
    public void ProjectsEmptyDataset() => Assert.Empty(PaymentDirectory.Project([], [], [], []));

    [Fact]
    public void DistinguishesAbsentAndMissingRelationships()
    {
        PaymentDirectoryItem absent = Assert.Single(PaymentDirectory.Project([Payment(1, GuidFrom(11), null, GuidFrom(31))], [], [], []));
        PaymentDirectoryItem missing = Assert.Single(PaymentDirectory.Project([Payment(2, GuidFrom(11), GuidFrom(99), GuidFrom(31))], [], [], []));

        Assert.Equal(PaymentDirectory.MissingMemberLabel, absent.MemberFullName);
        Assert.Equal(PaymentDirectory.MissingUserLabel, absent.RecordedBy);
        Assert.False(absent.HasMember);
        Assert.False(absent.HasRecordedByUser);
        Assert.Equal(PaymentDirectory.NoSubscriptionLabel, absent.SubscriptionLabel);
        Assert.Equal(PaymentDirectory.MissingSubscriptionLabel, missing.SubscriptionLabel);
        Assert.False(missing.HasSubscription);
    }

    [Theory]
    [InlineData("VK-001")]
    [InlineData("anna")]
    [InlineData("bianchi")]
    [InlineData("Anna Bianchi")]
    [InlineData("RIF-001")]
    [InlineData("reception.demo")]
    [InlineData("REBECCA RECEPTION")]
    [InlineData("  Anna    Bianchi  ")]
    public void SearchesRequiredFieldsCaseInsensitivelyAndNormalizesSpaces(string search) =>
        Assert.Equal(GuidFrom(1), Assert.Single(PaymentDirectory.Query(Items(), new(Search: search)).Items).Id);

    [Theory]
    [InlineData(PaymentMethod.Cash)]
    [InlineData(PaymentMethod.Card)]
    [InlineData(PaymentMethod.BankTransfer)]
    [InlineData(PaymentMethod.Other)]
    public void FiltersEveryMethod(PaymentMethod method) =>
        Assert.All(PaymentDirectory.Query(MethodItems(), new(Method: method)).Items, item => Assert.Equal(method, item.Method));

    [Theory]
    [InlineData(PaymentStatus.Pending)]
    [InlineData(PaymentStatus.Completed)]
    [InlineData(PaymentStatus.Failed)]
    [InlineData(PaymentStatus.Cancelled)]
    public void FiltersEveryStatus(PaymentStatus status) =>
        Assert.All(PaymentDirectory.Query(StatusItems(), new(Status: status)).Items, item => Assert.Equal(status, item.Status));

    [Fact]
    public void CombinesSearchAndFiltersAndReturnsNoResults()
    {
        Assert.Single(PaymentDirectory.Query(Items(), new("anna", PaymentMethod.Card, PaymentStatus.Completed)).Items);
        Assert.Empty(PaymentDirectory.Query(Items(), new("anna", Status: PaymentStatus.Failed)).Items);
        Assert.Equal(0, PaymentDirectory.Query(Items(), new(Search: "inesistente")).TotalCount);
    }

    [Theory]
    [InlineData(PaymentSortField.OccurredAt)]
    [InlineData(PaymentSortField.MemberNumber)]
    [InlineData(PaymentSortField.MemberName)]
    [InlineData(PaymentSortField.Amount)]
    [InlineData(PaymentSortField.Method)]
    [InlineData(PaymentSortField.Status)]
    public void SortsAscendingAndDescending(PaymentSortField field)
    {
        PaymentDirectoryResult ascending = PaymentDirectory.Query(Items(), new(SortBy: field, Direction: PaymentSortDirection.Ascending));
        PaymentDirectoryResult descending = PaymentDirectory.Query(Items(), new(SortBy: field, Direction: PaymentSortDirection.Descending));
        Assert.NotEqual(ascending.Items.Select(item => item.Id), descending.Items.Select(item => item.Id));
    }

    [Fact]
    public void SortsAmountsNumerically()
    {
        PaymentDirectoryItem two = Item(2, amount: 9m);
        PaymentDirectoryItem one = Item(1, amount: 100m);
        Assert.Equal([two.Id, one.Id], PaymentDirectory.Query([one, two], new(SortBy: PaymentSortField.Amount, Direction: PaymentSortDirection.Ascending)).Items.Select(item => item.Id));
    }

    [Fact]
    public void UsesIdAsStableTieBreakerIncludingMissingMembers()
    {
        PaymentDirectoryItem second = Item(2, memberNumber: PaymentDirectory.MissingMemberLabel);
        PaymentDirectoryItem first = Item(1, memberNumber: PaymentDirectory.MissingMemberLabel);
        Assert.Equal([first.Id, second.Id], PaymentDirectory.Query([second, first], new(SortBy: PaymentSortField.MemberNumber)).Items.Select(item => item.Id));
        Assert.Equal([first.Id, second.Id], PaymentDirectory.Query([second, first], new(SortBy: PaymentSortField.MemberNumber, Direction: PaymentSortDirection.Descending)).Items.Select(item => item.Id));
    }

    [Fact]
    public void PaginatesFirstLaterAndPastAvailablePages()
    {
        PaymentDirectoryItem[] items = Enumerable.Range(1, 5).Select(index => Item(index)).ToArray();
        Assert.Equal(2, PaymentDirectory.Query(items, new(SortBy: PaymentSortField.MemberNumber, Direction: PaymentSortDirection.Ascending, PageSize: 2)).Items.Count);
        Assert.Equal([GuidFrom(3), GuidFrom(4)], PaymentDirectory.Query(items, new(SortBy: PaymentSortField.MemberNumber, Direction: PaymentSortDirection.Ascending, Page: 2, PageSize: 2)).Items.Select(item => item.Id));
        PaymentDirectoryResult past = PaymentDirectory.Query(items, new(Page: 9, PageSize: 2));
        Assert.Empty(past.Items);
        Assert.Equal(5, past.TotalCount);
    }

    [Theory]
    [InlineData(PaymentMethod.Cash, "Contanti")]
    [InlineData(PaymentMethod.Card, "Carta")]
    [InlineData(PaymentMethod.BankTransfer, "Bonifico bancario")]
    [InlineData(PaymentMethod.Other, "Altro")]
    public void TranslatesMethods(PaymentMethod method, string expected) => Assert.Equal(expected, PaymentDirectory.MethodLabel(method));

    [Theory]
    [InlineData(PaymentStatus.Pending, "In attesa")]
    [InlineData(PaymentStatus.Completed, "Completato")]
    [InlineData(PaymentStatus.Failed, "Non riuscito")]
    [InlineData(PaymentStatus.Cancelled, "Annullato")]
    public void TranslatesStatuses(PaymentStatus status, string expected) => Assert.Equal(expected, PaymentDirectory.StatusLabel(status));

    [Fact]
    public void FallsBackForUnknownMethodAndStatus()
    {
        Assert.Equal("Metodo non disponibile", PaymentDirectory.MethodLabel((PaymentMethod)999));
        Assert.Equal("Stato non disponibile", PaymentDirectory.StatusLabel((PaymentStatus)999));
    }

    [Fact]
    public void FormatsAmountDeterministicallyAndAlwaysPresentsCurrency()
    {
        Assert.Equal("1.234,50 EUR", PaymentDirectory.FormatAmount(1234.5m, "eur"));
        Assert.Equal("12,00 Valuta non disponibile", PaymentDirectory.FormatAmount(12m, "?"));
        Assert.Equal("12,00 Valuta non disponibile", PaymentDirectory.FormatAmount(12m, null));
    }

    [Fact]
    public void PresentsPresentAndAbsentReferences()
    {
        Assert.Equal("RIF-001", PaymentDirectory.ReferenceLabel(" RIF-001 "));
        Assert.Equal(PaymentDirectory.MissingReferenceLabel, PaymentDirectory.ReferenceLabel("  "));
    }

    [Fact]
    public void FindsExistingDetailAndHandlesUnknownId()
    {
        PaymentDirectoryItem expected = Items()[0];
        Assert.Same(expected, PaymentDirectory.FindById([expected], expected.Id));
        Assert.Null(PaymentDirectory.FindById([expected], GuidFrom(999)));
    }

    [Fact]
    public void FormatsTheInstantDeterministicallyInUtc() =>
        Assert.Equal("01/09/2026 12:30 UTC", PaymentDirectory.FormatOccurredAt(new(2026, 9, 1, 14, 30, 0, TimeSpan.FromHours(2))));

    private static PaymentDirectoryItem[] Items() =>
    [
        Item(1, "VK-001", "Anna", "Bianchi", 25m, PaymentMethod.Card, PaymentStatus.Completed, Timestamp, "RIF-001", "Rebecca Reception (reception.demo)"),
        Item(2, "VK-002", "Luca", "Verdi", 10m, PaymentMethod.Cash, PaymentStatus.Failed, Timestamp.AddHours(1), "RIF-002", "Marco Manager (manager.demo)"),
        Item(3, "VK-003", "Sara", "Neri", 50m, PaymentMethod.BankTransfer, PaymentStatus.Pending, Timestamp.AddHours(2), null, "Marco Manager (manager.demo)")
    ];

    private static PaymentDirectoryItem[] MethodItems() => Enum.GetValues<PaymentMethod>().Select((value, index) => Item(index + 1, method: value)).ToArray();
    private static PaymentDirectoryItem[] StatusItems() => Enum.GetValues<PaymentStatus>().Select((value, index) => Item(index + 1, status: value)).ToArray();

    private static PaymentDirectoryItem Item(int id, string? memberNumber = null, string firstName = "Nome", string lastName = "Cognome", decimal amount = 20m, PaymentMethod method = PaymentMethod.Other, PaymentStatus status = PaymentStatus.Cancelled, DateTimeOffset? occurredAt = null, string? reference = null, string recordedBy = "Utente Demo (demo)") =>
        new(GuidFrom(id), GuidFrom(100 + id), GuidFrom(200 + id), GuidFrom(300 + id), occurredAt ?? Timestamp, amount, "EUR", method, status, reference, memberNumber ?? $"VK-{id:D3}", firstName, lastName, memberNumber != PaymentDirectory.MissingMemberLabel, true, recordedBy, true);

    private static Payment Payment(int id, Guid memberId, Guid? subscriptionId, Guid userId) => new()
    {
        Id = GuidFrom(id),
        CreatedAtUtc = Timestamp,
        UpdatedAtUtc = Timestamp,
        MemberId = memberId,
        SubscriptionId = subscriptionId,
        Amount = 25m,
        Currency = "EUR",
        OccurredAtUtc = Timestamp,
        Method = PaymentMethod.Card,
        Status = PaymentStatus.Completed,
        RecordedByUserId = userId,
        Reference = "RIF-001"
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
