. (Join-Path $PSScriptRoot "e15-build-log-redaction.ps1")

function Copy-E15BaselineBuildDiagnostics {
    [CmdletBinding()]
    param(
        [string]$SourceDirectory,
        [string]$DestinationRoot,
        [string[]]$SensitiveValues = @()
    )

    $sourceLogs = @()
    if ($SourceDirectory -and (Test-Path -LiteralPath $SourceDirectory -PathType Container)) {
        $sourceLogs = @(Get-ChildItem -LiteralPath $SourceDirectory -File -Filter "*.log" | Sort-Object Name)
    }
    if ($sourceLogs.Count -eq 0) {
        return [pscustomobject]@{
            passed = $false
            runRoot = ""
            files = @()
            reason = "No baseline Unity build logs were found."
        }
    }

    $runId = "baseline-build-failure-{0}-{1}" -f `
        (Get-Date).ToUniversalTime().ToString("yyyyMMdd-HHmmssfff"), `
        ([guid]::NewGuid().ToString("N").Substring(0, 8))
    $runRoot = Join-Path ([IO.Path]::GetFullPath($DestinationRoot)) $runId
    New-Item -ItemType Directory -Force -Path $runRoot | Out-Null

    $copiedFiles = New-Object System.Collections.Generic.List[object]
    foreach ($sourceLog in $sourceLogs) {
        $destination = Join-Path $runRoot $sourceLog.Name
        $sourceContent = Get-Content -LiteralPath $sourceLog.FullName -Encoding UTF8 -Raw
        $redactedContent = Protect-E15BuildLogText `
            -Content $sourceContent `
            -SensitiveValues $SensitiveValues
        [IO.File]::WriteAllText($destination, $redactedContent, [Text.UTF8Encoding]::new($false))
        $copied = Get-Item -LiteralPath $destination
        $copiedFiles.Add([pscustomobject]@{
            name = $copied.Name
            path = $copied.FullName
            bytes = $copied.Length
            sha256 = (Get-FileHash -LiteralPath $copied.FullName -Algorithm SHA256).Hash
        })
    }

    return [pscustomobject]@{
        passed = $true
        runRoot = $runRoot
        files = $copiedFiles.ToArray()
        reason = ""
    }
}
