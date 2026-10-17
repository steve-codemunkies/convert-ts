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
            items.AddRange(CreateItems(UnixTimestampConverter.GetNowTimestamp(), timeZone));
            items.Add(CreateHintItem());
        }
        else if (UnixTimestampConverter.TryConvertFromTimestamp(
                input,
                _settingsManager.BoundaryValue,
                out var fromTimestamp))
        {
            items.AddRange(CreateItems(fromTimestamp, timeZone, input));
        }
        else if (UnixTimestampConverter.TryConvertToTimestamp(input, timeZone, out var parsed))
        {
            items.AddRange(CreateItems(parsed, timeZone));
        }
        else
        {
            items.AddRange(CreateItems(UnixTimestampConverter.GetNowTimestamp(), timeZone));
            items.Add(CreateHintItem(input));
        }

        items.Add(CreateSettingsItem(_settingsManager));
        return items.ToArray();
    }

    private static ListItem[] CreateItems(Timestamp ts, TimeZoneInfo timeZone, string? input = null)
    {
        var values = ConversionResults.Build(ts, timeZone);
        var items = new ListItem[values.Length];

        for (var i = 0; i < values.Length; i++)
        {
            var value = values[i];
            items[i] = new ListItem(new CopyItemCommand(value.Text, $"Copy {value.Label}"))
            {
                Title = value.Text,
                Subtitle = value.Label,
                TextToSuggest = value.Text,
                Details = CreateTimestampDetails(value.Text, $"**{value.Label}**", ts, timeZone, input),
            };
        }

        return items;
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
        var isUtc = ConversionResults.IsUtc(timeZone);

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

        Tag[] tags = isUtc ? new[] { new Tag("UTC") } : new[] { new Tag("UTC"), new Tag(timeZone.Id) };
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
}
