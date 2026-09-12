using System.Security.Claims;
using ViteKlub.Core.Data;
using ViteKlub.Web.Authentication;
using ViteKlub.Web.Storage;
using Xunit;

namespace ViteKlub.Web.Tests;

public sealed class DemoAuthenticationStateProviderTests
{
    [Fact]
    public async Task LoginStoresSessionAndComposesClaims()
    {
        DemoDataset dataset = LoadDataset();
        DemoUser user = dataset.Users.Single(item => item.Role == DemoRole.Manager);
        var sessionStore = new TestSessionStore();
        var provider = CreateProvider(dataset, sessionStore);

        bool loggedIn = await provider.LoginAsync(user.Id);
        ClaimsPrincipal principal = (await provider.GetAuthenticationStateAsync()).User;

        Assert.True(loggedIn);
        Assert.Equal(user.Id.ToString("D"), sessionStore.UserId);
        Assert.True(principal.Identity?.IsAuthenticated);
        Assert.Equal(user.DisplayName, principal.Identity?.Name);
        Assert.Equal(user.Id.ToString("D"), principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        Assert.Equal(user.Username, principal.FindFirst("preferred_username")?.Value);
        Assert.True(principal.IsInRole(DemoRoles.Manager));
    }

    [Fact]
    public async Task LogoutClearsSessionAndReturnsAnonymousPrincipal()
    {
        DemoDataset dataset = LoadDataset();
        DemoUser user = dataset.Users[0];
        var sessionStore = new TestSessionStore { UserId = user.Id.ToString("D") };
        var provider = CreateProvider(dataset, sessionStore);

        await provider.LogoutAsync();

        Assert.Null(sessionStore.UserId);
        Assert.False((await provider.GetAuthenticationStateAsync()).User.Identity?.IsAuthenticated);
    }

    [Fact]
    public async Task AuthenticationStateRestoresStoredSession()
    {
        DemoDataset dataset = LoadDataset();
        DemoUser user = dataset.Users.Single(item => item.Role == DemoRole.Receptionist);
        var sessionStore = new TestSessionStore { UserId = user.Id.ToString("D") };

        ClaimsPrincipal principal = (await CreateProvider(dataset, sessionStore).GetAuthenticationStateAsync()).User;

        Assert.Equal(user.DisplayName, principal.Identity?.Name);
        Assert.True(principal.IsInRole(DemoRoles.Receptionist));
        Assert.Equal(0, sessionStore.ClearCount);
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000001")]
    public async Task InvalidOrUnknownSessionIsCleared(string storedUserId)
    {
        var sessionStore = new TestSessionStore { UserId = storedUserId };

        ClaimsPrincipal principal = (await CreateProvider(LoadDataset(), sessionStore).GetAuthenticationStateAsync()).User;

        Assert.False(principal.Identity?.IsAuthenticated);
        Assert.Null(sessionStore.UserId);
        Assert.Equal(1, sessionStore.ClearCount);
    }

    [Fact]
    public async Task DisabledUserCannotRestoreOrStartSession()
    {
        DemoDataset dataset = LoadDataset();
        DemoUser disabledUser = dataset.Users[0] with { IsActive = false };
        dataset = dataset with { Users = [disabledUser, .. dataset.Users.Skip(1)] };
        var sessionStore = new TestSessionStore { UserId = disabledUser.Id.ToString("D") };
        var provider = CreateProvider(dataset, sessionStore);

        ClaimsPrincipal restored = (await provider.GetAuthenticationStateAsync()).User;
        bool loggedIn = await provider.LoginAsync(disabledUser.Id);

        Assert.False(restored.Identity?.IsAuthenticated);
        Assert.False(loggedIn);
        Assert.Null(sessionStore.UserId);
    }

    [Fact]
    public async Task AvailableUsersExcludeDisabledProfiles()
    {
        DemoDataset dataset = LoadDataset();
        DemoUser disabledUser = dataset.Users[0] with { IsActive = false };
        dataset = dataset with { Users = [disabledUser, .. dataset.Users.Skip(1)] };

        IReadOnlyList<DemoUser> users = await CreateProvider(dataset, new()).GetAvailableUsersAsync();

        Assert.DoesNotContain(users, user => user.Id == disabledUser.Id);
        Assert.Equal(3, users.Count);
    }

    private static DemoAuthenticationStateProvider CreateProvider(DemoDataset dataset, TestSessionStore sessionStore) =>
        new(new TestDatasetStore(dataset), sessionStore);

    private static DemoDataset LoadDataset()
    {
        using Stream stream = typeof(DemoAuthenticationStateProviderTests).Assembly
            .GetManifestResourceStream("ViteKlub.Web.Tests.Data.initial-dataset.json")!;
        using var reader = new StreamReader(stream);
        return DemoDatasetJson.Deserialize(reader.ReadToEnd());
    }

    private sealed class TestDatasetStore(DemoDataset dataset) : IDemoDatasetStore
    {
        public Task<DemoDatasetSnapshot> LoadAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(new DemoDatasetSnapshot(dataset, DemoStorageMode.InMemory, 1));

        public Task<DemoDatasetSnapshot> SaveAsync(DemoDataset value, long expectedRevision, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<DemoDatasetSnapshot> RestoreInitialDatasetAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class TestSessionStore : IDemoSessionStore
    {
        public string? UserId { get; set; }

        public int ClearCount { get; private set; }

        public Task<string?> GetUserIdAsync(CancellationToken cancellationToken = default) => Task.FromResult(UserId);

        public Task SetUserIdAsync(string userId, CancellationToken cancellationToken = default)
        {
            UserId = userId;
            return Task.CompletedTask;
        }

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            UserId = null;
            ClearCount++;
            return Task.CompletedTask;
        }
    }
}
