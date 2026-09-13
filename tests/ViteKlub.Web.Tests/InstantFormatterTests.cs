using ViteKlub.Web.Time;
using Xunit;

namespace ViteKlub.Web.Tests;

public sealed class InstantFormatterTests
{
    private static readonly TimeZoneInfo Rome = TimeZoneInfo.FindSystemTimeZoneById("Europe/Rome");
    private static readonly InstantFormatter RomeFormatter = new(Rome);

    [Theory]
    [InlineData("2026-01-15T12:30:00Z", "15/01/2026 13:30 (UTC+01:00)")]
    [InlineData("2026-09-01T12:30:00Z", "01/09/2026 14:30 (UTC+02:00)")]
    [InlineData("2026-09-01T22:30:00Z", "02/09/2026 00:30 (UTC+02:00)")]
    [InlineData("2026-03-29T00:59:00Z", "29/03/2026 01:59 (UTC+01:00)")]
    [InlineData("2026-03-29T01:00:00Z", "29/03/2026 03:00 (UTC+02:00)")]
    [InlineData("2026-10-25T00:30:00Z", "25/10/2026 02:30 (UTC+02:00)")]
    [InlineData("2026-10-25T01:30:00Z", "25/10/2026 02:30 (UTC+01:00)")]
    public void FormatsRomeWithTheOffsetApplicableToTheInstant(string instant, string expected) =>
        Assert.Equal(expected, RomeFormatter.Format(DateTimeOffset.Parse(instant, System.Globalization.CultureInfo.InvariantCulture)));

    [Fact]
    public void NormalizesANonZeroInputOffsetToTheSameInstant() =>
        Assert.Equal(RomeFormatter.Format(new(2026, 9, 1, 14, 30, 0, TimeSpan.FromHours(2))),
            RomeFormatter.Format(new(2026, 9, 1, 12, 30, 0, TimeSpan.Zero)));

    [Fact]
    public void SupportsPreviousDayAndNegativeNumericOffset()
    {
        var formatter = new InstantFormatter(TimeZoneInfo.CreateCustomTimeZone("Test/MinusFive", TimeSpan.FromHours(-5), "Test", "Test"));
        Assert.Equal("31/08/2026 20:30 (UTC-05:00)", formatter.Format(new(2026, 9, 1, 1, 30, 0, TimeSpan.Zero)));
    }

    [Fact]
    public void SupportsFractionalOffsetsWithoutUsingTheMachineTimeZone()
    {
        var formatter = new InstantFormatter(TimeZoneInfo.CreateCustomTimeZone("Test/PlusFiveThirty", TimeSpan.FromMinutes(330), "Test", "Test"));
        Assert.Equal("01/09/2026 18:00 (UTC+05:30)", formatter.Format(new(2026, 9, 1, 12, 30, 0, TimeSpan.Zero)));
        Assert.NotEqual("Test/PlusFiveThirty", TimeZoneInfo.Local.Id);
    }

    [Fact]
    public void InvalidConfiguredZoneFailsExplicitly() =>
        Assert.Throws<InvalidOperationException>(() => new OperationalTimeZone(new() { TimeZoneId = "Invalid/Not-A-Zone" }));
}
