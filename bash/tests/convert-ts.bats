#!/usr/bin/env bats
# convert-ts.bats — Automated tests for convert-ts.sh
#
# Run with: bats bash/tests/convert-ts.bats
# Requires: bats-core (https://github.com/bats-core/bats-core)
#   Install: npm install -g bats
#            or: brew install bats-core
#            or: sudo apt-get install bats

SCRIPT_DIR="$(cd "$(dirname "${BATS_TEST_FILENAME}")/.." && pwd)"
SCRIPT="${SCRIPT_DIR}/convert-ts.sh"

# ---------------------------------------------------------------------------
# convertfrom_unixtimestamp
# ---------------------------------------------------------------------------

@test "convertfrom_unixtimestamp: millisecond timestamp auto-detected and converted" {
    # 1700000000000 ms = 2023-11-14T22:13:20Z
    run bash -c "source '${SCRIPT}'; convertfrom_unixtimestamp 1700000000000"
    [ "$status" -eq 0 ]
    [ "$output" = "2023-11-14T22:13:20Z" ]
}

@test "convertfrom_unixtimestamp: seconds timestamp auto-detected and converted" {
    # 1700000000 s = 2023-11-14T22:13:20Z
    run bash -c "source '${SCRIPT}'; convertfrom_unixtimestamp 1700000000"
    [ "$status" -eq 0 ]
    [ "$output" = "2023-11-14T22:13:20Z" ]
}

@test "convertfrom_unixtimestamp: ms and seconds timestamps produce the same result" {
    run bash -c "source '${SCRIPT}'
        a=\$(convertfrom_unixtimestamp 1700000000000)
        b=\$(convertfrom_unixtimestamp 1700000000)
        [ \"\$a\" = \"\$b\" ] && echo same"
    [ "$status" -eq 0 ]
    [ "$output" = "same" ]
}

@test "convertfrom_unixtimestamp: boundary value — value equal to boundary treated as seconds" {
    # 32503680000 equals the default boundary → seconds
    run bash -c "source '${SCRIPT}'; convertfrom_unixtimestamp 32503680000"
    [ "$status" -eq 0 ]
    # 32503680000 s = 3000-01-01T00:00:00Z
    [ "$output" = "3000-01-01T00:00:00Z" ]
}

@test "convertfrom_unixtimestamp: boundary value — value one above boundary treated as milliseconds" {
    # 32503680001 is > default boundary → milliseconds
    # 32503680001 ms / 1000 = 32503680 s (integer division) = 1971-01-12T04:48:00Z
    run bash -c "source '${SCRIPT}'; convertfrom_unixtimestamp 32503680001"
    [ "$status" -eq 0 ]
    [ "$output" = "1971-01-12T04:48:00Z" ]
}

@test "convertfrom_unixtimestamp: custom boundary via -b flag" {
    # With boundary=9999999999: value 10000000000 is > boundary → milliseconds
    # 10000000000 ms / 1000 = 10000000 s = 1970-04-26T17:46:40Z
    run bash -c "source '${SCRIPT}'; convertfrom_unixtimestamp -b 9999999999 10000000000"
    [ "$status" -eq 0 ]
    [ "$output" = "1970-04-26T17:46:40Z" ]
}

@test "convertfrom_unixtimestamp: custom boundary via CONVERT_TS_BOUNDARY env var" {
    # 10000000000 ms / 1000 = 10000000 s = 1970-04-26T17:46:40Z
    run bash -c "source '${SCRIPT}'; CONVERT_TS_BOUNDARY=9999999999 convertfrom_unixtimestamp 10000000000"
    [ "$status" -eq 0 ]
    [ "$output" = "1970-04-26T17:46:40Z" ]
}

@test "convertfrom_unixtimestamp: -b flag overrides CONVERT_TS_BOUNDARY env var" {
    # -b 5000000000: value 6000000000 > boundary → milliseconds
    # 6000000000 ms / 1000 = 6000000 s = 1970-03-11T10:40:00Z
    run bash -c "source '${SCRIPT}'; CONVERT_TS_BOUNDARY=99999999999 convertfrom_unixtimestamp -b 5000000000 6000000000"
    [ "$status" -eq 0 ]
    [ "$output" = "1970-03-11T10:40:00Z" ]
}

@test "convertfrom_unixtimestamp: custom output format via -f flag" {
    run bash -c "source '${SCRIPT}'; convertfrom_unixtimestamp -f '%d %B %Y' 1700000000000"
    [ "$status" -eq 0 ]
    [ "$output" = "14 November 2023" ]
}

@test "convertfrom_unixtimestamp: output is pipeable" {
    run bash -c "source '${SCRIPT}'; convertfrom_unixtimestamp 1700000000000 | tr -d '\n'"
    [ "$status" -eq 0 ]
    [ "$output" = "2023-11-14T22:13:20Z" ]
}

@test "convertfrom_unixtimestamp: missing argument prints error and returns non-zero" {
    run bash -c "source '${SCRIPT}'; convertfrom_unixtimestamp"
    [ "$status" -ne 0 ]
    [[ "$output" =~ "timestamp argument required" ]]
}

@test "convertfrom_unixtimestamp: script mode (bash convert-ts.sh from ...)" {
    run bash "${SCRIPT}" from 1700000000000
    [ "$status" -eq 0 ]
    [ "$output" = "2023-11-14T22:13:20Z" ]
}

# ---------------------------------------------------------------------------
# convertto_unixtimestamp
# ---------------------------------------------------------------------------

@test "convertto_unixtimestamp: ISO 8601 string → milliseconds (default)" {
    run bash -c "source '${SCRIPT}'; convertto_unixtimestamp '2023-11-14T22:13:20Z'"
    [ "$status" -eq 0 ]
    [ "$output" = "1700000000000" ]
}

@test "convertto_unixtimestamp: ISO 8601 string → seconds with -s flag" {
    run bash -c "source '${SCRIPT}'; convertto_unixtimestamp -s '2023-11-14T22:13:20Z'"
    [ "$status" -eq 0 ]
    [ "$output" = "1700000000" ]
}

@test "convertto_unixtimestamp: custom input format via -f flag" {
    run bash -c "source '${SCRIPT}'; convertto_unixtimestamp -f '%d %B %Y %H:%M:%S' '14 November 2023 22:13:20'"
    [ "$status" -eq 0 ]
    [ "$output" = "1700000000000" ]
}

@test "convertto_unixtimestamp: custom format with -s flag returns seconds" {
    run bash -c "source '${SCRIPT}'; convertto_unixtimestamp -f '%d %B %Y %H:%M:%S' -s '14 November 2023 22:13:20'"
    [ "$status" -eq 0 ]
    [ "$output" = "1700000000" ]
}

@test "convertto_unixtimestamp: output is pipeable" {
    run bash -c "source '${SCRIPT}'; convertto_unixtimestamp '2023-11-14T22:13:20Z' | tr -d '\n'"
    [ "$status" -eq 0 ]
    [ "$output" = "1700000000000" ]
}

@test "convertto_unixtimestamp: missing argument prints error and returns non-zero" {
    run bash -c "source '${SCRIPT}'; convertto_unixtimestamp"
    [ "$status" -ne 0 ]
    [[ "$output" =~ "date string argument required" ]]
}

@test "convertto_unixtimestamp: script mode (bash convert-ts.sh to ...)" {
    run bash "${SCRIPT}" to "2023-11-14T22:13:20Z"
    [ "$status" -eq 0 ]
    [ "$output" = "1700000000000" ]
}

# ---------------------------------------------------------------------------
# Round-trip
# ---------------------------------------------------------------------------

@test "round-trip: from-timestamp result feeds into to-timestamp" {
    run bash -c "
        source '${SCRIPT}'
        dt=\$(convertfrom_unixtimestamp 1700000000000)
        ts=\$(convertto_unixtimestamp \"\$dt\")
        echo \"\$ts\"
    "
    [ "$status" -eq 0 ]
    [ "$output" = "1700000000000" ]
}

@test "round-trip: to-timestamp result feeds into from-timestamp" {
    run bash -c "
        source '${SCRIPT}'
        ts=\$(convertto_unixtimestamp '2023-11-14T22:13:20Z')
        dt=\$(convertfrom_unixtimestamp \"\$ts\")
        echo \"\$dt\"
    "
    [ "$status" -eq 0 ]
    [ "$output" = "2023-11-14T22:13:20Z" ]
}

# ---------------------------------------------------------------------------
# Script mode — unknown command
# ---------------------------------------------------------------------------

@test "script mode: unknown command returns non-zero" {
    run bash "${SCRIPT}" unknown
    [ "$status" -ne 0 ]
}

@test "script mode: no arguments returns non-zero" {
    run bash "${SCRIPT}"
    [ "$status" -ne 0 ]
}
