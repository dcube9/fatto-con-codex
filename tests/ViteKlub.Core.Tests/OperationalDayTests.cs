using ViteKlub.Core.Dashboard;
using Xunit;

namespace ViteKlub.Core.Tests;

public sealed class OperationalDayTests
{
    private static readonly TimeZoneInfo Rome = TimeZoneInfo.FindSystemTimeZoneById("Europe/Rome");

    [Fact]
    public void RomeDayIncludesUtcPreviousDateAndExcludesUtcReferenceDateAfterLocalMidnight()
    {
        OperationalDay day = OperationalDay.For(new(2026, 9, 1), Rome);

        Assert.True(day.Contains(new DateTimeOffset(2026, 8, 31, 22, 0, 0, TimeSpan.Zero)));
        Assert.True(day.Contains(day.StartUtc));
        Assert.False(day.Contains(day.EndUtc));
        Assert.False(day.Contains(new DateTimeOffset(2026, 9, 1, 22, 0, 0, TimeSpan.Zero)));
    }

    [Theory]
    [InlineData(2026, 3, 29, 23)]
    [InlineData(2026, 10, 25, 25)]
    public void DurationReflectsDstTransitions(int year, int month, int day, int expectedHours) =>
        Assert.Equal(TimeSpan.FromHours(expectedHours), OperationalDay.For(new(year, month, day), Rome).Duration);
}
