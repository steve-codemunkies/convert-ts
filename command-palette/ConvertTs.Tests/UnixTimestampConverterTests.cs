using System;
using ConvertTs.CommandPalette.Utilities;
using Xunit;

namespace ConvertTs.CommandPalette.Tests;

public class UnixTimestampConverterTests
{
    [Fact]
    public void ConvertsSecondsTimestampUsingDefaultBoundary()
    {
        var success = UnixTimestampConverter.TryConvertFromTimestamp(
            "1700000000",
            UnixTimestampConverter.DefaultBoundaryValue,
            out var utc,
            out var seconds,
            out var milliseconds);

        Assert.True(success);
        Assert.Equal(new DateTimeOffset(2023, 11, 14, 22, 13, 20, TimeSpan.Zero), utc);
        Assert.Equal(1700000000L, seconds);
        Assert.Equal(1700000000000L, milliseconds);
    }

    [Fact]
    public void ConvertsMillisecondsTimestampUsingDefaultBoundary()
    {
        var success = UnixTimestampConverter.TryConvertFromTimestamp(
            "1700000000000",
            UnixTimestampConverter.DefaultBoundaryValue,
            out var utc,
            out var seconds,
            out var milliseconds);

        Assert.True(success);
        Assert.Equal(new DateTimeOffset(2023, 11, 14, 22, 13, 20, TimeSpan.Zero), utc);
        Assert.Equal(1700000000L, seconds);
        Assert.Equal(1700000000000L, milliseconds);
    }

    [Fact]
    public void ConvertsTimestampUsingCustomBoundary()
    {
        var success = UnixTimestampConverter.TryConvertFromTimestamp(
            "10000000000",
            9999999999L,
            out var utc,
            out var seconds,
            out var milliseconds);

        Assert.True(success);
        Assert.Equal(new DateTimeOffset(1970, 4, 26, 17, 46, 40, TimeSpan.Zero), utc);
        Assert.Equal(10000000L, seconds);
        Assert.Equal(10000000000L, milliseconds);
    }

    [Fact]
    public void ParsesIso8601UtcDateStringToEpoch()
    {
        var success = UnixTimestampConverter.TryConvertToTimestamp(
            "2023-11-14T22:13:20Z",
            out var utc,
            out var seconds,
            out var milliseconds);

        Assert.True(success);
        Assert.Equal(new DateTimeOffset(2023, 11, 14, 22, 13, 20, TimeSpan.Zero), utc);
        Assert.Equal(1700000000L, seconds);
        Assert.Equal(1700000000000L, milliseconds);
    }

    [Fact]
    public void ReturnsCurrentUtcTimestampValues()
    {
        UnixTimestampConverter.GetNowTimestamps(out var utc, out var seconds, out var milliseconds);

        Assert.Equal(utc.ToUnixTimeMilliseconds(), milliseconds);
        Assert.Equal(milliseconds / 1000L, seconds);
        Assert.True(Math.Abs((DateTimeOffset.UtcNow - utc).TotalSeconds) < 5);
    }
}
