using System.Net;
using Microsoft.JSInterop;
using ViteKlub.Core.Data;
using ViteKlub.Web.Services;
using Xunit;

namespace ViteKlub.Web.Tests;

public sealed class BrowserDemoDatasetStoreTests
{
    [Fact]
    public async Task FirstLoadPersistsValidatedSeedInIndexedDb()
    {
        DemoDataset expected = CreateDataset();
        var module = new FakeJsModule(true, null);
        using var httpClient = CreateHttpClient(expected);
        await using var store = CreateStore(httpClient, module);

        DemoDatasetSnapshot snapshot = await store.GetAsync();

        Assert.True(snapshot.InitializedFromSeed);
        Assert.Equal(DemoStorageMode.IndexedDb, snapshot.StorageMode);
        Assert.NotNull(module.StoredJson);
        Assert.Equal(expected.DatasetVersion, DemoDatasetJson.Deserialize(module.StoredJson).DatasetVersion);
    }

    [Fact]
    public async Task ExistingDatasetIsLoadedWithoutDownloadingSeed()
    {
        DemoDataset expected = CreateDataset();
        var module = new FakeJsModule(true, DemoDatasetJson.Serialize(expected));
        var handler = new StubHttpMessageHandler(DemoDatasetJson.Serialize(expected));
        using var httpClient = new HttpClient(handler) { BaseAddress = new("https://viteklub.invalid/") };
        await using var store = CreateStore(httpClient, module);

        DemoDatasetSnapshot snapshot = await store.GetAsync();

        Assert.False(snapshot.InitializedFromSeed);
        Assert.Equal(expected.DatasetVersion, snapshot.Dataset.DatasetVersion);
        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task UnsupportedIndexedDbFallsBackToMemory()
    {
        DemoDataset expected = CreateDataset();
        var module = new FakeJsModule(false, null);
        using var httpClient = CreateHttpClient(expected);
        await using var store = CreateStore(httpClient, module);

        DemoDatasetSnapshot snapshot = await store.GetAsync();

        Assert.Equal(DemoStorageMode.Memory, snapshot.StorageMode);
        Assert.Null(module.StoredJson);
    }

    private static BrowserDemoDatasetStore CreateStore(HttpClient httpClient, FakeJsModule module)
    {
        return new(
            httpClient,
            new FakeJsRuntime(module));
    }

    private static HttpClient CreateHttpClient(DemoDataset dataset)
    {
        return new(new StubHttpMessageHandler(DemoDatasetJson.Serialize(dataset)))
        {
            BaseAddress = new("https://viteklub.invalid/")
        };
    }

    private static DemoDataset CreateDataset()
    {
        DateTimeOffset timestamp = new(2026, 9, 1, 8, 0, 0, TimeSpan.Zero);
        var administrator = new DemoUser
        {
            Id = Guid.Parse("bdeea5a3-aa33-41bf-a709-f817a407d42f"),
            CreatedAtUtc = timestamp,
            UpdatedAtUtc = timestamp,
            Username = "admin.demo",
            DisplayName = "Administrator demo",
            Role = DemoRole.Administrator
        };

        return new()
        {
            SchemaVersion = DemoDataset.CurrentSchemaVersion,
            DatasetVersion = "test-v1",
            ReferenceDate = new(2026, 9, 1),
            Users = [administrator],
            Members = [],
            MembershipPlans = [],
            Subscriptions = [],
            Accesses = [],
            Payments = [],
            AuditEvents = []
        };
    }

    private sealed class StubHttpMessageHandler(string responseBody) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody)
            };
            return Task.FromResult(response);
        }
    }

    private sealed class FakeJsRuntime(FakeJsModule module) : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            return InvokeAsync<TValue>(identifier, CancellationToken.None, args);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args)
        {
            Assert.Equal("import", identifier);
            return ValueTask.FromResult((TValue)(object)module);
        }
    }

    private sealed class FakeJsModule(bool isSupported, string? storedJson) : IJSObjectReference
    {
        public string? StoredJson { get; private set; } = storedJson;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

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
                "isSupported" => isSupported,
                "readDataset" => StoredJson,
                "writeDataset" => WriteDataset(args),
                _ => throw new InvalidOperationException($"Chiamata JavaScript inattesa: {identifier}.")
            };
            return ValueTask.FromResult(result is null ? default! : (TValue)result);
        }

        private object? WriteDataset(object?[]? args)
        {
            StoredJson = Assert.IsType<string>(Assert.Single(args!));
            return null;
        }
    }
}
