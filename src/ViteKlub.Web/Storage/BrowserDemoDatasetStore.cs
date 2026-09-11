using Microsoft.JSInterop;
using ViteKlub.Core.Data;

namespace ViteKlub.Web.Storage;

public sealed class BrowserDemoDatasetStore(HttpClient httpClient, IJSRuntime jsRuntime) : IDemoDatasetStore
{
    private const string SeedPath = "data/initial-dataset.json";
    private DemoDataset? inMemoryDataset;

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
            return new(seedDataset, DemoStorageMode.IndexedDb);
        }
        catch (JSException)
        {
            inMemoryDataset = seedDataset;
            return new(seedDataset, DemoStorageMode.InMemory);
        }
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
        return new(seedDataset, DemoStorageMode.IndexedDb);
    }

    private async Task<DemoDatasetSnapshot> LoadSeedInMemoryAsync(CancellationToken cancellationToken)
    {
        string seedJson = await httpClient.GetStringAsync(SeedPath, cancellationToken);
        inMemoryDataset = DeserializeAndValidate(seedJson);
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
