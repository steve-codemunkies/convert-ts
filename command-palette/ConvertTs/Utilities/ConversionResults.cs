using System;

namespace ConvertTs.Utilities;

public readonly record struct ConversionValue(string Label, string Text);

public static class ConversionResults
{
    public const string UserTimeZoneLabel = "User time zone date/time";
    public const string UtcLabel = "UTC Date/Time";
    public const string MillisecondsLabel = "Unix Epoch Milliseconds";
    public const string SecondsLabel = "Unix Epoch Seconds";

    /// <summary>
    /// Builds one value per output, ordered: user time zone (only when a non-UTC zone is
    /// selected), UTC, Unix epoch milliseconds, Unix epoch seconds.
    /// </summary>
    public static ConversionValue[] Build(Timestamp ts, TimeZoneInfo timeZone)
    {
        var values = new System.Collections.Generic.List<ConversionValue>();

        if (!IsUtc(timeZone))
        {
            values.Add(new ConversionValue(UserTimeZoneLabel, UnixTimestampConverter.FormatInTimeZone(ts.Utc, timeZone)));
        }

        values.Add(new ConversionValue(UtcLabel, ts.UtcText));
        values.Add(new ConversionValue(MillisecondsLabel, ts.MillisecondsText));
        values.Add(new ConversionValue(SecondsLabel, ts.SecondsText));

        return values.ToArray();
    }

    /// <summary>
    /// Determines whether <paramref name="timeZone"/> is semantically equivalent to UTC, either by
    /// matching the well-known UTC id or by having a zero offset with no daylight-saving adjustments.
    /// </summary>
    public static bool IsUtc(TimeZoneInfo timeZone)
    {
        return string.Equals(timeZone.Id, TimeZoneInfo.Utc.Id, StringComparison.Ordinal)
            || (timeZone.BaseUtcOffset == TimeSpan.Zero && !timeZone.SupportsDaylightSavingTime);
    }
}
