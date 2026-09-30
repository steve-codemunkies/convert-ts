using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ConvertTs.Utilities;

public static partial class UnixTimestampConverter
{
    public const long DefaultBoundaryValue = 32503680000L;

    public const string UtcDisplayFormat = "yyyy-MM-ddTHH:mm:ss.fff'Z'";

    public static bool TryConvertFromTimestamp(string input, long boundary, out DateTimeOffset utc, out long seconds, out long milliseconds)
    {
        utc = default;
        seconds = 0;
        milliseconds = 0;

        if (!long.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out var timestamp))
        {
            return false;
        }

        if (timestamp > boundary)
        {
            milliseconds = timestamp;
            seconds = milliseconds / 1000L;
        }
        else
        {
            seconds = timestamp;
            milliseconds = seconds * 1000L;
        }

        try
        {
            utc = DateTimeOffset.FromUnixTimeMilliseconds(milliseconds);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            utc = default;
            seconds = 0;
            milliseconds = 0;
            return false;
        }
    }

    public static bool TryConvertToTimestamp(string input, out DateTimeOffset utc, out long seconds, out long milliseconds)
    {
        return TryConvertToTimestamp(input, TimeZoneInfo.Utc, out utc, out seconds, out milliseconds);
    }

    public static bool TryConvertToTimestamp(string input, TimeZoneInfo timeZone, out DateTimeOffset utc, out long seconds, out long milliseconds)
    {
        utc = default;
        seconds = 0;
        milliseconds = 0;

        if (!TryParseDateTimeOffset(input, timeZone, out utc))
        {
            return false;
        }

        try
        {
            milliseconds = utc.ToUnixTimeMilliseconds();
            seconds = milliseconds / 1000L;
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            utc = default;
            seconds = 0;
            milliseconds = 0;
            return false;
        }
    }

    public static void GetNowTimestamps(out DateTimeOffset utc, out long seconds, out long milliseconds)
    {
        utc = DateTimeOffset.UtcNow;
        milliseconds = utc.ToUnixTimeMilliseconds();
        seconds = milliseconds / 1000L;
    }

    /// <summary>
    /// Formats a UTC instant using the canonical display format, regardless of the
    /// timezone settings configured for input/output.
    /// </summary>
    public static string FormatUtc(DateTimeOffset utc)
    {
        return utc.ToUniversalTime().ToString(UtcDisplayFormat, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Converts a UTC instant into the supplied timezone and formats it, including the
    /// zone's offset from UTC.
    /// </summary>
    public static string FormatInTimeZone(DateTimeOffset utc, TimeZoneInfo timeZone)
    {
        var zoned = TimeZoneInfo.ConvertTime(utc, timeZone);
        return zoned.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Parses a date/time string to a UTC instant. If the input contains an explicit
    /// offset or zone designator (e.g. "Z" or "+02:00") that offset is honored as-is;
    /// otherwise the wall-clock value is interpreted as being in <paramref name="timeZone"/>.
    /// </summary>
    private static bool TryParseDateTimeOffset(string input, TimeZoneInfo timeZone, out DateTimeOffset utc)
    {
        if (HasExplicitOffset(input))
        {
            if (DateTimeOffset.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedOffset))
            {
                utc = parsedOffset.ToUniversalTime();
                return true;
            }

            utc = default;
            return false;
        }

        // No explicit offset was present; interpret the literal wall-clock value using the
        // configured timezone rather than assuming UTC or the ambient system timezone.
        if (!DateTime.TryParse(
                input,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces,
                out var wallClock))
        {
            utc = default;
            return false;
        }

        var unspecified = DateTime.SpecifyKind(wallClock, DateTimeKind.Unspecified);

        try
        {
            utc = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(unspecified, timeZone), TimeSpan.Zero);
            return true;
        }
        catch (ArgumentException)
        {
            utc = default;
            return false;
        }
    }

    private static bool HasExplicitOffset(string input)
    {
        return ExplicitOffsetPattern().IsMatch(input.Trim());
    }

    [GeneratedRegex(@"(Z|[+-]\d{2}:?\d{2})$", RegexOptions.IgnoreCase)]
    private static partial Regex ExplicitOffsetPattern();
}
