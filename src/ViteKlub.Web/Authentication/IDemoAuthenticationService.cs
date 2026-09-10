using ViteKlub.Core.Data;

namespace ViteKlub.Web.Authentication;

public interface IDemoAuthenticationService
{
    Task<IReadOnlyList<DemoUser>> GetAvailableUsersAsync(CancellationToken cancellationToken = default);

    Task<bool> LoginAsync(Guid userId, CancellationToken cancellationToken = default);

    Task LogoutAsync(CancellationToken cancellationToken = default);
}
