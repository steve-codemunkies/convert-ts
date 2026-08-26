using System.IO;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace ConvertTs;

internal sealed partial class SettingsManager : JsonSettingsManager
{
    private static readonly string _namespace = "ConvertTs.CommandPalette";

    private static string Namespaced(string propertyName) => $"{_namespace}.{propertyName}";

    internal static string SettingsJsonPath()
    {
        var directory = Microsoft.CommandPalette.Extensions.Toolkit.Utilities.BaseSettingsPath(_namespace);
        Directory.CreateDirectory(directory);

        return Path.Combine(directory, "settings.json");
    }

    public SettingsManager()
    {
        FilePath = SettingsJsonPath();

        LoadSettings();

        Settings.SettingsChanged += (_, _) => SaveSettings();
    }
}
