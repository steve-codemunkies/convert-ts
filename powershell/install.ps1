<#
.SYNOPSIS
    Installs the Convert-UnixTimestamp PowerShell module directly from GitHub.

.DESCRIPTION
    Downloads the Convert-UnixTimestamp module files from GitHub and places them in
    the current user's personal PowerShell modules directory.  Optionally adds an
    Import-Module line to the user's PowerShell profile so the functions are available
    in every new session.

    No git or manual file copying required.

.PARAMETER Branch
    The branch to download from.  Defaults to 'main'.

.PARAMETER AddToProfile
    When specified, the script adds 'Import-Module Convert-UnixTimestamp' to the
    current user's PowerShell profile ($PROFILE) if it is not already present.

.PARAMETER Force
    Overwrite any existing module files without prompting.

.EXAMPLE
    # Minimal install (download files only)
    irm https://raw.githubusercontent.com/steve-codemunkies/convert-ts/main/powershell/install.ps1 | iex

.EXAMPLE
    # Install and automatically register in the PowerShell profile
    & ([scriptblock]::Create((irm https://raw.githubusercontent.com/steve-codemunkies/convert-ts/main/powershell/install.ps1))) -AddToProfile

.EXAMPLE
    # Install from a specific branch
    & ([scriptblock]::Create((irm https://raw.githubusercontent.com/steve-codemunkies/convert-ts/main/powershell/install.ps1))) -Branch develop -AddToProfile
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [string] $Branch       = 'main',
    [switch] $AddToProfile,
    [switch] $Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoOwner  = 'steve-codemunkies'
$repoName   = 'convert-ts'
$moduleName = 'Convert-UnixTimestamp'

$rawBase = "https://raw.githubusercontent.com/$repoOwner/$repoName/$Branch/powershell"

# Files to download (relative to the powershell/ directory in the repo)
$moduleFiles = @(
    'Convert-UnixTimestamp.psd1'
    'Convert-UnixTimestamp.psm1'
)

# Resolve the personal modules directory in a cross-platform way
$modulesRoot = Join-Path ([System.Environment]::GetFolderPath('MyDocuments')) 'PowerShell' 'Modules'
if ($IsLinux -or $IsMacOS) {
    $modulesRoot = Join-Path $HOME '.local' 'share' 'powershell' 'Modules'
}

$moduleDestination = Join-Path $modulesRoot $moduleName

Write-Host "Installing $moduleName to: $moduleDestination"

# Create the destination directory
if (-not (Test-Path $moduleDestination)) {
    New-Item -ItemType Directory -Path $moduleDestination -Force | Out-Null
}

# Download each module file
foreach ($file in $moduleFiles) {
    $sourceUrl = "$rawBase/$file"
    $destPath  = Join-Path $moduleDestination $file

    if ((Test-Path $destPath) -and -not $Force) {
        Write-Host "  Skipping $file (already exists — use -Force to overwrite)"
        continue
    }

    if ($PSCmdlet.ShouldProcess($destPath, "Download $sourceUrl")) {
        Write-Host "  Downloading $file ..."
        Invoke-WebRequest -Uri $sourceUrl -OutFile $destPath -UseBasicParsing
    }
}

Write-Host "Module files installed successfully."

# Optionally add Import-Module to the user profile
if ($AddToProfile) {
    $importLine = "Import-Module $moduleName"

    # $PROFILE may refer to a file that doesn't exist yet
    if (-not (Test-Path $PROFILE)) {
        if ($PSCmdlet.ShouldProcess($PROFILE, 'Create PowerShell profile')) {
            New-Item -ItemType File -Path $PROFILE -Force | Out-Null
            Write-Host "Created profile: $PROFILE"
        }
    }

    $profileContent = Get-Content -Path $PROFILE -Raw -ErrorAction SilentlyContinue
    if ($profileContent -match [regex]::Escape($importLine)) {
        Write-Host "Profile already contains '$importLine' — no changes made."
    }
    else {
        if ($PSCmdlet.ShouldProcess($PROFILE, "Append '$importLine'")) {
            Add-Content -Path $PROFILE -Value "`n$importLine"
            Write-Host "Added '$importLine' to profile: $PROFILE"
        }
    }
}

Write-Host ""
Write-Host "Installation complete."
Write-Host "To start using the functions in this session run:"
Write-Host "  Import-Module $moduleName"
Write-Host ""
Write-Host "To verify:"
Write-Host "  Get-Command -Module $moduleName"
