namespace ViteKlub.Core.Data;

public sealed record DemoUser : DemoEntity
{
    public required string Username { get; init; }

    public required string DisplayName { get; init; }

    public required DemoRole Role { get; init; }

    public bool IsActive { get; init; } = true;
}
