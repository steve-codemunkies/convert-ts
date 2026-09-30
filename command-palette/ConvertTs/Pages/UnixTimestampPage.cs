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

        var item = new ListItem(new NoOpCommand())
        {
            Title = "Now",
            Subtitle = $"UTC: {UnixTimestampConverter.FormatUtc(utc)}{FormatZoneSuffix(utc, timeZone)} · Seconds: {seconds} · Milliseconds: {milliseconds}",
            TextToSuggest = milliseconds.ToString(CultureInfo.InvariantCulture),
            MoreCommands = [new CommandContextItem(new CopyTextCommand(milliseconds.ToString(CultureInfo.InvariantCulture)))],
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
        };
    }

    private static ListItem CreateSecondsResultItem(DateTimeOffset utc, long seconds, TimeZoneInfo timeZone)
    {
        var secondsText = seconds.ToString(CultureInfo.InvariantCulture);
        return new ListItem(new NoOpCommand())
        {
            Title = secondsText,
            Subtitle = $"UTC: {UnixTimestampConverter.FormatUtc(utc)}{FormatZoneSuffix(utc, timeZone)}",
            TextToSuggest = secondsText,
            MoreCommands = [new CommandContextItem(new CopyTextCommand(secondsText))],
        };
    }

    private static ListItem CreateMillisecondsResultItem(DateTimeOffset utc, long milliseconds, TimeZoneInfo timeZone)
    {
        var millisecondsText = milliseconds.ToString(CultureInfo.InvariantCulture);
        return new ListItem(new NoOpCommand())
        {
            Title = millisecondsText,
            Subtitle = $"UTC: {UnixTimestampConverter.FormatUtc(utc)}{FormatZoneSuffix(utc, timeZone)}",
            TextToSuggest = millisecondsText,
            MoreCommands = [new CommandContextItem(new CopyTextCommand(millisecondsText))],
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

    private static string FormatZoneSuffix(DateTimeOffset utc, TimeZoneInfo timeZone)
    {
        if (string.Equals(timeZone.Id, TimeZoneInfo.Utc.Id, StringComparison.Ordinal))
        {
            return string.Empty;
        }

        return $" · {timeZone.Id}: {UnixTimestampConverter.FormatInTimeZone(utc, timeZone)}";
    }
}
