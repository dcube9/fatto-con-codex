using ViteKlub.Core.Data;

namespace ViteKlub.Web.Services;

public interface IDemoDatasetStore
{
    Task<DemoDatasetSnapshot> GetAsync(CancellationToken cancellationToken = default);

    Task<DemoDatasetSnapshot> SaveAsync(
        DemoDataset dataset,
        CancellationToken cancellationToken = default);

    Task<DemoDatasetSnapshot> ResetAsync(CancellationToken cancellationToken = default);
}
