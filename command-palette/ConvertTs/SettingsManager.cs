using System.IO;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace ConvertTs;

internal sealed partial class SettingsManager : JsonSettingsManager
{
    private const string Namespace = "ConvertTs.CommandPalette";

    internal static string SettingsJsonPath()
    {
        var directory = Microsoft.CommandPalette.Extensions.Toolkit.Utilities.BaseSettingsPath(Namespace);
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
