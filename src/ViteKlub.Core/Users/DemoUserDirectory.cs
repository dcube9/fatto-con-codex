using ViteKlub.Core.Data;

namespace ViteKlub.Core.Users;

public sealed record DemoUserDirectoryQuery(
    string? Search = null,
    DemoRole? Role = null,
    int Page = 1,
    int PageSize = 10);

public sealed record DemoUserDirectoryResult(
    IReadOnlyList<DemoUser> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int PageCount => TotalCount == 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
}

public static class DemoUserDirectory
{
    public static DemoUserDirectoryResult Query(IEnumerable<DemoUser> users, DemoUserDirectoryQuery query)
    {
        ArgumentNullException.ThrowIfNull(users);
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
        IEnumerable<DemoUser> filtered = users;

        if (search.Length > 0)
        {
            filtered = filtered.Where(user => SearchableValues(user)
                .Any(value => value.Contains(search, StringComparison.OrdinalIgnoreCase)));
        }

        if (query.Role is not null)
        {
            filtered = filtered.Where(user => user.Role == query.Role);
        }

        DemoUser[] ordered = filtered
            .OrderBy(user => user.DisplayName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ThenBy(user => user.Username ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ThenBy(user => user.Id)
            .ToArray();
        long offset = (long)(query.Page - 1) * query.PageSize;
        DemoUser[] page = offset >= ordered.Length
            ? []
            : ordered.Skip((int)offset).Take(query.PageSize).ToArray();

        return new(page, ordered.Length, query.Page, query.PageSize);
    }

    public static DemoUser? FindById(IEnumerable<DemoUser> users, Guid id)
    {
        ArgumentNullException.ThrowIfNull(users);
        return users.SingleOrDefault(user => user.Id == id);
    }

    public static string RoleLabel(DemoRole role) => role switch
    {
        DemoRole.Administrator => "Amministratore",
        DemoRole.Manager => "Responsabile",
        DemoRole.Receptionist => "Receptionist",
        DemoRole.Viewer => "Consultazione",
        _ => "Ruolo non riconosciuto"
    };

    private static string NormalizeSearch(string? value) =>
        string.Join(' ', (value ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static IEnumerable<string> SearchableValues(DemoUser user)
    {
        yield return user.DisplayName ?? string.Empty;
        yield return user.Username ?? string.Empty;
    }
}
