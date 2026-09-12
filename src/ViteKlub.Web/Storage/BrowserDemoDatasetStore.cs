using Microsoft.JSInterop;
using ViteKlub.Core.Data;

namespace ViteKlub.Web.Storage;

public sealed class BrowserDemoDatasetStore(HttpClient httpClient, IJSRuntime jsRuntime) : IDemoDatasetStore
{
    private const string SeedPath = "data/initial-dataset.json";
    private DemoDataset? inMemoryDataset;
    private DemoDataset? currentDataset;
    private DemoStorageMode? activeMode;
    private long revision;

    public async Task<DemoDatasetSnapshot> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (inMemoryDataset is not null)
        {
            return Track(inMemoryDataset, DemoStorageMode.InMemory);
        }

        try
        {
            string? storedJson = await jsRuntime.InvokeAsync<string?>(
                "demoStorage.load",
                cancellationToken);
            if (!string.IsNullOrWhiteSpace(storedJson))
            {
                DemoDataset storedDataset = DeserializeAndValidate(storedJson);
                activeMode = DemoStorageMode.IndexedDb;
                return Track(storedDataset, DemoStorageMode.IndexedDb);
            }
        }
        catch (JSException)
        {
            return await LoadSeedInMemoryAsync(cancellationToken);
        }

        string seedJson = await httpClient.GetStringAsync(SeedPath, cancellationToken);
        DemoDataset seedDataset = DeserializeAndValidate(seedJson);

        try
        {
            await jsRuntime.InvokeVoidAsync("demoStorage.save", cancellationToken, seedJson);
            activeMode = DemoStorageMode.IndexedDb;
            return Track(seedDataset, DemoStorageMode.IndexedDb);
        }
        catch (JSException)
        {
            inMemoryDataset = seedDataset;
            activeMode = DemoStorageMode.InMemory;
            return Track(seedDataset, DemoStorageMode.InMemory);
        }
    }

    public async Task<DemoDatasetSnapshot> SaveAsync(
        DemoDataset dataset,
        long expectedRevision,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dataset);
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<DatasetValidationError> errors = DemoDatasetValidator.Validate(dataset);
        if (errors.Count > 0)
        {
            throw new InvalidDataException($"Il dataset demo non è valido: {string.Join("; ", errors.Select(error => error.Message))}");
        }

        // Il round-trip crea lo snapshot persistito senza trattenere o mutare l'istanza del chiamante.
        string json = DemoDatasetJson.Serialize(dataset);
        DemoDataset copy = DemoDatasetJson.Deserialize(json);
        if (activeMode is null)
        {
            await LoadAsync(cancellationToken);
        }
        if (currentDataset is null || expectedRevision != revision)
        {
            throw new DemoDatasetConflictException();
        }
        if (activeMode == DemoStorageMode.InMemory)
        {
            inMemoryDataset = copy;
            return Track(copy, DemoStorageMode.InMemory);
        }

        await jsRuntime.InvokeVoidAsync("demoStorage.save", cancellationToken, json);
        return Track(copy, DemoStorageMode.IndexedDb);
    }

    public async Task<DemoDatasetSnapshot> RestoreInitialDatasetAsync(
        CancellationToken cancellationToken = default)
    {
        string seedJson = await httpClient.GetStringAsync(SeedPath, cancellationToken);
        DemoDataset seedDataset = DeserializeAndValidate(seedJson);

        if (inMemoryDataset is not null)
        {
            inMemoryDataset = seedDataset;
            return Track(seedDataset, DemoStorageMode.InMemory);
        }

        await jsRuntime.InvokeVoidAsync("demoStorage.save", cancellationToken, seedJson);
        activeMode = DemoStorageMode.IndexedDb;
        return Track(seedDataset, DemoStorageMode.IndexedDb);
    }

    private async Task<DemoDatasetSnapshot> LoadSeedInMemoryAsync(CancellationToken cancellationToken)
    {
        string seedJson = await httpClient.GetStringAsync(SeedPath, cancellationToken);
        inMemoryDataset = DeserializeAndValidate(seedJson);
        activeMode = DemoStorageMode.InMemory;
        return Track(inMemoryDataset, DemoStorageMode.InMemory);
    }

    private DemoDatasetSnapshot Track(DemoDataset dataset, DemoStorageMode mode)
    {
        currentDataset = dataset;
        activeMode = mode;
        revision = checked(revision + 1);
        return new(dataset, mode, revision);
    }

    private static DemoDataset DeserializeAndValidate(string json)
    {
        DemoDataset dataset = DemoDatasetJson.Deserialize(json);
        IReadOnlyList<DatasetValidationError> errors = DemoDatasetValidator.Validate(dataset);
        if (errors.Count > 0)
        {
            throw new InvalidDataException(
                $"Il dataset demo non è valido: {string.Join("; ", errors.Select(error => error.Message))}");
        }

        return dataset;
    }
}
