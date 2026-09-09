namespace ViteKlub.Core.Data;

public sealed record DemoDataset
{
    public const int CurrentSchemaVersion = 1;

    public required int SchemaVersion { get; init; }

    public required string DatasetVersion { get; init; }

    public required DateOnly ReferenceDate { get; init; }

    public required IReadOnlyList<DemoUser> Users { get; init; }

    public required IReadOnlyList<Member> Members { get; init; }

    public required IReadOnlyList<MembershipPlan> MembershipPlans { get; init; }

    public required IReadOnlyList<MemberSubscription> Subscriptions { get; init; }

    public required IReadOnlyList<GymAccess> Accesses { get; init; }

    public required IReadOnlyList<Payment> Payments { get; init; }

    public required IReadOnlyList<AuditEvent> AuditEvents { get; init; }
}
