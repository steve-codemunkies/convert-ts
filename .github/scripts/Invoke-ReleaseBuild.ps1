<#
.SYNOPSIS
    Runs the full release build locally, mirroring the CI workflow.

.DESCRIPTION
    Executes the same scripts used by .github/workflows/command-palette-build.yml,
    in the same order: version validation, manifest stamping, restore/build, and
    artifact collection. Use this to test the release pipeline without pushing
    a tag.

.EXAMPLE
    .\.github\scripts\Invoke-ReleaseBuild.ps1 -Version 0.3.0 -Platform x64
#>
[CmdletBinding()]
param(
    # Three-part version, e.g. 0.3.0 (same rules as the CI tag/input).
    [Parameter(Mandatory)]
    [string]$Version,

    [ValidateSet('x64', 'ARM64')]
    [string]$Platform = 'x64'
)

$ErrorActionPreference = 'Stop'

$resolvedVersion = . (Join-Path $PSScriptRoot 'Get-BuildVersion.ps1') -InputVersion $Version
Write-Host "Building ConvertTs $resolvedVersion ($Platform)"

. (Join-Path $PSScriptRoot 'Set-AppxVersion.ps1') -Version $resolvedVersion
. (Join-Path $PSScriptRoot 'Build-ConvertTs.ps1') -Platform $Platform -Version $resolvedVersion
. (Join-Path $PSScriptRoot 'Export-MsixArtifact.ps1')

$buildDir = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\build'))
Write-Host ''
Write-Host "Done. MSIX packages collected in $buildDir"
