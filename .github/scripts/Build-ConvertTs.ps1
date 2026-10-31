<#
.SYNOPSIS
    Restores and builds the ConvertTs Command Palette extension (MSIX).

.DESCRIPTION
    Runs the same msbuild invocations as the CI workflow. msbuild is taken from
    PATH (provided by microsoft/setup-msbuild in CI, or a Developer PowerShell
    locally); otherwise the latest Visual Studio installation is located via
    vswhere.

.EXAMPLE
    .\.github\scripts\Build-ConvertTs.ps1 -Platform x64 -Version '0.3.0'
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet('x64', 'ARM64')]
    [string]$Platform,

    [Parameter(Mandatory)]
    [string]$Version,

    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$solution = Join-Path $repoRoot 'command-palette\ConvertTs.sln'
$project  = Join-Path $repoRoot 'command-palette\ConvertTs\ConvertTs.csproj'

$msbuild = (Get-Command msbuild.exe -ErrorAction SilentlyContinue).Source
if (-not $msbuild) {
    $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    if (Test-Path $vswhere) {
        $msbuild = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
    }
}
if (-not $msbuild) {
    throw 'msbuild.exe not found. Install Visual Studio Build Tools or run from a Developer PowerShell.'
}

Write-Host "Using msbuild: $msbuild"

& $msbuild $solution /t:Restore /p:Configuration=$Configuration /p:Platform=$Platform
if ($LASTEXITCODE -ne 0) { throw "Restore failed (exit code $LASTEXITCODE)." }

& $msbuild $project /p:Configuration=$Configuration /p:Platform=$Platform /p:Version=$Version
if ($LASTEXITCODE -ne 0) { throw "Build failed (exit code $LASTEXITCODE)." }
