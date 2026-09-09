using ViteKlub.Core.Data;

namespace ViteKlub.Web.Auth;

public interface IDemoAuthenticationService
{
    DemoUser? CurrentUser { get; }

    Task<IReadOnlyList<DemoUser>> GetProfilesAsync(CancellationToken cancellationToken = default);

    Task<bool> LoginAsync(Guid userId, CancellationToken cancellationToken = default);

    Task LogoutAsync(CancellationToken cancellationToken = default);
}
