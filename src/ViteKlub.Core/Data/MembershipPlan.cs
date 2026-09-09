namespace ViteKlub.Core.Data;

public sealed record MembershipPlan : DemoEntity
{
    public required string Name { get; init; }

    public required string Description { get; init; }

    public required MembershipPlanType Type { get; init; }

    public required int DurationDays { get; init; }

    public int? IncludedEntries { get; init; }

    public required decimal Price { get; init; }

    public string Currency { get; init; } = "EUR";

    public bool IsActive { get; init; } = true;
}
