function Protect-E15BuildLogText {
    [CmdletBinding()]
    param(
        [AllowEmptyString()]
        [string]$Content,
        [string[]]$SensitiveValues = @()
    )

    $redacted = $Content
    foreach ($sensitiveValue in @($SensitiveValues | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })) {
        $redacted = $redacted -replace [regex]::Escape($sensitiveValue), "[REDACTED]"
    }
    $redacted = $redacted -replace `
        '(?im)^(\s*[A-Za-z_][A-Za-z0-9_]*(?:PASSWORD|PASS|TOKEN|SECRET|API_KEY|PRIVATE_KEY)[A-Za-z0-9_]*\s*=\s*).+$', `
        '${1}[REDACTED]'
    $redacted = $redacted -replace `
        '(?i)(<S N="[^"]*(?:PASSWORD|PASS|TOKEN|SECRET|API_KEY|PRIVATE_KEY)[^"]*">)[^<]*(</S>)', `
        '${1}[REDACTED]${2}'
    return $redacted
}

function Protect-E15BuildLogFile {
    [CmdletBinding()]
    param(
        [string]$Path,
        [string[]]$SensitiveValues = @()
    )

    if (-not $Path -or -not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        return [pscustomobject]@{
            passed = $false
            path = $Path
            sha256 = ""
            bytes = 0L
            reason = "Build log is missing."
        }
    }

    $content = Get-Content -LiteralPath $Path -Encoding UTF8 -Raw
    $redacted = Protect-E15BuildLogText -Content $content -SensitiveValues $SensitiveValues
    [IO.File]::WriteAllText($Path, $redacted, [Text.UTF8Encoding]::new($false))
    $file = Get-Item -LiteralPath $Path
    return [pscustomobject]@{
        passed = $true
        path = $file.FullName
        sha256 = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
        bytes = $file.Length
        reason = ""
    }
}
