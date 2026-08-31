function Test-E15ArtifactBuildProvenance {
    [CmdletBinding()]
    param(
        [string]$ProvenancePath,
        [string]$ArtifactPath,
        [ValidateSet("Aab", "Apk")]
        [string]$ExpectedArtifact,
        [string]$ExpectedGitHead,
        [string]$ExpectedUnityVersion,
        [string]$ExpectedArtifactRelativePath = ""
    )

    $reasons = New-Object System.Collections.Generic.List[string]
    $evidence = $null
    if (-not $ProvenancePath -or -not (Test-Path -LiteralPath $ProvenancePath -PathType Leaf)) {
        $reasons.Add("Artifact build provenance JSON is missing: $ProvenancePath")
    }
    else {
        try {
            $evidence = Get-Content -LiteralPath $ProvenancePath -Encoding UTF8 -Raw | ConvertFrom-Json
        }
        catch {
            $reasons.Add("Artifact build provenance is not valid JSON: $($_.Exception.Message)")
        }
    }

    if (-not $ArtifactPath -or -not (Test-Path -LiteralPath $ArtifactPath -PathType Leaf)) {
        $reasons.Add("Artifact file is missing: $ArtifactPath")
    }

    if ($null -ne $evidence) {
        $requiredProperties = @(
            "schemaVersion",
            "passed",
            "artifact",
            "artifactRelativePath",
            "artifactSha256",
            "artifactBytes",
            "buildMethod",
            "buildStartedAtUtc",
            "buildCompletedAtUtc",
            "gitHeadBefore",
            "gitHeadAfter",
            "gitBranch",
            "cleanWorkingTreeBefore",
            "cleanWorkingTreeAfter",
            "allowDirtyWorkingTree",
            "projectSettingsRestored",
            "projectSettingsSha256Before",
            "projectSettingsSha256After",
            "unityVersion"
        )
        $missingProperties = @($requiredProperties | Where-Object {
            $null -eq $evidence.PSObject.Properties[$_]
        })
        if ($missingProperties.Count -gt 0) {
            $reasons.Add("Artifact provenance schema is incomplete: $($missingProperties -join ', ').")
        }
        else {
            $expectedMethod = if ($ExpectedArtifact -eq "Aab") {
                "Phase11ProjectSetup.BuildSignedAab"
            }
            else {
                "Phase11ProjectSetup.BuildSignedApk"
            }
            if ([int]$evidence.schemaVersion -ne 1 `
                -or -not [bool]$evidence.passed `
                -or $evidence.artifact -ne $ExpectedArtifact `
                -or $evidence.buildMethod -ne $expectedMethod) {
                $reasons.Add("Artifact provenance type or build method does not match $ExpectedArtifact.")
            }
            if ($ExpectedArtifactRelativePath `
                -and $evidence.artifactRelativePath -ne $ExpectedArtifactRelativePath.Replace('\', '/')) {
                $reasons.Add("Artifact provenance path does not match '$ExpectedArtifactRelativePath'.")
            }
            if ($evidence.gitHeadBefore -ne $ExpectedGitHead `
                -or $evidence.gitHeadAfter -ne $ExpectedGitHead) {
                $reasons.Add("Artifact provenance is not bound to the current Git HEAD '$ExpectedGitHead'.")
            }
            if (-not [bool]$evidence.cleanWorkingTreeBefore `
                -or -not [bool]$evidence.cleanWorkingTreeAfter `
                -or [bool]$evidence.allowDirtyWorkingTree) {
                $reasons.Add("Release artifacts must be built from and leave a clean working tree without an override.")
            }
            if (-not [bool]$evidence.projectSettingsRestored `
                -or $evidence.projectSettingsSha256Before -ne $evidence.projectSettingsSha256After) {
                $reasons.Add("Artifact build did not prove restoration of ProjectSettings.asset.")
            }
            if ($evidence.unityVersion -ne $ExpectedUnityVersion) {
                $reasons.Add("Artifact provenance Unity version '$($evidence.unityVersion)' does not match '$ExpectedUnityVersion'.")
            }

            $startedAt = [DateTime]::MinValue
            $completedAt = [DateTime]::MinValue
            $timestampsValid = [DateTime]::TryParse(
                [string]$evidence.buildStartedAtUtc,
                [Globalization.CultureInfo]::InvariantCulture,
                [Globalization.DateTimeStyles]::RoundtripKind,
                [ref]$startedAt) `
                -and [DateTime]::TryParse(
                    [string]$evidence.buildCompletedAtUtc,
                    [Globalization.CultureInfo]::InvariantCulture,
                    [Globalization.DateTimeStyles]::RoundtripKind,
                    [ref]$completedAt)
            if (-not $timestampsValid -or $completedAt -lt $startedAt) {
                $reasons.Add("Artifact provenance build timestamps are invalid.")
            }
        }
    }

    if ($null -ne $evidence -and (Test-Path -LiteralPath $ArtifactPath -PathType Leaf)) {
        $artifactFile = Get-Item -LiteralPath $ArtifactPath
        $actualSha256 = (Get-FileHash -LiteralPath $ArtifactPath -Algorithm SHA256).Hash
        if ($evidence.artifactSha256 -ine $actualSha256 `
            -or [long]$evidence.artifactBytes -ne [long]$artifactFile.Length) {
            $reasons.Add("Artifact bytes or SHA-256 do not match the build provenance.")
        }
    }

    return [pscustomobject]@{
        required = $true
        passed = $reasons.Count -eq 0
        path = $ProvenancePath
        sha256 = if ($ProvenancePath -and (Test-Path -LiteralPath $ProvenancePath -PathType Leaf)) {
            (Get-FileHash -LiteralPath $ProvenancePath -Algorithm SHA256).Hash
        }
        else {
            ""
        }
        reasons = $reasons.ToArray()
        evidence = $evidence
    }
}

function Test-E15BaselineBuildProvenance {
    [CmdletBinding()]
    param(
        [string]$ProvenancePath,
        [string]$ArtifactPath,
        [string]$ExpectedBaselineCommit,
        [string]$ExpectedOrchestratorGitHead,
        [string]$ExpectedUnityVersion
    )

    $reasons = New-Object System.Collections.Generic.List[string]
    $evidence = $null
    if (-not $ProvenancePath -or -not (Test-Path -LiteralPath $ProvenancePath -PathType Leaf)) {
        $reasons.Add("Baseline build provenance JSON is missing: $ProvenancePath")
    }
    else {
        try {
            $evidence = Get-Content -LiteralPath $ProvenancePath -Encoding UTF8 -Raw | ConvertFrom-Json
        }
        catch {
            $reasons.Add("Baseline build provenance is not valid JSON: $($_.Exception.Message)")
        }
    }

    if (-not $ArtifactPath -or -not (Test-Path -LiteralPath $ArtifactPath -PathType Leaf)) {
        $reasons.Add("Baseline APK is missing: $ArtifactPath")
    }

    if ($null -ne $evidence) {
        $requiredProperties = @(
            "schemaVersion",
            "passed",
            "artifact",
            "artifactFileName",
            "artifactSha256",
            "artifactBytes",
            "packageName",
            "versionName",
            "versionCode",
            "baselineCommitRequested",
            "baselineCommitResolved",
            "orchestratorGitHeadBefore",
            "orchestratorGitHeadAfter",
            "orchestratorGitBranch",
            "currentWorkingTreeCleanBefore",
            "currentWorkingTreeCleanAfter",
            "sourceWorktreeCleanBefore",
            "sourceWorktreeCleanAfter",
            "sourceProjectSettingsRestored",
            "sourceProjectSettingsSha256Before",
            "sourceProjectSettingsSha256After",
            "baselineAabSha256",
            "baselineAabBytes",
            "apksSha256",
            "apksBytes",
            "transformation",
            "bundletoolSha256",
            "unityVersion",
            "buildStartedAtUtc",
            "buildCompletedAtUtc"
        )
        $missingProperties = @($requiredProperties | Where-Object {
            $null -eq $evidence.PSObject.Properties[$_]
        })
        if ($missingProperties.Count -gt 0) {
            $reasons.Add("Baseline provenance schema is incomplete: $($missingProperties -join ', ').")
        }
        else {
            if ([int]$evidence.schemaVersion -ne 1 `
                -or -not [bool]$evidence.passed `
                -or $evidence.artifact -ne "BaselineUniversalApk" `
                -or $evidence.packageName -ne "com.berserk031999.catguardtower" `
                -or $evidence.versionName -ne "0.1.0" `
                -or [int]$evidence.versionCode -ne 1 `
                -or $evidence.transformation -ne "bundletool build-apks --mode universal") {
                $reasons.Add("Baseline provenance identity or transformation contract is invalid.")
            }
            if ($evidence.baselineCommitResolved -ne $ExpectedBaselineCommit) {
                $reasons.Add("Baseline provenance is not bound to expected source commit '$ExpectedBaselineCommit'.")
            }
            if ($evidence.orchestratorGitHeadBefore -ne $ExpectedOrchestratorGitHead `
                -or $evidence.orchestratorGitHeadAfter -ne $ExpectedOrchestratorGitHead) {
                $reasons.Add("Baseline provenance was not produced by the current orchestrator Git HEAD '$ExpectedOrchestratorGitHead'.")
            }
            if (-not [bool]$evidence.currentWorkingTreeCleanBefore `
                -or -not [bool]$evidence.currentWorkingTreeCleanAfter `
                -or -not [bool]$evidence.sourceWorktreeCleanBefore `
                -or -not [bool]$evidence.sourceWorktreeCleanAfter) {
                $reasons.Add("Baseline provenance does not prove clean current and historical source worktrees.")
            }
            if (-not [bool]$evidence.sourceProjectSettingsRestored `
                -or $evidence.sourceProjectSettingsSha256Before -ne $evidence.sourceProjectSettingsSha256After) {
                $reasons.Add("Baseline provenance does not prove historical ProjectSettings.asset restoration.")
            }
            if ($evidence.unityVersion -ne $ExpectedUnityVersion) {
                $reasons.Add("Baseline provenance Unity version '$($evidence.unityVersion)' does not match '$ExpectedUnityVersion'.")
            }
            foreach ($hashProperty in @("baselineAabSha256", "apksSha256", "bundletoolSha256")) {
                if ([string]$evidence.$hashProperty -notmatch '^[0-9A-Fa-f]{64}$') {
                    $reasons.Add("Baseline provenance $hashProperty is not a SHA-256 value.")
                }
            }
            if ([long]$evidence.baselineAabBytes -le 0 -or [long]$evidence.apksBytes -le 0) {
                $reasons.Add("Baseline provenance intermediate artifact sizes are invalid.")
            }

            $startedAt = [DateTime]::MinValue
            $completedAt = [DateTime]::MinValue
            $timestampsValid = [DateTime]::TryParse(
                [string]$evidence.buildStartedAtUtc,
                [Globalization.CultureInfo]::InvariantCulture,
                [Globalization.DateTimeStyles]::RoundtripKind,
                [ref]$startedAt) `
                -and [DateTime]::TryParse(
                    [string]$evidence.buildCompletedAtUtc,
                    [Globalization.CultureInfo]::InvariantCulture,
                    [Globalization.DateTimeStyles]::RoundtripKind,
                    [ref]$completedAt)
            if (-not $timestampsValid -or $completedAt -lt $startedAt) {
                $reasons.Add("Baseline provenance build timestamps are invalid.")
            }
        }
    }

    if ($null -ne $evidence -and (Test-Path -LiteralPath $ArtifactPath -PathType Leaf)) {
        $artifactFile = Get-Item -LiteralPath $ArtifactPath
        $actualSha256 = (Get-FileHash -LiteralPath $ArtifactPath -Algorithm SHA256).Hash
        if ($evidence.artifactFileName -ne $artifactFile.Name `
            -or $evidence.artifactSha256 -ine $actualSha256 `
            -or [long]$evidence.artifactBytes -ne [long]$artifactFile.Length) {
            $reasons.Add("Baseline APK name, bytes, or SHA-256 do not match the provenance.")
        }
    }

    return [pscustomobject]@{
        required = $true
        passed = $reasons.Count -eq 0
        path = $ProvenancePath
        sha256 = if ($ProvenancePath -and (Test-Path -LiteralPath $ProvenancePath -PathType Leaf)) {
            (Get-FileHash -LiteralPath $ProvenancePath -Algorithm SHA256).Hash
        }
        else {
            ""
        }
        reasons = $reasons.ToArray()
        evidence = $evidence
    }
}
