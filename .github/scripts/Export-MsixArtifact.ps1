<#
.SYNOPSIS
    Collects built MSIX packages into the build\ artifact folder.

.DESCRIPTION
    Copies every .msix under the extension project (excluding the Dependencies
    subfolders) into -Destination, mirroring the CI 'Prepare artifact' step.
    Fails when nothing is found so an empty artifact upload cannot slip through.

    Note: when run locally, stale packages from earlier builds in AppPackages
    are collected too. Clean command-palette\ConvertTs\AppPackages first if you
    only want the current build.

.EXAMPLE
    .\.github\scripts\Export-MsixArtifact.ps1
#>
[CmdletBinding()]
param(
    [string]$SourcePath = (Join-Path $PSScriptRoot '..\..\command-palette\ConvertTs'),
    [string]$Destination = (Join-Path $PSScriptRoot '..\..\build')
)

$ErrorActionPreference = 'Stop'

New-Item -ItemType Directory -Force -Path $Destination | Out-Null

$packages = @(Get-ChildItem -Path $SourcePath -Recurse -Filter *.msix |
    Where-Object { $_.FullName -notmatch '\\Dependencies\\' })

if ($packages.Count -eq 0) {
    throw "No .msix packages found under '$SourcePath'. Run Build-ConvertTs.ps1 first."
}

foreach ($package in $packages) {
    Copy-Item -Path $package.FullName -Destination $Destination
    Write-Host "Copied $($package.Name) -> $Destination"
}
