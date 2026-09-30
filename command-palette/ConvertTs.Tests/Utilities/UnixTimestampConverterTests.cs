using System;
using Xunit;
using ConvertTs.Utilities;

namespace ConvertTs.Tests.Utilities;

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

    [Fact]
    public void ParsesDateStringWithExplicitOffsetRegardlessOfConfiguredTimeZone()
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");

        var success = UnixTimestampConverter.TryConvertToTimestamp(
            "2023-11-14T22:13:20+02:00",
            timeZone,
            out var utc,
            out var seconds,
            out var milliseconds);

        Assert.True(success);
        Assert.Equal(new DateTimeOffset(2023, 11, 14, 20, 13, 20, TimeSpan.Zero), utc);
        Assert.Equal(1699992800L, seconds);
        Assert.Equal(1699992800000L, milliseconds);
    }

    [Fact]
    public void ParsesDateStringWithoutOffsetUsingConfiguredTimeZone()
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");

        var success = UnixTimestampConverter.TryConvertToTimestamp(
            "2023-11-14 22:13:20",
            timeZone,
            out var utc,
            out var seconds,
            out var milliseconds);

        Assert.True(success);
        Assert.Equal(new DateTimeOffset(2023, 11, 14, 16, 43, 20, TimeSpan.Zero), utc);
        Assert.Equal(milliseconds / 1000L, seconds);
        Assert.Equal(utc.ToUnixTimeMilliseconds(), milliseconds);
    }

    [Fact]
    public void ParsesDateStringWithoutOffsetAsUtcWhenNoTimeZoneOverloadUsed()
    {
        var success = UnixTimestampConverter.TryConvertToTimestamp(
            "2023-11-14 22:13:20",
            out var utc,
            out var seconds,
            out var milliseconds);

        Assert.True(success);
        Assert.Equal(new DateTimeOffset(2023, 11, 14, 22, 13, 20, TimeSpan.Zero), utc);
        Assert.Equal(1700000000L, seconds);
        Assert.Equal(1700000000000L, milliseconds);
    }

    [Fact]
    public void FormatsUtcInstantUsingCanonicalFormat()
    {
        var utc = new DateTimeOffset(2023, 11, 14, 22, 13, 20, TimeSpan.Zero);

        Assert.Equal("2023-11-14T22:13:20.000Z", UnixTimestampConverter.FormatUtc(utc));
    }

    [Fact]
    public void FormatsUtcInstantInTargetTimeZoneWithOffset()
    {
        var utc = new DateTimeOffset(2023, 11, 14, 22, 13, 20, TimeSpan.Zero);
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");

        Assert.Equal("2023-11-15T03:43:20.000+05:30", UnixTimestampConverter.FormatInTimeZone(utc, timeZone));
    }
}
