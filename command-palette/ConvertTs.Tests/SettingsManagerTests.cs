using System;
using System.Globalization;
using System.IO;
using Xunit;

namespace ConvertTs.Tests;

public class SettingsManagerTests : IDisposable
{
    private readonly string _filePath;

    public SettingsManagerTests()
    {
        _filePath = Path.Combine(Path.GetTempPath(), $"convert-ts-settings-tests-{Guid.NewGuid():N}.json");
    }

    public void Dispose()
    {
        if (File.Exists(_filePath))
        {
            File.Delete(_filePath);
        }
    }

    [Fact]
    public void DefaultsMatchPreExistingBehavior()
    {
        var settingsManager = new SettingsManager(_filePath);

        Assert.Equal(Utilities.UnixTimestampConverter.DefaultBoundaryValue, settingsManager.BoundaryValue);
        Assert.False(settingsManager.UseLocalTimeZone);
        Assert.Equal("UTC", settingsManager.TimeZone.Id);
    }

    [Fact]
    public void BoundaryValueDescriptionIncludesUtcEquivalent()
    {
        var settingsManager = new SettingsManager(_filePath);

        Assert.Contains("3000-01-01T00:00:00.000Z", settingsManager.BoundaryValueDescription, StringComparison.Ordinal);
    }

    [Fact]
    public void BoundaryValueDescriptionUpdatesWhenValueChanges()
    {
        var settingsManager = new SettingsManager(_filePath);

        settingsManager.Settings.Update(BuildPayload(boundaryValue: "1700000000"));
        settingsManager.UpdateBoundaryDescription();

        Assert.Equal(1700000000L, settingsManager.BoundaryValue);
        Assert.Contains("2023-11-14T22:13:20.000Z", settingsManager.BoundaryValueDescription, StringComparison.Ordinal);
    }

    [Fact]
    public void FallsBackToDefaultBoundaryWhenStoredValueIsNotNumeric()
    {
        var settingsManager = new SettingsManager(_filePath);

        settingsManager.Settings.Update(BuildPayload(boundaryValue: "not-a-number"));

        Assert.Equal(Utilities.UnixTimestampConverter.DefaultBoundaryValue, settingsManager.BoundaryValue);
    }

    [Fact]
    public void UsesLocalTimeZoneWhenSelected()
    {
        var settingsManager = new SettingsManager(_filePath);

        settingsManager.Settings.Update(BuildPayload(timeZone: SettingsManager.LocalTimeZoneChoiceValue));

        Assert.True(settingsManager.UseLocalTimeZone);
        Assert.Equal(TimeZoneInfo.Local.Id, settingsManager.TimeZone.Id);
    }

    [Fact]
    public void ResolvesConfiguredNamedTimeZone()
    {
        var settingsManager = new SettingsManager(_filePath);

        settingsManager.Settings.Update(BuildPayload(timeZone: "Asia/Kolkata"));

        Assert.False(settingsManager.UseLocalTimeZone);
        Assert.Equal("Asia/Kolkata", settingsManager.TimeZone.Id);
    }

    [Fact]
    public void PersistsSettingsAcrossInstances()
    {
        var first = new SettingsManager(_filePath);
        first.Settings.Update(BuildPayload(boundaryValue: "1234567890123", timeZone: "Asia/Kolkata"));
        first.SaveSettings();

        var second = new SettingsManager(_filePath);

        Assert.Equal(1234567890123L, second.BoundaryValue);
        Assert.Equal("Asia/Kolkata", second.TimeZone.Id);
    }

    private static string BuildPayload(string? boundaryValue = null, string? timeZone = null)
    {
        boundaryValue ??= Utilities.UnixTimestampConverter.DefaultBoundaryValue.ToString(CultureInfo.InvariantCulture);
        timeZone ??= "UTC";

        return $$"""
        {
          "boundaryValue": "{{boundaryValue}}",
          "timeZone": "{{timeZone}}"
        }
        """;
    }
}
