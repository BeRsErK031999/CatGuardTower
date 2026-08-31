function Get-E15RequiredTechnicalGatePreconditionIds {
    return @(
        "PLAYTEST-001",
        "DEVICE-QA-001",
        "STORE-ACCOUNT-001",
        "release-owner",
        "release-decision-state",
        "readiness-state",
        "clean-worktree",
        "upstream-sync",
        "candidate-base",
        "physical-device-input",
        "emulator-input",
        "store-reset-confirmation",
        "adb",
        "emulator-target",
        "physical-device-target",
        "artifact-set-manifest",
        "artifact-set-contract",
        "heavy-wave-performance-evidence"
    )
}

function Get-E15RequiredTechnicalGateStepIds {
    $ids = New-Object System.Collections.Generic.List[string]
    foreach ($name in @(
        "test-android-qa-provenance",
        "test-e15-artifact-provenance",
        "test-e15-artifact-set-manifest",
        "test-e15-baseline-build-diagnostics",
        "test-e15-baseline-provenance",
        "test-e15-block-manifest",
        "test-e15-java-temp",
        "test-e15-signing-credential-bundle",
        "test-e15-signing-credential-access",
        "test-e15-signing-credential-rotation",
        "test-e15-performance-evidence")) {
        $ids.Add("desktop:$name")
    }
    foreach ($phase in 1..11) {
        $ids.Add("unity:Phase${phase}ProjectSetup.Validate")
    }
    foreach ($section in 1..14) {
        $ids.Add("unity:E${section}ProjectSetup.Validate")
    }
    $ids.Add("unity:E15ProjectSetup.ValidateReadiness")
    foreach ($id in @(
        "build:development-apk",
        "android:e14-functional",
        "android:level12-default-economy",
        "android:signed-physical-release",
        "store:assets",
        "git:candidate-diff-check",
        "git:post-gate-clean-worktree",
        "artifact:artifact-set-stability",
        "artifact:hashes")) {
        $ids.Add($id)
    }

    return $ids.ToArray()
}

function Test-E15PathInsideDirectory {
    param(
        [string]$Path,
        [string]$Directory
    )

    if (-not $Path -or -not $Directory) {
        return $false
    }
    $root = [IO.Path]::GetFullPath($Directory).TrimEnd('\', '/') + [IO.Path]::DirectorySeparatorChar
    $candidate = [IO.Path]::GetFullPath($Path)
    return $candidate.StartsWith($root, [StringComparison]::OrdinalIgnoreCase)
}

function Test-E15TechnicalGateManifest {
    [CmdletBinding()]
    param(
        [string]$ManifestPath,
        [string]$ExpectedGitHead,
        [string]$ExpectedCandidateBase,
        [System.Collections.IDictionary]$ExpectedArtifactHashes,
        [string]$ExpectedSourceAssetLicenseAuditSha256,
        [switch]$RequireEvidenceFiles
    )

    $reasons = New-Object System.Collections.Generic.List[string]
    $manifest = $null
    if (-not $ManifestPath -or -not (Test-Path -LiteralPath $ManifestPath -PathType Leaf)) {
        $reasons.Add("E15 technical gate manifest is missing: $ManifestPath")
    }
    else {
        try {
            $manifest = Get-Content -LiteralPath $ManifestPath -Encoding UTF8 -Raw | ConvertFrom-Json
        }
        catch {
            $reasons.Add("E15 technical gate manifest is not valid JSON: $($_.Exception.Message)")
        }
    }

    if ($null -ne $manifest) {
        $requiredProperties = @(
            "schemaVersion",
            "generatedAtUtc",
            "state",
            "passed",
            "preflightOnly",
            "gitBranch",
            "gitHead",
            "candidateBase",
            "candidateFiles",
            "sourceAssetLicenseAuditSha256",
            "runRoot",
            "preconditions",
            "steps"
        )
        $missingProperties = @($requiredProperties | Where-Object {
            $null -eq $manifest.PSObject.Properties[$_]
        })
        if ($missingProperties.Count -gt 0) {
            $reasons.Add("E15 technical gate manifest schema is incomplete: $($missingProperties -join ', ').")
        }
        else {
            if ([int]$manifest.schemaVersion -ne 1 `
                -or $manifest.state -ne "technical_gate_passed" `
                -or -not [bool]$manifest.passed `
                -or [bool]$manifest.preflightOnly) {
                $reasons.Add("E15 manifest is not a successful full technical gate.")
            }
            if ([string]::IsNullOrWhiteSpace([string]$manifest.gitBranch)) {
                $reasons.Add("E15 manifest Git branch is missing.")
            }
            if ([string]$manifest.gitHead -notmatch '^[0-9a-f]{40}$' `
                -or ($ExpectedGitHead -and $manifest.gitHead -ne $ExpectedGitHead)) {
                $reasons.Add("E15 manifest Git HEAD does not match '$ExpectedGitHead'.")
            }
            if ([string]$manifest.candidateBase -notmatch '^[0-9a-f]{40}$' `
                -or ($ExpectedCandidateBase -and $manifest.candidateBase -ne $ExpectedCandidateBase)) {
                $reasons.Add("E15 manifest candidate base does not match '$ExpectedCandidateBase'.")
            }
            if ([string]$manifest.sourceAssetLicenseAuditSha256 -notmatch '^[0-9A-Fa-f]{64}$' `
                -or ($ExpectedSourceAssetLicenseAuditSha256 `
                    -and $manifest.sourceAssetLicenseAuditSha256 -ine $ExpectedSourceAssetLicenseAuditSha256)) {
                $reasons.Add("E15 manifest source/asset license audit hash does not match the expected file.")
            }

            $generatedAt = [DateTime]::MinValue
            if (-not [DateTime]::TryParse(
                [string]$manifest.generatedAtUtc,
                [Globalization.CultureInfo]::InvariantCulture,
                [Globalization.DateTimeStyles]::RoundtripKind,
                [ref]$generatedAt)) {
                $reasons.Add("E15 manifest generation timestamp is invalid.")
            }

            $manifestDirectory = [IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($ManifestPath))
            $resolvedRunRoot = if ($manifest.runRoot) { [IO.Path]::GetFullPath([string]$manifest.runRoot) } else { "" }
            if (-not $resolvedRunRoot `
                -or $manifestDirectory.TrimEnd('\', '/') -ine $resolvedRunRoot.TrimEnd('\', '/')) {
                $reasons.Add("E15 manifest path is not rooted in its declared run directory.")
            }

            $preconditions = @($manifest.preconditions)
            $preconditionIds = @($preconditions | ForEach-Object { [string]$_.id })
            $duplicatePreconditions = @($preconditionIds | Group-Object | Where-Object { $_.Count -gt 1 })
            if ($duplicatePreconditions.Count -gt 0) {
                $reasons.Add("E15 manifest contains duplicate precondition IDs: $($duplicatePreconditions.Name -join ', ').")
            }
            $missingPreconditions = @(Get-E15RequiredTechnicalGatePreconditionIds | Where-Object {
                $_ -notin $preconditionIds
            })
            if ($missingPreconditions.Count -gt 0) {
                $reasons.Add("E15 manifest is missing required preconditions: $($missingPreconditions -join ', ').")
            }
            $artifactPreconditions = @($preconditions | Where-Object { [string]$_.id -like 'artifact:*' })
            if ($artifactPreconditions.Count -ne 6) {
                $reasons.Add("E15 manifest must contain exactly six artifact file preconditions.")
            }
            $failedPreconditions = @($preconditions | Where-Object { -not [bool]$_.passed })
            if ($failedPreconditions.Count -gt 0) {
                $reasons.Add("E15 manifest contains failed preconditions: $($failedPreconditions.id -join ', ').")
            }

            $steps = @($manifest.steps)
            $stepIds = @($steps | ForEach-Object { [string]$_.id })
            $duplicateSteps = @($stepIds | Group-Object | Where-Object { $_.Count -gt 1 })
            if ($duplicateSteps.Count -gt 0) {
                $reasons.Add("E15 manifest contains duplicate step IDs: $($duplicateSteps.Name -join ', ').")
            }
            $requiredStepIds = @(Get-E15RequiredTechnicalGateStepIds)
            $missingSteps = @($requiredStepIds | Where-Object { $_ -notin $stepIds })
            if ($missingSteps.Count -gt 0) {
                $reasons.Add("E15 manifest is missing required steps: $($missingSteps -join ', ').")
            }
            $unexpectedSteps = @($stepIds | Where-Object { $_ -notin $requiredStepIds })
            if ($unexpectedSteps.Count -gt 0) {
                $reasons.Add("E15 manifest contains unexpected steps: $($unexpectedSteps -join ', ').")
            }
            $failedSteps = @($steps | Where-Object {
                -not [bool]$_.passed -or [int]$_.exitCode -ne 0
            })
            if ($failedSteps.Count -gt 0) {
                $reasons.Add("E15 manifest contains failed steps: $($failedSteps.id -join ', ').")
            }

            foreach ($step in @($steps | Where-Object { $_.id -ne 'artifact:hashes' })) {
                $stepPropertyNames = @($step.PSObject.Properties.Name)
                if ("logPath" -notin $stepPropertyNames `
                    -or "logSha256" -notin $stepPropertyNames `
                    -or [string]$step.logSha256 -notmatch '^[0-9A-Fa-f]{64}$') {
                    $reasons.Add("E15 step '$($step.id)' does not contain a valid log hash.")
                    continue
                }
                if (-not (Test-E15PathInsideDirectory -Path ([string]$step.logPath) -Directory $resolvedRunRoot)) {
                    $reasons.Add("E15 step '$($step.id)' log is outside the declared run directory.")
                }
                elseif ($RequireEvidenceFiles) {
                    if (-not (Test-Path -LiteralPath $step.logPath -PathType Leaf)) {
                        $reasons.Add("E15 step '$($step.id)' log is missing: $($step.logPath)")
                    }
                    elseif ((Get-FileHash -LiteralPath $step.logPath -Algorithm SHA256).Hash -ine $step.logSha256) {
                        $reasons.Add("E15 step '$($step.id)' log hash does not match its bytes.")
                    }
                }

                if ([string]$step.id -like 'unity:*') {
                    if ([string]$step.requiredPattern -ne "validation passed" `
                        -or -not [bool]$step.evidencePassed `
                        -or [string]$step.evidenceLogSha256 -notmatch '^[0-9A-Fa-f]{64}$' `
                        -or -not (Test-E15PathInsideDirectory -Path ([string]$step.evidenceLogPath) -Directory $resolvedRunRoot)) {
                        $reasons.Add("E15 Unity step '$($step.id)' does not contain valid validation-marker evidence.")
                    }
                    elseif ($RequireEvidenceFiles) {
                        if (-not (Test-Path -LiteralPath $step.evidenceLogPath -PathType Leaf)) {
                            $reasons.Add("E15 Unity step '$($step.id)' evidence log is missing: $($step.evidenceLogPath)")
                        }
                        elseif ((Get-FileHash -LiteralPath $step.evidenceLogPath -Algorithm SHA256).Hash -ine $step.evidenceLogSha256) {
                            $reasons.Add("E15 Unity step '$($step.id)' evidence log hash does not match its bytes.")
                        }
                    }
                }
            }

            $hashStep = @($steps | Where-Object { $_.id -eq 'artifact:hashes' } | Select-Object -First 1)
            if ($hashStep.Count -eq 1 -and $null -ne $hashStep[0].PSObject.Properties["hashes"]) {
                $requiredHashNames = @(
                    "developmentApk",
                    "baselineApk",
                    "baselineApkProvenance",
                    "candidateApk",
                    "candidateAab",
                    "candidateApkProvenance",
                    "candidateAabProvenance",
                    "artifactSetManifest"
                )
                foreach ($hashName in $requiredHashNames) {
                    $property = $hashStep[0].hashes.PSObject.Properties[$hashName]
                    if ($null -eq $property -or [string]$property.Value -notmatch '^[0-9A-Fa-f]{64}$') {
                        $reasons.Add("E15 artifact hash '$hashName' is missing or invalid.")
                    }
                    elseif ($ExpectedArtifactHashes) {
                        if (-not $ExpectedArtifactHashes.Contains($hashName)) {
                            $reasons.Add("Expected E15 artifact hash '$hashName' was not supplied to the validator.")
                        }
                        elseif ([string]$property.Value -ine [string]$ExpectedArtifactHashes[$hashName]) {
                            $reasons.Add("E15 artifact hash '$hashName' does not match the expected file.")
                        }
                    }
                }
            }
            else {
                $reasons.Add("E15 manifest artifact hash step is missing its hash map.")
            }
        }
    }

    return [pscustomobject]@{
        required = $true
        passed = $reasons.Count -eq 0
        path = $ManifestPath
        sha256 = if ($ManifestPath -and (Test-Path -LiteralPath $ManifestPath -PathType Leaf)) {
            (Get-FileHash -LiteralPath $ManifestPath -Algorithm SHA256).Hash
        }
        else {
            ""
        }
        reasons = $reasons.ToArray()
        evidence = $manifest
    }
}
