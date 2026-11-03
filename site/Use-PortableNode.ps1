<#
.SYNOPSIS
    Downloads a portable Node.js build (no install, no admin rights) and puts it on PATH.

.DESCRIPTION
    Fetches the official Node.js zip build for Windows, extracts it, verifies node.exe runs,
    and prepends the binaries to $env:PATH.

    Dot-source the script so the PATH change applies to the current terminal session:

        . .\site\Use-PortableNode.ps1

    When the script is executed instead of dot-sourced, the download/extract still happens but
    the PATH change is lost when the script exits — a warning is shown in that case.

.PARAMETER Version
    Node.js version to download, without the leading 'v'. Defaults to 22.11.0 (Node 22 LTS).

.PARAMETER Destination
    Folder to download and extract into. Defaults to $env:TEMP\node-portable.

.EXAMPLE
    . .\site\Use-PortableNode.ps1
    node --version
    npm test
#>
[CmdletBinding()]
param(
    [string]$Version = '22.11.0',
    [string]$Destination = (Join-Path $env:TEMP 'node-portable')
)

$ErrorActionPreference = 'Stop'

$nodeDir = Join-Path $Destination "node-v$Version-win-x64"
$nodeExe = Join-Path $nodeDir 'node.exe'

if (-not (Test-Path $nodeExe)) {
    $zipPath = Join-Path $Destination "node-v$Version-win-x64.zip"

    if (-not (Test-Path $zipPath)) {
        $url = "https://nodejs.org/dist/v$Version/node-v$Version-win-x64.zip"
        Write-Host "Downloading $url ..."
        $ProgressPreference = 'SilentlyContinue' # much faster Invoke-WebRequest
        Invoke-WebRequest -Uri $url -OutFile $zipPath
    }

    Write-Host "Extracting to $Destination ..."
    Expand-Archive -Path $zipPath -DestinationPath $Destination -Force
}
else {
    Write-Host "Portable Node.js already present at $nodeDir"
}

$nodeVersion = & $nodeExe --version
Write-Host "node $nodeVersion ready at $nodeDir"

if ($MyInvocation.InvocationName -eq '.') {
    if ($env:PATH -notlike "*$nodeDir*") {
        $env:PATH = "$nodeDir;$env:PATH"
    }
    Write-Host 'PATH updated for this terminal session. You can now run: node, npm test, npm start'
}
else {
    Write-Warning "The script was executed, not dot-sourced, so the PATH change only applied inside the script.`nTo update PATH for your current terminal, dot-source it instead:`n`n    . .\site\Use-PortableNode.ps1`n`nOr call node directly:`n`n    & `"$nodeExe`" --version"
}
