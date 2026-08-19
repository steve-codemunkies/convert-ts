# Convert-UnixTimestamp — Bash Functions

Bash functions for converting between Unix epoch timestamps and human-readable date/time values.

| Function | Purpose |
|---|---|
| `convertfrom_unixtimestamp` | Convert a Unix epoch timestamp to a date/time string |
| `convertto_unixtimestamp` | Convert a date/time string to a Unix epoch timestamp |

---

## Installation

### Quick install (one-liner)

No need to clone the repository or copy files manually. Open a terminal and run one of the commands below.

**Install script files only** (you can then add a `source` line to your profile manually):

```bash
bash <(curl -fsSL https://raw.githubusercontent.com/steve-codemunkies/convert-ts/main/bash/install.sh)
```

**Install and automatically add to your shell profile** (functions available in every new session):

```bash
bash <(curl -fsSL https://raw.githubusercontent.com/steve-codemunkies/convert-ts/main/bash/install.sh) --add-to-profile
```

After installation, reload your profile to start using the functions immediately:

```bash
source ~/.bashrc   # or ~/.bash_profile / ~/.zshrc depending on your shell
```

> **Install from a specific branch** (useful for testing a pre-release version):
> ```bash
> bash <(curl -fsSL https://raw.githubusercontent.com/steve-codemunkies/convert-ts/main/bash/install.sh) --branch develop --add-to-profile
> ```

---

### Manual installation (alternative)

If you prefer not to run a remote script, follow these steps.

#### 1. Download the script

```bash
mkdir -p ~/.local/share/convert-ts
curl -fsSL https://raw.githubusercontent.com/steve-codemunkies/convert-ts/main/bash/convert-ts.sh \
     -o ~/.local/share/convert-ts/convert-ts.sh
```

#### 2. Add to your shell profile

Open your shell profile (`~/.bashrc`, `~/.bash_profile`, or `~/.zshrc`) and add:

```bash
# convert-ts — Unix timestamp conversion functions
source "$HOME/.local/share/convert-ts/convert-ts.sh"
```

#### 3. Reload your profile

```bash
source ~/.bashrc
```

---

## Functions

### `convertfrom_unixtimestamp`

Converts a Unix epoch timestamp to a human-readable date/time string.

Automatically detects whether the input is a seconds (10-digit) or milliseconds (13-digit) value
by comparing it against a configurable boundary value (default: **32,503,680,000**, which equals
Wednesday, 1 January 3000 00:00:00 UTC expressed as a Unix seconds timestamp).

**Syntax**

```
convertfrom_unixtimestamp [-f format] [-b boundary] <timestamp>
```

**Options**

| Option | Description |
|---|---|
| `-f <format>` | `strftime`-compatible output format. Default: `%Y-%m-%dT%H:%M:%SZ` (ISO 8601 UTC) |
| `-b <boundary>` | Override the ms/s boundary value for this call |

**Environment variables**

| Variable | Description |
|---|---|
| `CONVERT_TS_BOUNDARY` | Override the default boundary value for all calls in the session |

**Precedence for boundary**: `-b` flag → `CONVERT_TS_BOUNDARY` env var → built-in default (`32503680000`).

**Output**

A date/time string written to stdout. The output can be piped to other commands.

**Examples**

```bash
# Millisecond timestamp (auto-detected)
convertfrom_unixtimestamp 1700000000000
# => 2023-11-14T22:13:20Z

# Seconds timestamp (auto-detected)
convertfrom_unixtimestamp 1700000000
# => 2023-11-14T22:13:20Z

# Custom output format
convertfrom_unixtimestamp -f "%d %B %Y %H:%M:%S" 1700000000000
# => 14 November 2023 22:13:20

# Custom boundary value at the command line
convertfrom_unixtimestamp -b 9999999999 10000000000

# Custom boundary value via environment variable
CONVERT_TS_BOUNDARY=9999999999 convertfrom_unixtimestamp 10000000000

# Pipe the output to another command
convertfrom_unixtimestamp 1700000000000 | xargs echo "Timestamp is:"
```

**Working with timezones**

By default all output is in UTC. To work in a different timezone, prefix the call with `TZ=`:

```bash
# Output in US Eastern time
TZ=America/New_York convertfrom_unixtimestamp -f "%Y-%m-%dT%H:%M:%S%z" 1700000000000
# => 2023-11-14T17:13:20-0500

# Output in UK time
TZ=Europe/London convertfrom_unixtimestamp -f "%Y-%m-%dT%H:%M:%S%z" 1700000000000
```

> **Note**: when using a `TZ=` override, omit the literal `Z` suffix from your format string
> and use `%z` or `%Z` instead so the timezone offset is shown correctly.

---

### `convertto_unixtimestamp`

Converts a date/time string to a Unix epoch timestamp. Returns **milliseconds** by default.

**Syntax**

```
convertto_unixtimestamp [-f format] [-s] <date_string>
```

**Options**

| Option | Description |
|---|---|
| `-f <format>` | `strftime`-compatible input format. Default: `%Y-%m-%dT%H:%M:%SZ` (ISO 8601 UTC) |
| `-s` | Output a **seconds** (10-digit) timestamp instead of milliseconds |

**Output**

A Unix epoch timestamp written to stdout. The output can be piped to other commands.

**Examples**

```bash
# Default (milliseconds output)
convertto_unixtimestamp "2023-11-14T22:13:20Z"
# => 1700000000000

# Seconds output
convertto_unixtimestamp -s "2023-11-14T22:13:20Z"
# => 1700000000

# Custom input format
convertto_unixtimestamp -f "%d %B %Y %H:%M:%S" "14 November 2023 22:13:20"
# => 1700000000000

# Custom input format, seconds output
convertto_unixtimestamp -f "%d %B %Y %H:%M:%S" -s "14 November 2023 22:13:20"
# => 1700000000

# Pipe the output to another command
convertto_unixtimestamp "2023-11-14T22:13:20Z" | xargs echo "Epoch ms:"
```

**Working with timezones**

By default the input is interpreted as UTC. To parse a date/time in a different timezone,
prefix the call with `TZ=`:

```bash
# Parse a US Eastern date/time (strip the trailing Z from the default format)
TZ=America/New_York convertto_unixtimestamp -f "%Y-%m-%dT%H:%M:%S" "2023-11-14T17:13:20"
# => 1700000000000

# Parse a UK date/time
TZ=Europe/London convertto_unixtimestamp -f "%Y-%m-%dT%H:%M:%S" "2023-11-14T22:13:20"
# => 1700000000000
```

---

## Running as a script

`convert-ts.sh` can also be executed directly (without sourcing) using a `from` or `to` sub-command:

```bash
bash convert-ts.sh from 1700000000000
# => 2023-11-14T22:13:20Z

bash convert-ts.sh from -f "%d %B %Y" 1700000000000
# => 14 November 2023

bash convert-ts.sh to "2023-11-14T22:13:20Z"
# => 1700000000000

bash convert-ts.sh to -s "2023-11-14T22:13:20Z"
# => 1700000000
```

---

## Compatibility notes

The functions work on both **GNU date** (Linux, WSL) and **BSD date** (macOS).

- **GNU date** (`-d` flag) is used when `date --version` succeeds (Linux / WSL).
- **BSD date** (`-j -f` flags) is used as a fallback (macOS).

If you encounter unexpected output, verify which `date` implementation is active:

```bash
date --version 2>/dev/null && echo "GNU date" || echo "BSD date"
```

---

## Automated tests

Tests are written using [bats-core](https://github.com/bats-core/bats-core) and live in `bash/tests/`.

Install bats and run the tests:

```bash
npm install -g bats   # or: brew install bats-core
bats bash/tests/convert-ts.bats
```
