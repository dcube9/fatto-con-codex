using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using ViteKlub.Core.Data;
using ViteKlub.Web.Storage;

namespace ViteKlub.Web.Authentication;

public sealed class DemoAuthenticationStateProvider(
    IDemoDatasetStore datasetStore,
    IDemoSessionStore sessionStore) : AuthenticationStateProvider, IDemoAuthenticationService
{
    private static readonly AuthenticationState AnonymousState = new(new ClaimsPrincipal(new ClaimsIdentity()));
    private AuthenticationState? cachedState;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (cachedState is not null)
        {
            return cachedState;
        }

        string? storedUserId = await sessionStore.GetUserIdAsync();
        if (!Guid.TryParse(storedUserId, out Guid userId))
        {
            if (storedUserId is not null)
            {
                await sessionStore.ClearAsync();
            }

            return cachedState = AnonymousState;
        }

        DemoUser? user = await FindActiveUserAsync(userId);
        if (user is null)
        {
            await sessionStore.ClearAsync();
            return cachedState = AnonymousState;
        }

        return cachedState = CreateAuthenticatedState(user);
    }

    public async Task<IReadOnlyList<DemoUser>> GetAvailableUsersAsync(CancellationToken cancellationToken = default)
    {
        DemoDatasetSnapshot snapshot = await datasetStore.LoadAsync(cancellationToken);
        return snapshot.Dataset.Users
            .Where(user => user.IsActive)
            .OrderBy(user => user.Role)
            .ThenBy(user => user.DisplayName, StringComparer.Ordinal)
            .ToArray();
    }

    public async Task<bool> LoginAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        DemoUser? user = await FindActiveUserAsync(userId, cancellationToken);
        if (user is null)
        {
            await sessionStore.ClearAsync(cancellationToken);
            SetState(AnonymousState);
            return false;
        }

        await sessionStore.SetUserIdAsync(user.Id.ToString("D"), cancellationToken);
        SetState(CreateAuthenticatedState(user));
        return true;
    }

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        await sessionStore.ClearAsync(cancellationToken);
        SetState(AnonymousState);
    }

    private async Task<DemoUser?> FindActiveUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        DemoDatasetSnapshot snapshot = await datasetStore.LoadAsync(cancellationToken);
        return snapshot.Dataset.Users.SingleOrDefault(user => user.Id == userId && user.IsActive);
    }

    private void SetState(AuthenticationState state)
    {
        cachedState = state;
        NotifyAuthenticationStateChanged(Task.FromResult(state));
    }

    internal static AuthenticationState CreateAuthenticatedState(DemoUser user)
    {
        Claim[] claims =
        [
            new(ClaimTypes.NameIdentifier, user.Id.ToString("D")),
            new(ClaimTypes.Name, user.DisplayName),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("preferred_username", user.Username),
        ];

        return new(new ClaimsPrincipal(new ClaimsIdentity(claims, "DemoBrowser")));
    }
}
