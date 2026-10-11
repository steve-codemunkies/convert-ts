using System;
using System.Collections.Generic;
using System.Globalization;
using ConvertTs.Toolkit;
using ConvertTs.Utilities;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace ConvertTs.Pages;

internal sealed partial class UnixTimestampPage : DynamicListPage
{
    private readonly SettingsManager _settingsManager;

    private string _query = string.Empty;

    public UnixTimestampPage(SettingsManager settingsManager)
    {
        _settingsManager = settingsManager;

        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
        Title = "Unix Epoch Converter";
        Name = "Open";
        PlaceholderText = "Type a Unix timestamp or date/time string";
        ShowDetails = true;

        _settingsManager.Settings.SettingsChanged += (_, _) => RaiseItemsChanged();
    }

    public override void UpdateSearchText(string oldSearch, string newSearch)
    {
        _query = newSearch;
        RaiseItemsChanged();
    }

    public override IListItem[] GetItems()
    {
        var timeZone = _settingsManager.TimeZone;

        var items = new List<IListItem>();

        var input = _query.Trim();
        if (string.IsNullOrWhiteSpace(input))
        {
            items.Add(CreateNowItem(timeZone));
            items.Add(CreateHintItem());
        }
        else if (UnixTimestampConverter.TryConvertFromTimestamp(
                input,
                _settingsManager.BoundaryValue,
                out var fromTimestamp))
        {
            items.Add(CreateFromTimestampItem(input, fromTimestamp, timeZone));
        }
        else if (UnixTimestampConverter.TryConvertToTimestamp(input, timeZone, out var parsed))
        {
            items.Add(CreateResultItem(parsed, parsed.SecondsText, "s", timeZone));
            items.Add(CreateResultItem(parsed, parsed.MillisecondsText, "ms", timeZone));
        }
        else
        {
            items.Add(CreateNowItem(timeZone));
            items.Add(CreateHintItem(input));
        }

        items.Add(CreateSettingsItem(_settingsManager));
        return items.ToArray();
    }

    private static ListItem CreateNowItem(TimeZoneInfo timeZone)
    {
        var ts = UnixTimestampConverter.GetNowTimestamp();

        return CreateItem(
            "Now",
            $"UTC: {ts.UtcText}{FormatZoneSuffix(ts.Utc, timeZone)} · Seconds: {ts.Seconds} · Milliseconds: {ts.Milliseconds}",
            $"**Now** → **{ts.UtcText}**{FormatZoneSuffix(ts.Utc, timeZone)}",
            ts.MillisecondsText,
            ts,
            timeZone);
    }

    private static ListItem CreateFromTimestampItem(string input, Timestamp ts, TimeZoneInfo timeZone)
    {
        return CreateItem(
            ts.UtcText,
            $"Input: {input}{FormatZoneSuffix(ts.Utc, timeZone)} · Seconds: {ts.Seconds} · Milliseconds: {ts.Milliseconds}",
            $"**{input}** → **{ts.UtcText}**{FormatZoneSuffix(ts.Utc, timeZone)}",
            ts.UtcText,
            ts,
            timeZone,
            input);
    }

    private static ListItem CreateResultItem(Timestamp ts, string valueText, string unit, TimeZoneInfo timeZone)
    {
        return CreateItem(
            valueText,
            $"UTC: {ts.UtcText}{FormatZoneSuffix(ts.Utc, timeZone)}",
            $"**{ts.UtcText}** → **{valueText} {unit}**{FormatZoneSuffix(ts.Utc, timeZone)}",
            valueText,
            ts,
            timeZone);
    }

    private static ListItem CreateItem(
        string title,
        string subtitle,
        string body,
        string primaryText,
        Timestamp ts,
        TimeZoneInfo timeZone,
        string? input = null)
    {
        return new ListItem(new CopyItemCommand(primaryText, "Copy"))
        {
            Title = title,
            Subtitle = subtitle,
            TextToSuggest = primaryText,
            MoreCommands = CreateCopyCommands(ts, timeZone),
            Details = CreateTimestampDetails(primaryText, body, ts, timeZone, input),
        };
    }

    private static IContextItem[] CreateCopyCommands(Timestamp ts, TimeZoneInfo timeZone)
    {
        var commands = new List<IContextItem>
        {
            new CommandContextItem(new CopyItemCommand(ts.MillisecondsText, "Copy Milliseconds")),
            new CommandContextItem(new CopyItemCommand(ts.SecondsText, "Copy Seconds")),
            new CommandContextItem(new CopyItemCommand(ts.UtcText, "Copy UTC")),
        };

        if (!IsUtc(timeZone))
        {
            commands.Add(new CommandContextItem(new CopyItemCommand(UnixTimestampConverter.FormatInTimeZone(ts.Utc, timeZone), $"Copy {timeZone.Id}")));
        }

        return commands.ToArray();
    }

    /// <summary>
    /// Builds the side-panel "Details" shown when a result item is selected, surfacing the
    /// UTC/local timestamps, seconds/milliseconds values, and unit tags at a glance.
    /// </summary>
    private static Details CreateTimestampDetails(
        string title,
        string body,
        Timestamp ts,
        TimeZoneInfo timeZone,
        string? input = null)
    {
        var isUtc = IsUtc(timeZone);

        var metadata = new List<IDetailsElement>();

        if (!string.IsNullOrEmpty(input))
        {
            metadata.Add(new DetailsElement { Key = "Input", Data = new DetailsLink { Text = input } });
        }

        metadata.Add(new DetailsElement { Key = "UTC", Data = new DetailsLink { Text = ts.UtcText } });

        if (!isUtc)
        {
            metadata.Add(new DetailsElement { Key = timeZone.Id, Data = new DetailsLink { Text = UnixTimestampConverter.FormatInTimeZone(ts.Utc, timeZone) } });
        }

        metadata.Add(new DetailsElement { Key = "Seconds", Data = new DetailsLink { Text = ts.SecondsText } });
        metadata.Add(new DetailsElement { Key = "Milliseconds", Data = new DetailsLink { Text = ts.MillisecondsText } });

        Tag[] tags = isUtc ? [new Tag("UTC")] : [new Tag("UTC"), new Tag(timeZone.Id)];
        metadata.Add(new DetailsElement { Key = "Tags", Data = new DetailsTags { Tags = tags } });

        return new Details
        {
            Title = title,
            Body = body,
            Metadata = metadata.ToArray(),
        };
    }

    private static ListItem CreateHintItem(string? input = null)
    {
        var subtitle = string.IsNullOrWhiteSpace(input)
            ? "Enter a Unix timestamp or date/time string"
            : $"No conversion result for '{input}'";

        return new ListItem(new NoOpCommand())
        {
            Title = "Unix Epoch Converter",
            Subtitle = subtitle,
        };
    }

    private static ListItem CreateSettingsItem(SettingsManager settingsManager)
    {
        return new ListItem(settingsManager.Settings.SettingsPage)
        {
            Title = "Open settings",
            Subtitle = "Boundary value and timezone",
            Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png"),
        };
    }

    private static string FormatZoneSuffix(DateTimeOffset utc, TimeZoneInfo timeZone)
    {
        if (IsUtc(timeZone))
        {
            return string.Empty;
        }

        return $" · {timeZone.Id}: {UnixTimestampConverter.FormatInTimeZone(utc, timeZone)}";
    }

    /// <summary>
    /// Determines whether <paramref name="timeZone"/> is semantically equivalent to UTC, either by
    /// matching the well-known UTC id or by having a zero offset with no daylight-saving adjustments.
    /// </summary>
    private static bool IsUtc(TimeZoneInfo timeZone)
    {
        return string.Equals(timeZone.Id, TimeZoneInfo.Utc.Id, StringComparison.Ordinal)
            || (timeZone.BaseUtcOffset == TimeSpan.Zero && !timeZone.SupportsDaylightSavingTime);
    }
}
