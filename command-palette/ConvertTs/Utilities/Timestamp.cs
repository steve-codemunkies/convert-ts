using System;
using System.Globalization;

namespace ConvertTs.Utilities;

public readonly record struct Timestamp(DateTimeOffset Utc, long Seconds, long Milliseconds)
{
    public string UtcText => UnixTimestampConverter.FormatUtc(Utc);

    public string SecondsText => Seconds.ToString(CultureInfo.InvariantCulture);

    public string MillisecondsText => Milliseconds.ToString(CultureInfo.InvariantCulture);
}
