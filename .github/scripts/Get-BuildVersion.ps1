<#
.SYNOPSIS
    Resolves and validates the build version for CI and local builds.

.DESCRIPTION
    Uses the workflow_dispatch input when the event is 'workflow_dispatch',
    otherwise the pushed tag name. The version must have exactly three numeric
    components in the range 0-65535 without leading zeros (e.g. 0.3.0), because
    the AppX identity requires a four-part version (the fourth part is fixed to 0).

    In CI (when GITHUB_ENV is set) the validated version is appended to GITHUB_ENV
    as VERSION. Locally it is written to the output stream so callers can capture it:

        $version = .\.github\scripts\Get-BuildVersion.ps1 -InputVersion '0.3.0'
#>
[CmdletBinding()]
param(
    # Version supplied via the workflow_dispatch input (or passed locally).
    [string]$InputVersion,

    # Tag name when the workflow runs on a tag push.
    [string]$RefName,

    # GitHub event name. Defaults to manual dispatch semantics for local runs.
    [string]$EventName = 'workflow_dispatch'
)

$ErrorActionPreference = 'Stop'

$version = if ($EventName -eq 'workflow_dispatch') { $InputVersion } else { $RefName }

if ($version -notmatch '^(0|[1-9][0-9]{0,4})(\.(0|[1-9][0-9]{0,4})){2}\z' -or
    @($version.Split('.') | Where-Object { [int]$_ -gt 65535 }).Count -gt 0) {
    throw "Invalid version '$version'. Use three numeric components in the range 0-65535 without leading zeros, e.g. 0.3.0."
}

if ($env:GITHUB_ENV) {
    "VERSION=$version" | Out-File -FilePath $env:GITHUB_ENV -Append -Encoding utf8
    Write-Host "VERSION=$version written to GITHUB_ENV"
}
else {
    Write-Output $version
}
