function Test-E15ArtifactSetPathInsideDirectory {
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

function Test-E15ArtifactSetManifest {
    [CmdletBinding()]
    param(
        [string]$ManifestPath,
        [string]$ExpectedGitHead,
        [string]$ExpectedUpstream,
        [string]$ExpectedBaselineCommit,
        [string]$ExpectedRepositoryRoot,
        [System.Collections.IDictionary]$ExpectedArtifactPaths,
        [switch]$RequireEvidenceFiles
    )

    $reasons = New-Object System.Collections.Generic.List[string]
    $manifest = $null
    if (-not $ManifestPath -or -not (Test-Path -LiteralPath $ManifestPath -PathType Leaf)) {
        $reasons.Add("E15 artifact-set manifest is missing: $ManifestPath")
    }
    else {
        try {
            $manifest = Get-Content -LiteralPath $ManifestPath -Encoding UTF8 -Raw | ConvertFrom-Json
        }
        catch {
            $reasons.Add("E15 artifact-set manifest is not valid JSON: $($_.Exception.Message)")
        }
    }

    if ($null -ne $manifest) {
        $requiredProperties = @(
            "schemaVersion",
            "state",
            "passed",
            "generatedAtUtc",
            "buildStartedAtUtc",
            "gitBranch",
            "gitHead",
            "upstream",
            "baselineCommit",
            "runRoot",
            "artifacts",
            "hashes",
            "artifactPreflightManifestPath",
            "artifactPreflightManifestSha256",
            "artifactPreflightLogPath",
            "artifactPreflightLogSha256"
        )
        $missingProperties = @($requiredProperties | Where-Object {
            $null -eq $manifest.PSObject.Properties[$_]
        })
        if ($missingProperties.Count -gt 0) {
            $reasons.Add("E15 artifact-set manifest schema is incomplete: $($missingProperties -join ', ').")
        }
        else {
            if ([int]$manifest.schemaVersion -ne 1 `
                -or $manifest.state -ne "artifact_set_prepared" `
                -or -not [bool]$manifest.passed) {
                $reasons.Add("E15 artifact-set manifest does not represent a successful preparation run.")
            }
            if ([string]::IsNullOrWhiteSpace([string]$manifest.gitBranch)) {
                $reasons.Add("E15 artifact-set Git branch is missing.")
            }
            if ([string]$manifest.gitHead -notmatch '^[0-9a-f]{40}$' `
                -or $manifest.gitHead -ne $manifest.upstream `
                -or ($ExpectedGitHead -and $manifest.gitHead -ne $ExpectedGitHead) `
                -or ($ExpectedUpstream -and $manifest.upstream -ne $ExpectedUpstream)) {
                $reasons.Add("E15 artifact set is not bound to the expected pushed Git HEAD.")
            }
            if ([string]$manifest.baselineCommit -notmatch '^[0-9a-f]{40}$' `
                -or ($ExpectedBaselineCommit -and $manifest.baselineCommit -ne $ExpectedBaselineCommit)) {
                $reasons.Add("E15 artifact set is not bound to the expected historical baseline commit.")
            }

            $startedAt = [DateTime]::MinValue
            $generatedAt = [DateTime]::MinValue
            $timestampsValid = [DateTime]::TryParse(
                [string]$manifest.buildStartedAtUtc,
                [Globalization.CultureInfo]::InvariantCulture,
                [Globalization.DateTimeStyles]::RoundtripKind,
                [ref]$startedAt) `
                -and [DateTime]::TryParse(
                    [string]$manifest.generatedAtUtc,
                    [Globalization.CultureInfo]::InvariantCulture,
                    [Globalization.DateTimeStyles]::RoundtripKind,
                    [ref]$generatedAt)
            if (-not $timestampsValid -or $generatedAt -lt $startedAt) {
                $reasons.Add("E15 artifact-set timestamps are invalid.")
            }

            $manifestDirectory = [IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($ManifestPath))
            $runRoot = if ($manifest.runRoot) { [IO.Path]::GetFullPath([string]$manifest.runRoot) } else { "" }
            if (-not $runRoot `
                -or $manifestDirectory.TrimEnd('\', '/') -ine $runRoot.TrimEnd('\', '/')) {
                $reasons.Add("E15 artifact-set manifest is outside its declared run directory.")
            }

            $artifactNames = @(
                "baselineApk",
                "baselineApkProvenance",
                "candidateApk",
                "candidateAab",
                "candidateApkProvenance",
                "candidateAabProvenance"
            )
            foreach ($name in $artifactNames) {
                $pathProperty = $manifest.artifacts.PSObject.Properties[$name]
                $hashProperty = $manifest.hashes.PSObject.Properties[$name]
                if ($null -eq $pathProperty -or [string]::IsNullOrWhiteSpace([string]$pathProperty.Value)) {
                    $reasons.Add("E15 artifact-set path '$name' is missing.")
                    continue
                }
                if ($null -eq $hashProperty -or [string]$hashProperty.Value -notmatch '^[0-9A-Fa-f]{64}$') {
                    $reasons.Add("E15 artifact-set hash '$name' is missing or invalid.")
                    continue
                }
                $artifactPath = [string]$pathProperty.Value
                if ($null -ne $ExpectedArtifactPaths) {
                    if (-not $ExpectedArtifactPaths.Contains($name)) {
                        $reasons.Add("E15 expected artifact path '$name' is missing from the validation input.")
                    }
                    elseif ([IO.Path]::GetFullPath($artifactPath) -ine [IO.Path]::GetFullPath([string]$ExpectedArtifactPaths[$name])) {
                        $reasons.Add("E15 artifact-set path '$name' does not match the artifact selected for the full gate.")
                    }
                }
                if ($ExpectedRepositoryRoot `
                    -and -not (Test-E15ArtifactSetPathInsideDirectory -Path $artifactPath -Directory $ExpectedRepositoryRoot)) {
                    $reasons.Add("E15 artifact '$name' is outside the expected repository.")
                }
                if ($RequireEvidenceFiles) {
                    if (-not (Test-Path -LiteralPath $artifactPath -PathType Leaf)) {
                        $reasons.Add("E15 artifact '$name' is missing: $artifactPath")
                    }
                    elseif ((Get-FileHash -LiteralPath $artifactPath -Algorithm SHA256).Hash -ine [string]$hashProperty.Value) {
                        $reasons.Add("E15 artifact '$name' hash does not match its bytes.")
                    }
                }
            }

            foreach ($evidence in @(
                [pscustomobject]@{
                    name = "artifact-only preflight manifest"
                    path = [string]$manifest.artifactPreflightManifestPath
                    hash = [string]$manifest.artifactPreflightManifestSha256
                },
                [pscustomobject]@{
                    name = "artifact-only preflight log"
                    path = [string]$manifest.artifactPreflightLogPath
                    hash = [string]$manifest.artifactPreflightLogSha256
                })) {
                if (-not (Test-E15ArtifactSetPathInsideDirectory -Path $evidence.path -Directory $runRoot)) {
                    $reasons.Add("E15 $($evidence.name) is outside the declared run directory.")
                }
                if ($evidence.hash -notmatch '^[0-9A-Fa-f]{64}$') {
                    $reasons.Add("E15 $($evidence.name) hash is missing or invalid.")
                }
                elseif ($RequireEvidenceFiles) {
                    if (-not (Test-Path -LiteralPath $evidence.path -PathType Leaf)) {
                        $reasons.Add("E15 $($evidence.name) is missing: $($evidence.path)")
                    }
                    elseif ((Get-FileHash -LiteralPath $evidence.path -Algorithm SHA256).Hash -ine $evidence.hash) {
                        $reasons.Add("E15 $($evidence.name) hash does not match its bytes.")
                    }
                }
            }

            $preflight = $null
            if (Test-Path -LiteralPath $manifest.artifactPreflightManifestPath -PathType Leaf) {
                try {
                    $preflight = Get-Content -LiteralPath $manifest.artifactPreflightManifestPath -Encoding UTF8 -Raw |
                        ConvertFrom-Json
                }
                catch {
                    $reasons.Add("E15 artifact-only preflight manifest is not valid JSON.")
                }
            }
            if ($RequireEvidenceFiles -and $null -ne $preflight) {
                $preflightProperties = @($preflight.PSObject.Properties.Name)
                $requiredPreflightProperties = @(
                    "passed",
                    "kind",
                    "fullReleaseGate",
                    "gitHead",
                    "baselineApk",
                    "baselineBuildProvenance",
                    "candidateApk",
                    "candidateAab",
                    "candidateApkBuildProvenance",
                    "candidateAabBuildProvenance"
                )
                $missingPreflightProperties = @($requiredPreflightProperties | Where-Object { $_ -notin $preflightProperties })
                if ($missingPreflightProperties.Count -gt 0) {
                    $reasons.Add("E15 artifact-only preflight schema is incomplete: $($missingPreflightProperties -join ', ').")
                }
                else {
                    if (-not [bool]$preflight.passed `
                        -or $preflight.kind -ne "artifact-only-preflight" `
                        -or [bool]$preflight.fullReleaseGate `
                        -or $preflight.gitHead -ne $manifest.gitHead) {
                        $reasons.Add("E15 artifact-only preflight is not bound to the artifact-set Git HEAD.")
                    }
                    $preflightBindings = @(
                        [pscustomobject]@{ value = $preflight.baselineApk.sha256; expected = $manifest.hashes.baselineApk; name = "baseline APK" },
                        [pscustomobject]@{ value = $preflight.candidateApk.sha256; expected = $manifest.hashes.candidateApk; name = "candidate APK" },
                        [pscustomobject]@{ value = $preflight.candidateAab.sha256; expected = $manifest.hashes.candidateAab; name = "candidate AAB" },
                        [pscustomobject]@{ value = $preflight.baselineBuildProvenance.sha256; expected = $manifest.hashes.baselineApkProvenance; name = "baseline provenance" },
                        [pscustomobject]@{ value = $preflight.candidateApkBuildProvenance.sha256; expected = $manifest.hashes.candidateApkProvenance; name = "candidate APK provenance" },
                        [pscustomobject]@{ value = $preflight.candidateAabBuildProvenance.sha256; expected = $manifest.hashes.candidateAabProvenance; name = "candidate AAB provenance" }
                    )
                    foreach ($binding in $preflightBindings) {
                        if ([string]$binding.value -ine [string]$binding.expected) {
                            $reasons.Add("E15 artifact-only preflight $($binding.name) hash does not match the artifact set.")
                        }
                    }
                    foreach ($provenance in @(
                        $preflight.baselineBuildProvenance,
                        $preflight.candidateApkBuildProvenance,
                        $preflight.candidateAabBuildProvenance)) {
                        if (-not [bool]$provenance.passed) {
                            $reasons.Add("E15 artifact-only preflight contains failed build provenance.")
                        }
                    }
                }
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
