using System;
using ConvertTs.Toolkit;
using ConvertTs.Utilities;
using Xunit;

namespace ConvertTs.Tests.Utilities;

public class ConversionResultsTests
{
    private static readonly Timestamp Sample =
        new(new DateTimeOffset(2023, 11, 14, 22, 13, 20, TimeSpan.Zero), 1700000000L, 1700000000000L);

    [Fact]
    public void WithoutTimeZoneReturnsUtcMillisecondsSeconds()
    {
        var values = ConversionResults.Build(Sample, TimeZoneInfo.Utc);

        Assert.Equal(3, values.Length);
        Assert.Equal(new ConversionValue("UTC Date/Time", "2023-11-14T22:13:20.000Z"), values[0]);
        Assert.Equal(new ConversionValue("Unix Epoch Milliseconds", "1700000000000"), values[1]);
        Assert.Equal(new ConversionValue("Unix Epoch Seconds", "1700000000"), values[2]);
    }

    [Fact]
    public void WithTimeZoneReturnsZoneFirst()
    {
        var zone = TimeZoneInfo.CreateCustomTimeZone("Test", TimeSpan.FromHours(2), "Test", "Test");

        var values = ConversionResults.Build(Sample, zone);

        Assert.Equal(4, values.Length);
        Assert.Equal(new ConversionValue("User time zone date/time", "2023-11-15T00:13:20.000+02:00"), values[0]);
        Assert.Equal("UTC Date/Time", values[1].Label);
        Assert.Equal("Unix Epoch Milliseconds", values[2].Label);
        Assert.Equal("Unix Epoch Seconds", values[3].Label);
    }

    [Fact]
    public void CopyItemCommandHoldsValue()
    {
        var command = new CopyItemCommand("abc", "Copy x");

        Assert.Equal("abc", command.Text);
    }
}
