namespace ViteKlub.Core.Data;

public sealed record AuditEvent : DemoEntity
{
    public required DateTimeOffset OccurredAtUtc { get; init; }

    public required Guid ActorUserId { get; init; }

    public required string Action { get; init; }

    public required string EntityType { get; init; }

    public required Guid EntityId { get; init; }

    public required string Summary { get; init; }
}
