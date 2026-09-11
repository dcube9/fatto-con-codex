using Microsoft.JSInterop;
using ViteKlub.Core.Data;

namespace ViteKlub.Web.Storage;

public sealed class BrowserDemoDatasetStore(HttpClient httpClient, IJSRuntime jsRuntime) : IDemoDatasetStore
{
    private const string SeedPath = "data/initial-dataset.json";
    private DemoDataset? inMemoryDataset;
    private DemoStorageMode? activeMode;

    public async Task<DemoDatasetSnapshot> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (inMemoryDataset is not null)
        {
            return new(inMemoryDataset, DemoStorageMode.InMemory);
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
                return new(storedDataset, DemoStorageMode.IndexedDb);
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
            return new(seedDataset, DemoStorageMode.IndexedDb);
        }
        catch (JSException)
        {
            inMemoryDataset = seedDataset;
            activeMode = DemoStorageMode.InMemory;
            return new(seedDataset, DemoStorageMode.InMemory);
        }
    }

    public async Task<DemoDatasetSnapshot> SaveAsync(DemoDataset dataset, CancellationToken cancellationToken = default)
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
        if (activeMode == DemoStorageMode.InMemory)
        {
            inMemoryDataset = copy;
            return new(copy, DemoStorageMode.InMemory);
        }

        await jsRuntime.InvokeVoidAsync("demoStorage.save", cancellationToken, json);
        return new(copy, DemoStorageMode.IndexedDb);
    }

    public async Task<DemoDatasetSnapshot> RestoreInitialDatasetAsync(
        CancellationToken cancellationToken = default)
    {
        string seedJson = await httpClient.GetStringAsync(SeedPath, cancellationToken);
        DemoDataset seedDataset = DeserializeAndValidate(seedJson);

        if (inMemoryDataset is not null)
        {
            inMemoryDataset = seedDataset;
            return new(seedDataset, DemoStorageMode.InMemory);
        }

        await jsRuntime.InvokeVoidAsync("demoStorage.save", cancellationToken, seedJson);
        activeMode = DemoStorageMode.IndexedDb;
        return new(seedDataset, DemoStorageMode.IndexedDb);
    }

    private async Task<DemoDatasetSnapshot> LoadSeedInMemoryAsync(CancellationToken cancellationToken)
    {
        string seedJson = await httpClient.GetStringAsync(SeedPath, cancellationToken);
        inMemoryDataset = DeserializeAndValidate(seedJson);
        activeMode = DemoStorageMode.InMemory;
        return new(inMemoryDataset, DemoStorageMode.InMemory);
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
