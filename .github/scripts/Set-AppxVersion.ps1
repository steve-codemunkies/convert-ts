<#
.SYNOPSIS
    Stamps the four-part package version into Package.appxmanifest.

.DESCRIPTION
    Sets the Identity/@Version attribute of every .appxmanifest under -Path to
    "<Version>.0". Fails if no manifest is found, so a wrong path cannot slip
    through CI unnoticed.

.EXAMPLE
    .\.github\scripts\Set-AppxVersion.ps1 -Version '0.3.0'
#>
[CmdletBinding()]
param(
    # Three-part version; '.0' is appended for the manifest identity.
    [Parameter(Mandatory)]
    [string]$Version,

    # Folder to search for .appxmanifest files.
    [string]$Path = (Join-Path $PSScriptRoot '..\..\command-palette\ConvertTs')
)

$ErrorActionPreference = 'Stop'

$manifests = @(Get-ChildItem -Path $Path -Filter *.appxmanifest -Recurse)
if ($manifests.Count -eq 0) {
    throw "No .appxmanifest files found under '$Path'."
}

$xIdentity = [System.Xml.Linq.XName]::Get('{http://schemas.microsoft.com/appx/manifest/foundation/windows10}Identity')

foreach ($manifestFile in $manifests) {
    $manifest = [System.Xml.Linq.XDocument]::Load($manifestFile.FullName)
    $manifest.Root.Element($xIdentity).Attribute('Version').Value = "$Version.0"
    $manifest.Save($manifestFile.FullName)
    Write-Host "Set Identity Version=$Version.0 in $($manifestFile.FullName)"
}
