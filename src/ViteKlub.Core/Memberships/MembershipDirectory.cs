using System.Globalization;
using ViteKlub.Core.Data;

namespace ViteKlub.Core.Memberships;

public enum MembershipSortField
{
    MemberNumber,
    MemberName,
    PlanName,
    StartsOn,
    EndsOn,
    Status
}

public enum MembershipSortDirection
{
    Ascending,
    Descending
}

public sealed record MembershipDirectoryItem(
    Guid Id,
    Guid MemberId,
    Guid MembershipPlanId,
    string MemberNumber,
    string MemberFirstName,
    string MemberLastName,
    bool HasMember,
    string PlanName,
    MembershipPlanType? PlanType,
    bool HasPlan,
    SubscriptionStatus Status,
    DateOnly StartsOn,
    DateOnly EndsOn,
    int? RemainingEntries,
    decimal PurchasePrice,
    string Currency)
{
    public string MemberFullName => HasMember
        ? $"{MemberFirstName} {MemberLastName}"
        : MembershipDirectory.MissingMemberLabel;

    public string RemainingEntriesLabel => MembershipDirectory.RemainingEntriesLabel(PlanType, RemainingEntries);
}

public sealed record MembershipDirectoryQuery(
    string? Search = null,
    SubscriptionStatus? Status = null,
    MembershipPlanType? PlanType = null,
    MembershipSortField SortBy = MembershipSortField.MemberNumber,
    MembershipSortDirection Direction = MembershipSortDirection.Ascending,
    int Page = 1,
    int PageSize = 10);

public sealed record MembershipDirectoryResult(
    IReadOnlyList<MembershipDirectoryItem> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int PageCount => TotalCount == 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
}

public static class MembershipDirectory
{
    public const string MissingMemberLabel = "Iscritto non disponibile";
    public const string MissingPlanLabel = "Piano non disponibile";

    public static IReadOnlyList<MembershipDirectoryItem> Project(
        IEnumerable<MemberSubscription> subscriptions,
        IEnumerable<Member> members,
        IEnumerable<MembershipPlan> plans)
    {
        ArgumentNullException.ThrowIfNull(subscriptions);
        ArgumentNullException.ThrowIfNull(members);
        ArgumentNullException.ThrowIfNull(plans);

        Dictionary<Guid, Member> membersById = members
            .GroupBy(member => member.Id)
            .ToDictionary(group => group.Key, group => group.First());
        Dictionary<Guid, MembershipPlan> plansById = plans
            .GroupBy(plan => plan.Id)
            .ToDictionary(group => group.Key, group => group.First());

        return subscriptions.Select(subscription =>
        {
            membersById.TryGetValue(subscription.MemberId, out Member? member);
            plansById.TryGetValue(subscription.MembershipPlanId, out MembershipPlan? plan);
            return new MembershipDirectoryItem(
                subscription.Id,
                subscription.MemberId,
                subscription.MembershipPlanId,
                member?.MemberNumber ?? MissingMemberLabel,
                member?.FirstName ?? string.Empty,
                member?.LastName ?? string.Empty,
                member is not null,
                plan?.Name ?? MissingPlanLabel,
                plan?.Type,
                plan is not null,
                subscription.Status,
                subscription.StartsOn,
                subscription.EndsOn,
                subscription.RemainingEntries,
                subscription.PurchasePrice,
                subscription.Currency);
        }).ToArray();
    }

    public static MembershipDirectoryResult Query(
        IEnumerable<MembershipDirectoryItem> memberships,
        MembershipDirectoryQuery query)
    {
        ArgumentNullException.ThrowIfNull(memberships);
        ArgumentNullException.ThrowIfNull(query);
        if (query.Page < 1 || query.PageSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(query), "Pagina e dimensione pagina devono essere almeno 1.");
        }

        string search = NormalizeSearch(query.Search);
        IEnumerable<MembershipDirectoryItem> filtered = memberships;
        if (search.Length > 0)
        {
            filtered = filtered.Where(item => SearchableValues(item)
                .Any(value => value.Contains(search, StringComparison.OrdinalIgnoreCase)));
        }

        if (query.Status is not null)
        {
            filtered = filtered.Where(item => item.Status == query.Status);
        }

        if (query.PlanType is not null)
        {
            filtered = filtered.Where(item => item.PlanType == query.PlanType);
        }

        MembershipDirectoryItem[] ordered = Order(filtered, query.SortBy, query.Direction).ToArray();
        long offset = (long)(query.Page - 1) * query.PageSize;
        MembershipDirectoryItem[] page = offset >= ordered.Length
            ? []
            : ordered.Skip((int)offset).Take(query.PageSize).ToArray();
        return new(page, ordered.Length, query.Page, query.PageSize);
    }

    public static MembershipDirectoryItem? FindById(
        IEnumerable<MembershipDirectoryItem> memberships,
        Guid id) => memberships.SingleOrDefault(item => item.Id == id);

    public static string SubscriptionStatusLabel(SubscriptionStatus status) => status switch
    {
        SubscriptionStatus.Scheduled => "Programmato",
        SubscriptionStatus.Active => "Attivo",
        SubscriptionStatus.Suspended => "Sospeso",
        SubscriptionStatus.Expired => "Scaduto",
        SubscriptionStatus.Cancelled => "Annullato",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };

    public static string PlanTypeLabel(MembershipPlanType? type) => type switch
    {
        MembershipPlanType.TimeBased => "A tempo",
        MembershipPlanType.EntryBased => "A ingressi",
        null => MissingPlanLabel,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    public static string RemainingEntriesLabel(MembershipPlanType? type, int? remainingEntries) => type switch
    {
        MembershipPlanType.TimeBased => "Non applicabile",
        MembershipPlanType.EntryBased => remainingEntries?.ToString(CultureInfo.InvariantCulture) ?? "Dato non disponibile",
        null => "Dato non disponibile",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };

    public static string FormatAmount(decimal amount, string currency) =>
        $"{amount.ToString("N2", CultureInfo.GetCultureInfo("it-IT"))} {currency}";

    private static string NormalizeSearch(string? value) =>
        string.Join(' ', (value ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static IEnumerable<string> SearchableValues(MembershipDirectoryItem item)
    {
        yield return item.MemberNumber;
        yield return item.MemberFirstName;
        yield return item.MemberLastName;
        yield return $"{item.MemberFirstName} {item.MemberLastName}";
        yield return $"{item.MemberLastName} {item.MemberFirstName}";
        yield return item.PlanName;
    }

    private static IOrderedEnumerable<MembershipDirectoryItem> Order(
        IEnumerable<MembershipDirectoryItem> items,
        MembershipSortField sortBy,
        MembershipSortDirection direction)
    {
        IOrderedEnumerable<MembershipDirectoryItem> ordered = sortBy switch
        {
            MembershipSortField.MemberNumber => ApplyDirection(items, item => item.MemberNumber, direction, StringComparer.OrdinalIgnoreCase),
            MembershipSortField.MemberName => ApplyDirection(items, item => $"{item.MemberLastName}\0{item.MemberFirstName}", direction, StringComparer.OrdinalIgnoreCase),
            MembershipSortField.PlanName => ApplyDirection(items, item => item.PlanName, direction, StringComparer.OrdinalIgnoreCase),
            MembershipSortField.StartsOn => ApplyDirection(items, item => item.StartsOn, direction),
            MembershipSortField.EndsOn => ApplyDirection(items, item => item.EndsOn, direction),
            MembershipSortField.Status => ApplyDirection(items, item => item.Status, direction),
            _ => throw new ArgumentOutOfRangeException(nameof(sortBy), sortBy, null)
        };
        return ordered.ThenBy(item => item.Id);
    }

    private static IOrderedEnumerable<MembershipDirectoryItem> ApplyDirection<TKey>(
        IEnumerable<MembershipDirectoryItem> items,
        Func<MembershipDirectoryItem, TKey> selector,
        MembershipSortDirection direction,
        IComparer<TKey>? comparer = null) => direction == MembershipSortDirection.Ascending
            ? items.OrderBy(selector, comparer)
            : items.OrderByDescending(selector, comparer);
}
