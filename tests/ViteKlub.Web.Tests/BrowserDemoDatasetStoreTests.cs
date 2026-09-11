using System.Net;
using System.Reflection;
using Microsoft.JSInterop;
using ViteKlub.Core.Dashboard;
using ViteKlub.Core.Data;
using ViteKlub.Web.Storage;
using Xunit;

namespace ViteKlub.Web.Tests;

public sealed class BrowserDemoDatasetStoreTests
{
    private static readonly string SeedJson = LoadSeedJson();

    [Fact]
    public async Task LoadAsyncSeedsEmptyIndexedDbAndExposesMetadataAndCounts()
    {
        var jsRuntime = new TestJsRuntime();
        var store = CreateStore(jsRuntime);

        DemoDatasetSnapshot snapshot = await store.LoadAsync();

        Assert.Equal(DemoStorageMode.IndexedDb, snapshot.StorageMode);
        Assert.Equal("initial-v1", snapshot.Dataset.DatasetVersion);
        Assert.Equal(1, snapshot.Dataset.SchemaVersion);
        Assert.Equal(new DateOnly(2026, 9, 1), snapshot.Dataset.ReferenceDate);
        Assert.Equal(
            [
                new DemoDatasetCollectionCount("Utenti demo", 4),
                new DemoDatasetCollectionCount("Iscritti", 75),
                new DemoDatasetCollectionCount("Piani di abbonamento", 8),
                new DemoDatasetCollectionCount("Abbonamenti", 90),
                new DemoDatasetCollectionCount("Accessi", 350),
                new DemoDatasetCollectionCount("Pagamenti", 150),
                new DemoDatasetCollectionCount("Eventi di audit", 25),
            ],
            snapshot.CollectionCounts);
        Assert.Equal(SeedJson, jsRuntime.StoredJson);

        DashboardSummary dashboard = DashboardProjection.Create(snapshot.Dataset);
        Assert.Equal(snapshot.Dataset.Members.Count, dashboard.TotalMembers);
        Assert.Equal(snapshot.Dataset.ReferenceDate, dashboard.ReferenceDate);
    }

    [Fact]
    public async Task LoadAsyncUsesDatasetAlreadyStoredInIndexedDb()
    {
        string storedJson = CreateDatasetJson(datasetVersion: "stored-v2");
        var jsRuntime = new TestJsRuntime(storedJson);
        var store = CreateStore(jsRuntime);

        DemoDatasetSnapshot snapshot = await store.LoadAsync();

        Assert.Equal(DemoStorageMode.IndexedDb, snapshot.StorageMode);
        Assert.Equal("stored-v2", snapshot.Dataset.DatasetVersion);
        Assert.Equal(0, jsRuntime.SaveAttempts);
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

    [Fact]
    public async Task RestoreUsesInitialSeedAndPersistsItInIndexedDb()
    {
        var jsRuntime = new TestJsRuntime(CreateDatasetJson(datasetVersion: "locally-edited"));
        var store = CreateStore(jsRuntime);
        Assert.Equal("locally-edited", (await store.LoadAsync()).Dataset.DatasetVersion);

        DemoDatasetSnapshot restored = await store.RestoreInitialDatasetAsync();
        DemoDatasetSnapshot loadedAgain = await store.LoadAsync();

        Assert.Equal(DemoStorageMode.IndexedDb, restored.StorageMode);
        Assert.Equal("initial-v1", restored.Dataset.DatasetVersion);
        Assert.Equal(SeedJson, jsRuntime.StoredJson);
        Assert.Equal("initial-v1", loadedAgain.Dataset.DatasetVersion);
        Assert.Equal(1, jsRuntime.SaveAttempts);
    }

    [Fact]
    public async Task RestoreUpdatesMemoryFallbackWithoutRetryingIndexedDb()
    {
        var jsRuntime = new TestJsRuntime { IsUnavailable = true };
        var handler = new SeedMessageHandler(CreateDatasetJson(datasetVersion: "first-seed"));
        var store = CreateStore(jsRuntime, handler);
        Assert.Equal("first-seed", (await store.LoadAsync()).Dataset.DatasetVersion);
        handler.Json = SeedJson;

        DemoDatasetSnapshot restored = await store.RestoreInitialDatasetAsync();
        DemoDatasetSnapshot loadedAgain = await store.LoadAsync();

        Assert.Equal(DemoStorageMode.InMemory, restored.StorageMode);
        Assert.Equal("initial-v1", restored.Dataset.DatasetVersion);
        Assert.Same(restored.Dataset, loadedAgain.Dataset);
        Assert.Equal(0, jsRuntime.SaveAttempts);
    }

    [Fact]
    public async Task RestoreDoesNotSaveMalformedSeed()
    {
        var jsRuntime = new TestJsRuntime(CreateDatasetJson(datasetVersion: "current"));
        var store = CreateStore(jsRuntime, new SeedMessageHandler("{ malformed"));

        await Assert.ThrowsAsync<System.Text.Json.JsonException>(() => store.RestoreInitialDatasetAsync());

        Assert.Equal(0, jsRuntime.SaveAttempts);
        Assert.Contains("current", jsRuntime.StoredJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RestoreValidatesSeedBeforeSavingAndKeepsCurrentDatasetOnFailure()
    {
        var jsRuntime = new TestJsRuntime(CreateDatasetJson(datasetVersion: "current"));
        var store = CreateStore(jsRuntime, new SeedMessageHandler(CreateDatasetJson(schemaVersion: 999)));

        InvalidDataException exception = await Assert.ThrowsAsync<InvalidDataException>(
            () => store.RestoreInitialDatasetAsync());

        Assert.Contains("non è valido", exception.Message, StringComparison.Ordinal);
        Assert.Equal(0, jsRuntime.SaveAttempts);
        Assert.Contains("current", jsRuntime.StoredJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RestorePropagatesSeedReadFailureWithoutSaving()
    {
        var jsRuntime = new TestJsRuntime(CreateDatasetJson(datasetVersion: "current"));
        var handler = new SeedMessageHandler(SeedJson) { ReadException = new HttpRequestException("lettura fallita") };
        var store = CreateStore(jsRuntime, handler);

        await Assert.ThrowsAsync<HttpRequestException>(() => store.RestoreInitialDatasetAsync());

        Assert.Equal(0, jsRuntime.SaveAttempts);
        Assert.Contains("current", jsRuntime.StoredJson, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RestorePropagatesSaveFailureAndKeepsCurrentIndexedDbDataset()
    {
        string currentJson = CreateDatasetJson(datasetVersion: "current");
        var jsRuntime = new TestJsRuntime(currentJson) { SaveFails = true };
        var store = CreateStore(jsRuntime);

        await Assert.ThrowsAsync<JSException>(() => store.RestoreInitialDatasetAsync());

        Assert.Equal(currentJson, jsRuntime.StoredJson);
        Assert.Equal("current", (await store.LoadAsync()).Dataset.DatasetVersion);
    }

    [Fact]
    public async Task RestoreHonorsCancellation()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();
        var jsRuntime = new TestJsRuntime();
        var store = CreateStore(jsRuntime);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => store.RestoreInitialDatasetAsync(source.Token));
        Assert.Equal(0, jsRuntime.SaveAttempts);
    }

    private static BrowserDemoDatasetStore CreateStore(
        TestJsRuntime jsRuntime,
        SeedMessageHandler? handler = null)
    {
        var httpClient = new HttpClient(handler ?? new SeedMessageHandler(SeedJson))
        {
            BaseAddress = new Uri("https://viteklub.invalid/")
        };
        return new BrowserDemoDatasetStore(httpClient, jsRuntime);
    }

    private static string CreateDatasetJson(string? datasetVersion = null, int? schemaVersion = null)
    {
        DemoDataset dataset = DemoDatasetJson.Deserialize(SeedJson) with
        {
            DatasetVersion = datasetVersion ?? "initial-v1",
            SchemaVersion = schemaVersion ?? DemoDataset.CurrentSchemaVersion
        };
        return DemoDatasetJson.Serialize(dataset);
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

    private sealed class SeedMessageHandler(string json) : HttpMessageHandler
    {
        public string Json { get; set; } = json;

        public Exception? ReadException { get; init; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Assert.Equal("data/initial-dataset.json", request.RequestUri?.PathAndQuery.TrimStart('/'));
            if (ReadException is not null)
            {
                return Task.FromException<HttpResponseMessage>(ReadException);
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(Json)
            });
        }
    }

    private sealed class TestJsRuntime(string? storedJson = null) : IJSRuntime
    {
        public bool IsUnavailable { get; init; }

        public bool SaveFails { get; init; }

        public int LoadAttempts { get; private set; }

        public int SaveAttempts { get; private set; }

        public string? StoredJson { get; private set; } = storedJson;

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            InvokeAsync<TValue>(identifier, CancellationToken.None, args);

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args)
        {
            cancellationToken.ThrowIfCancellationRequested();
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
                return ValueTask.FromResult((TValue)(object?)StoredJson!);
            }

            if (identifier == "demoStorage.save")
            {
                SaveAttempts++;
                if (SaveFails)
                {
                    throw new JSException("Salvataggio fallito.");
                }

                StoredJson = Assert.IsType<string>(args?[0]);
                return ValueTask.FromResult(default(TValue)!);
            }

            throw new InvalidOperationException($"Invocazione JavaScript inattesa: {identifier}.");
        }
    }
}
