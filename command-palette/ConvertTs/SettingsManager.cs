using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ConvertTs.Utilities;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace ConvertTs;

internal sealed partial class SettingsManager : JsonSettingsManager
{
    internal const string LocalTimeZoneChoiceValue = "__local__";

    private const string Namespace = "ConvertTs.CommandPalette";

    private readonly TextSetting _boundaryValue;
    private readonly ChoiceSetSetting _timeZone;

    internal static string SettingsJsonPath()
    {
        var directory = Microsoft.CommandPalette.Extensions.Toolkit.Utilities.BaseSettingsPath(Namespace);
        Directory.CreateDirectory(directory);

        return Path.Combine(directory, "settings.json");
    }

    public SettingsManager()
        : this(SettingsJsonPath())
    {
    }

    internal SettingsManager(string filePath)
    {
        FilePath = filePath;

        _boundaryValue = new TextSetting(
            "boundaryValue",
            "Millisecond boundary value",
            BuildBoundaryDescription(UnixTimestampConverter.DefaultBoundaryValue),
            UnixTimestampConverter.DefaultBoundaryValue.ToString(CultureInfo.InvariantCulture));

        _timeZone = new ChoiceSetSetting(
            "timeZone",
            "Timezone",
            "Timezone used to interpret date/time text that has no explicit offset, and to display converted results.",
            BuildTimeZoneChoices());

        Settings.Add(_boundaryValue);
        Settings.Add(_timeZone);

        LoadSettings();

        UpdateBoundaryDescription();

        Settings.SettingsChanged += (_, _) =>
        {
            UpdateBoundaryDescription();
            SaveSettings();
        };
    }

    /// <summary>
    /// Gets the configured boundary value used to decide whether a bare numeric
    /// timestamp should be treated as seconds or milliseconds. Falls back to
    /// <see cref="UnixTimestampConverter.DefaultBoundaryValue"/> if the stored value
    /// cannot be parsed.
    /// </summary>
    public long BoundaryValue => long.TryParse(_boundaryValue.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
        ? value
        : UnixTimestampConverter.DefaultBoundaryValue;

    /// <summary>
    /// Gets a value indicating whether the current device's local timezone should be
    /// used instead of a fixed, user-selected timezone.
    /// </summary>
    public bool UseLocalTimeZone => _timeZone.Value == LocalTimeZoneChoiceValue;

    /// <summary>
    /// Gets the timezone that should be used to interpret and display date/time values.
    /// </summary>
    public TimeZoneInfo TimeZone
    {
        get
        {
            if (UseLocalTimeZone || string.IsNullOrEmpty(_timeZone.Value))
            {
                return TimeZoneInfo.Local;
            }

            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(_timeZone.Value);
            }
            catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
            {
                return TimeZoneInfo.Utc;
            }
        }
    }

    /// <summary>
    /// Gets the current human-readable description for the boundary-value setting,
    /// including the live UTC-equivalent instant. Exposed for testing.
    /// </summary>
    internal string BoundaryValueDescription => _boundaryValue.Description;

    internal void UpdateBoundaryDescription()
    {
        _boundaryValue.Description = BuildBoundaryDescription(BoundaryValue);
    }

    private static string BuildBoundaryDescription(long boundary)
    {
        string utcText;
        try
        {
            utcText = UnixTimestampConverter.FormatUtc(DateTimeOffset.FromUnixTimeSeconds(boundary));
        }
        catch (ArgumentOutOfRangeException)
        {
            utcText = "out of range";
        }

        return "Timestamps greater than this value are treated as milliseconds; otherwise seconds. "
            + $"Equivalent UTC instant if this value were interpreted as seconds (regardless of the timezone setting below): {utcText}";
    }

    private static List<ChoiceSetSetting.Choice> BuildTimeZoneChoices()
    {
        var choices = new List<ChoiceSetSetting.Choice>
        {
            new("UTC", "UTC"),
            new("Use local timezone", LocalTimeZoneChoiceValue),
        };

        choices.AddRange(
            TimeZoneInfo.GetSystemTimeZones()
                .Where(tz => !string.Equals(tz.Id, "UTC", StringComparison.OrdinalIgnoreCase))
                .OrderBy(tz => tz.BaseUtcOffset)
                .ThenBy(tz => tz.DisplayName, StringComparer.OrdinalIgnoreCase)
                .Select(tz => new ChoiceSetSetting.Choice(tz.DisplayName, tz.Id)));

        return choices;
    }
}
