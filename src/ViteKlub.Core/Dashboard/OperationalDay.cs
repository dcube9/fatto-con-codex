namespace ViteKlub.Core.Dashboard;

public readonly record struct OperationalDay(DateTimeOffset StartUtc, DateTimeOffset EndUtc)
{
    public TimeSpan Duration => EndUtc - StartUtc;

    public bool Contains(DateTimeOffset instant) =>
        instant.ToUniversalTime() >= StartUtc && instant.ToUniversalTime() < EndUtc;

    public static OperationalDay For(DateOnly date, TimeZoneInfo timeZone)
    {
        ArgumentNullException.ThrowIfNull(timeZone);
        DateTime startLocal = DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        DateTime endLocal = DateTime.SpecifyKind(date.AddDays(1).ToDateTime(TimeOnly.MinValue), DateTimeKind.Unspecified);
        return new(
            new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(startLocal, timeZone), TimeSpan.Zero),
            new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(endLocal, timeZone), TimeSpan.Zero));
    }
}
