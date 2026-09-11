using System.Globalization;
using ViteKlub.Core.Accesses;
using ViteKlub.Core.Data;
using ViteKlub.Core.Memberships;

namespace ViteKlub.Core.Dashboard;

public sealed record ExpiringMembershipSummary(
    Guid Id, Guid MemberId, string MemberName, string PlanName, DateOnly ExpiresOn);

public enum MedicalCertificateCondition { Expired, Expiring }

public sealed record MedicalCertificateSummary(
    Guid MemberId, string MemberName, DateOnly ExpiresOn, MedicalCertificateCondition Condition);

public sealed record RecentAccessSummary(
    Guid Id, Guid MemberId, string MemberName, DateTimeOffset OccurredAtUtc, AccessOutcome Outcome)
{
    public string OutcomeLabel => AccessDirectory.OutcomeLabel(Outcome);
}

public sealed record DashboardSummary(
    DateOnly ReferenceDate,
    int TotalMembers,
    int ActiveMembers,
    int SuspendedMembers,
    int ArchivedMembers,
    int ActiveMemberships,
    int ExpiringMemberships,
    int ExpiredMedicalCertificates,
    int ExpiringMedicalCertificates,
    int AccessesToday,
    int GrantedAccessesToday,
    int DeniedAccessesToday,
    int CompletedPayments,
    decimal CompletedPaymentsAmount,
    IReadOnlyList<ExpiringMembershipSummary> ExpiringMembershipItems,
    IReadOnlyList<MedicalCertificateSummary> MedicalCertificateItems,
    IReadOnlyList<RecentAccessSummary> RecentAccessItems)
{
    public string FormattedCompletedPaymentsAmount =>
        CompletedPaymentsAmount.ToString("C2", CultureInfo.GetCultureInfo("it-IT"));
}

public static class DashboardProjection
{
    public const int ExpirationWindowDays = 30;
    public const int MembershipListLimit = 5;
    public const int MedicalCertificateListLimit = 5;
    public const int RecentAccessListLimit = 5;

    public static DashboardSummary Create(DemoDataset dataset)
    {
        ArgumentNullException.ThrowIfNull(dataset);

        DateOnly referenceDate = dataset.ReferenceDate;
        DateOnly expirationLimit = referenceDate.AddDays(ExpirationWindowDays);
        Dictionary<Guid, Member> members = dataset.Members
            .GroupBy(member => member.Id).ToDictionary(group => group.Key, group => group.First());
        Dictionary<Guid, MembershipPlan> plans = dataset.MembershipPlans
            .GroupBy(plan => plan.Id).ToDictionary(group => group.Key, group => group.First());

        MemberSubscription[] activeMemberships = dataset.Subscriptions
            .Where(subscription => subscription.Status == SubscriptionStatus.Active
                && subscription.StartsOn <= referenceDate
                && subscription.EndsOn >= referenceDate)
            .ToArray();
        MemberSubscription[] expiringMemberships = activeMemberships
            .Where(subscription => subscription.EndsOn <= expirationLimit)
            .ToArray();

        MedicalCertificateSummary[] certificates = dataset.Members
            .Where(member => member.MedicalCertificateExpiresOn is not null
                && member.MedicalCertificateExpiresOn <= expirationLimit)
            .Select(member => new MedicalCertificateSummary(
                member.Id,
                FullName(member),
                member.MedicalCertificateExpiresOn!.Value,
                member.MedicalCertificateExpiresOn < referenceDate
                    ? MedicalCertificateCondition.Expired
                    : MedicalCertificateCondition.Expiring))
            .OrderBy(item => item.ExpiresOn)
            .ThenBy(item => item.MemberName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.MemberId)
            .ToArray();

        GymAccess[] accessesToday = dataset.Accesses
            .Where(access => UtcDate(access.OccurredAtUtc) == referenceDate)
            .ToArray();
        Payment[] completedPayments = dataset.Payments
            .Where(payment => payment.Status == PaymentStatus.Completed)
            .ToArray();

        return new DashboardSummary(
            referenceDate,
            dataset.Members.Count,
            dataset.Members.Count(member => member.Status == MemberStatus.Active),
            dataset.Members.Count(member => member.Status == MemberStatus.Suspended),
            dataset.Members.Count(member => member.Status == MemberStatus.Archived),
            activeMemberships.Length,
            expiringMemberships.Length,
            certificates.Count(item => item.Condition == MedicalCertificateCondition.Expired),
            certificates.Count(item => item.Condition == MedicalCertificateCondition.Expiring),
            accessesToday.Length,
            accessesToday.Count(access => access.Outcome == AccessOutcome.Granted),
            accessesToday.Count(access => access.Outcome == AccessOutcome.Denied),
            completedPayments.Length,
            completedPayments.Sum(payment => payment.Amount),
            expiringMemberships
                .Select(subscription => new ExpiringMembershipSummary(
                    subscription.Id,
                    subscription.MemberId,
                    members.TryGetValue(subscription.MemberId, out Member? member)
                        ? FullName(member) : MembershipDirectory.MissingMemberLabel,
                    plans.TryGetValue(subscription.MembershipPlanId, out MembershipPlan? plan)
                        ? plan.Name : MembershipDirectory.MissingPlanLabel,
                    subscription.EndsOn))
                .OrderBy(item => item.ExpiresOn)
                .ThenBy(item => item.MemberName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(item => item.Id)
                .Take(MembershipListLimit)
                .ToArray(),
            certificates.Take(MedicalCertificateListLimit).ToArray(),
            dataset.Accesses
                .Where(access => UtcDate(access.OccurredAtUtc) <= referenceDate)
                .OrderByDescending(access => access.OccurredAtUtc)
                .ThenBy(access => access.Id)
                .Take(RecentAccessListLimit)
                .Select(access => new RecentAccessSummary(
                    access.Id,
                    access.MemberId,
                    members.TryGetValue(access.MemberId, out Member? member)
                        ? FullName(member) : AccessDirectory.MissingMemberLabel,
                    access.OccurredAtUtc,
                    access.Outcome))
                .ToArray());
    }

    private static string FullName(Member member) => $"{member.FirstName} {member.LastName}";

    private static DateOnly UtcDate(DateTimeOffset timestamp)
    {
        DateTime utc = timestamp.UtcDateTime;
        return new DateOnly(utc.Year, utc.Month, utc.Day);
    }
}
