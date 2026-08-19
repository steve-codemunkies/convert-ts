#!/usr/bin/env bash
# convert-ts.sh — Functions for converting between Unix epoch timestamps
#                 and human-readable date/time values.
#
# Usage (sourced in a bash profile):
#   source /path/to/convert-ts.sh
#   convertfrom_unixtimestamp 1700000000000
#   convertto_unixtimestamp "2023-11-14T22:13:20Z"
#
# Usage (run directly as a script):
#   bash convert-ts.sh from 1700000000000
#   bash convert-ts.sh to   "2023-11-14T22:13:20Z"
#
# Compatibility:
#   Tested on GNU/Linux (GNU date) and macOS/WSL (BSD date).
#   The script detects which date implementation is available and adjusts
#   its behaviour accordingly.  See the _convert_ts_date_parse helper below.

set -euo pipefail

# ---------------------------------------------------------------------------
# Constants
# ---------------------------------------------------------------------------

# Default boundary value: 32,503,680,000
# Any timestamp GREATER than this is treated as milliseconds; otherwise seconds.
# This value equals Wednesday, 1 January 3000 00:00:00 UTC expressed as Unix
# seconds — safely beyond any real-world seconds timestamp in current use.
readonly _CONVERT_TS_DEFAULT_BOUNDARY=32503680000

# Default input/output format (ISO 8601 UTC).
readonly _CONVERT_TS_DEFAULT_FORMAT='%Y-%m-%dT%H:%M:%SZ'

# ---------------------------------------------------------------------------
# Internal helpers
# ---------------------------------------------------------------------------

# _convert_ts_resolve_boundary [explicit_value]
#   Returns the boundary to use:
#     1. Explicit value passed as $1 (non-empty)
#     2. CONVERT_TS_BOUNDARY environment variable (if set and non-empty)
#     3. Default: _CONVERT_TS_DEFAULT_BOUNDARY
_convert_ts_resolve_boundary() {
    local explicit="${1:-}"
    if [[ -n "$explicit" ]]; then
        echo "$explicit"
    elif [[ -n "${CONVERT_TS_BOUNDARY:-}" ]]; then
        echo "$CONVERT_TS_BOUNDARY"
    else
        echo "$_CONVERT_TS_DEFAULT_BOUNDARY"
    fi
}

# _convert_ts_date_to_epoch <format> <date_string>
#   Converts a date/time string to a Unix epoch (seconds) using either
#   GNU date (-d flag) or BSD date (-j -f flags).
#   Outputs an error to stderr and returns 1 on failure.
_convert_ts_date_to_epoch() {
    local fmt="$1"
    local dt="$2"
    local epoch

    # Detect GNU date vs BSD date.
    # GNU date: supports -d <string> and --version
    # BSD date: supports -j (no set) and -f <format>
    if date --version >/dev/null 2>&1; then
        # GNU date (Linux / WSL)
        # Strip a trailing 'Z' from the format/input and force UTC via TZ.
        local clean_fmt="${fmt/%Z/}"   # remove trailing Z from format token %...Z if present
        local clean_dt="${dt/%Z/}"     # remove trailing literal Z from date string if present
        # Replace %Z in format with nothing (it is a timezone designator, not needed for -d)
        clean_fmt="${clean_fmt//%Z/}"
        epoch=$(TZ=UTC date -d "${clean_dt}" +"${fmt//%Z/}" 2>/dev/null) || {
            # Try with explicit format via intermediate conversion
            # GNU date -d can parse ISO 8601 natively; for custom formats we
            # use the format as a hint only and pass the raw string.
            epoch=$(TZ=UTC date -d "${clean_dt}" +%s 2>/dev/null) || {
                echo "convert-ts: unable to parse date string '${dt}' with GNU date" >&2
                return 1
            }
        }
        # Re-run cleanly to get the epoch seconds
        epoch=$(TZ=UTC date -d "${clean_dt}" +%s 2>/dev/null) || {
            echo "convert-ts: unable to parse date string '${dt}' with GNU date" >&2
            return 1
        }
    else
        # BSD date (macOS)
        # BSD date requires -f <input_format> -j <date_string>
        # Strip trailing Z from format for BSD date (it uses %Z differently)
        local bsd_fmt="${fmt//%Z/}"
        local bsd_dt="${dt/%Z/}"     # remove trailing literal Z
        epoch=$(TZ=UTC date -j -f "${bsd_fmt}" "${bsd_dt}" +%s 2>/dev/null) || {
            echo "convert-ts: unable to parse date string '${dt}' with BSD date" >&2
            return 1
        }
    fi
    echo "$epoch"
}

# _convert_ts_epoch_to_date <format> <epoch_seconds>
#   Formats a Unix epoch (seconds) as a date string using the given strftime format.
#   Works with both GNU date and BSD date.
_convert_ts_epoch_to_date() {
    local fmt="$1"
    local epoch_s="$2"
    TZ=UTC date -d "@${epoch_s}" +"${fmt}" 2>/dev/null || \
    TZ=UTC date -r "${epoch_s}"  +"${fmt}" 2>/dev/null || {
        echo "convert-ts: unable to format epoch '${epoch_s}'" >&2
        return 1
    }
}

# ---------------------------------------------------------------------------
# Public functions
# ---------------------------------------------------------------------------

# convertfrom_unixtimestamp [options] <timestamp>
#
#   Converts a Unix epoch timestamp to a human-readable date/time string.
#
#   Options:
#     -f <format>    strftime output format (default: %Y-%m-%dT%H:%M:%SZ)
#     -b <boundary>  boundary value for ms/s detection
#                    (default: CONVERT_TS_BOUNDARY env var, then 32503680000)
#
#   Arguments:
#     <timestamp>    Unix epoch timestamp (seconds or milliseconds)
#
#   Output:
#     Date/time string written to stdout — suitable for piping.
#
#   Environment:
#     CONVERT_TS_BOUNDARY   Override the default boundary value.
#
#   Examples:
#     # Millisecond timestamp (auto-detected)
#     convertfrom_unixtimestamp 1700000000000
#     # => 2023-11-14T22:13:20Z
#
#     # Seconds timestamp (auto-detected)
#     convertfrom_unixtimestamp 1700000000
#     # => 2023-11-14T22:13:20Z
#
#     # Custom output format
#     convertfrom_unixtimestamp -f "%d %B %Y %H:%M:%S" 1700000000000
#     # => 14 November 2023 22:13:20
#
#     # Custom boundary value
#     convertfrom_unixtimestamp -b 9999999999 10000000000
#
#     # Override boundary via environment variable
#     CONVERT_TS_BOUNDARY=9999999999 convertfrom_unixtimestamp 10000000000
#
#     # Use a different timezone (output is formatted in the given TZ):
#     TZ=America/New_York convertfrom_unixtimestamp 1700000000000
#     # NOTE: omit the trailing Z in the format when overriding TZ, e.g.:
#     TZ=America/New_York convertfrom_unixtimestamp -f "%Y-%m-%dT%H:%M:%S%z" 1700000000000
convertfrom_unixtimestamp() {
    local fmt="$_CONVERT_TS_DEFAULT_FORMAT"
    local boundary_override=""

    # Parse options
    local OPTIND opt
    while getopts ":f:b:" opt; do
        case "$opt" in
            f) fmt="$OPTARG" ;;
            b) boundary_override="$OPTARG" ;;
            :) echo "convertfrom_unixtimestamp: option -${OPTARG} requires an argument" >&2; return 1 ;;
            \?) echo "convertfrom_unixtimestamp: unknown option -${OPTARG}" >&2; return 1 ;;
        esac
    done
    shift $((OPTIND - 1))

    if [[ $# -lt 1 ]]; then
        echo "convertfrom_unixtimestamp: timestamp argument required" >&2
        echo "Usage: convertfrom_unixtimestamp [-f format] [-b boundary] <timestamp>" >&2
        return 1
    fi

    local ts="$1"
    local boundary
    boundary=$(_convert_ts_resolve_boundary "$boundary_override")

    # Determine seconds vs milliseconds.
    # Arithmetic comparison: if ts > boundary treat as milliseconds.
    local epoch_s
    if (( ts > boundary )); then
        # Milliseconds — divide by 1000 (integer division is fine here)
        epoch_s=$(( ts / 1000 ))
    else
        epoch_s="$ts"
    fi

    _convert_ts_epoch_to_date "$fmt" "$epoch_s"
}

# convertto_unixtimestamp [options] <date_string>
#
#   Converts a date/time string to a Unix epoch timestamp.
#
#   Options:
#     -f <format>    strftime input format (default: %Y-%m-%dT%H:%M:%SZ)
#     -s             output a seconds (10-digit) timestamp instead of milliseconds
#
#   Arguments:
#     <date_string>  Date/time string to convert
#
#   Output:
#     Millisecond timestamp (or seconds timestamp with -s) written to stdout.
#
#   Examples:
#     # Default (milliseconds output)
#     convertto_unixtimestamp "2023-11-14T22:13:20Z"
#     # => 1700000000000
#
#     # Seconds output
#     convertto_unixtimestamp -s "2023-11-14T22:13:20Z"
#     # => 1700000000
#
#     # Custom input format
#     convertto_unixtimestamp -f "%d %B %Y %H:%M:%S" "14 November 2023 22:13:20"
#     # => 1700000000000
#
#     # Use a different input timezone:
#     TZ=America/New_York convertto_unixtimestamp -f "%Y-%m-%dT%H:%M:%S" "2023-11-14T17:13:20"
#     # => 1700000000000
convertto_unixtimestamp() {
    local fmt="$_CONVERT_TS_DEFAULT_FORMAT"
    local seconds_mode=0

    # Parse options
    local OPTIND opt
    while getopts ":f:s" opt; do
        case "$opt" in
            f) fmt="$OPTARG" ;;
            s) seconds_mode=1 ;;
            :) echo "convertto_unixtimestamp: option -${OPTARG} requires an argument" >&2; return 1 ;;
            \?) echo "convertto_unixtimestamp: unknown option -${OPTARG}" >&2; return 1 ;;
        esac
    done
    shift $((OPTIND - 1))

    if [[ $# -lt 1 ]]; then
        echo "convertto_unixtimestamp: date string argument required" >&2
        echo "Usage: convertto_unixtimestamp [-f format] [-s] <date_string>" >&2
        return 1
    fi

    local dt="$1"
    local epoch_s
    epoch_s=$(_convert_ts_date_to_epoch "$fmt" "$dt")

    if [[ "$seconds_mode" -eq 1 ]]; then
        echo "$epoch_s"
    else
        echo $(( epoch_s * 1000 ))
    fi
}

# ---------------------------------------------------------------------------
# Script mode — allow running directly: bash convert-ts.sh <command> [args...]
# ---------------------------------------------------------------------------
# When this file is executed (not sourced), dispatch to the right function.

if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    if [[ $# -lt 1 ]]; then
        echo "Usage: $(basename "$0") from|to [options] <value>" >&2
        exit 1
    fi
    cmd="$1"; shift
    case "$cmd" in
        from) convertfrom_unixtimestamp "$@" ;;
        to)   convertto_unixtimestamp   "$@" ;;
        *)
            echo "Unknown command: '${cmd}'. Use 'from' or 'to'." >&2
            exit 1
            ;;
    esac
fi
