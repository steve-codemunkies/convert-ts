# Command Palette extension

This folder contains the initial PowerToys Command Palette extension for converting Unix epoch timestamps.

## What it does

The extension mirrors the behavior of the existing PowerShell and Bash scripts in this repository:

- Detects whether the input is a Unix timestamp or a date/time string
- Treats values above the default boundary (`32503680000`) as milliseconds
- Converts timestamp input to UTC date/time output
- Converts date/time input to both Unix seconds and milliseconds
- Shows a live `Now` item with current UTC epoch seconds and milliseconds
- Adds a Copy action to each result item

The first version is UTC-only.

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
dotnet build .\command-palette\ConvertTs.CommandPalette\ConvertTs.CommandPalette.csproj
```

> Note: the project targets `net10.0-windows10.0.26100.0`. Ensure both the .NET 10 SDK and Windows SDK 10.0.26100.0 are installed before building (see [Prerequisites](#prerequisites)).

## Install into PowerToys Command Palette (local)

1. Build the extension project:

```powershell
dotnet build .\command-palette\ConvertTs.CommandPalette\ConvertTs.CommandPalette.csproj
```

2. Package/install the app from Visual Studio:
   - Open `command-palette/ConvertTs.CommandPalette/ConvertTs.CommandPalette.csproj` in Visual Studio.
   - Right-click the project and use **Package and Publish** (or **Publish**) to create/install the MSIX package.
   - If prompted, trust/install the local signing certificate used for the package.

3. Ensure PowerToys is running, then open **Command Palette**.

4. Search for **convert ts** (the extension top-level command) and open it.

5. Try inputs like:
   - `1700000000`
   - `1700000000000`
   - `2023-11-14T22:13:20Z`

If the command does not appear, restart PowerToys after installation and reopen Command Palette.

## Test

Run the helper tests:

```powershell
dotnet test .\command-palette\ConvertTs.CommandPalette.Tests\ConvertTs.CommandPalette.Tests.csproj
```

## Expected inputs

Examples of inputs the extension should accept:

- `1700000000`
- `1700000000000`
- `2023-11-14T22:13:20Z`
- `2024-03-15`

## Follow-up work

The initial version intentionally leaves room for later additions such as:

- timezone overrides
- custom input/output formats
- boundary override settings
- packaging/distribution guidance
