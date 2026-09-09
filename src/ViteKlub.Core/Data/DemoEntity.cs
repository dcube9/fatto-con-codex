namespace ViteKlub.Core.Data;

public abstract record DemoEntity
{
    public required Guid Id { get; init; }

    public required DateTimeOffset CreatedAtUtc { get; init; }

    public required DateTimeOffset UpdatedAtUtc { get; init; }

    public int Version { get; init; } = 1;
}
