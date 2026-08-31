Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "android-qa-provenance.ps1")

$cases = @(
    [pscustomobject]@{
        name = "swiftshader"
        expectedClassification = "emulator-software"
        expectedRenderer = "ANGLE (Google, Vulkan 1.3.0 (SwiftShader Device))"
        expectedEligible = $false
        parameters = @{
            SurfaceFlingerLines = @(
                "GLES: Google (Google Inc.), ANGLE (Google, Vulkan 1.3.0 (SwiftShader Device)), OpenGL ES 3.2"
            )
            IsEmulator = $true
        }
    },
    [pscustomobject]@{
        name = "emulator-host-gpu"
        expectedClassification = "emulator-host-gpu"
        expectedRenderer = "ANGLE (NVIDIA GeForce RTX)"
        expectedEligible = $false
        parameters = @{
            SurfaceFlingerLines = @(
                "GLES: Google Inc., ANGLE (NVIDIA GeForce RTX), OpenGL ES 3.2"
            )
            HardwareEgl = "emulation"
            IsEmulator = $true
        }
    },
    [pscustomobject]@{
        name = "physical-hardware"
        expectedClassification = "physical-hardware"
        expectedRenderer = "Adreno (TM) 740"
        expectedEligible = $true
        parameters = @{
            SurfaceFlingerLines = @(
                "GLES: Qualcomm, Adreno (TM) 740, OpenGL ES 3.2"
            )
            HardwareEgl = "adreno"
            IsEmulator = $false
        }
    },
    [pscustomobject]@{
        name = "physical-software"
        expectedClassification = "physical-software"
        expectedRenderer = "llvmpipe"
        expectedEligible = $false
        parameters = @{
            SurfaceFlingerLines = @(
                "GLES: Mesa, llvmpipe, OpenGL ES 3.2"
            )
            HardwareEgl = "mesa"
            IsEmulator = $false
        }
    },
    [pscustomobject]@{
        name = "unknown"
        expectedClassification = "unknown"
        expectedRenderer = ""
        expectedEligible = $false
        parameters = @{
            IsEmulator = $false
        }
    }
)

foreach ($case in $cases) {
    $parameters = $case.parameters
    $result = Get-AndroidGraphicsProvenance @parameters
    if ($result.classification -ne $case.expectedClassification `
        -or $result.glesRenderer -ne $case.expectedRenderer `
        -or [bool]$result.releaseAcceptanceEligible -ne [bool]$case.expectedEligible) {
        throw "Graphics provenance case '$($case.name)' failed: $($result | ConvertTo-Json -Compress)."
    }

    Write-Host "$($case.name): $($result.classification), renderer '$($result.glesRenderer)', eligible $($result.releaseAcceptanceEligible)."
}

Write-Host "Android QA graphics provenance tests passed: $($cases.Count)/$($cases.Count)."
