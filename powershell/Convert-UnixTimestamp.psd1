#
# Module manifest for module 'Convert-UnixTimestamp'
#

@{
    # Script module or binary module file associated with this manifest.
    RootModule        = 'Convert-UnixTimestamp.psm1'

    # Version number of this module.
    ModuleVersion     = '1.0.0'

    # Supported PSEditions
    CompatiblePSEditions = @('Desktop', 'Core')

    # ID used to uniquely identify this module
    GUID              = 'a3f2c1e4-8b7d-4e5f-9a6c-2d1f0e3b4c5a'

    # Author of this module
    Author            = 'steve-codemunkies'

    # Description of the functionality provided by this module
    Description       = 'Functions for converting between Unix epoch timestamps (seconds or milliseconds) and human-readable DateTimeOffset values.'

    # Minimum version of the PowerShell engine required by this module
    PowerShellVersion = '5.1'

    # Functions to export from this module
    FunctionsToExport = @(
        'ConvertFrom-UnixTimestamp'
        'ConvertTo-UnixTimestamp'
    )

    # Cmdlets to export from this module
    CmdletsToExport   = @()

    # Variables to export from this module
    VariablesToExport = @()

    # Aliases to export from this module
    AliasesToExport   = @()

    # Private data to pass to the module specified in RootModule/ModuleToProcess.
    PrivateData       = @{
        PSData = @{
            Tags       = @('Unix', 'Epoch', 'Timestamp', 'DateTime', 'Conversion')
            ProjectUri = 'https://github.com/steve-codemunkies/convert-ts'
        }
    }
}
