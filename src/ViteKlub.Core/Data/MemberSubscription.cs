namespace ViteKlub.Core.Data;

public sealed record MemberSubscription : DemoEntity
{
    public required Guid MemberId { get; init; }

    public required Guid MembershipPlanId { get; init; }

    public required DateOnly StartsOn { get; init; }

    public required DateOnly EndsOn { get; init; }

    public required SubscriptionStatus Status { get; init; }

    public int? RemainingEntries { get; init; }

    public required decimal PurchasePrice { get; init; }

    public string Currency { get; init; } = "EUR";
}
