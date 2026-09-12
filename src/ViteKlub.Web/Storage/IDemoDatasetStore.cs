using ViteKlub.Core.Data;

namespace ViteKlub.Web.Storage;

public interface IDemoDatasetStore
{
    Task<DemoDatasetSnapshot> LoadAsync(CancellationToken cancellationToken = default);

    Task<DemoDatasetSnapshot> SaveAsync(
        DemoDataset dataset,
        long expectedRevision,
        CancellationToken cancellationToken = default);

    Task<DemoDatasetSnapshot> RestoreInitialDatasetAsync(CancellationToken cancellationToken = default);
}

public sealed class DemoDatasetConflictException()
    : InvalidOperationException("I dati locali sono cambiati. Ricarica i dati e riprova.");
