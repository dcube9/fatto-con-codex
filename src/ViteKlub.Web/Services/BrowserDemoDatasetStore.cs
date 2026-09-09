using Microsoft.JSInterop;
using ViteKlub.Core.Data;

namespace ViteKlub.Web.Services;

public sealed class BrowserDemoDatasetStore(
    HttpClient httpClient,
    IJSRuntime jsRuntime) : IDemoDatasetStore, IAsyncDisposable
{
    private const string ModulePath = "./js/demoStorage.js";
    private const string SeedPath = "data/initial-dataset.json";
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IJSObjectReference? _module;
    private DemoDatasetSnapshot? _snapshot;
    private DemoStorageMode? _storageMode;

    public async Task<DemoDatasetSnapshot> GetAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_snapshot is not null)
            {
                return _snapshot;
            }

            await EnsureStorageModeAsync(cancellationToken);
            string? storedJson = await ReadStoredJsonAsync(cancellationToken);
            if (storedJson is not null)
            {
                DemoDataset storedDataset = DeserializeAndValidate(storedJson);
                return _snapshot = new(storedDataset, _storageMode!.Value, false);
            }

            DemoDataset seed = await LoadSeedAsync(cancellationToken);
            await PersistAsync(seed, cancellationToken);
            return _snapshot = new(seed, _storageMode!.Value, true);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<DemoDatasetSnapshot> SaveAsync(
        DemoDataset dataset,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dataset);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            ThrowIfInvalid(dataset);
            await EnsureStorageModeAsync(cancellationToken);
            await PersistAsync(dataset, cancellationToken);
            return _snapshot = new(dataset, _storageMode!.Value, false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<DemoDatasetSnapshot> ResetAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            DemoDataset seed = await LoadSeedAsync(cancellationToken);
            await EnsureStorageModeAsync(cancellationToken);
            await PersistAsync(seed, cancellationToken);
            return _snapshot = new(seed, _storageMode!.Value, true);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            try
            {
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // The browser context has already gone away.
            }
        }

        _gate.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task EnsureStorageModeAsync(CancellationToken cancellationToken)
    {
        if (_storageMode is not null)
        {
            return;
        }

        try
        {
            IJSObjectReference module = await GetModuleAsync(cancellationToken);
            bool isSupported = await module.InvokeAsync<bool>("isSupported", cancellationToken);
            _storageMode = isSupported ? DemoStorageMode.IndexedDb : DemoStorageMode.Memory;
        }
        catch (JSException)
        {
            SwitchToMemory();
        }
    }

    private async Task<string?> ReadStoredJsonAsync(CancellationToken cancellationToken)
    {
        if (_storageMode != DemoStorageMode.IndexedDb)
        {
            return null;
        }

        try
        {
            IJSObjectReference module = await GetModuleAsync(cancellationToken);
            return await module.InvokeAsync<string?>("readDataset", cancellationToken);
        }
        catch (JSException)
        {
            SwitchToMemory();
            return null;
        }
    }

    private async Task PersistAsync(DemoDataset dataset, CancellationToken cancellationToken)
    {
        if (_storageMode != DemoStorageMode.IndexedDb)
        {
            return;
        }

        try
        {
            string json = DemoDatasetJson.Serialize(dataset);
            IJSObjectReference module = await GetModuleAsync(cancellationToken);
            await module.InvokeVoidAsync("writeDataset", cancellationToken, json);
        }
        catch (JSException)
        {
            SwitchToMemory();
        }
    }

    private async Task<DemoDataset> LoadSeedAsync(CancellationToken cancellationToken)
    {
        string json = await httpClient.GetStringAsync(SeedPath, cancellationToken);
        return DeserializeAndValidate(json);
    }

    private async Task<IJSObjectReference> GetModuleAsync(CancellationToken cancellationToken)
    {
        _module ??= await jsRuntime.InvokeAsync<IJSObjectReference>("import", cancellationToken, ModulePath);
        return _module;
    }

    private static DemoDataset DeserializeAndValidate(string json)
    {
        DemoDataset dataset = DemoDatasetJson.Deserialize(json);
        ThrowIfInvalid(dataset);
        return dataset;
    }

    private static void ThrowIfInvalid(DemoDataset dataset)
    {
        IReadOnlyList<DatasetValidationError> errors = DemoDatasetValidator.Validate(dataset);
        if (errors.Count > 0)
        {
            string details = string.Join("; ", errors.Take(5).Select(error => $"{error.Path}: {error.Message}"));
            throw new InvalidDataException($"Il dataset demo non è valido. {details}");
        }
    }

    private void SwitchToMemory()
    {
        _storageMode = DemoStorageMode.Memory;
    }
}
