using ViteKlub.Core.Data;

namespace ViteKlub.Web.Services;

public sealed record DemoDatasetSnapshot(
    DemoDataset Dataset,
    DemoStorageMode StorageMode,
    bool InitializedFromSeed);
