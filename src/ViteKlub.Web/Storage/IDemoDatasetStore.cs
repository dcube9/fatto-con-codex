namespace ViteKlub.Web.Storage;

public interface IDemoDatasetStore
{
    Task<DemoDatasetSnapshot> LoadAsync(CancellationToken cancellationToken = default);

    Task<DemoDatasetSnapshot> RestoreInitialDatasetAsync(CancellationToken cancellationToken = default);
}
