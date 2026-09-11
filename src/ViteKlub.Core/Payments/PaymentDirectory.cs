using System.Globalization;
using ViteKlub.Core.Data;

namespace ViteKlub.Core.Payments;

public enum PaymentSortField { OccurredAt, MemberNumber, MemberName, Amount, Method, Status }
public enum PaymentSortDirection { Ascending, Descending }

public sealed record PaymentDirectoryItem(
    Guid Id,
    Guid MemberId,
    Guid? SubscriptionId,
    Guid RecordedByUserId,
    DateTimeOffset OccurredAtUtc,
    decimal Amount,
    string? Currency,
    PaymentMethod Method,
    PaymentStatus Status,
    string? Reference,
    string MemberNumber,
    string MemberFirstName,
    string MemberLastName,
    bool HasMember,
    bool HasSubscription,
    string RecordedBy,
    bool HasRecordedByUser)
{
    public string MemberFullName => HasMember ? $"{MemberFirstName} {MemberLastName}" : PaymentDirectory.MissingMemberLabel;
    public string SubscriptionLabel => PaymentDirectory.SubscriptionLabel(SubscriptionId, HasSubscription);
    public string ReferenceLabel => PaymentDirectory.ReferenceLabel(Reference);
    public string FormattedAmount => PaymentDirectory.FormatAmount(Amount, Currency);
}

public sealed record PaymentDirectoryQuery(
    string? Search = null,
    PaymentMethod? Method = null,
    PaymentStatus? Status = null,
    PaymentSortField SortBy = PaymentSortField.OccurredAt,
    PaymentSortDirection Direction = PaymentSortDirection.Descending,
    int Page = 1,
    int PageSize = 10);

public sealed record PaymentDirectoryResult(IReadOnlyList<PaymentDirectoryItem> Items, int TotalCount, int Page, int PageSize)
{
    public int PageCount => TotalCount == 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
}

public static class PaymentDirectory
{
    public const string MissingMemberLabel = "Iscritto non disponibile";
    public const string NoSubscriptionLabel = "Nessun abbonamento associato";
    public const string MissingSubscriptionLabel = "Abbonamento non disponibile";
    public const string MissingUserLabel = "Utente demo non disponibile";
    public const string MissingReferenceLabel = "Nessun riferimento";
    public const string MissingCurrencyLabel = "Valuta non disponibile";

    public static IReadOnlyList<PaymentDirectoryItem> Project(
        IEnumerable<Payment> payments,
        IEnumerable<Member> members,
        IEnumerable<MemberSubscription> subscriptions,
        IEnumerable<DemoUser> users)
    {
        ArgumentNullException.ThrowIfNull(payments);
        ArgumentNullException.ThrowIfNull(members);
        ArgumentNullException.ThrowIfNull(subscriptions);
        ArgumentNullException.ThrowIfNull(users);

        Dictionary<Guid, Member> membersById = ById(members);
        Dictionary<Guid, MemberSubscription> subscriptionsById = ById(subscriptions);
        Dictionary<Guid, DemoUser> usersById = ById(users);

        return payments.Select(payment =>
        {
            membersById.TryGetValue(payment.MemberId, out Member? member);
            bool hasSubscription = payment.SubscriptionId is Guid subscriptionId && subscriptionsById.ContainsKey(subscriptionId);
            usersById.TryGetValue(payment.RecordedByUserId, out DemoUser? user);
            return new PaymentDirectoryItem(
                payment.Id, payment.MemberId, payment.SubscriptionId, payment.RecordedByUserId,
                payment.OccurredAtUtc, payment.Amount, payment.Currency, payment.Method, payment.Status,
                payment.Reference, member?.MemberNumber ?? MissingMemberLabel, member?.FirstName ?? string.Empty,
                member?.LastName ?? string.Empty, member is not null, hasSubscription,
                user is null ? MissingUserLabel : $"{user.DisplayName} ({user.Username})", user is not null);
        }).ToArray();
    }

    public static PaymentDirectoryResult Query(IEnumerable<PaymentDirectoryItem> payments, PaymentDirectoryQuery query)
    {
        ArgumentNullException.ThrowIfNull(payments);
        ArgumentNullException.ThrowIfNull(query);
        if (query.Page < 1 || query.PageSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(query), "Pagina e dimensione pagina devono essere almeno 1.");
        }

        string search = NormalizeSearch(query.Search);
        IEnumerable<PaymentDirectoryItem> filtered = payments;
        if (search.Length > 0)
        {
            filtered = filtered.Where(item => SearchableValues(item)
                .Any(value => NormalizeSearch(value).Contains(search, StringComparison.OrdinalIgnoreCase)));
        }

        if (query.Method is not null) filtered = filtered.Where(item => item.Method == query.Method);
        if (query.Status is not null) filtered = filtered.Where(item => item.Status == query.Status);

        PaymentDirectoryItem[] ordered = Order(filtered, query.SortBy, query.Direction).ToArray();
        long offset = (long)(query.Page - 1) * query.PageSize;
        PaymentDirectoryItem[] page = offset >= ordered.Length ? [] : ordered.Skip((int)offset).Take(query.PageSize).ToArray();
        return new(page, ordered.Length, query.Page, query.PageSize);
    }

    public static PaymentDirectoryItem? FindById(IEnumerable<PaymentDirectoryItem> payments, Guid id) =>
        payments.FirstOrDefault(item => item.Id == id);

    public static string MethodLabel(PaymentMethod method) => method switch
    {
        PaymentMethod.Cash => "Contanti",
        PaymentMethod.Card => "Carta",
        PaymentMethod.BankTransfer => "Bonifico bancario",
        PaymentMethod.Other => "Altro",
        _ => "Metodo non disponibile"
    };

    public static string StatusLabel(PaymentStatus status) => status switch
    {
        PaymentStatus.Pending => "In attesa",
        PaymentStatus.Completed => "Completato",
        PaymentStatus.Failed => "Non riuscito",
        PaymentStatus.Cancelled => "Annullato",
        _ => "Stato non disponibile"
    };

    public static string FormatAmount(decimal amount, string? currency)
    {
        string formatted = amount.ToString("N2", CultureInfo.GetCultureInfo("it-IT"));
        string normalizedCurrency = (currency ?? string.Empty).Trim().ToUpperInvariant();
        bool validCurrency = normalizedCurrency.Length == 3 && normalizedCurrency.All(char.IsAsciiLetter);
        return validCurrency ? $"{formatted} {normalizedCurrency}" : $"{formatted} {MissingCurrencyLabel}";
    }

    public static string FormatOccurredAt(DateTimeOffset occurredAt) =>
        occurredAt.ToUniversalTime().ToString("dd/MM/yyyy HH:mm 'UTC'", CultureInfo.GetCultureInfo("it-IT"));

    public static string ReferenceLabel(string? reference) =>
        string.IsNullOrWhiteSpace(reference) ? MissingReferenceLabel : reference.Trim();

    public static string SubscriptionLabel(Guid? subscriptionId, bool hasSubscription) => subscriptionId switch
    {
        null => NoSubscriptionLabel,
        _ when !hasSubscription => MissingSubscriptionLabel,
        Guid id => $"Abbonamento {id:D}"
    };

    private static Dictionary<Guid, T> ById<T>(IEnumerable<T> entities) where T : DemoEntity =>
        entities.GroupBy(entity => entity.Id).ToDictionary(group => group.Key, group => group.First());

    private static string NormalizeSearch(string? value) =>
        string.Join(' ', (value ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static IEnumerable<string> SearchableValues(PaymentDirectoryItem item)
    {
        yield return item.MemberNumber;
        yield return item.MemberFirstName;
        yield return item.MemberLastName;
        yield return $"{item.MemberFirstName} {item.MemberLastName}";
        yield return $"{item.MemberLastName} {item.MemberFirstName}";
        yield return item.Reference ?? string.Empty;
        yield return item.RecordedBy;
    }

    private static IOrderedEnumerable<PaymentDirectoryItem> Order(IEnumerable<PaymentDirectoryItem> items, PaymentSortField field, PaymentSortDirection direction)
    {
        IOrderedEnumerable<PaymentDirectoryItem> ordered = field switch
        {
            PaymentSortField.OccurredAt => ApplyDirection(items, item => item.OccurredAtUtc.UtcTicks, direction),
            PaymentSortField.MemberNumber => ApplyDirection(items, item => item.MemberNumber, direction, StringComparer.OrdinalIgnoreCase),
            PaymentSortField.MemberName => ApplyDirection(items, item => $"{item.MemberLastName}\0{item.MemberFirstName}", direction, StringComparer.OrdinalIgnoreCase),
            PaymentSortField.Amount => ApplyDirection(items, item => item.Amount, direction),
            PaymentSortField.Method => ApplyDirection(items, item => item.Method, direction),
            PaymentSortField.Status => ApplyDirection(items, item => item.Status, direction),
            _ => throw new ArgumentOutOfRangeException(nameof(field), field, null)
        };
        return ordered.ThenBy(item => item.Id);
    }

    private static IOrderedEnumerable<PaymentDirectoryItem> ApplyDirection<TKey>(IEnumerable<PaymentDirectoryItem> items, Func<PaymentDirectoryItem, TKey> selector, PaymentSortDirection direction, IComparer<TKey>? comparer = null) =>
        direction == PaymentSortDirection.Ascending ? items.OrderBy(selector, comparer) : items.OrderByDescending(selector, comparer);
}
