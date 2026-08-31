Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "e15-baseline-build-diagnostics.ps1")

$tempBase = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$tempRoot = [IO.Path]::GetFullPath((Join-Path $tempBase ("catguard-e15-baseline-diagnostics-" + [guid]::NewGuid().ToString("N"))))
if (-not $tempRoot.StartsWith($tempBase, [StringComparison]::OrdinalIgnoreCase) `
    -or (Split-Path -Leaf $tempRoot) -notmatch '^catguard-e15-baseline-diagnostics-[0-9a-f]{32}$') {
    throw "Unsafe temporary test path: $tempRoot"
}

try {
    $source = Join-Path $tempRoot "source"
    $destination = Join-Path $tempRoot "destination"
    New-Item -ItemType Directory -Force -Path $source | Out-Null
    $keystoreSecret = "fixture-keystore-secret"
    $keySecret = "fixture-key-secret"
    @(
        "FAILURE: Unable to establish loopback connection",
        "CATGUARD_ANDROID_KEYSTORE_PASSWORD = $keystoreSecret",
        "unstructured signing value: $keySecret",
        "GITHUB_TOKEN = fixture-token"
    ) | Set-Content -LiteralPath (Join-Path $source "first.log") -Encoding UTF8
    "second Unity log" | Set-Content -LiteralPath (Join-Path $source "second.log") -Encoding UTF8
    "must not be copied" | Set-Content -LiteralPath (Join-Path $source "password.txt") -Encoding UTF8

    $copied = Copy-E15BaselineBuildDiagnostics `
        -SourceDirectory $source `
        -DestinationRoot $destination `
        -SensitiveValues @($keystoreSecret, $keySecret)
    if (-not [bool]$copied.passed -or @($copied.files).Count -ne 2) {
        throw "valid expected two preserved logs: $($copied | ConvertTo-Json -Depth 6 -Compress)"
    }
    if (-not ([IO.Path]::GetFullPath($copied.runRoot)).StartsWith(
        ([IO.Path]::GetFullPath($destination).TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar),
        [StringComparison]::OrdinalIgnoreCase)) {
        throw "valid copied logs outside the expected destination root."
    }
    foreach ($file in $copied.files) {
        if (-not (Test-Path -LiteralPath $file.path -PathType Leaf) `
            -or $file.sha256 -notmatch '^[0-9A-Fa-f]{64}$' `
            -or $file.bytes -le 0) {
            throw "valid produced invalid preserved-log evidence."
        }
    }
    $preservedText = @($copied.files | ForEach-Object {
        Get-Content -LiteralPath $_.path -Encoding UTF8 -Raw
    }) -join [Environment]::NewLine
    if ($preservedText -match [regex]::Escape($keystoreSecret) `
        -or $preservedText -match [regex]::Escape($keySecret) `
        -or $preservedText -match "fixture-token" `
        -or $preservedText -notmatch "Unable to establish loopback connection" `
        -or $preservedText -notmatch "\[REDACTED\]") {
        throw "valid did not redact secrets while preserving diagnostic evidence."
    }
    if (Test-Path -LiteralPath (Join-Path $copied.runRoot "password.txt") -PathType Leaf) {
        throw "valid copied a non-log file."
    }
    Write-Host "valid: passed=True."

    $missing = Copy-E15BaselineBuildDiagnostics `
        -SourceDirectory (Join-Path $tempRoot "missing") `
        -DestinationRoot $destination
    if ([bool]$missing.passed `
        -or $missing.runRoot `
        -or @($missing.files).Count -ne 0 `
        -or $missing.reason -notmatch "No baseline Unity build logs") {
        throw "missing expected a diagnostic failure result."
    }
    Write-Host "missing: passed=False."
}
finally {
    if (Test-Path -LiteralPath $tempRoot -PathType Container) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force
    }
}

Write-Host "E15 baseline build diagnostic tests passed: 2/2."
