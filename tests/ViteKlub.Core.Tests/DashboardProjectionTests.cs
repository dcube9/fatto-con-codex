using ViteKlub.Core.Dashboard;
using ViteKlub.Core.Data;
using Xunit;

namespace ViteKlub.Core.Tests;

public sealed class DashboardProjectionTests
{
    private static readonly DateOnly ReferenceDate = new(2040, 2, 10);
    private static readonly DateTimeOffset Stamp = new(2040, 1, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CreateCountsMembersByStatusAndHandlesEmptyCollections()
    {
        DemoDataset empty = Dataset();
        DashboardSummary emptySummary = DashboardProjection.Create(empty);
        Assert.Equal(0, emptySummary.TotalMembers);
        Assert.Empty(emptySummary.ExpiringMembershipItems);
        Assert.Empty(emptySummary.MedicalCertificateItems);
        Assert.Empty(emptySummary.RecentAccessItems);

        DemoDataset dataset = Dataset() with
        {
            Members =
            [
                Member(Guid.NewGuid(), "Anna", "Attiva", MemberStatus.Active),
                Member(Guid.NewGuid(), "Sara", "Sospesa", MemberStatus.Suspended),
                Member(Guid.NewGuid(), "Arturo", "Archiviato", MemberStatus.Archived),
            ]
        };

        DashboardSummary summary = DashboardProjection.Create(dataset);
        Assert.Equal(3, summary.TotalMembers);
        Assert.Equal(1, summary.ActiveMembers);
        Assert.Equal(1, summary.SuspendedMembers);
        Assert.Equal(1, summary.ArchivedMembers);
    }

    [Fact]
    public void MembershipWindowIsInclusiveAndUsesOnlyActiveStatusAndReferenceDate()
    {
        Member member = Member(Guid.NewGuid(), "Ada", "Lodi", MemberStatus.Active);
        MembershipPlan plan = Plan(Guid.NewGuid());
        DemoDataset dataset = Dataset() with
        {
            ReferenceDate = ReferenceDate,
            Members = [member],
            MembershipPlans = [plan],
            Subscriptions =
            [
                Subscription(Guid.Parse("00000000-0000-0000-0000-000000000004"), member.Id, plan.Id, ReferenceDate.AddDays(-2), ReferenceDate.AddDays(-1)),
                Subscription(Guid.Parse("00000000-0000-0000-0000-000000000003"), member.Id, plan.Id, ReferenceDate, ReferenceDate.AddDays(30)),
                Subscription(Guid.Parse("00000000-0000-0000-0000-000000000002"), member.Id, plan.Id, ReferenceDate, ReferenceDate.AddDays(31)),
                Subscription(Guid.Parse("00000000-0000-0000-0000-000000000001"), member.Id, plan.Id, ReferenceDate, ReferenceDate.AddDays(1), SubscriptionStatus.Suspended),
            ]
        };

        DashboardSummary summary = DashboardProjection.Create(dataset);

        Assert.Equal(2, summary.ActiveMemberships);
        Assert.Equal(1, summary.ExpiringMemberships);
        Assert.Equal(ReferenceDate.AddDays(30), Assert.Single(summary.ExpiringMembershipItems).ExpiresOn);
    }

    [Fact]
    public void CertificateWindowIncludesReferenceAndLimitAndHasDeterministicOrder()
    {
        List<Member> members =
        [
            Member(Guid.Parse("00000000-0000-0000-0000-000000000003"), "Zeno", "Blu", MemberStatus.Active, ReferenceDate.AddDays(30)),
            Member(Guid.Parse("00000000-0000-0000-0000-000000000002"), "Anna", "Blu", MemberStatus.Active, ReferenceDate.AddDays(-1)),
            Member(Guid.Parse("00000000-0000-0000-0000-000000000001"), "Anna", "Blu", MemberStatus.Active, ReferenceDate.AddDays(-1)),
            Member(Guid.NewGuid(), "Oltre", "Limite", MemberStatus.Active, ReferenceDate.AddDays(31)),
            Member(Guid.NewGuid(), "Quarto", "Nome", MemberStatus.Active, ReferenceDate.AddDays(2)),
            Member(Guid.NewGuid(), "Quinto", "Nome", MemberStatus.Active, ReferenceDate.AddDays(3)),
            Member(Guid.NewGuid(), "Sesto", "Nome", MemberStatus.Active, ReferenceDate.AddDays(4)),
        ];
        Guid[] originalOrder = members.Select(member => member.Id).ToArray();

        DashboardSummary summary = DashboardProjection.Create(Dataset() with { Members = members });

        Assert.Equal(2, summary.ExpiredMedicalCertificates);
        Assert.Equal(4, summary.ExpiringMedicalCertificates);
        Assert.Equal(DashboardProjection.MedicalCertificateListLimit, summary.MedicalCertificateItems.Count);
        Assert.Equal(Guid.Parse("00000000-0000-0000-0000-000000000001"), summary.MedicalCertificateItems[0].MemberId);
        Assert.Equal(originalOrder, members.Select(member => member.Id));

        DashboardSummary boundary = DashboardProjection.Create(Dataset() with
        {
            Members = [Member(Guid.NewGuid(), "Data", "Limite", MemberStatus.Active, ReferenceDate.AddDays(30))]
        });
        Assert.Equal(1, boundary.ExpiringMedicalCertificates);
        Assert.Equal(ReferenceDate.AddDays(30), Assert.Single(boundary.MedicalCertificateItems).ExpiresOn);
    }

    [Fact]
    public void AccessesUseUtcReferenceDateAndClassifyOutcomes()
    {
        Member member = Member(Guid.NewGuid(), "Leo", "Verdi", MemberStatus.Active);
        DemoDataset dataset = Dataset() with
        {
            Members = [member],
            Accesses =
            [
                Access(Guid.NewGuid(), member.Id, ReferenceDate.ToDateTime(new TimeOnly(8, 0)), AccessOutcome.Granted),
                Access(Guid.NewGuid(), member.Id, ReferenceDate.ToDateTime(new TimeOnly(9, 0)), AccessOutcome.Denied),
                Access(Guid.NewGuid(), member.Id, ReferenceDate.ToDateTime(new TimeOnly(10, 0)), AccessOutcome.Cancelled),
                Access(Guid.NewGuid(), member.Id, ReferenceDate.AddDays(-1).ToDateTime(new TimeOnly(23, 0)), AccessOutcome.Granted),
                Access(Guid.NewGuid(), member.Id, ReferenceDate.AddDays(-2).ToDateTime(new TimeOnly(23, 0)), AccessOutcome.Granted),
                Access(Guid.NewGuid(), member.Id, ReferenceDate.AddDays(-3).ToDateTime(new TimeOnly(23, 0)), AccessOutcome.Granted),
                Access(Guid.NewGuid(), member.Id, ReferenceDate.AddDays(-4).ToDateTime(new TimeOnly(23, 0)), AccessOutcome.Granted),
                Access(Guid.NewGuid(), member.Id, ReferenceDate.AddDays(1).ToDateTime(new TimeOnly(0, 0)), AccessOutcome.Denied),
            ]
        };

        DashboardSummary summary = DashboardProjection.Create(dataset);

        Assert.Equal(3, summary.AccessesToday);
        Assert.Equal(1, summary.GrantedAccessesToday);
        Assert.Equal(1, summary.DeniedAccessesToday);
        Assert.Equal(DashboardProjection.RecentAccessListLimit, summary.RecentAccessItems.Count);
        Assert.Equal(AccessOutcome.Cancelled, summary.RecentAccessItems[0].Outcome);
        Assert.DoesNotContain(summary.RecentAccessItems, item => item.OccurredAtUtc.UtcDateTime.Date > ReferenceDate.ToDateTime(TimeOnly.MinValue).Date);
    }

    [Fact]
    public void CompletedPaymentsAloneContributeToCountAmountAndEuroFormatting()
    {
        DemoDataset dataset = Dataset() with
        {
            Payments =
            [
                Payment(Guid.NewGuid(), 10.50m, PaymentStatus.Completed),
                Payment(Guid.NewGuid(), 2m, PaymentStatus.Completed),
                Payment(Guid.NewGuid(), 99m, PaymentStatus.Pending),
                Payment(Guid.NewGuid(), 99m, PaymentStatus.Failed),
                Payment(Guid.NewGuid(), 99m, PaymentStatus.Cancelled),
            ]
        };

        DashboardSummary summary = DashboardProjection.Create(dataset);

        Assert.Equal(2, summary.CompletedPayments);
        Assert.Equal(12.50m, summary.CompletedPaymentsAmount);
        Assert.Equal("12,50 €", summary.FormattedCompletedPaymentsAmount.Replace('\u00A0', ' '));
    }

    [Fact]
    public void SummaryListsAreDeterministicLimitedAndDoNotMutateSource()
    {
        Member member = Member(Guid.NewGuid(), "Mara", "Rossi", MemberStatus.Active);
        MembershipPlan plan = Plan(Guid.NewGuid());
        List<MemberSubscription> subscriptions = Enumerable.Range(1, 8)
            .Select(index => Subscription(
                Guid.Parse($"00000000-0000-0000-0000-{index:D12}"), member.Id, plan.Id,
                ReferenceDate, ReferenceDate.AddDays(index % 3)))
            .Reverse().ToList();
        Guid[] originalOrder = subscriptions.Select(subscription => subscription.Id).ToArray();

        DashboardSummary summary = DashboardProjection.Create(Dataset() with
        {
            Members = [member],
            MembershipPlans = [plan],
            Subscriptions = subscriptions
        });

        Assert.Equal(8, summary.ExpiringMemberships);
        Assert.Equal(DashboardProjection.MembershipListLimit, summary.ExpiringMembershipItems.Count);
        Assert.Equal(summary.ExpiringMembershipItems.OrderBy(item => item.ExpiresOn).ThenBy(item => item.MemberName).ThenBy(item => item.Id), summary.ExpiringMembershipItems);
        Assert.Equal(originalOrder, subscriptions.Select(subscription => subscription.Id));
    }

    private static DemoDataset Dataset() => new()
    {
        SchemaVersion = 1,
        DatasetVersion = "test",
        ReferenceDate = ReferenceDate,
        Users = [],
        Members = [],
        MembershipPlans = [],
        Subscriptions = [],
        Accesses = [],
        Payments = [],
        AuditEvents = []
    };

    private static Member Member(Guid id, string firstName, string lastName, MemberStatus status, DateOnly? certificate = null) => new()
    {
        Id = id,
        CreatedAtUtc = Stamp,
        UpdatedAtUtc = Stamp,
        MemberNumber = id.ToString("N"),
        FirstName = firstName,
        LastName = lastName,
        DateOfBirth = new(1990, 1, 1),
        Email = "test@example.invalid",
        Phone = "-",
        JoinedOn = new(2030, 1, 1),
        Status = status,
        MedicalCertificateExpiresOn = certificate,
        PrivacyConsent = true
    };

    private static MembershipPlan Plan(Guid id) => new()
    {
        Id = id,
        CreatedAtUtc = Stamp,
        UpdatedAtUtc = Stamp,
        Name = "Piano",
        Description = "Test",
        Type = MembershipPlanType.TimeBased,
        DurationDays = 30,
        Price = 10m
    };

    private static MemberSubscription Subscription(Guid id, Guid memberId, Guid planId, DateOnly starts, DateOnly ends, SubscriptionStatus status = SubscriptionStatus.Active) => new()
    {
        Id = id,
        CreatedAtUtc = Stamp,
        UpdatedAtUtc = Stamp,
        MemberId = memberId,
        MembershipPlanId = planId,
        StartsOn = starts,
        EndsOn = ends,
        Status = status,
        PurchasePrice = 10m
    };

    private static GymAccess Access(Guid id, Guid memberId, DateTime occurredAt, AccessOutcome outcome) => new()
    {
        Id = id,
        CreatedAtUtc = Stamp,
        UpdatedAtUtc = Stamp,
        MemberId = memberId,
        OccurredAtUtc = new DateTimeOffset(occurredAt, TimeSpan.Zero),
        Outcome = outcome,
        DenialReason = outcome == AccessOutcome.Denied ? AccessDenialReason.NoValidSubscription : AccessDenialReason.None,
        Source = AccessSource.FrontDesk,
        RecordedByUserId = Guid.Empty
    };

    private static Payment Payment(Guid id, decimal amount, PaymentStatus status) => new()
    {
        Id = id,
        CreatedAtUtc = Stamp,
        UpdatedAtUtc = Stamp,
        MemberId = Guid.Empty,
        Amount = amount,
        OccurredAtUtc = Stamp,
        Method = PaymentMethod.Card,
        Status = status,
        RecordedByUserId = Guid.Empty
    };
}
