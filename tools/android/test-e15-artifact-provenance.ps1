Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

. (Join-Path $PSScriptRoot "e15-artifact-provenance.ps1")

$expectedHead = "0123456789abcdef0123456789abcdef01234567"
$expectedUnity = "6000.4.12f1"
$tempBase = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
$tempRoot = [IO.Path]::GetFullPath((Join-Path $tempBase ("catguard-e15-artifact-" + [guid]::NewGuid().ToString("N"))))
if (-not $tempRoot.StartsWith($tempBase, [StringComparison]::OrdinalIgnoreCase) `
    -or (Split-Path -Leaf $tempRoot) -notmatch '^catguard-e15-artifact-[0-9a-f]{32}$') {
    throw "Unsafe temporary test path: $tempRoot"
}

function Copy-Evidence {
    param([object]$Evidence)

    return $Evidence | ConvertTo-Json -Depth 8 | ConvertFrom-Json
}

function Invoke-Fixture {
    param(
        [string]$Name,
        [object]$Evidence,
        [bool]$ExpectedPassed,
        [string]$ExpectedReason = ""
    )

    $path = Join-Path $tempRoot "$Name.json"
    $Evidence | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $path -Encoding UTF8
    $result = Test-E15ArtifactBuildProvenance `
        -ProvenancePath $path `
        -ArtifactPath $script:ArtifactPath `
        -ExpectedArtifact "Apk" `
        -ExpectedGitHead $expectedHead `
        -ExpectedUnityVersion $expectedUnity `
        -ExpectedArtifactRelativePath "Builds/Android/CatGuardTowerDefense-store.apk"
    if ([bool]$result.passed -ne $ExpectedPassed) {
        throw "$Name expected passed=$ExpectedPassed but got $($result.passed): $($result.reasons -join ' ')"
    }
    if ($ExpectedReason -and -not (($result.reasons -join " ") -match $ExpectedReason)) {
        throw "$Name did not emit expected reason '$ExpectedReason': $($result.reasons -join ' ')"
    }

    Write-Host "$Name`: passed=$($result.passed)."
}

New-Item -ItemType Directory -Force -Path $tempRoot | Out-Null
try {
    $script:ArtifactPath = Join-Path $tempRoot "candidate.apk"
    [IO.File]::WriteAllBytes($script:ArtifactPath, [byte[]](1, 2, 3, 4, 5))
    $artifact = Get-Item -LiteralPath $script:ArtifactPath
    $artifactHash = (Get-FileHash -LiteralPath $script:ArtifactPath -Algorithm SHA256).Hash
    $valid = [pscustomobject]@{
        schemaVersion = 1
        passed = $true
        artifact = "Apk"
        artifactRelativePath = "Builds/Android/CatGuardTowerDefense-store.apk"
        artifactSha256 = $artifactHash
        artifactBytes = $artifact.Length
        buildMethod = "Phase11ProjectSetup.BuildSignedApk"
        buildStartedAtUtc = "2026-08-12T08:00:00.0000000Z"
        buildCompletedAtUtc = "2026-08-12T08:02:00.0000000Z"
        gitHeadBefore = $expectedHead
        gitHeadAfter = $expectedHead
        gitBranch = "codex/e15-release-gate"
        cleanWorkingTreeBefore = $true
        cleanWorkingTreeAfter = $true
        allowDirtyWorkingTree = $false
        projectSettingsRestored = $true
        projectSettingsSha256Before = "AABB"
        projectSettingsSha256After = "AABB"
        unityVersion = $expectedUnity
    }

    Invoke-Fixture -Name "valid" -Evidence (Copy-Evidence $valid) -ExpectedPassed $true

    $wrongHead = Copy-Evidence $valid
    $wrongHead.gitHeadAfter = "ffffffffffffffffffffffffffffffffffffffff"
    Invoke-Fixture -Name "wrong-head" -Evidence $wrongHead -ExpectedPassed $false -ExpectedReason "current Git HEAD"

    $dirty = Copy-Evidence $valid
    $dirty.cleanWorkingTreeBefore = $false
    $dirty.allowDirtyWorkingTree = $true
    Invoke-Fixture -Name "dirty-source" -Evidence $dirty -ExpectedPassed $false -ExpectedReason "clean working tree"

    $notRestored = Copy-Evidence $valid
    $notRestored.projectSettingsRestored = $false
    $notRestored.projectSettingsSha256After = "CCDD"
    Invoke-Fixture -Name "settings-not-restored" -Evidence $notRestored -ExpectedPassed $false -ExpectedReason "restoration"

    $wrongHash = Copy-Evidence $valid
    $wrongHash.artifactSha256 = "DEADBEEF"
    Invoke-Fixture -Name "wrong-hash" -Evidence $wrongHash -ExpectedPassed $false -ExpectedReason "SHA-256"
}
finally {
    if (Test-Path -LiteralPath $tempRoot -PathType Container) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force
    }
}

Write-Host "E15 artifact provenance contract tests passed: 5/5."
