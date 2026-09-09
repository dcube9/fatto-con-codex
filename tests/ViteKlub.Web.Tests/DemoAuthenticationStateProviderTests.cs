using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using ViteKlub.Core.Data;
using ViteKlub.Web.Auth;
using ViteKlub.Web.Services;
using Xunit;

namespace ViteKlub.Web.Tests;

public sealed class DemoAuthenticationStateProviderTests
{
    [Fact]
    public async Task LoginCreatesPrincipalAndPersistsSelectedProfile()
    {
        DemoDataset dataset = CreateDataset();
        var jsRuntime = new FakeSessionJsRuntime();
        using var provider = new DemoAuthenticationStateProvider(new StubDatasetStore(dataset), jsRuntime);

        bool loggedIn = await provider.LoginAsync(dataset.Users[0].Id);
        AuthenticationState state = await provider.GetAuthenticationStateAsync();

        Assert.True(loggedIn);
        Assert.Equal("Administrator demo", state.User.Identity?.Name);
        Assert.True(state.User.IsInRole(nameof(DemoRole.Administrator)));
        Assert.Equal(dataset.Users[0].Id.ToString(), jsRuntime.StoredUserId);
    }

    [Fact]
    public async Task StoredActiveProfileRestoresAuthenticationState()
    {
        DemoDataset dataset = CreateDataset();
        var jsRuntime = new FakeSessionJsRuntime(dataset.Users[1].Id.ToString());
        using var provider = new DemoAuthenticationStateProvider(new StubDatasetStore(dataset), jsRuntime);

        AuthenticationState state = await provider.GetAuthenticationStateAsync();

        Assert.Equal("Viewer demo", state.User.Identity?.Name);
        Assert.True(state.User.IsInRole(nameof(DemoRole.Viewer)));
    }

    [Fact]
    public async Task InactiveProfileCannotLogIn()
    {
        DemoDataset dataset = CreateDataset();
        DemoUser inactiveUser = dataset.Users[1] with { IsActive = false };
        dataset = dataset with { Users = [dataset.Users[0], inactiveUser] };
        var jsRuntime = new FakeSessionJsRuntime();
        using var provider = new DemoAuthenticationStateProvider(new StubDatasetStore(dataset), jsRuntime);

        bool loggedIn = await provider.LoginAsync(inactiveUser.Id);

        Assert.False(loggedIn);
        Assert.Null(provider.CurrentUser);
        Assert.Null(jsRuntime.StoredUserId);
    }

    [Fact]
    public async Task LogoutClearsAuthenticationStateAndSession()
    {
        DemoDataset dataset = CreateDataset();
        var jsRuntime = new FakeSessionJsRuntime(dataset.Users[0].Id.ToString());
        using var provider = new DemoAuthenticationStateProvider(new StubDatasetStore(dataset), jsRuntime);
        _ = await provider.GetAuthenticationStateAsync();

        await provider.LogoutAsync();
        AuthenticationState state = await provider.GetAuthenticationStateAsync();

        Assert.False(state.User.Identity?.IsAuthenticated);
        Assert.Null(provider.CurrentUser);
        Assert.Null(jsRuntime.StoredUserId);
    }

    private static DemoDataset CreateDataset()
    {
        DateTimeOffset timestamp = new(2026, 9, 1, 8, 0, 0, TimeSpan.Zero);
        return new()
        {
            SchemaVersion = DemoDataset.CurrentSchemaVersion,
            DatasetVersion = "auth-test-v1",
            ReferenceDate = new(2026, 9, 1),
            Users =
            [
                CreateUser(
                    "bdeea5a3-aa33-41bf-a709-f817a407d42f",
                    "admin.demo",
                    "Administrator demo",
                    DemoRole.Administrator,
                    timestamp),
                CreateUser(
                    "933354fa-2a19-4bc8-b738-902826471fe1",
                    "viewer.demo",
                    "Viewer demo",
                    DemoRole.Viewer,
                    timestamp)
            ],
            Members = [],
            MembershipPlans = [],
            Subscriptions = [],
            Accesses = [],
            Payments = [],
            AuditEvents = []
        };
    }

    private static DemoUser CreateUser(
        string id,
        string username,
        string displayName,
        DemoRole role,
        DateTimeOffset timestamp)
    {
        return new()
        {
            Id = Guid.Parse(id),
            CreatedAtUtc = timestamp,
            UpdatedAtUtc = timestamp,
            Username = username,
            DisplayName = displayName,
            Role = role
        };
    }

    private sealed class StubDatasetStore(DemoDataset dataset) : IDemoDatasetStore
    {
        public Task<DemoDatasetSnapshot> GetAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new DemoDatasetSnapshot(dataset, DemoStorageMode.Memory, false));
        }

        public Task<DemoDatasetSnapshot> SaveAsync(
            DemoDataset updatedDataset,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<DemoDatasetSnapshot> ResetAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FakeSessionJsRuntime(string? storedUserId = null) : IJSRuntime
    {
        public string? StoredUserId { get; private set; } = storedUserId;

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            return InvokeAsync<TValue>(identifier, CancellationToken.None, args);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args)
        {
            object? result = identifier switch
            {
                "sessionStorage.getItem" => StoredUserId,
                "sessionStorage.setItem" => SetStoredUser(args),
                "sessionStorage.removeItem" => ClearStoredUser(),
                _ => throw new InvalidOperationException($"Chiamata JavaScript inattesa: {identifier}.")
            };
            return ValueTask.FromResult(result is null ? default! : (TValue)result);
        }

        private object? SetStoredUser(object?[]? args)
        {
            object?[] values = Assert.IsType<object?[]>(args);
            Assert.Equal("viteklub.demo-user-id", values[0]);
            StoredUserId = Assert.IsType<string>(values[1]);
            return null;
        }

        private object? ClearStoredUser()
        {
            StoredUserId = null;
            return null;
        }
    }
}
