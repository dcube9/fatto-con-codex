using System.Globalization;
using ViteKlub.Core.Data;

namespace ViteKlub.Core.Accesses;

public enum AccessSortField { OccurredAt, MemberNumber, MemberName, Outcome, Source }
public enum AccessSortDirection { Ascending, Descending }

public sealed record AccessDirectoryItem(
    Guid Id,
    Guid MemberId,
    Guid? SubscriptionId,
    Guid RecordedByUserId,
    DateTimeOffset OccurredAtUtc,
    AccessOutcome Outcome,
    AccessSource Source,
    AccessDenialReason DenialReason,
    string? CancellationReason,
    string MemberNumber,
    string MemberFirstName,
    string MemberLastName,
    bool HasMember,
    bool HasSubscription,
    string RecordedBy,
    bool HasRecordedByUser)
{
    public string MemberFullName => HasMember ? $"{MemberFirstName} {MemberLastName}" : AccessDirectory.MissingMemberLabel;
    public string SubscriptionLabel => AccessDirectory.SubscriptionLabel(SubscriptionId, HasSubscription);
    public string DenialReasonLabel => AccessDirectory.DenialReasonPresentation(Outcome, DenialReason);
}

public sealed record AccessDirectoryQuery(
    string? Search = null,
    AccessOutcome? Outcome = null,
    AccessSource? Source = null,
    AccessDenialReason? DenialReason = null,
    AccessSortField SortBy = AccessSortField.OccurredAt,
    AccessSortDirection Direction = AccessSortDirection.Descending,
    int Page = 1,
    int PageSize = 10);

public sealed record AccessDirectoryResult(IReadOnlyList<AccessDirectoryItem> Items, int TotalCount, int Page, int PageSize)
{
    public int PageCount => TotalCount == 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
}

public static class AccessDirectory
{
    public const string MissingMemberLabel = "Iscritto non disponibile";
    public const string NoSubscriptionLabel = "Nessun abbonamento associato";
    public const string MissingSubscriptionLabel = "Abbonamento non disponibile";
    public const string MissingUserLabel = "Utente demo non disponibile";
    public const string NotApplicableLabel = "Non applicabile";
    public const string MissingDenialReasonLabel = "Motivo del rifiuto non disponibile";

    public static IReadOnlyList<AccessDirectoryItem> Project(
        IEnumerable<GymAccess> accesses,
        IEnumerable<Member> members,
        IEnumerable<MemberSubscription> subscriptions,
        IEnumerable<DemoUser> users)
    {
        ArgumentNullException.ThrowIfNull(accesses);
        ArgumentNullException.ThrowIfNull(members);
        ArgumentNullException.ThrowIfNull(subscriptions);
        ArgumentNullException.ThrowIfNull(users);

        Dictionary<Guid, Member> membersById = ById(members);
        Dictionary<Guid, MemberSubscription> subscriptionsById = ById(subscriptions);
        Dictionary<Guid, DemoUser> usersById = ById(users);

        return accesses.Select(access =>
        {
            membersById.TryGetValue(access.MemberId, out Member? member);
            bool hasSubscription = access.SubscriptionId is Guid subscriptionId && subscriptionsById.ContainsKey(subscriptionId);
            usersById.TryGetValue(access.RecordedByUserId, out DemoUser? user);
            return new AccessDirectoryItem(
                access.Id, access.MemberId, access.SubscriptionId, access.RecordedByUserId,
                access.OccurredAtUtc, access.Outcome, access.Source, access.DenialReason,
                access.CancellationReason, member?.MemberNumber ?? MissingMemberLabel,
                member?.FirstName ?? string.Empty, member?.LastName ?? string.Empty, member is not null,
                hasSubscription, user is null ? MissingUserLabel : $"{user.DisplayName} ({user.Username})", user is not null);
        }).ToArray();
    }

    public static AccessDirectoryResult Query(IEnumerable<AccessDirectoryItem> accesses, AccessDirectoryQuery query)
    {
        ArgumentNullException.ThrowIfNull(accesses);
        ArgumentNullException.ThrowIfNull(query);
        if (query.Page < 1 || query.PageSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(query), "Pagina e dimensione pagina devono essere almeno 1.");
        }

        string search = NormalizeSearch(query.Search);
        IEnumerable<AccessDirectoryItem> filtered = accesses;
        if (search.Length > 0)
        {
            filtered = filtered.Where(item => SearchableValues(item).Any(value => NormalizeSearch(value).Contains(search, StringComparison.OrdinalIgnoreCase)));
        }

        if (query.Outcome is not null) filtered = filtered.Where(item => item.Outcome == query.Outcome);
        if (query.Source is not null) filtered = filtered.Where(item => item.Source == query.Source);
        if (query.DenialReason is not null) filtered = filtered.Where(item => item.DenialReason == query.DenialReason);

        AccessDirectoryItem[] ordered = Order(filtered, query.SortBy, query.Direction).ToArray();
        long offset = (long)(query.Page - 1) * query.PageSize;
        AccessDirectoryItem[] page = offset >= ordered.Length ? [] : ordered.Skip((int)offset).Take(query.PageSize).ToArray();
        return new(page, ordered.Length, query.Page, query.PageSize);
    }

    public static AccessDirectoryItem? FindById(IEnumerable<AccessDirectoryItem> accesses, Guid id) =>
        accesses.FirstOrDefault(item => item.Id == id);

    public static string OutcomeLabel(AccessOutcome outcome) => outcome switch
    {
        AccessOutcome.Granted => "Consentito",
        AccessOutcome.Denied => "Negato",
        AccessOutcome.Cancelled => "Annullato",
        _ => "Esito non disponibile"
    };

    public static string SourceLabel(AccessSource source) => source switch
    {
        AccessSource.FrontDesk => "Reception",
        AccessSource.Simulator => "Simulatore",
        _ => "Origine non disponibile"
    };

    public static string DenialReasonLabel(AccessDenialReason reason) => reason switch
    {
        AccessDenialReason.None => "Nessuno",
        AccessDenialReason.MemberUnavailable => "Iscritto non disponibile",
        AccessDenialReason.NoValidSubscription => "Nessun abbonamento valido",
        AccessDenialReason.SubscriptionSuspended => "Abbonamento sospeso",
        AccessDenialReason.EntriesExhausted => "Ingressi esauriti",
        AccessDenialReason.MedicalCertificateExpired => "Certificato medico scaduto",
        AccessDenialReason.DuplicateCheckIn => "Accesso duplicato",
        _ => MissingDenialReasonLabel
    };

    public static string DenialReasonPresentation(AccessOutcome outcome, AccessDenialReason reason) => outcome switch
    {
        AccessOutcome.Granted or AccessOutcome.Cancelled => NotApplicableLabel,
        AccessOutcome.Denied when reason == AccessDenialReason.None => MissingDenialReasonLabel,
        AccessOutcome.Denied => DenialReasonLabel(reason),
        _ => MissingDenialReasonLabel
    };

    public static string SubscriptionLabel(Guid? subscriptionId, bool hasSubscription) => subscriptionId switch
    {
        null => NoSubscriptionLabel,
        _ when !hasSubscription => MissingSubscriptionLabel,
        Guid id => $"Abbonamento {id:D}"
    };

    public static string FormatOccurredAt(DateTimeOffset occurredAt) =>
        occurredAt.ToUniversalTime().ToString("dd/MM/yyyy HH:mm 'UTC'", CultureInfo.GetCultureInfo("it-IT"));

    private static Dictionary<Guid, T> ById<T>(IEnumerable<T> entities) where T : DemoEntity =>
        entities.GroupBy(entity => entity.Id).ToDictionary(group => group.Key, group => group.First());

    private static string NormalizeSearch(string? value) =>
        string.Join(' ', (value ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static IEnumerable<string> SearchableValues(AccessDirectoryItem item)
    {
        yield return item.MemberNumber;
        yield return item.MemberFirstName;
        yield return item.MemberLastName;
        yield return $"{item.MemberFirstName} {item.MemberLastName}";
        yield return $"{item.MemberLastName} {item.MemberFirstName}";
        yield return item.RecordedBy;
    }

    private static IOrderedEnumerable<AccessDirectoryItem> Order(IEnumerable<AccessDirectoryItem> items, AccessSortField field, AccessSortDirection direction)
    {
        IOrderedEnumerable<AccessDirectoryItem> ordered = field switch
        {
            AccessSortField.OccurredAt => ApplyDirection(items, item => item.OccurredAtUtc.UtcTicks, direction),
            AccessSortField.MemberNumber => ApplyDirection(items, item => item.MemberNumber, direction, StringComparer.OrdinalIgnoreCase),
            AccessSortField.MemberName => ApplyDirection(items, item => $"{item.MemberLastName}\0{item.MemberFirstName}", direction, StringComparer.OrdinalIgnoreCase),
            AccessSortField.Outcome => ApplyDirection(items, item => item.Outcome, direction),
            AccessSortField.Source => ApplyDirection(items, item => item.Source, direction),
            _ => throw new ArgumentOutOfRangeException(nameof(field), field, null)
        };
        return ordered.ThenBy(item => item.Id);
    }

    private static IOrderedEnumerable<AccessDirectoryItem> ApplyDirection<TKey>(IEnumerable<AccessDirectoryItem> items, Func<AccessDirectoryItem, TKey> selector, AccessSortDirection direction, IComparer<TKey>? comparer = null) =>
        direction == AccessSortDirection.Ascending ? items.OrderBy(selector, comparer) : items.OrderByDescending(selector, comparer);
}
