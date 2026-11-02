<#
.SYNOPSIS
    Generates the MSIX visual asset PNGs from the SVG sources.

.DESCRIPTION
    Rasterizes the square and wide SVG logos with resvg into the
    scale-100/200/400 PNGs referenced by Package.appxmanifest:
      convert-ts.svg       -> StoreLogo, Square44x44Logo, Square150x150Logo
      convert-ts.wide.svg  -> Wide310x150Logo, SplashScreen

    resvg accepts only one of --width/--height (they conflict), so the width
    is passed and the height follows from the SVG aspect ratio. The wide SVG's
    1033:500 viewBox is within 0.04% of 31:15, so heights round to the exact
    required values (150, 300, 600, 1200).

.EXAMPLE
    .\.github\scripts\Generate-Pngs.ps1
#>
[CmdletBinding()]
param(
    [string]$AssetsDir = (Join-Path $PSScriptRoot '..\..\command-palette\ConvertTs\Assets')
)

$ErrorActionPreference = 'Stop'

$resvg = (Get-Command resvg.exe -ErrorAction SilentlyContinue).Source
if (-not $resvg) {
    throw 'resvg.exe not found on PATH. Download from https://github.com/linebender/resvg/releases'
}

$sets = @(
    @{
        Svg    = 'convert-ts.svg'
        Assets = [ordered]@{
            'StoreLogo'         = 50
            'Square44x44Logo'   = 44
            'Square150x150Logo' = 150
        }
    }
    @{
        Svg    = 'convert-ts.wide.svg'
        Assets = [ordered]@{
            'Wide310x150Logo' = 310
            'SplashScreen'      = 620
        }
    }
)

$scales = [ordered]@{ 'scale-100' = 1; 'scale-200' = 2; 'scale-400' = 4 }

foreach ($set in $sets) {
    $svgPath = Join-Path $AssetsDir $set.Svg
    if (-not (Test-Path $svgPath)) { throw "SVG source not found: $svgPath" }

    foreach ($name in $set.Assets.Keys) {
        foreach ($scale in $scales.Keys) {
            $width = $set.Assets[$name] * $scales[$scale]
            $out = Join-Path $AssetsDir "$name.$scale.png"

            & $resvg --width $width $svgPath $out
            if ($LASTEXITCODE -ne 0) { throw "resvg failed for '$out' (exit code $LASTEXITCODE)." }

            Write-Host "Wrote $name.$scale.png (width $width)"
        }
    }
}