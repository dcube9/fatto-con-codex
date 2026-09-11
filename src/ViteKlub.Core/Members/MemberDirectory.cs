using ViteKlub.Core.Data;

namespace ViteKlub.Core.Members;

public enum MemberSortField
{
    MemberNumber,
    Name,
    JoinedOn,
    MedicalCertificateExpiresOn
}

public enum MemberSortDirection
{
    Ascending,
    Descending
}

public enum MedicalCertificateState
{
    Missing,
    Expired,
    ExpiringSoon,
    Valid
}

public sealed record MemberDirectoryQuery(
    string? Search = null,
    MemberStatus? Status = null,
    MemberSortField SortBy = MemberSortField.MemberNumber,
    MemberSortDirection Direction = MemberSortDirection.Ascending,
    int Page = 1,
    int PageSize = 10);

public sealed record MemberDirectoryResult(
    IReadOnlyList<Member> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int PageCount => TotalCount == 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
}

public static class MemberDirectory
{
    public static MemberDirectoryResult Query(IEnumerable<Member> members, MemberDirectoryQuery query)
    {
        ArgumentNullException.ThrowIfNull(members);
        ArgumentNullException.ThrowIfNull(query);

        if (query.Page < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(query), "La pagina deve essere almeno 1.");
        }

        if (query.PageSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(query), "La dimensione pagina deve essere almeno 1.");
        }

        string search = NormalizeSearch(query.Search);
        IEnumerable<Member> filtered = members;

        if (search.Length > 0)
        {
            filtered = filtered.Where(member => SearchableValues(member)
                .Any(value => value.Contains(search, StringComparison.OrdinalIgnoreCase)));
        }

        if (query.Status is not null)
        {
            filtered = filtered.Where(member => member.Status == query.Status);
        }

        IOrderedEnumerable<Member> ordered = Order(filtered, query.SortBy, query.Direction);
        Member[] projected = ordered.ToArray();
        long offset = (long)(query.Page - 1) * query.PageSize;
        Member[] page = offset >= projected.Length
            ? []
            : projected.Skip((int)offset).Take(query.PageSize).ToArray();

        return new(page, projected.Length, query.Page, query.PageSize);
    }

    public static Member? FindById(IEnumerable<Member> members, Guid id) =>
        members.SingleOrDefault(member => member.Id == id);

    public static MedicalCertificateState ClassifyCertificate(DateOnly? expiresOn, DateOnly referenceDate)
    {
        if (expiresOn is null)
        {
            return MedicalCertificateState.Missing;
        }

        if (expiresOn < referenceDate)
        {
            return MedicalCertificateState.Expired;
        }

        return expiresOn <= referenceDate.AddDays(30)
            ? MedicalCertificateState.ExpiringSoon
            : MedicalCertificateState.Valid;
    }

    public static string MemberStatusLabel(MemberStatus status) => status switch
    {
        MemberStatus.Active => "Attivo",
        MemberStatus.Suspended => "Sospeso",
        MemberStatus.Archived => "Archiviato",
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
    };

    public static string CertificateStateLabel(MedicalCertificateState state) => state switch
    {
        MedicalCertificateState.Missing => "Certificato non presente",
        MedicalCertificateState.Expired => "Certificato scaduto",
        MedicalCertificateState.ExpiringSoon => "Certificato in scadenza entro 30 giorni",
        MedicalCertificateState.Valid => "Certificato valido",
        _ => throw new ArgumentOutOfRangeException(nameof(state), state, null)
    };

    private static string NormalizeSearch(string? value) =>
        string.Join(' ', (value ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static IEnumerable<string> SearchableValues(Member member)
    {
        yield return member.MemberNumber;
        yield return member.FirstName;
        yield return member.LastName;
        yield return $"{member.FirstName} {member.LastName}";
        yield return $"{member.LastName} {member.FirstName}";
    }

    private static IOrderedEnumerable<Member> Order(
        IEnumerable<Member> members,
        MemberSortField sortBy,
        MemberSortDirection direction)
    {
        IOrderedEnumerable<Member> ordered = sortBy switch
        {
            MemberSortField.MemberNumber => ApplyDirection(
                members, member => member.MemberNumber, direction, StringComparer.OrdinalIgnoreCase),
            MemberSortField.Name => ApplyDirection(
                members, member => $"{member.LastName}\u0000{member.FirstName}", direction, StringComparer.OrdinalIgnoreCase),
            MemberSortField.JoinedOn => ApplyDirection(
                members, member => member.JoinedOn, direction),
            MemberSortField.MedicalCertificateExpiresOn => ApplyDirection(
                members, member => member.MedicalCertificateExpiresOn, direction),
            _ => throw new ArgumentOutOfRangeException(nameof(sortBy), sortBy, null)
        };

        return ordered.ThenBy(member => member.Id);
    }

    private static IOrderedEnumerable<Member> ApplyDirection<TKey>(
        IEnumerable<Member> members,
        Func<Member, TKey> selector,
        MemberSortDirection direction,
        IComparer<TKey>? comparer = null) =>
        direction == MemberSortDirection.Ascending
            ? members.OrderBy(selector, comparer)
            : members.OrderByDescending(selector, comparer);
}
