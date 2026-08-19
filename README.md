# convert-ts
Utilities for working with non-human timestamps

## PowerShell

A PowerShell module providing functions to convert between Unix epoch timestamps and human-readable date/time values.

### Installation

Install the module directly from GitHub with a single command (no git or manual file copying required):

```powershell
irm https://raw.githubusercontent.com/steve-codemunkies/convert-ts/main/powershell/install.ps1 | iex
```

To install and automatically add the module to your PowerShell profile so it loads in every new session:

```powershell
& ([scriptblock]::Create((irm https://raw.githubusercontent.com/steve-codemunkies/convert-ts/main/powershell/install.ps1))) -AddToProfile
```

### Functions

#### `ConvertFrom-UnixTimestamp`

Converts a Unix epoch timestamp (seconds or milliseconds) to a `DateTimeOffset` (UTC). Automatically detects whether the input is a 10-digit seconds value or a 13-digit milliseconds value using a configurable boundary.

#### `ConvertTo-UnixTimestamp`

Converts a `DateTime`, `DateTimeOffset`, or date/time string to Unix epoch timestamps in both seconds and milliseconds.

### Documentation

Full parameter reference and examples are available in [docs/powershell/Convert-UnixTimestamp.md](docs/powershell/Convert-UnixTimestamp.md).

---

## Bash

Bash functions for converting between Unix epoch timestamps and human-readable date/time values. Works on Linux and WSL (Windows Subsystem for Linux).

### Installation

Install the functions directly from GitHub with a single command (no git or manual file copying required):

```bash
bash <(curl -fsSL https://raw.githubusercontent.com/steve-codemunkies/convert-ts/main/bash/install.sh)
```

To install and automatically add the functions to your shell profile so they load in every new session:

```bash
bash <(curl -fsSL https://raw.githubusercontent.com/steve-codemunkies/convert-ts/main/bash/install.sh) --add-to-profile
```

### Functions

#### `convertfrom_unixtimestamp`

Converts a Unix epoch timestamp (seconds or milliseconds) to a date/time string (UTC by default). Automatically detects whether the input is a seconds or milliseconds value using a configurable boundary.

#### `convertto_unixtimestamp`

Converts a date/time string to a Unix epoch timestamp. Returns milliseconds by default; use the `-s` flag for seconds.

### Documentation

Full parameter reference and examples are available in [docs/bash/Convert-UnixTimestamp.md](docs/bash/Convert-UnixTimestamp.md).
