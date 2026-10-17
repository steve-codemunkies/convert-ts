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
            };
        }

        return items;
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
