using System.Net;
using System.Reflection;
using Microsoft.JSInterop;
using ViteKlub.Core.Data;
using ViteKlub.Web.Storage;
using Xunit;

namespace ViteKlub.Web.Tests;

public sealed class BrowserDemoDatasetStoreTests
{
    private static readonly string SeedJson = LoadSeedJson();

    [Fact]
    public async Task LoadAsyncSeedsEmptyIndexedDb()
    {
        var jsRuntime = new TestJsRuntime();
        var store = CreateStore(jsRuntime);

        DemoDatasetSnapshot snapshot = await store.LoadAsync();

        Assert.Equal(DemoStorageMode.IndexedDb, snapshot.StorageMode);
        Assert.Equal("initial-v1", snapshot.Dataset.DatasetVersion);
        Assert.Equal(SeedJson, jsRuntime.SavedJson);
    }

    [Fact]
    public async Task LoadAsyncUsesDatasetAlreadyStoredInIndexedDb()
    {
        DemoDataset storedDataset = DemoDatasetJson.Deserialize(SeedJson) with
        {
            DatasetVersion = "stored-v2"
        };
        var jsRuntime = new TestJsRuntime(DemoDatasetJson.Serialize(storedDataset));
        var store = CreateStore(jsRuntime);

        DemoDatasetSnapshot snapshot = await store.LoadAsync();

        Assert.Equal(DemoStorageMode.IndexedDb, snapshot.StorageMode);
        Assert.Equal("stored-v2", snapshot.Dataset.DatasetVersion);
        Assert.Null(jsRuntime.SavedJson);
    }

    [Fact]
    public async Task LoadAsyncFallsBackToMemoryWhenIndexedDbIsUnavailable()
    {
        var jsRuntime = new TestJsRuntime { IsUnavailable = true };
        var store = CreateStore(jsRuntime);

        DemoDatasetSnapshot first = await store.LoadAsync();
        DemoDatasetSnapshot second = await store.LoadAsync();

        Assert.Equal(DemoStorageMode.InMemory, first.StorageMode);
        Assert.Same(first.Dataset, second.Dataset);
        Assert.Equal(1, jsRuntime.LoadAttempts);
    }

    private static BrowserDemoDatasetStore CreateStore(TestJsRuntime jsRuntime)
    {
        var httpClient = new HttpClient(new SeedMessageHandler())
        {
            BaseAddress = new Uri("https://viteklub.invalid/")
        };
        return new BrowserDemoDatasetStore(httpClient, jsRuntime);
    }

    private static string LoadSeedJson()
    {
        Assembly assembly = typeof(BrowserDemoDatasetStoreTests).Assembly;
        string resourceName = assembly.GetManifestResourceNames().Single(
            name => name.EndsWith("initial-dataset.json", StringComparison.Ordinal));
        using Stream stream = assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private sealed class SeedMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Assert.Equal("data/initial-dataset.json", request.RequestUri?.PathAndQuery.TrimStart('/'));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(SeedJson)
            });
        }
    }

    private sealed class TestJsRuntime(string? storedJson = null) : IJSRuntime
    {
        public bool IsUnavailable { get; init; }

        public int LoadAttempts { get; private set; }

        public string? SavedJson { get; private set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args)
        {
            if (identifier == "demoStorage.load")
            {
                LoadAttempts++;
            }

            if (IsUnavailable)
            {
                throw new JSException("IndexedDB non disponibile.");
            }

            if (identifier == "demoStorage.load")
            {
                return ValueTask.FromResult((TValue)(object?)storedJson!);
            }

            if (identifier == "demoStorage.save")
            {
                SavedJson = Assert.IsType<string>(args?[0]);
                return ValueTask.FromResult(default(TValue)!);
            }

            throw new InvalidOperationException($"Invocazione JavaScript inattesa: {identifier}.");
        }
    }
}
