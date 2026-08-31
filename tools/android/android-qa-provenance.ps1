function Get-AndroidGraphicsProvenance {
    [CmdletBinding()]
    param(
        [string[]]$SurfaceFlingerLines = @(),
        [string]$HardwareEgl = "",
        [string]$HardwareVulkan = "",
        [string]$QemuGles = "",
        [bool]$IsEmulator = $false
    )

    $glesLine = [string](@($SurfaceFlingerLines | Where-Object {
        $_ -match '^\s*GLES:\s*'
    } | Select-Object -First 1))
    $glesVendor = ""
    $glesRenderer = ""
    $glesVersion = ""
    if ($glesLine) {
        $glesValue = $glesLine -replace '^\s*GLES:\s*', ''
        $glesMatch = [regex]::Match(
            $glesValue,
            '^(?<vendor>.*?),\s*(?<renderer>.*),\s*(?<version>OpenGL ES.*)$')
        if ($glesMatch.Success) {
            $glesVendor = $glesMatch.Groups['vendor'].Value.Trim()
            $glesRenderer = $glesMatch.Groups['renderer'].Value.Trim()
            $glesVersion = $glesMatch.Groups['version'].Value.Trim()
        }
        else {
            $glesRenderer = $glesValue.Trim()
        }
    }

    $signals = @($glesLine, $HardwareEgl, $HardwareVulkan, $QemuGles) -join " | "
    $softwareRenderer = $signals -match '(?i)swiftshader|llvmpipe|lavapipe|software\s+(rasterizer|renderer)'
    $rendererIdentified = -not [string]::IsNullOrWhiteSpace($glesRenderer) `
        -or -not [string]::IsNullOrWhiteSpace($HardwareEgl) `
        -or -not [string]::IsNullOrWhiteSpace($HardwareVulkan)
    $classification = if ($IsEmulator -and $softwareRenderer) {
        "emulator-software"
    }
    elseif ($IsEmulator) {
        "emulator-host-gpu"
    }
    elseif ($softwareRenderer) {
        "physical-software"
    }
    elseif ($rendererIdentified) {
        "physical-hardware"
    }
    else {
        "unknown"
    }
    $releaseAcceptanceEligible = -not $IsEmulator `
        -and $rendererIdentified `
        -and -not $softwareRenderer

    return [pscustomobject]@{
        classification = $classification
        releaseAcceptanceEligible = $releaseAcceptanceEligible
        rendererIdentified = $rendererIdentified
        softwareRenderer = $softwareRenderer
        glesLine = $glesLine
        glesVendor = $glesVendor
        glesRenderer = $glesRenderer
        glesVersion = $glesVersion
        hardwareEgl = $HardwareEgl
        hardwareVulkan = $HardwareVulkan
        qemuGles = $QemuGles
    }
}
