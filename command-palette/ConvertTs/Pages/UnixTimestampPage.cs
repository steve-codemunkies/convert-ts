using System;
using System.Collections.Generic;
using System.Globalization;
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

        var items = new List<IListItem>
        {
            CreateNowItem(timeZone),
        };

        var input = _query.Trim();
        if (string.IsNullOrWhiteSpace(input))
        {
            items.Add(CreateHintItem());
            items.Add(CreateSettingsItem(_settingsManager));
            return items.ToArray();
        }

        if (UnixTimestampConverter.TryConvertFromTimestamp(
                input,
                _settingsManager.BoundaryValue,
                out var utc,
                out var seconds,
                out var milliseconds))
        {
            items.Add(CreateFromTimestampItem(input, utc, seconds, milliseconds, timeZone));
            return items.ToArray();
        }

        if (UnixTimestampConverter.TryConvertToTimestamp(input, timeZone, out var parsedUtc, out var parsedSeconds, out var parsedMilliseconds))
        {
            items.Add(CreateSecondsResultItem(parsedUtc, parsedSeconds, timeZone));
            items.Add(CreateMillisecondsResultItem(parsedUtc, parsedMilliseconds, timeZone));
            return items.ToArray();
        }

        items.Add(CreateHintItem(input));
        return items.ToArray();
    }

    private static ListItem CreateNowItem(TimeZoneInfo timeZone)
    {
        UnixTimestampConverter.GetNowTimestamps(out var utc, out var seconds, out var milliseconds);
        var millisecondsText = milliseconds.ToString(CultureInfo.InvariantCulture);

        var item = new ListItem(new NoOpCommand())
        {
            Title = "Now",
            Subtitle = $"UTC: {UnixTimestampConverter.FormatUtc(utc)}{FormatZoneSuffix(utc, timeZone)} · Seconds: {seconds} · Milliseconds: {milliseconds}",
            TextToSuggest = millisecondsText,
            MoreCommands = [new CommandContextItem(new CopyTextCommand(millisecondsText))],
            Details = CreateTimestampDetails(
                millisecondsText,
                $"**Now** → **{UnixTimestampConverter.FormatUtc(utc)}**{FormatZoneSuffix(utc, timeZone)}",
                utc,
                seconds,
                milliseconds,
                timeZone),
        };

        return item;
    }

    private static ListItem CreateFromTimestampItem(string input, DateTimeOffset utc, long seconds, long milliseconds, TimeZoneInfo timeZone)
    {
        var utcText = UnixTimestampConverter.FormatUtc(utc);
        return new ListItem(new NoOpCommand())
        {
            Title = utcText,
            Subtitle = $"Input: {input}{FormatZoneSuffix(utc, timeZone)} · Seconds: {seconds} · Milliseconds: {milliseconds}",
            TextToSuggest = utcText,
            MoreCommands = [new CommandContextItem(new CopyTextCommand(utcText))],
            Details = CreateTimestampDetails(
                utcText,
                $"**{input}** → **{utcText}**{FormatZoneSuffix(utc, timeZone)}",
                utc,
                seconds,
                milliseconds,
                timeZone,
                input),
        };
    }

    private static ListItem CreateSecondsResultItem(DateTimeOffset utc, long seconds, TimeZoneInfo timeZone)
    {
        var secondsText = seconds.ToString(CultureInfo.InvariantCulture);
        var utcText = UnixTimestampConverter.FormatUtc(utc);
        return new ListItem(new NoOpCommand())
        {
            Title = secondsText,
            Subtitle = $"UTC: {utcText}{FormatZoneSuffix(utc, timeZone)}",
            TextToSuggest = secondsText,
            MoreCommands = [new CommandContextItem(new CopyTextCommand(secondsText))],
            Details = CreateTimestampDetails(
                secondsText,
                $"**{utcText}** → **{secondsText} s**{FormatZoneSuffix(utc, timeZone)}",
                utc,
                seconds,
                utc.ToUnixTimeMilliseconds(),
                timeZone),
        };
    }

    private static ListItem CreateMillisecondsResultItem(DateTimeOffset utc, long milliseconds, TimeZoneInfo timeZone)
    {
        var millisecondsText = milliseconds.ToString(CultureInfo.InvariantCulture);
        var utcText = UnixTimestampConverter.FormatUtc(utc);
        return new ListItem(new NoOpCommand())
        {
            Title = millisecondsText,
            Subtitle = $"UTC: {utcText}{FormatZoneSuffix(utc, timeZone)}",
            TextToSuggest = millisecondsText,
            MoreCommands = [new CommandContextItem(new CopyTextCommand(millisecondsText))],
            Details = CreateTimestampDetails(
                millisecondsText,
                $"**{utcText}** → **{millisecondsText} ms**{FormatZoneSuffix(utc, timeZone)}",
                utc,
                milliseconds / 1000L,
                milliseconds,
                timeZone),
        };
    }

    /// <summary>
    /// Builds the side-panel "Details" shown when a result item is selected, surfacing the
    /// UTC/local timestamps, seconds/milliseconds values, and unit tags at a glance.
    /// </summary>
    private static Details CreateTimestampDetails(
        string title,
        string body,
        DateTimeOffset utc,
        long seconds,
        long milliseconds,
        TimeZoneInfo timeZone,
        string? input = null)
    {
        var isUtc = string.Equals(timeZone.Id, TimeZoneInfo.Utc.Id, StringComparison.Ordinal);

        var metadata = new List<IDetailsElement>();

        if (!string.IsNullOrEmpty(input))
        {
            metadata.Add(new DetailsElement { Key = "Input", Data = new DetailsLink { Text = input } });
        }

        metadata.Add(new DetailsElement { Key = "UTC", Data = new DetailsLink { Text = UnixTimestampConverter.FormatUtc(utc) } });

        if (!isUtc)
        {
            metadata.Add(new DetailsElement { Key = timeZone.Id, Data = new DetailsLink { Text = UnixTimestampConverter.FormatInTimeZone(utc, timeZone) } });
        }

        metadata.Add(new DetailsElement { Key = "Seconds", Data = new DetailsLink { Text = seconds.ToString(CultureInfo.InvariantCulture) } });
        metadata.Add(new DetailsElement { Key = "Milliseconds", Data = new DetailsLink { Text = milliseconds.ToString(CultureInfo.InvariantCulture) } });

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
        if (string.Equals(timeZone.Id, TimeZoneInfo.Utc.Id, StringComparison.Ordinal))
        {
            return string.Empty;
        }

        return $" · {timeZone.Id}: {UnixTimestampConverter.FormatInTimeZone(utc, timeZone)}";
    }
}
