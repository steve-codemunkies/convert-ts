using System;
using System.Collections.Generic;
using System.Globalization;
using ConvertTs.Utilities;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace ConvertTs.Pages;

internal sealed partial class UnixTimestampPage : DynamicListPage
{
    private const string UtcDisplayFormat = "yyyy-MM-ddTHH:mm:ss'Z'";

    private string _query = string.Empty;

    public UnixTimestampPage()
    {
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
        Title = "Unix Epoch Converter";
        Name = "Open";
        PlaceholderText = "Type a Unix timestamp or date/time string";
        ShowDetails = true;
    }

    public override void UpdateSearchText(string oldSearch, string newSearch)
    {
        _query = newSearch;
        RaiseItemsChanged();
    }

    public override IListItem[] GetItems()
    {
        var items = new List<IListItem>
        {
            CreateNowItem(),
        };

        var input = _query.Trim();
        if (string.IsNullOrWhiteSpace(input))
        {
            items.Add(CreateHintItem());
            return items.ToArray();
        }

        if (UnixTimestampConverter.TryConvertFromTimestamp(
                input,
                UnixTimestampConverter.DefaultBoundaryValue,
                out var utc,
                out var seconds,
                out var milliseconds))
        {
            items.Add(CreateFromTimestampItem(input, utc, seconds, milliseconds));
            return items.ToArray();
        }

        if (UnixTimestampConverter.TryConvertToTimestamp(input, out var parsedUtc, out var parsedSeconds, out var parsedMilliseconds))
        {
            items.Add(CreateSecondsResultItem(parsedUtc, parsedSeconds));
            items.Add(CreateMillisecondsResultItem(parsedUtc, parsedMilliseconds));
            return items.ToArray();
        }

        items.Add(CreateHintItem(input));
        return items.ToArray();
    }

    private static ListItem CreateNowItem()
    {
        UnixTimestampConverter.GetNowTimestamps(out var utc, out var seconds, out var milliseconds);

        var item = new ListItem(new NoOpCommand())
        {
            Title = "Now",
            Subtitle = $"UTC: {FormatUtc(utc)} · Seconds: {seconds} · Milliseconds: {milliseconds}",
            TextToSuggest = milliseconds.ToString(CultureInfo.InvariantCulture),
            MoreCommands = [new CommandContextItem(new CopyTextCommand(milliseconds.ToString(CultureInfo.InvariantCulture)))],
        };

        return item;
    }

    private static ListItem CreateFromTimestampItem(string input, DateTimeOffset utc, long seconds, long milliseconds)
    {
        var utcText = FormatUtc(utc);
        return new ListItem(new NoOpCommand())
        {
            Title = utcText,
            Subtitle = $"Input: {input} · Seconds: {seconds} · Milliseconds: {milliseconds}",
            TextToSuggest = utcText,
            MoreCommands = [new CommandContextItem(new CopyTextCommand(utcText))],
        };
    }

    private static ListItem CreateSecondsResultItem(DateTimeOffset utc, long seconds)
    {
        var secondsText = seconds.ToString(CultureInfo.InvariantCulture);
        return new ListItem(new NoOpCommand())
        {
            Title = secondsText,
            Subtitle = $"UTC: {FormatUtc(utc)}",
            TextToSuggest = secondsText,
            MoreCommands = [new CommandContextItem(new CopyTextCommand(secondsText))],
        };
    }

    private static ListItem CreateMillisecondsResultItem(DateTimeOffset utc, long milliseconds)
    {
        var millisecondsText = milliseconds.ToString(CultureInfo.InvariantCulture);
        return new ListItem(new NoOpCommand())
        {
            Title = millisecondsText,
            Subtitle = $"UTC: {FormatUtc(utc)}",
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

    private static string FormatUtc(DateTimeOffset utc)
    {
        return utc.ToUniversalTime().ToString(UtcDisplayFormat, CultureInfo.InvariantCulture);
    }
}
