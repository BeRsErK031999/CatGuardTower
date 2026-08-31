Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "e15-java-temp.ps1")

$root = "C:\cgjtmp-test-" + [guid]::NewGuid().ToString("N").Substring(0, 8)
$session = $null
try {
    $validation = Test-E15JavaTempRoot -Root $root
    if (-not [bool]$validation.passed) {
        throw "valid rejected the short absolute root: $($validation.reason)"
    }
    $session = New-E15JavaTempDirectory -Root $root
    if (-not (Test-Path -LiteralPath $session.path -PathType Container) `
        -or -not $session.path.StartsWith(($validation.root + [IO.Path]::DirectorySeparatorChar), [StringComparison]::OrdinalIgnoreCase)) {
        throw "valid did not create a bounded Java temp directory."
    }
    Remove-E15JavaTempDirectory -Path $session.path -Root $root
    $session = $null
    Write-Host "valid: passed=True."

    $tooLong = Test-E15JavaTempRoot -Root ("C:\" + ("x" * 40))
    if ([bool]$tooLong.passed -or $tooLong.reason -notmatch "at most 32 characters") {
        throw "too-long expected the AF_UNIX path guard to reject the root."
    }
    Write-Host "too-long: passed=False."

    $unsafeRejected = $false
    try {
        Remove-E15JavaTempDirectory -Path (Join-Path $root "not-owned") -Root $root
    }
    catch {
        $unsafeRejected = $_.Exception.Message -match "unexpected Java temp path"
    }
    if (-not $unsafeRejected) {
        throw "unsafe-cleanup expected an ownership guard failure."
    }
    Write-Host "unsafe-cleanup: passed=False."
}
finally {
    if ($null -ne $session -and (Test-Path -LiteralPath $session.path -PathType Container)) {
        Remove-E15JavaTempDirectory -Path $session.path -Root $root
    }
    if (Test-Path -LiteralPath $root -PathType Container) {
        [IO.Directory]::Delete($root, $false)
    }
}

Write-Host "E15 Java temp contract tests passed: 3/3."
