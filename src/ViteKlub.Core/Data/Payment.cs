namespace ViteKlub.Core.Data;

public sealed record Payment : DemoEntity
{
    public required Guid MemberId { get; init; }

    public Guid? SubscriptionId { get; init; }

    public required decimal Amount { get; init; }

    public string Currency { get; init; } = "EUR";

    public required DateTimeOffset OccurredAtUtc { get; init; }

    public required PaymentMethod Method { get; init; }

    public required PaymentStatus Status { get; init; }

    public required Guid RecordedByUserId { get; init; }

    public string? Reference { get; init; }

    public string? Notes { get; init; }
}
