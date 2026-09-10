namespace ViteKlub.Web.Authentication;

public interface IDemoSessionStore
{
    Task<string?> GetUserIdAsync(CancellationToken cancellationToken = default);

    Task SetUserIdAsync(string userId, CancellationToken cancellationToken = default);

    Task ClearAsync(CancellationToken cancellationToken = default);
}
