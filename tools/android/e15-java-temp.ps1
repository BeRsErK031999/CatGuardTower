function Test-E15JavaTempRoot {
    [CmdletBinding()]
    param([string]$Root)

    $reason = ""
    $resolved = ""
    try {
        if ([string]::IsNullOrWhiteSpace($Root)) {
            throw "Java temp root is missing."
        }
        if (-not [IO.Path]::IsPathRooted($Root)) {
            throw "Java temp root must be an absolute path."
        }
        $resolved = [IO.Path]::GetFullPath($Root).TrimEnd('\', '/')
        if ($resolved.Length -gt 32) {
            throw "Java temp root must be at most 32 characters for Windows AF_UNIX compatibility."
        }
        if ([string]::IsNullOrWhiteSpace([IO.Path]::GetFileName($resolved))) {
            throw "Java temp root must not be a drive root."
        }
    }
    catch {
        $reason = $_.Exception.Message
    }

    return [pscustomobject]@{
        passed = -not $reason
        root = $resolved
        reason = $reason
    }
}

function New-E15JavaTempDirectory {
    [CmdletBinding()]
    param([string]$Root)

    $validation = Test-E15JavaTempRoot -Root $Root
    if (-not [bool]$validation.passed) {
        throw $validation.reason
    }
    New-Item -ItemType Directory -Force -Path $validation.root | Out-Null
    $runPath = Join-Path $validation.root ("CGE15J-" + [guid]::NewGuid().ToString("N").Substring(0, 8))
    New-Item -ItemType Directory -Path $runPath | Out-Null
    return [pscustomobject]@{
        root = $validation.root
        path = [IO.Path]::GetFullPath($runPath)
    }
}

function Remove-E15JavaTempDirectory {
    [CmdletBinding()]
    param(
        [string]$Path,
        [string]$Root
    )

    $validation = Test-E15JavaTempRoot -Root $Root
    if (-not [bool]$validation.passed) {
        throw $validation.reason
    }
    $resolvedPath = [IO.Path]::GetFullPath($Path)
    $requiredPrefix = $validation.root.TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
    if (-not $resolvedPath.StartsWith($requiredPrefix, [StringComparison]::OrdinalIgnoreCase) `
        -or [IO.Path]::GetFileName($resolvedPath) -notmatch '^CGE15J-[0-9a-f]{8}$') {
        throw "Refusing to clean an unexpected Java temp path: $resolvedPath"
    }
    if ([IO.Directory]::Exists($resolvedPath)) {
        [IO.Directory]::Delete($resolvedPath, $true)
    }
    if ([IO.Directory]::Exists($resolvedPath)) {
        throw "Could not clean the Java temp directory: $resolvedPath"
    }
}
