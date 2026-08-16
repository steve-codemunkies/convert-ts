<#
.SYNOPSIS
    Functions for converting between Unix epoch timestamps and human-readable date/time values.

.DESCRIPTION
    This module provides two functions:
      - ConvertFrom-UnixTimestamp: converts a Unix epoch timestamp (seconds or milliseconds) to a DateTimeOffset (UTC).
      - ConvertTo-UnixTimestamp:   converts a DateTime, DateTimeOffset, or date/time string to Unix epoch timestamps.

    The boundary value (default 32,503,680,000) is used to distinguish 10-digit second timestamps from
    13-digit millisecond timestamps. Values greater than the boundary are treated as milliseconds.
#>

Set-StrictMode -Version Latest

[long] $script:DefaultBoundaryValue = 32503680000L

function ConvertFrom-UnixTimestamp {
    <#
    .SYNOPSIS
        Converts a Unix epoch timestamp to a DateTimeOffset (UTC).

    .DESCRIPTION
        Accepts a Unix epoch timestamp as either a 10-digit (seconds) or 13-digit (milliseconds) value.
        The distinction is made by comparing the timestamp against a boundary value (default: 32,503,680,000,
        which represents 2001-01-01T00:00:00Z in seconds but more usefully represents the year 3000 in seconds).

        The boundary value can be overridden via the -BoundaryValue parameter or the
        UNIX_TIMESTAMP_BOUNDARY environment variable.

    .PARAMETER Timestamp
        The Unix epoch timestamp to convert. Can be a 10-digit (seconds) or 13-digit (milliseconds) value.

    .PARAMETER BoundaryValue
        Optional. A custom boundary value to distinguish seconds from milliseconds timestamps.
        If not provided, the UNIX_TIMESTAMP_BOUNDARY environment variable is checked, then the
        default value of 32,503,680,000 is used.

    .OUTPUTS
        PSCustomObject with the following properties:
          - DateTimeOffset        [DateTimeOffset] The converted UTC date/time.
          - OriginalTimestamp     [long]           The original input timestamp.
          - SecondsTimestamp      [long]           The 10-digit seconds variant.
          - MillisecondsTimestamp [long]           The 13-digit milliseconds variant.

    .EXAMPLE
        ConvertFrom-UnixTimestamp -Timestamp 1700000000000

        Converts a millisecond timestamp.

    .EXAMPLE
        ConvertFrom-UnixTimestamp -Timestamp 1700000000

        Converts a seconds timestamp.

    .EXAMPLE
        ConvertFrom-UnixTimestamp -Timestamp 1700000000 -BoundaryValue 9999999999

        Converts using a custom boundary value supplied at the command line.

    .EXAMPLE
        $env:UNIX_TIMESTAMP_BOUNDARY = '9999999999'
        ConvertFrom-UnixTimestamp -Timestamp 1700000000

        Converts using a custom boundary value supplied via an environment variable.
    #>
    [CmdletBinding()]
    [OutputType([PSCustomObject])]
    param(
        [Parameter(Mandatory, Position = 0, ValueFromPipeline)]
        [long] $Timestamp,

        [Parameter()]
        [Nullable[long]] $BoundaryValue = $null
    )

    process {
        $boundary = Resolve-BoundaryValue -BoundaryValue $BoundaryValue

        [long] $seconds      = 0
        [long] $milliseconds = 0

        if ($Timestamp -gt $boundary) {
            # Milliseconds variant
            $milliseconds = $Timestamp
            $seconds      = [Math]::DivRem($milliseconds, 1000L, [ref] $null)
        }
        else {
            # Seconds variant
            $seconds      = $Timestamp
            $milliseconds = $seconds * 1000L
        }

        $epoch = [System.DateTimeOffset]::new(1970, 1, 1, 0, 0, 0, [System.TimeSpan]::Zero)
        $dto   = $epoch.AddMilliseconds($milliseconds)

        [PSCustomObject] @{
            DateTimeOffset        = $dto
            OriginalTimestamp     = $Timestamp
            SecondsTimestamp      = $seconds
            MillisecondsTimestamp = $milliseconds
        }
    }
}

function ConvertTo-UnixTimestamp {
    <#
    .SYNOPSIS
        Converts a date/time value to Unix epoch timestamps.

    .DESCRIPTION
        Accepts a DateTime, DateTimeOffset, or a parseable date/time string and returns the
        corresponding Unix epoch timestamps in both seconds and milliseconds.

        DateTime inputs are treated as UTC. DateTimeOffset inputs preserve their offset when
        calculating the epoch delta. String inputs are parsed as UTC.

        If a string contains only a date (no time), midnight (00:00:00) UTC is assumed.

    .PARAMETER DateTime
        A [DateTime] value to convert.

    .PARAMETER DateTimeOffset
        A [DateTimeOffset] value to convert.

    .PARAMETER DateTimeString
        A date/time string to parse and convert.

    .PARAMETER Format
        An optional format string (e.g. 'yyyy-MM-dd HH:mm:ss') used when parsing DateTimeString.
        If omitted, the string is parsed using the current culture's standard formats.

    .OUTPUTS
        PSCustomObject with the following properties:
          - DateTimeOffset        [DateTimeOffset] The input date/time as a UTC DateTimeOffset.
          - SecondsTimestamp      [long]           The 10-digit Unix seconds timestamp.
          - MillisecondsTimestamp [long]           The 13-digit Unix milliseconds timestamp.

    .EXAMPLE
        ConvertTo-UnixTimestamp -DateTime (Get-Date)

        Converts the current local DateTime (treated as UTC).

    .EXAMPLE
        ConvertTo-UnixTimestamp -DateTimeOffset ([System.DateTimeOffset]::UtcNow)

        Converts the current UTC DateTimeOffset.

    .EXAMPLE
        ConvertTo-UnixTimestamp -DateTimeString '2024-03-15'

        Converts a date string; time defaults to midnight UTC.

    .EXAMPLE
        ConvertTo-UnixTimestamp -DateTimeString '15/03/2024 14:30:00' -Format 'dd/MM/yyyy HH:mm:ss'

        Converts a string using an explicit format.
    #>
    [CmdletBinding(DefaultParameterSetName = 'FromDateTimeOffset')]
    [OutputType([PSCustomObject])]
    param(
        [Parameter(Mandatory, ParameterSetName = 'FromDateTime', Position = 0, ValueFromPipeline)]
        [System.DateTime] $DateTime,

        [Parameter(Mandatory, ParameterSetName = 'FromDateTimeOffset', Position = 0, ValueFromPipeline)]
        [System.DateTimeOffset] $DateTimeOffset,

        [Parameter(Mandatory, ParameterSetName = 'FromString', Position = 0, ValueFromPipeline)]
        [string] $DateTimeString,

        [Parameter(ParameterSetName = 'FromString')]
        [string] $Format
    )

    process {
        $epoch = [System.DateTimeOffset]::new(1970, 1, 1, 0, 0, 0, [System.TimeSpan]::Zero)

        switch ($PSCmdlet.ParameterSetName) {
            'FromDateTime' {
                # Treat DateTime as UTC regardless of its Kind
                $dto = [System.DateTimeOffset]::new(
                    [System.DateTime]::SpecifyKind($DateTime, [System.DateTimeKind]::Utc),
                    [System.TimeSpan]::Zero
                )
            }
            'FromDateTimeOffset' {
                # Convert to UTC
                $dto = $DateTimeOffset.ToUniversalTime()
            }
            'FromString' {
                $dto = ConvertStringToDateTimeOffset -Value $DateTimeString -Format $Format
            }
        }

        $totalMs      = [long] ($dto - $epoch).TotalMilliseconds
        $totalSeconds = [Math]::DivRem($totalMs, 1000L, [ref] $null)

        [PSCustomObject] @{
            DateTimeOffset        = $dto
            SecondsTimestamp      = $totalSeconds
            MillisecondsTimestamp = $totalMs
        }
    }
}

# ---------------------------------------------------------------------------
# Internal helpers
# ---------------------------------------------------------------------------

function Resolve-BoundaryValue {
    param([Nullable[long]] $BoundaryValue)

    if ($null -ne $BoundaryValue) {
        return $BoundaryValue
    }

    $envVal = $env:UNIX_TIMESTAMP_BOUNDARY
    if (-not [string]::IsNullOrWhiteSpace($envVal)) {
        [long] $parsed = 0
        if ([long]::TryParse($envVal, [ref] $parsed)) {
            return $parsed
        }
        Write-Warning "UNIX_TIMESTAMP_BOUNDARY environment variable '$envVal' is not a valid integer. Using default boundary value."
    }

    return $script:DefaultBoundaryValue
}

function ConvertStringToDateTimeOffset {
    param(
        [string] $Value,
        [string] $Format
    )

    $utcOffset = [System.TimeSpan]::Zero

    if (-not [string]::IsNullOrWhiteSpace($Format)) {
        # ParseExact requires a specific format; assume UTC offset
        $parsed = [System.DateTimeOffset]::ParseExact(
            $Value,
            $Format,
            [System.Globalization.CultureInfo]::CurrentCulture,
            [System.Globalization.DateTimeStyles]::AssumeUniversal -bor
            [System.Globalization.DateTimeStyles]::AdjustToUniversal
        )
        return $parsed
    }

    # No format provided — try culture-aware parse with AssumeUniversal
    $parsed = [System.DateTimeOffset]::Parse(
        $Value,
        [System.Globalization.CultureInfo]::CurrentCulture,
        [System.Globalization.DateTimeStyles]::AssumeUniversal -bor
        [System.Globalization.DateTimeStyles]::AdjustToUniversal
    )
    return $parsed
}

Export-ModuleMember -Function ConvertFrom-UnixTimestamp, ConvertTo-UnixTimestamp
