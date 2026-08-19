#Requires -Module Pester

BeforeAll {
    $modulePath = Join-Path $PSScriptRoot '..' 'Convert-UnixTimestamp.psd1'
    Import-Module $modulePath -Force
}

AfterAll {
    Remove-Module Convert-UnixTimestamp -ErrorAction SilentlyContinue
}

Describe 'ConvertFrom-UnixTimestamp' {

    Context 'Millisecond timestamp (value > default boundary)' {
        It 'Returns a DateTimeOffset in UTC' {
            $result = ConvertFrom-UnixTimestamp -Timestamp 1700000000000
            $result.DateTimeOffset | Should -BeOfType [System.DateTimeOffset]
            $result.DateTimeOffset.Offset | Should -Be ([System.TimeSpan]::Zero)
        }

        It 'Returns the correct UTC date/time for a known millisecond timestamp' {
            # 1700000000000 ms = 2023-11-14T22:13:20Z
            $result = ConvertFrom-UnixTimestamp -Timestamp 1700000000000
            $result.DateTimeOffset.Year   | Should -Be 2023
            $result.DateTimeOffset.Month  | Should -Be 11
            $result.DateTimeOffset.Day    | Should -Be 14
            $result.DateTimeOffset.Hour   | Should -Be 22
            $result.DateTimeOffset.Minute | Should -Be 13
            $result.DateTimeOffset.Second | Should -Be 20
        }

        It 'Populates OriginalTimestamp with the input value' {
            $result = ConvertFrom-UnixTimestamp -Timestamp 1700000000000
            $result.OriginalTimestamp | Should -Be 1700000000000
        }

        It 'Derives the SecondsTimestamp from the millisecond input' {
            $result = ConvertFrom-UnixTimestamp -Timestamp 1700000000000
            $result.SecondsTimestamp | Should -Be 1700000000
        }

        It 'Populates MillisecondsTimestamp with the original value' {
            $result = ConvertFrom-UnixTimestamp -Timestamp 1700000000000
            $result.MillisecondsTimestamp | Should -Be 1700000000000
        }
    }

    Context 'Seconds timestamp (value <= default boundary)' {
        It 'Returns a DateTimeOffset in UTC' {
            $result = ConvertFrom-UnixTimestamp -Timestamp 1700000000
            $result.DateTimeOffset | Should -BeOfType [System.DateTimeOffset]
            $result.DateTimeOffset.Offset | Should -Be ([System.TimeSpan]::Zero)
        }

        It 'Returns the correct UTC date/time for a known seconds timestamp' {
            # 1700000000 s = 2023-11-14T22:13:20Z
            $result = ConvertFrom-UnixTimestamp -Timestamp 1700000000
            $result.DateTimeOffset.Year   | Should -Be 2023
            $result.DateTimeOffset.Month  | Should -Be 11
            $result.DateTimeOffset.Day    | Should -Be 14
            $result.DateTimeOffset.Hour   | Should -Be 22
            $result.DateTimeOffset.Minute | Should -Be 13
            $result.DateTimeOffset.Second | Should -Be 20
        }

        It 'Populates OriginalTimestamp with the input value' {
            $result = ConvertFrom-UnixTimestamp -Timestamp 1700000000
            $result.OriginalTimestamp | Should -Be 1700000000
        }

        It 'Populates SecondsTimestamp with the original value' {
            $result = ConvertFrom-UnixTimestamp -Timestamp 1700000000
            $result.SecondsTimestamp | Should -Be 1700000000
        }

        It 'Derives the MillisecondsTimestamp from the seconds input' {
            $result = ConvertFrom-UnixTimestamp -Timestamp 1700000000
            $result.MillisecondsTimestamp | Should -Be 1700000000000
        }
    }

    Context 'Boundary detection — default boundary value (32503680000)' {
        It 'Treats a value equal to the boundary as seconds' {
            $result = ConvertFrom-UnixTimestamp -Timestamp 32503680000
            $result.SecondsTimestamp | Should -Be 32503680000
            $result.MillisecondsTimestamp | Should -Be 32503680000000
        }

        It 'Treats a value one above the boundary as milliseconds' {
            $result = ConvertFrom-UnixTimestamp -Timestamp 32503680001
            $result.MillisecondsTimestamp | Should -Be 32503680001
            $result.SecondsTimestamp | Should -Be 32503680
        }
    }

    Context 'Custom boundary via -BoundaryValue parameter' {
        It 'Uses the custom boundary when provided on the command line' {
            # With boundary = 9999999999, value 10000000000 is treated as milliseconds
            $result = ConvertFrom-UnixTimestamp -Timestamp 10000000000 -BoundaryValue 9999999999
            $result.MillisecondsTimestamp | Should -Be 10000000000
            $result.SecondsTimestamp      | Should -Be 10000000
        }

        It 'Overrides the environment variable when -BoundaryValue is supplied' {
            $env:UNIX_TIMESTAMP_BOUNDARY = '5000000000'
            try {
                # env var boundary = 5000000000; with that boundary 7000000000 would be ms
                # but the explicit -BoundaryValue 9999999999 overrides it, so 7000000000
                # is treated as seconds (7000000000 <= 9999999999)
                $result = ConvertFrom-UnixTimestamp -Timestamp 7000000000 -BoundaryValue 9999999999
                $result.SecondsTimestamp      | Should -Be 7000000000
                $result.MillisecondsTimestamp | Should -Be 7000000000000
            }
            finally {
                Remove-Item Env:UNIX_TIMESTAMP_BOUNDARY -ErrorAction SilentlyContinue
            }
        }
    }

    Context 'Custom boundary via UNIX_TIMESTAMP_BOUNDARY environment variable' {
        It 'Uses the environment variable boundary when set' {
            $env:UNIX_TIMESTAMP_BOUNDARY = '9999999999'
            try {
                # 10000000000 > 9999999999 → milliseconds
                $result = ConvertFrom-UnixTimestamp -Timestamp 10000000000
                $result.MillisecondsTimestamp | Should -Be 10000000000
                $result.SecondsTimestamp      | Should -Be 10000000
            }
            finally {
                Remove-Item Env:UNIX_TIMESTAMP_BOUNDARY -ErrorAction SilentlyContinue
            }
        }

        It 'Falls back to default boundary when environment variable is invalid' {
            $env:UNIX_TIMESTAMP_BOUNDARY = 'not-a-number'
            try {
                { ConvertFrom-UnixTimestamp -Timestamp 1700000000 -WarningAction SilentlyContinue } | Should -Not -Throw
            }
            finally {
                Remove-Item Env:UNIX_TIMESTAMP_BOUNDARY -ErrorAction SilentlyContinue
            }
        }
    }

    Context 'Pipeline input' {
        It 'Accepts multiple timestamps from the pipeline' {
            $results = 1700000000, 1700000000000 | ConvertFrom-UnixTimestamp
            $results.Count | Should -Be 2
        }
    }

    Context 'Epoch zero' {
        It 'Converts timestamp 0 to 1970-01-01T00:00:00Z' {
            $result = ConvertFrom-UnixTimestamp -Timestamp 0
            $result.DateTimeOffset.Year   | Should -Be 1970
            $result.DateTimeOffset.Month  | Should -Be 1
            $result.DateTimeOffset.Day    | Should -Be 1
            $result.DateTimeOffset.Hour   | Should -Be 0
            $result.DateTimeOffset.Minute | Should -Be 0
            $result.DateTimeOffset.Second | Should -Be 0
        }
    }
}

Describe 'ConvertTo-UnixTimestamp' {

    BeforeAll {
        # Known reference values
        # 2023-11-14T22:13:20Z = 1700000000 seconds = 1700000000000 milliseconds
        $script:refYear   = 2023
        $script:refMonth  = 11
        $script:refDay    = 14
        $script:refHour   = 22
        $script:refMin    = 13
        $script:refSec    = 20
        $script:refSecTs  = 1700000000L
        $script:refMsTs   = 1700000000000L
    }

    Context 'From [DateTime] input' {
        It 'Returns a PSCustomObject with the expected properties' {
            $dt     = [System.DateTime]::new($refYear, $refMonth, $refDay, $refHour, $refMin, $refSec, [System.DateTimeKind]::Utc)
            $result = ConvertTo-UnixTimestamp -DateTime $dt
            $result.PSObject.Properties.Name | Should -Contain 'DateTimeOffset'
            $result.PSObject.Properties.Name | Should -Contain 'SecondsTimestamp'
            $result.PSObject.Properties.Name | Should -Contain 'MillisecondsTimestamp'
        }

        It 'Returns the correct SecondsTimestamp' {
            $dt     = [System.DateTime]::new($refYear, $refMonth, $refDay, $refHour, $refMin, $refSec, [System.DateTimeKind]::Utc)
            $result = ConvertTo-UnixTimestamp -DateTime $dt
            $result.SecondsTimestamp | Should -Be $refSecTs
        }

        It 'Returns the correct MillisecondsTimestamp' {
            $dt     = [System.DateTime]::new($refYear, $refMonth, $refDay, $refHour, $refMin, $refSec, [System.DateTimeKind]::Utc)
            $result = ConvertTo-UnixTimestamp -DateTime $dt
            $result.MillisecondsTimestamp | Should -Be $refMsTs
        }

        It 'Returns a UTC DateTimeOffset' {
            $dt     = [System.DateTime]::new($refYear, $refMonth, $refDay, $refHour, $refMin, $refSec, [System.DateTimeKind]::Utc)
            $result = ConvertTo-UnixTimestamp -DateTime $dt
            $result.DateTimeOffset.Offset | Should -Be ([System.TimeSpan]::Zero)
        }

        It 'Treats an Unspecified-kind DateTime as UTC' {
            $dt     = [System.DateTime]::new($refYear, $refMonth, $refDay, $refHour, $refMin, $refSec, [System.DateTimeKind]::Unspecified)
            $result = ConvertTo-UnixTimestamp -DateTime $dt
            $result.SecondsTimestamp | Should -Be $refSecTs
        }
    }

    Context 'From [DateTimeOffset] input' {
        It 'Returns the correct SecondsTimestamp for a UTC DateTimeOffset' {
            $dto    = [System.DateTimeOffset]::new($refYear, $refMonth, $refDay, $refHour, $refMin, $refSec, [System.TimeSpan]::Zero)
            $result = ConvertTo-UnixTimestamp -DateTimeOffset $dto
            $result.SecondsTimestamp | Should -Be $refSecTs
        }

        It 'Returns the correct SecondsTimestamp for a non-UTC DateTimeOffset' {
            # Same instant as refSecTs, expressed as UTC+5
            # UTC: 2023-11-14 22:13:20 → UTC+5: 2023-11-15 03:13:20
            $offset = [System.TimeSpan]::FromHours(5)
            $dto    = [System.DateTimeOffset]::new(2023, 11, 15, 3, 13, 20, $offset)
            $result = ConvertTo-UnixTimestamp -DateTimeOffset $dto
            $result.SecondsTimestamp | Should -Be $refSecTs
        }

        It 'Returns a UTC DateTimeOffset in the output' {
            $offset = [System.TimeSpan]::FromHours(-3)
            $dto    = [System.DateTimeOffset]::new($refYear, $refMonth, $refDay, $refHour - 3, $refMin, $refSec, $offset)
            $result = ConvertTo-UnixTimestamp -DateTimeOffset $dto
            $result.DateTimeOffset.Offset | Should -Be ([System.TimeSpan]::Zero)
        }
    }

    Context 'From string input — no format' {
        It 'Parses an ISO 8601 date-time string' {
            $result = ConvertTo-UnixTimestamp -DateTimeString '2023-11-14T22:13:20Z'
            $result.SecondsTimestamp | Should -Be $refSecTs
        }

        It 'Parses a date-only string and defaults to midnight UTC' {
            # 2023-11-14T00:00:00Z = 1699920800 — let's compute the expected value
            $epoch    = [System.DateTimeOffset]::new(1970, 1, 1, 0, 0, 0, [System.TimeSpan]::Zero)
            $midnight = [System.DateTimeOffset]::new(2023, 11, 14, 0, 0, 0, [System.TimeSpan]::Zero)
            $expected = [long] ($midnight - $epoch).TotalSeconds

            $result = ConvertTo-UnixTimestamp -DateTimeString '2023-11-14'
            $result.SecondsTimestamp | Should -Be $expected
            $result.DateTimeOffset.Hour   | Should -Be 0
            $result.DateTimeOffset.Minute | Should -Be 0
            $result.DateTimeOffset.Second | Should -Be 0
        }
    }

    Context 'From string input — with explicit format' {
        It 'Parses a string using the supplied format' {
            $result = ConvertTo-UnixTimestamp -DateTimeString '14/11/2023 22:13:20' -Format 'dd/MM/yyyy HH:mm:ss'
            $result.SecondsTimestamp | Should -Be $refSecTs
        }

        It 'Parses a date-only string with explicit format and defaults to midnight UTC' {
            $epoch    = [System.DateTimeOffset]::new(1970, 1, 1, 0, 0, 0, [System.TimeSpan]::Zero)
            $midnight = [System.DateTimeOffset]::new(2023, 11, 14, 0, 0, 0, [System.TimeSpan]::Zero)
            $expected = [long] ($midnight - $epoch).TotalSeconds

            $result = ConvertTo-UnixTimestamp -DateTimeString '14/11/2023' -Format 'dd/MM/yyyy'
            $result.SecondsTimestamp | Should -Be $expected
        }
    }

    Context 'Round-trip with ConvertFrom-UnixTimestamp' {
        It 'Round-trips a millisecond timestamp' {
            $original = 1700000000000L
            $dto      = (ConvertFrom-UnixTimestamp -Timestamp $original).DateTimeOffset
            $result   = ConvertTo-UnixTimestamp -DateTimeOffset $dto
            $result.MillisecondsTimestamp | Should -Be $original
        }

        It 'Round-trips a seconds timestamp' {
            $original = 1700000000L
            $dto      = (ConvertFrom-UnixTimestamp -Timestamp $original).DateTimeOffset
            $result   = ConvertTo-UnixTimestamp -DateTimeOffset $dto
            $result.SecondsTimestamp | Should -Be $original
        }
    }

    Context 'Pipeline input' {
        It 'Accepts multiple DateTimeOffset values from the pipeline' {
            $dto1   = [System.DateTimeOffset]::new(2023, 1,  1, 0, 0, 0, [System.TimeSpan]::Zero)
            $dto2   = [System.DateTimeOffset]::new(2023, 6, 15, 12, 0, 0, [System.TimeSpan]::Zero)
            $results = $dto1, $dto2 | ConvertTo-UnixTimestamp
            $results.Count | Should -Be 2
        }
    }
}
