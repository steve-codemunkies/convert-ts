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
            out var result);

        Assert.True(success);
        Assert.Equal(new DateTimeOffset(2023, 11, 14, 22, 13, 20, TimeSpan.Zero), result.Utc);
        Assert.Equal(1700000000L, result.Seconds);
        Assert.Equal(1700000000000L, result.Milliseconds);
    }

    [Fact]
    public void ConvertsMillisecondsTimestampUsingDefaultBoundary()
    {
        var success = UnixTimestampConverter.TryConvertFromTimestamp(
            "1700000000000",
            UnixTimestampConverter.DefaultBoundaryValue,
            out var result);

        Assert.True(success);
        Assert.Equal(new DateTimeOffset(2023, 11, 14, 22, 13, 20, TimeSpan.Zero), result.Utc);
        Assert.Equal(1700000000L, result.Seconds);
        Assert.Equal(1700000000000L, result.Milliseconds);
    }

    [Fact]
    public void ConvertsTimestampUsingCustomBoundary()
    {
        var success = UnixTimestampConverter.TryConvertFromTimestamp(
            "10000000000",
            9999999999L,
            out var result);

        Assert.True(success);
        Assert.Equal(new DateTimeOffset(1970, 4, 26, 17, 46, 40, TimeSpan.Zero), result.Utc);
        Assert.Equal(10000000L, result.Seconds);
        Assert.Equal(10000000000L, result.Milliseconds);
    }

    [Fact]
    public void ParsesIso8601UtcDateStringToEpoch()
    {
        var success = UnixTimestampConverter.TryConvertToTimestamp(
            "2023-11-14T22:13:20Z",
            out var result);

        Assert.True(success);
        Assert.Equal(new DateTimeOffset(2023, 11, 14, 22, 13, 20, TimeSpan.Zero), result.Utc);
        Assert.Equal(1700000000L, result.Seconds);
        Assert.Equal(1700000000000L, result.Milliseconds);
    }

    [Fact]
    public void ReturnsCurrentUtcTimestampValues()
    {
        var now = UnixTimestampConverter.GetNowTimestamp();

        Assert.Equal(now.Utc.ToUnixTimeMilliseconds(), now.Milliseconds);
        Assert.Equal(now.Milliseconds / 1000L, now.Seconds);
        Assert.True(Math.Abs((DateTimeOffset.UtcNow - now.Utc).TotalSeconds) < 5);
    }

    [Fact]
    public void ParsesDateStringWithExplicitOffsetRegardlessOfConfiguredTimeZone()
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");

        var success = UnixTimestampConverter.TryConvertToTimestamp(
            "2023-11-14T22:13:20+02:00",
            timeZone,
            out var result);

        Assert.True(success);
        Assert.Equal(new DateTimeOffset(2023, 11, 14, 20, 13, 20, TimeSpan.Zero), result.Utc);
        Assert.Equal(1699992800L, result.Seconds);
        Assert.Equal(1699992800000L, result.Milliseconds);
    }

    [Fact]
    public void ParsesDateStringWithoutOffsetUsingConfiguredTimeZone()
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");

        var success = UnixTimestampConverter.TryConvertToTimestamp(
            "2023-11-14 22:13:20",
            timeZone,
            out var result);

        Assert.True(success);
        Assert.Equal(new DateTimeOffset(2023, 11, 14, 16, 43, 20, TimeSpan.Zero), result.Utc);
        Assert.Equal(result.Milliseconds / 1000L, result.Seconds);
        Assert.Equal(result.Utc.ToUnixTimeMilliseconds(), result.Milliseconds);
    }

    [Fact]
    public void ParsesDateStringWithoutOffsetAsUtcWhenNoTimeZoneOverloadUsed()
    {
        var success = UnixTimestampConverter.TryConvertToTimestamp(
            "2023-11-14 22:13:20",
            out var result);

        Assert.True(success);
        Assert.Equal(new DateTimeOffset(2023, 11, 14, 22, 13, 20, TimeSpan.Zero), result.Utc);
        Assert.Equal(1700000000L, result.Seconds);
        Assert.Equal(1700000000000L, result.Milliseconds);
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
