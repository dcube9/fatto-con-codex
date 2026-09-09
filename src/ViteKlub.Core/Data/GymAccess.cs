namespace ViteKlub.Core.Data;

public sealed record GymAccess : DemoEntity
{
    public required Guid MemberId { get; init; }

    public Guid? SubscriptionId { get; init; }

    public required DateTimeOffset OccurredAtUtc { get; init; }

    public required AccessOutcome Outcome { get; init; }

    public AccessDenialReason DenialReason { get; init; }

    public required AccessSource Source { get; init; }

    public required Guid RecordedByUserId { get; init; }

    public string? CancellationReason { get; init; }
}
