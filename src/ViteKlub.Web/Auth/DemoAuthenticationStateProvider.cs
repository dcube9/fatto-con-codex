using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using ViteKlub.Core.Data;
using ViteKlub.Web.Services;

namespace ViteKlub.Web.Auth;

public sealed class DemoAuthenticationStateProvider(
    IDemoDatasetStore datasetStore,
    IJSRuntime jsRuntime) : AuthenticationStateProvider, IDemoAuthenticationService, IDisposable
{
    private const string SessionKey = "viteklub.demo-user-id";
    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _initialized;

    public DemoUser? CurrentUser { get; private set; }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        await EnsureInitializedAsync();
        return CreateAuthenticationState(CurrentUser);
    }

    public async Task<IReadOnlyList<DemoUser>> GetProfilesAsync(CancellationToken cancellationToken = default)
    {
        DemoDatasetSnapshot snapshot = await datasetStore.GetAsync(cancellationToken);
        return snapshot.Dataset.Users
            .Where(user => user.IsActive)
            .OrderBy(user => user.Role)
            .ThenBy(user => user.DisplayName, StringComparer.CurrentCulture)
            .ToArray();
    }

    public async Task<bool> LoginAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        DemoDatasetSnapshot snapshot = await datasetStore.GetAsync(cancellationToken);
        DemoUser? user = snapshot.Dataset.Users.SingleOrDefault(candidate => candidate.Id == userId && candidate.IsActive);
        if (user is null)
        {
            return false;
        }

        CurrentUser = user;
        _initialized = true;
        await TrySetSessionAsync(user.Id, cancellationToken);
        NotifyAuthenticationStateChanged(Task.FromResult(CreateAuthenticationState(user)));
        return true;
    }

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        CurrentUser = null;
        _initialized = true;
        await TryClearSessionAsync(cancellationToken);
        NotifyAuthenticationStateChanged(Task.FromResult(CreateAuthenticationState(null)));
    }

    public void Dispose()
    {
        _gate.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task EnsureInitializedAsync()
    {
        if (_initialized)
        {
            return;
        }

        await _gate.WaitAsync();
        try
        {
            if (_initialized)
            {
                return;
            }

            string? storedUserId = await TryGetSessionAsync();
            if (Guid.TryParse(storedUserId, out Guid userId))
            {
                DemoDatasetSnapshot snapshot = await datasetStore.GetAsync();
                CurrentUser = snapshot.Dataset.Users.SingleOrDefault(
                    user => user.Id == userId && user.IsActive);
            }

            if (storedUserId is not null && CurrentUser is null)
            {
                await TryClearSessionAsync();
            }

            _initialized = true;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<string?> TryGetSessionAsync()
    {
        try
        {
            return await jsRuntime.InvokeAsync<string?>("sessionStorage.getItem", SessionKey);
        }
        catch (JSException)
        {
            return null;
        }
    }

    private async Task TrySetSessionAsync(Guid userId, CancellationToken cancellationToken)
    {
        try
        {
            await jsRuntime.InvokeVoidAsync(
                "sessionStorage.setItem",
                cancellationToken,
                SessionKey,
                userId.ToString("D", CultureInfo.InvariantCulture));
        }
        catch (JSException)
        {
            // The in-memory session remains valid for the lifetime of the current tab.
        }
    }

    private async Task TryClearSessionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", cancellationToken, SessionKey);
        }
        catch (JSException)
        {
            // There is no persistent session to clear when browser storage is unavailable.
        }
    }

    private static AuthenticationState CreateAuthenticationState(DemoUser? user)
    {
        if (user is null)
        {
            return new(new ClaimsPrincipal(new ClaimsIdentity()));
        }

        Claim[] claims =
        [
            new(ClaimTypes.NameIdentifier, user.Id.ToString("D", CultureInfo.InvariantCulture)),
            new(ClaimTypes.Name, user.DisplayName),
            new(ClaimTypes.Role, user.Role.ToString())
        ];
        return new(new ClaimsPrincipal(new ClaimsIdentity(claims, "ViteKlubDemo")));
    }
}
