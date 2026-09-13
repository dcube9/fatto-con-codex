using System.Globalization;

namespace ViteKlub.Web.Time;

public sealed class OperationalTimeOptions
{
    public const string SectionName = "OperationalTime";
    public const string DefaultTimeZoneId = "Europe/Rome";

    public string TimeZoneId { get; set; } = DefaultTimeZoneId;
}

public interface IOperationalTimeZone
{
    TimeZoneInfo Value { get; }
}

public sealed class OperationalTimeZone : IOperationalTimeZone
{
    public OperationalTimeZone(OperationalTimeOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (string.IsNullOrWhiteSpace(options.TimeZoneId))
        {
            throw new InvalidOperationException("OperationalTime:TimeZoneId deve contenere un identificatore di fuso IANA.");
        }

        try
        {
            Value = TimeZoneInfo.FindSystemTimeZoneById(options.TimeZoneId);
        }
        catch (TimeZoneNotFoundException exception)
        {
            throw new InvalidOperationException($"Il fuso operativo configurato '{options.TimeZoneId}' non è disponibile nel runtime.", exception);
        }
        catch (InvalidTimeZoneException exception)
        {
            throw new InvalidOperationException($"Il fuso operativo configurato '{options.TimeZoneId}' contiene dati non validi.", exception);
        }
    }

    public TimeZoneInfo Value { get; }
}

public interface IInstantFormatter
{
    string Format(DateTimeOffset instant);
}

public sealed class InstantFormatter : IInstantFormatter
{
    private static readonly CultureInfo ItalianCulture = CultureInfo.GetCultureInfo("it-IT");
    private readonly TimeZoneInfo _timeZone;

    public InstantFormatter(IOperationalTimeZone timeZone) : this(timeZone.Value) { }

    public InstantFormatter(TimeZoneInfo timeZone) =>
        _timeZone = timeZone ?? throw new ArgumentNullException(nameof(timeZone));

    public string Format(DateTimeOffset instant)
    {
        DateTimeOffset local = TimeZoneInfo.ConvertTime(instant.ToUniversalTime(), _timeZone);
        string sign = local.Offset < TimeSpan.Zero ? "-" : "+";
        TimeSpan absoluteOffset = local.Offset.Duration();
        return string.Format(
            ItalianCulture,
            "{0:dd/MM/yyyy HH:mm} (UTC{1}{2:00}:{3:00})",
            local,
            sign,
            (int)absoluteOffset.TotalHours,
            absoluteOffset.Minutes);
    }
}
