# Convert-UnixTimestamp — PowerShell Module

A PowerShell module providing two functions for working with Unix epoch timestamps.

| Function | Purpose |
|---|---|
| `ConvertFrom-UnixTimestamp` | Convert a Unix epoch timestamp to a `DateTimeOffset` |
| `ConvertTo-UnixTimestamp` | Convert a date/time value to Unix epoch timestamps |

---

## Installation

### Quick install (one-liner)

No need to clone the repository or copy files manually. Open a PowerShell terminal and run one of the commands below.

**Install module files only** (you can then `Import-Module` in each session, or add it to your profile manually):

```powershell
irm https://raw.githubusercontent.com/steve-codemunkies/convert-ts/main/powershell/install.ps1 | iex
```

**Install and automatically add to your PowerShell profile** (functions available in every new session):

```powershell
& ([scriptblock]::Create((irm https://raw.githubusercontent.com/steve-codemunkies/convert-ts/main/powershell/install.ps1))) -AddToProfile
```

After installing, load the module in your current session:

```powershell
Import-Module Convert-UnixTimestamp
```

> **Note — execution policy**: if PowerShell reports a security error about running scripts, you may need to allow remote-signed scripts for the current user first:
> ```powershell
> Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
> ```

---

### Manual installation (alternative)

If you prefer not to run a remote script, follow these steps instead.

#### 1. Copy the module files

Download the two module files and place them in a folder named `Convert-UnixTimestamp` inside your personal PowerShell modules directory.

On **Windows** the personal modules directory is typically:

```
C:\Users\<YourName>\Documents\PowerShell\Modules\Convert-UnixTimestamp\
```

On **Linux/macOS** it is typically:

```
~/.local/share/powershell/Modules/Convert-UnixTimestamp/
```

Files to download:

- [`Convert-UnixTimestamp.psd1`](https://raw.githubusercontent.com/steve-codemunkies/convert-ts/main/powershell/Convert-UnixTimestamp.psd1)
- [`Convert-UnixTimestamp.psm1`](https://raw.githubusercontent.com/steve-codemunkies/convert-ts/main/powershell/Convert-UnixTimestamp.psm1)

Or, using PowerShell:

```powershell
$dest = Join-Path ([System.Environment]::GetFolderPath('MyDocuments')) 'PowerShell' 'Modules' 'Convert-UnixTimestamp'
New-Item -ItemType Directory -Path $dest -Force | Out-Null
$base = 'https://raw.githubusercontent.com/steve-codemunkies/convert-ts/main/powershell'
Invoke-WebRequest "$base/Convert-UnixTimestamp.psd1" -OutFile (Join-Path $dest 'Convert-UnixTimestamp.psd1') -UseBasicParsing
Invoke-WebRequest "$base/Convert-UnixTimestamp.psm1" -OutFile (Join-Path $dest 'Convert-UnixTimestamp.psm1') -UseBasicParsing
```

#### 2. Import the module in your PowerShell profile

Add the following line to your `$PROFILE` so the module loads automatically in every new session:

```powershell
# Open (or create) your profile
if (-not (Test-Path $PROFILE)) { New-Item -ItemType File -Path $PROFILE -Force }
Add-Content -Path $PROFILE -Value "`nImport-Module Convert-UnixTimestamp"
```

#### 3. Verify the installation

```powershell
Get-Command -Module Convert-UnixTimestamp
```

Expected output:

```
CommandType  Name                       Version  Source
-----------  ----                       -------  ------
Function     ConvertFrom-UnixTimestamp  1.0.0    Convert-UnixTimestamp
Function     ConvertTo-UnixTimestamp    1.0.0    Convert-UnixTimestamp
```

---

## `ConvertFrom-UnixTimestamp`

Converts a Unix epoch timestamp (10-digit seconds **or** 13-digit milliseconds) to a `DateTimeOffset` in UTC.

### Parameters

| Parameter | Type | Required | Description |
|---|---|---|---|
| `Timestamp` | `[long]` | Yes | The Unix epoch timestamp to convert. |
| `BoundaryValue` | `[long]` | No | Override the threshold used to distinguish seconds from milliseconds. See [Boundary Value](#boundary-value) below. |

### Output properties

| Property | Type | Description |
|---|---|---|
| `DateTimeOffset` | `[DateTimeOffset]` | The converted UTC date/time. |
| `OriginalTimestamp` | `[long]` | The input value as supplied. |
| `SecondsTimestamp` | `[long]` | The 10-digit seconds variant. |
| `MillisecondsTimestamp` | `[long]` | The 13-digit milliseconds variant. |

### Examples

```powershell
# Convert a millisecond timestamp
ConvertFrom-UnixTimestamp -Timestamp 1700000000000
```

```
DateTimeOffset        : 14/11/2023 22:13:20 +00:00
OriginalTimestamp     : 1700000000000
SecondsTimestamp      : 1700000000
MillisecondsTimestamp : 1700000000000
```

```powershell
# Convert a seconds timestamp
ConvertFrom-UnixTimestamp -Timestamp 1700000000
```

```
DateTimeOffset        : 14/11/2023 22:13:20 +00:00
OriginalTimestamp     : 1700000000
SecondsTimestamp      : 1700000000
MillisecondsTimestamp : 1700000000000
```

```powershell
# Access individual properties
$result = ConvertFrom-UnixTimestamp -Timestamp 1700000000000
$result.DateTimeOffset          # [DateTimeOffset] 2023-11-14 22:13:20 +00:00
$result.SecondsTimestamp        # 1700000000
$result.MillisecondsTimestamp   # 1700000000000
```

```powershell
# Pipeline: convert multiple timestamps at once
1700000000, 1700000001, 1700000002 | ConvertFrom-UnixTimestamp | Select-Object OriginalTimestamp, DateTimeOffset
```

#### Working with other time zones

The returned `DateTimeOffset` is always UTC. To display or work with a different time zone:

```powershell
# Convert the UTC DateTimeOffset to a specific Windows/IANA time zone
$result = ConvertFrom-UnixTimestamp -Timestamp 1700000000000
$tz     = [System.TimeZoneInfo]::FindSystemTimeZoneById('Eastern Standard Time')   # Windows
# $tz  = [System.TimeZoneInfo]::FindSystemTimeZoneById('America/New_York')         # Linux/macOS (IANA)
$local  = [System.TimeZoneInfo]::ConvertTime($result.DateTimeOffset, $tz)
$local  # 14/11/2023 17:13:20 -05:00
```

```powershell
# Create a DateTimeOffset with an explicit UTC offset
$result    = ConvertFrom-UnixTimestamp -Timestamp 1700000000000
$offsetDto = $result.DateTimeOffset.ToOffset([System.TimeSpan]::FromHours(1))   # UTC+1
$offsetDto # 14/11/2023 23:13:20 +01:00
```

---

## `ConvertTo-UnixTimestamp`

Converts a date/time value to Unix epoch timestamps. All inputs are treated as UTC. The returned timestamps represent the number of seconds/milliseconds since 1970-01-01T00:00:00Z.

### Parameters

| Parameter | Type | Required | Parameter set | Description |
|---|---|---|---|---|
| `DateTime` | `[DateTime]` | Yes | `FromDateTime` | A `DateTime` value to convert. Treated as UTC regardless of its `Kind`. |
| `DateTimeOffset` | `[DateTimeOffset]` | Yes | `FromDateTimeOffset` | A `DateTimeOffset` value to convert. Converted to UTC automatically. |
| `DateTimeString` | `[string]` | Yes | `FromString` | A date/time string to parse and convert. |
| `Format` | `[string]` | No | `FromString` | An optional format string (e.g. `'yyyy-MM-dd HH:mm:ss'`). |

### Output properties

| Property | Type | Description |
|---|---|---|
| `DateTimeOffset` | `[DateTimeOffset]` | The input date/time as a UTC `DateTimeOffset`. |
| `SecondsTimestamp` | `[long]` | The 10-digit Unix seconds timestamp. |
| `MillisecondsTimestamp` | `[long]` | The 13-digit Unix milliseconds timestamp. |

### Examples

```powershell
# From a DateTime (treated as UTC)
$dt = [System.DateTime]::new(2023, 11, 14, 22, 13, 20, [System.DateTimeKind]::Utc)
ConvertTo-UnixTimestamp -DateTime $dt
```

```
DateTimeOffset        : 14/11/2023 22:13:20 +00:00
SecondsTimestamp      : 1700000000
MillisecondsTimestamp : 1700000000000
```

```powershell
# From a DateTimeOffset
$dto = [System.DateTimeOffset]::UtcNow
ConvertTo-UnixTimestamp -DateTimeOffset $dto
```

```powershell
# From an ISO 8601 string
ConvertTo-UnixTimestamp -DateTimeString '2023-11-14T22:13:20Z'
```

```powershell
# From a date-only string (time defaults to midnight UTC — 00:00:00)
ConvertTo-UnixTimestamp -DateTimeString '2023-11-14'
```

```powershell
# From a string with an explicit format
ConvertTo-UnixTimestamp -DateTimeString '14/11/2023 22:13:20' -Format 'dd/MM/yyyy HH:mm:ss'
```

```powershell
# Use Get-Date to get the current time
ConvertTo-UnixTimestamp -DateTime (Get-Date).ToUniversalTime()
```

#### Working with other time zones

When you have a local time in a known time zone, create a `DateTimeOffset` with the correct offset before passing it to `ConvertTo-UnixTimestamp`:

```powershell
# A time expressed in Eastern Standard Time (UTC-5)
$tz     = [System.TimeZoneInfo]::FindSystemTimeZoneById('Eastern Standard Time')  # Windows
# $tz  = [System.TimeZoneInfo]::FindSystemTimeZoneById('America/New_York')        # Linux/macOS
$local  = [System.DateTime]::new(2023, 11, 14, 17, 13, 20)   # local wall-clock time
$offset = $tz.GetUtcOffset($local)
$dto    = [System.DateTimeOffset]::new($local, $offset)

ConvertTo-UnixTimestamp -DateTimeOffset $dto
```

```
DateTimeOffset        : 14/11/2023 22:13:20 +00:00
SecondsTimestamp      : 1700000000
MillisecondsTimestamp : 1700000000000
```

---

## Boundary Value

The module uses a boundary value to automatically determine whether a timestamp is in **seconds** or **milliseconds**:

- If the timestamp is **greater than** the boundary → treated as **milliseconds**
- If the timestamp is **less than or equal to** the boundary → treated as **seconds**

The default boundary is **32,503,680,000**, which corresponds to **Wednesday, 1 January 3000 00:00:00 UTC** expressed as a seconds timestamp — a date safely beyond any real-world seconds timestamp in use today.

### Override via command-line parameter

```powershell
ConvertFrom-UnixTimestamp -Timestamp 10000000000 -BoundaryValue 9999999999
```

### Override via environment variable

Set the `UNIX_TIMESTAMP_BOUNDARY` environment variable before calling the function. The command-line parameter takes precedence over the environment variable.

```powershell
# For the current session
$env:UNIX_TIMESTAMP_BOUNDARY = '9999999999'
ConvertFrom-UnixTimestamp -Timestamp 10000000000

# Persistently (Windows — user scope)
[System.Environment]::SetEnvironmentVariable('UNIX_TIMESTAMP_BOUNDARY', '9999999999', 'User')

# Persistently (Linux/macOS — add to ~/.profile or ~/.bashrc)
# export UNIX_TIMESTAMP_BOUNDARY=9999999999
```

---

## Running the Tests

The module ships with a [Pester](https://pester.dev/) v5 test suite.

```powershell
# Install Pester v5 if not already installed
Install-Module Pester -Force -SkipPublisherCheck

# Run the tests directly from GitHub (no clone required)
$testUrl = 'https://raw.githubusercontent.com/steve-codemunkies/convert-ts/main/powershell/Tests/Convert-UnixTimestamp.Tests.ps1'
$tmp = New-TemporaryFile | Rename-Item -NewName { $_.Name -replace '\.tmp$', '.Tests.ps1' } -PassThru
Invoke-WebRequest $testUrl -OutFile $tmp.FullName -UseBasicParsing
Invoke-Pester -Path $tmp.FullName -Output Detailed

# Or, if you have the repository cloned locally, run from the repo root:
Invoke-Pester -Path './powershell/Tests/Convert-UnixTimestamp.Tests.ps1' -Output Detailed
```
