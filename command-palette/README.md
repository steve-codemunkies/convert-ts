# Command Palette extension

This folder contains the initial PowerToys Command Palette extension for converting Unix epoch timestamps.

> [!TIP]
> In order to be able to run the built extension locally you must [enable Developer Mode in Windows Settings](https://learn.microsoft.com/en-us/windows/advanced-settings/developer-mode)

## What it does

The extension mirrors the behavior of the existing PowerShell and Bash scripts in this repository:

- Detects whether the input is a Unix timestamp or a date/time string
- Treats values above a configurable boundary (default `32503680000`) as milliseconds
- Converts timestamp input to UTC date/time output, plus the configured timezone's local time when it differs from UTC
- Converts date/time input to both Unix seconds and milliseconds, interpreting inputs without an explicit offset using the configured timezone
- Shows a live `Now` item with current UTC epoch seconds and milliseconds
- Adds a Copy action to each result item

The boundary value and timezone are user-configurable — see [Settings](#settings) below.

## Project layout

- `ConvertTs/` — the PowerToys Command Palette extension project
- `ConvertTs.Tests/` — helper-focused unit tests for the shared converter logic

## Prerequisites

- Windows 11 with PowerToys (version supporting Command Palette extensions)
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- **Windows SDK 10.0.26100.0** (Windows 11 24H2) — required by the `Microsoft.CommandPalette.Extensions` NuGet package

### Installing the Windows SDK

The extension project targets `net10.0-windows10.0.26100.0`, which requires the Windows 11 SDK version 10.0.26100.0. Without it the build fails with error `APPX3217`.

**Option A — Visual Studio Installer (recommended)**

1. Open **Visual Studio Installer**.
2. Select **Modify** on your Visual Studio installation.
3. Go to the **Individual components** tab.
4. Search for **Windows 11 SDK (10.0.26100.0)** and tick it.
5. Click **Modify** and wait for installation to complete.

**Option B — Standalone installer**

Download and run the Windows 11 SDK installer directly from Microsoft:
<https://developer.microsoft.com/windows/downloads/windows-sdk/>

Select version **10.0.26100.0** (Windows 11, version 24H2).

## Build

Build the extension project:

```powershell
dotnet build command-palette\ConvertTs\ConvertTs.csproj -c Debug /p:Platform=x64
```

> Note: the project targets `net10.0-windows10.0.26100.0`. Ensure both the .NET 10 SDK and Windows SDK 10.0.26100.0 are installed before building (see [Prerequisites](#prerequisites)).

## Install into PowerToys Command Palette (local)

1. Build the extension project:

```powershell
dotnet publish -c Debug /p:Platform=x64 /p:GenerateAppxPackageOnBuild=true command-palette\ConvertTs\ConvertTs.csproj
```
2. Use the `Add-AppxPackage` cmdlet to install the local build:

```powershell
cd ".\command-palette\ConvertTs\bin\x64\Debug\net10.0-windows10.0.26100.0\win-x64"
Add-AppxPackage -Register .\AppxManifest.xml
```

> [!WARNING]
> If you encounter an error because you've previously installed the extension you can either check using the process at the end of this document or force the install

3. Ensure PowerToys is running, then open **Command Palette**.

4. Search for **convert ts** (the extension top-level command) and open it.

5. Try inputs like:
   - `1700000000`
   - `1700000000000`
   - `2023-11-14T22:13:20Z`

If the command does not appear, restart PowerToys after installation and reopen Command Palette.

### Forced install

> [!WARNING]
> Here be dragons

```powershell
cd ".\command-palette\ConvertTs\bin\x64\Debug\net10.0-windows10.0.26100.0\win-x64"
Add-AppxPackage -Register .\AppxManifest.xml -ForceUpdateFromAnyVersion
```

## Test

Run the helper tests:

```powershell
dotnet test .\command-palette\ConvertTs.CommandPalette.Tests\ConvertTs.CommandPalette.Tests.csproj
```

## Settings

The extension exposes two configurable settings:

| Setting | Type | Default | Description |
| ------- | ---- | ------- | ------------ |
| Millisecond boundary value | Text (integer) | `32503680000` | Bare numeric timestamps greater than this value are treated as milliseconds; otherwise they're treated as seconds. The setting's description live-updates to show the equivalent UTC instant if the entered value were interpreted as seconds, regardless of the timezone setting below, so you can see what date the boundary represents as you type. |
| Timezone | Choice (dropdown) | `UTC` | Timezone used to interpret date/time text that has no explicit offset (e.g. `2024-03-15 10:00:00`), and to display converted results alongside UTC. Includes a **Use local timezone** option that always tracks the device's current system timezone, plus every timezone known to the operating system (`TimeZoneInfo.GetSystemTimeZones()`). Input that includes an explicit offset or `Z` designator (e.g. `2023-11-14T22:13:20Z`, `2023-11-14T22:13:20+02:00`) always honors that offset, ignoring this setting. |

### Storage

Settings are persisted as JSON via the Command Palette toolkit's `JsonSettingsManager`, in a file at:

```
%LOCALAPPDATA%\Microsoft\CommandPalette\Settings\ConvertTs.CommandPalette\settings.json
```

The file is created on first run and updated automatically whenever a setting changes — there is nothing to configure manually.

### Accessing settings

You can open the settings page in any of the following ways:

1. **From the extension's results** — open **convert ts**, clear the search box, and select the **Open settings** item shown alongside the **Now** item.
2. **From the command's context menu** — open **convert ts**, then use the context menu (or its keyboard shortcut) on the top-level command and choose the settings entry.
3. **From Command Palette's global settings** — open Command Palette's own **Settings → Extensions** page and select this extension's entry.

## Expected inputs

Examples of inputs the extension should accept:

- `1700000000`
- `1700000000000`
- `2023-11-14T22:13:20Z`
- `2024-03-15`

## Follow-up work

The initial version intentionally leaves room for later additions such as:

- custom input/output formats
- packaging/distribution guidance

## Uninstalling from PowerToys Command Palette (local)

The easiest way I have found to remove the extension is as follows:

1. Run the `Get-AppxPackage` cmdlet and direct the output to a text file:

```powershell
Get-AppxPackage > .\output.txt
```

2. Open the file and search for `ConvertTs`, you should find an entry similar to the following:

```
Name              : ConvertTs
Publisher         : CN=Microsoft Corporation, O=Microsoft Corporation, L=Redmond, S=Washington, C=US
Architecture      : X64
ResourceId        : 
Version           : 0.0.1.0
PackageFullName   : ConvertTs_0.0.1.0_x64__8wekyb3d8bbwe
InstallLocation   : C:\Users\myuser\source\repos\convert-ts\command-palette\ConvertTs\bin\x64\Debug\net10.0-windows10.0.26100.0\win-x64
IsFramework       : False
PackageFamilyName : ConvertTs_8wekyb3d8bbwe
PublisherId       : 8wekyb3d8bbwe
IsResourcePackage : False
IsBundle          : False
IsDevelopmentMode : True
NonRemovable      : False
IsPartiallyStaged : False
SignatureKind     : None
Status            : DeploymentInProgress, Servicing
Capabilities      : internetClient,
                    runFullTrust,
                    unknownCapability(S-1-15-3-1024-2579371802-50273823-2532007077-778130756-637227457-1650229637-1599285538-2684141260),
                    cellularData,
                    wifiData,
                    uniqueAppPackageCapability
```

3. Use the `PackageFullName` to remove the package:

```powershell
Remove-AppxPackage -Package "ConvertTs_0.0.1.0_x64__8wekyb3d8bbwe"
```