using System;
using System.Globalization;

namespace ConvertTs.Utilities;

public static class UnixTimestampConverter
{
    public const long DefaultBoundaryValue = 32503680000L;

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
        utc = default;
        seconds = 0;
        milliseconds = 0;

        if (!TryParseDateTimeOffset(input, out utc))
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

    private static bool TryParseDateTimeOffset(string input, out DateTimeOffset utc)
    {
        if (DateTimeOffset.TryParse(
                input,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsedOffset))
        {
            utc = parsedOffset.ToUniversalTime();
            return true;
        }

        if (DateTime.TryParse(
                input,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var parsedDateTime))
        {
            utc = new DateTimeOffset(DateTime.SpecifyKind(parsedDateTime, DateTimeKind.Utc));
            return true;
        }

        utc = default;
        return false;
    }
}
