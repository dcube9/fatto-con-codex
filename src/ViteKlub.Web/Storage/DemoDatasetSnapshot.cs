using ViteKlub.Core.Data;

namespace ViteKlub.Web.Storage;

public sealed record DemoDatasetSnapshot(DemoDataset Dataset, DemoStorageMode StorageMode)
{
    public IReadOnlyList<DemoDatasetCollectionCount> CollectionCounts { get; } =
    [
        new("Utenti demo", Dataset.Users.Count),
        new("Iscritti", Dataset.Members.Count),
        new("Piani di abbonamento", Dataset.MembershipPlans.Count),
        new("Abbonamenti", Dataset.Subscriptions.Count),
        new("Accessi", Dataset.Accesses.Count),
        new("Pagamenti", Dataset.Payments.Count),
        new("Eventi di audit", Dataset.AuditEvents.Count),
    ];
}

public sealed record DemoDatasetCollectionCount(string Label, int Count);
