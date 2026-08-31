using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

public static class E15ProjectSetup
{
    private const string WorkflowPath = "docs/planning/E15_RELEASE_GATE_WORKFLOW.md";
    private const string ReadinessPath = "docs/planning/E15_RELEASE_READINESS.md";
    private const string ReportPath = "docs/planning/E15_EXPANSION_RELEASE_REPORT.md";
    private const string ReleaseNotesPath = "docs/release/RELEASE_NOTES_0.2.0.md";
    private const string KnownIssuesPath = "docs/release/KNOWN_ISSUES_0.2.0.md";
    private const string LicenseAuditPath = "docs/release/SOURCE_ASSET_LICENSE_AUDIT.md";
    private const string DecisionPath = "docs/release/E15_RELEASE_DECISION.md";
    private const string ExternalBacklogPath = "docs/planning/EXTERNAL_PRODUCTION_BACKLOG.md";
    private const string StoreAssetFolder = "docs/store/assets/screenshots";

    private static readonly string[] LandscapeStoreScreenshots =
    {
        "01-home-hub-campaign-1920x1080.png",
        "02-tower-placement-1920x1080.png",
        "03-boss-combat-1920x1080.png",
        "04-victory-progression-1920x1080.png",
        "05-quests-achievements-1920x1080.png"
    };

    [MenuItem("Cat Guard/Validate E15 Release Readiness")]
    public static void ValidateReadiness()
    {
        ValidateAndExit(false);
    }

    public static void Validate()
    {
        ValidateAndExit(true);
    }

    private static void ValidateAndExit(bool requireFinalApproval)
    {
        var errors = new List<string>();
        Phase11ProjectSetup.RunWithTemporaryStoreBuildSettings(
            () => ValidateAndroidReleaseSettings(errors));
        ValidateSdkAndPermissionContract(errors);
        ValidateReleaseArtifacts(errors);
        ValidateNoCommittedSecrets(errors);

        if (requireFinalApproval)
        {
            ValidateLandscapeStoreAssets(errors);
            ValidateOwnerApproval(errors);
        }

        if (errors.Count > 0)
        {
            foreach (var error in errors)
            {
                Debug.LogError(error);
            }

            EditorApplication.Exit(1);
            return;
        }

        AssetDatabase.SaveAssets();
        var gate = requireFinalApproval ? "final release" : "internal readiness";
        Debug.Log($"E15 {gate} validation passed: Android store identity {Phase11ProjectSetup.StoreApplicationIdentifier} {Phase11ProjectSetup.StoreVersionName} ({Phase11ProjectSetup.StoreVersionCode}), ARM64/IL2CPP, API 36 readiness, no-live-SDK Data Safety boundary, secret-free signing workflow, release documents, source/license audit, and E15 QA orchestration are present.");
        EditorApplication.Exit(0);
    }

    private static void ValidateAndroidReleaseSettings(ICollection<string> errors)
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
        {
            errors.Add("E15 requires Unity Android Build Support.");
        }

        if (PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android)
            != Phase11ProjectSetup.StoreApplicationIdentifier)
        {
            errors.Add("E15 store package identity is not configured.");
        }

        if (PlayerSettings.bundleVersion != Phase11ProjectSetup.StoreVersionName
            || PlayerSettings.Android.bundleVersionCode != Phase11ProjectSetup.StoreVersionCode)
        {
            errors.Add("E15 expansion candidate must use versionName 0.2.0 and versionCode 2.");
        }

        if (PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) != ScriptingImplementation.IL2CPP
            || PlayerSettings.Android.targetArchitectures != AndroidArchitecture.ARM64)
        {
            errors.Add("E15 Google Play artifacts must use IL2CPP and ARM64 only.");
        }

        if (PlayerSettings.Android.minSdkVersion != AndroidSdkVersions.AndroidApiLevel25)
        {
            errors.Add("E15 minimum Android API must remain 25.");
        }

        var targetApi = (int)PlayerSettings.Android.targetSdkVersion;
        if (targetApi != (int)AndroidSdkVersions.AndroidApiLevelAuto && targetApi < 36)
        {
            errors.Add("E15 must resolve target API 36 or newer for the 2026 Google Play deadline.");
        }

        AndroidOrientationSettings.ValidateLandscapeAutoRotation(errors);
        if (PlayerSettings.Android.forceInternetPermission || PlayerSettings.Android.forceSDCardPermission)
        {
            errors.Add("E15 no-live-SDK candidate must not force Internet or external-storage permissions.");
        }
    }

    private static void ValidateSdkAndPermissionContract(ICollection<string> errors)
    {
        ValidateFileContains(
            "ProjectSettings/UnityConnectSettings.asset",
            new[]
            {
                "UnityPurchasingSettings:",
                "UnityAnalyticsSettings:",
                "UnityAdsSettings:",
                "m_EnableCloudDiagnosticsReporting: 0"
            },
            errors);

        var connectSettings = ReadText("ProjectSettings/UnityConnectSettings.asset");
        var enabledServiceLines = connectSettings
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(line => line.Trim() == "m_Enabled: 1")
            .ToArray();
        if (enabledServiceLines.Length > 0)
        {
            errors.Add("A Unity online service is enabled but the E15 Data Safety declaration says no data is transmitted.");
        }

        var manifest = ReadText("Packages/manifest.json");
        foreach (var forbiddenPackage in new[] { "firebase", "crashlytics", "purchasing", "advertisement" })
        {
            if (manifest.Contains(forbiddenPackage, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"Live SDK package '{forbiddenPackage}' requires a new privacy/Data Safety review.");
            }
        }

        var defines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Android);
        if (defines.Contains("CATGUARD_FIREBASE_ANALYTICS", StringComparison.Ordinal))
        {
            errors.Add("CATGUARD_FIREBASE_ANALYTICS must remain disabled for the E15 no-data-collection candidate.");
        }
    }

    private static void ValidateReleaseArtifacts(ICollection<string> errors)
    {
        var requiredFiles = new Dictionary<string, string[]>
        {
            [WorkflowPath] = new[] { "clean install", "upgrade install", "offline", "physical device", "run-e15-release-gate.ps1", "run-e15-default-economy-qa.ps1" },
            [ReadinessPath] = new[] { "Status:", "PLAYTEST-001", "DEVICE-QA-001", "STORE-ACCOUNT-001" },
            [ReportPath] = new[] { "Status:", "PLAYTEST-001", "DEVICE-QA-001", "STORE-ACCOUNT-001", "technical_gate_passed", "Technical gate status:", "Remote verification:", "Completion rule" },
            [ReleaseNotesPath] = new[] { "0.2.0", "12", "landscape", "save schema v5" },
            [KnownIssuesPath] = new[] { "performance", "physical", "store" },
            [LicenseAuditPath] = new[] { "OpenAI image generation", "procedural", "third-party", "paid" },
            [DecisionPath] = new[] { "Status:", "Owner", "Release decision", "Remaining risks" },
            ["docs/release/E15_PLAYTEST_HANDOFF.md"] = new[] { "PLAYTEST-001", "Required ratings", "Completion rule" },
            ["docs/release/E15_PLAYTEST_FINDINGS.md"] = new[] { "PLAYTEST-001", "Owner results", "Cohort progress" },
            ["docs/release/E15_OWNER_INPUT_HANDOFF.md"] = new[] { "STORE-ACCOUNT-001", "Upload-key decision", "Completion rule" },
            ["docs/store/STORE_LISTING_DRAFT.md"] = new[] { "0.2.0", "12", "landscape", "Guardian ultimates" },
            ["docs/store/DATA_SAFETY_DRAFT.md"] = new[] { "2026-08-10", "target SDK 36", "No" },
            ["docs/store/PRIVACY_POLICY_DRAFT.md"] = new[] { "0.2.0", "quests", "achievements", "text scale" },
            ["docs/store/STORE_ASSET_CHECKLIST.md"] = new[] { "1920 x 1080", "E15", "legacy" },
            ["Assets/_Project/Scripts/Core/Localization/LocalizationService.cs"] = new[] { "August 10, 2026", "10 августа 2026", "codex discoveries", "записи кодекса" },
            ["tools/android/build-signed-store-aab.ps1"] = new[] { "BuildSignedAab", "BuildSignedApk", "Artifact", "provenance.json", "gitHeadBefore", "projectSettingsRestored", "JavaTempRoot", "Protect-E15BuildLogFile" },
            ["tools/android/build-e15-baseline-apk.ps1"] = new[] { "28f7e88", "worktree", "bundletool", "universal.apk", "BaselineUniversalApk", "orchestratorGitHeadBefore", "sourceProjectSettingsRestored", "JavaTempRoot", "Copy-E15BaselineBuildDiagnostics", "Preserved diagnostics" },
            ["tools/android/prepare-e15-release-artifacts.ps1"] = new[] { "PreflightOnly", "upstream-sync", "java-temp-root", "build-e15-baseline-apk.ps1", "e15-build-log-redaction.ps1", "e15-baseline-build-diagnostics.ps1", "e15-java-temp.ps1", "build-signed-store-aab.ps1", "run-e15-release-gate.ps1", "ArtifactOnly", "Test-E15ArtifactSetManifest", "artifact_set_prepared", "artifact_set_contract_failed", "e15-artifact-set.json" },
            ["tools/android/run-e15-release-gate.ps1"] = new[] { "BaselineApkPath", "BaselineApkProvenancePath", "BaselineCommit", "CandidateApkPath", "CandidateAabPath", "CandidateApkProvenancePath", "CandidateAabProvenancePath", "HeavyWavePerformanceEvidencePath", "level_12-heavy-wave", "physical-hardware", "runtimeCheckpointBefore", "runtimeCheckpointAfter", "upgrade" },
            ["tools/android/run-e15-default-economy-qa.ps1"] = new[] { "level_12", "StartingLives = 0", "StartingBattleFish = 0", "RequireVictory", "com.catguard.towerdefense.qa" },
            ["tools/android/run-e15-block-gate.ps1"] = new[] { "PreflightOnly", "PLAYTEST-001", "DEVICE-QA-001", "STORE-ACCOUNT-001", "ArtifactSetManifestPath", "artifact-set-contract", "artifact:artifact-set-stability", "artifactSetManifest", "ConfirmStorePackageReset", "test-android-qa-provenance.ps1", "test-e15-artifact-provenance.ps1", "test-e15-artifact-set-manifest.ps1", "test-e15-baseline-build-diagnostics.ps1", "test-e15-baseline-provenance.ps1", "test-e15-block-manifest.ps1", "test-e15-java-temp.ps1", "test-e15-performance-evidence.ps1", "Test-E15TechnicalGateManifest", "technical_gate_passed" },
            ["tools/android/e15-artifact-provenance.ps1"] = new[] { "Test-E15ArtifactBuildProvenance", "Test-E15BaselineBuildProvenance", "cleanWorkingTreeBefore", "projectSettingsRestored", "ExpectedGitHead" },
            ["tools/android/e15-artifact-set-manifest.ps1"] = new[] { "Test-E15ArtifactSetManifest", "artifact_set_prepared", "artifact-only-preflight", "ExpectedRepositoryRoot", "ExpectedArtifactPaths", "RequireEvidenceFiles" },
            ["tools/android/e15-build-log-redaction.ps1"] = new[] { "Protect-E15BuildLogText", "Protect-E15BuildLogFile", "SensitiveValues", "[REDACTED]" },
            ["tools/android/e15-baseline-build-diagnostics.ps1"] = new[] { "Copy-E15BaselineBuildDiagnostics", "baseline-build-failure", "SensitiveValues", "*.log", "sha256" },
            ["tools/android/e15-java-temp.ps1"] = new[] { "Test-E15JavaTempRoot", "New-E15JavaTempDirectory", "Remove-E15JavaTempDirectory", "CGE15J-", "32 characters" },
            ["tools/android/e15-block-manifest.ps1"] = new[] { "Test-E15TechnicalGateManifest", "technical_gate_passed", "artifact:hashes", "artifactSetManifest", "logSha256", "RequireEvidenceFiles" },
            ["tools/android/test-e15-artifact-provenance.ps1"] = new[] { "wrong-head", "dirty-source", "settings-not-restored", "wrong-hash", "contract tests passed" },
            ["tools/android/test-e15-artifact-set-manifest.ps1"] = new[] { "mismatched-input", "wrong-head", "missing-artifact", "wrong-hash", "wrong-preflight", "tampered-log", "contract tests passed" },
            ["tools/android/test-e15-baseline-build-diagnostics.ps1"] = new[] { "valid", "missing", "password.txt", "diagnostic tests passed" },
            ["tools/android/test-e15-baseline-provenance.ps1"] = new[] { "wrong-source", "wrong-orchestrator", "dirty-source", "wrong-hash", "contract tests passed" },
            ["tools/android/test-e15-block-manifest.ps1"] = new[] { "wrong-head", "missing-step", "duplicate-step", "failed-precondition", "wrong-artifact-hash", "tampered-log", "contract tests passed" },
            ["tools/android/test-e15-java-temp.ps1"] = new[] { "valid", "too-long", "unsafe-cleanup", "contract tests passed" },
            ["tools/android/android-qa-provenance.ps1"] = new[] { "emulator-software", "emulator-host-gpu", "physical-hardware", "releaseAcceptanceEligible", "SwiftShader" },
            ["tools/android/test-android-qa-provenance.ps1"] = new[] { "physical-software", "expectedEligible", "provenance tests passed" },
            ["tools/android/test-e15-performance-evidence.ps1"] = new[] { "software-renderer", "short-window", "load-exited", "wrong-apk", "contract tests passed" },
            ["tools/android/run-device-qa.ps1"] = new[] { "RequireReleasePerformanceEvidence", "level_12-heavy-wave", "installedApkSha256", "graphicsProvenance", "runtimeCheckpointBefore", "runtimeCheckpointAfter" },
            ["Assets/_Project/Scripts/QA/ReleasePerformanceCheckpoint.cs"] = new[] { "level_12", "heavyWaveEligible", "activeEnemyCount", "graphicsDeviceName" },
            ["tools/store/validate-store-assets.ps1"] = new[] { "1920", "1080", "boss-combat" }
        };

        foreach (var pair in requiredFiles)
        {
            ValidateFileContains(pair.Key, pair.Value, errors);
        }
    }

    private static void ValidateNoCommittedSecrets(ICollection<string> errors)
    {
        var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        foreach (var relativeRoot in new[] { "Assets", "Packages", "ProjectSettings", "docs", "tools" })
        {
            var searchRoot = Path.Combine(projectRoot, relativeRoot);
            if (!Directory.Exists(searchRoot))
            {
                continue;
            }

            foreach (var pattern in new[] { "*.keystore", "*.jks", "google-services.json" })
            {
                foreach (var path in Directory.GetFiles(searchRoot, pattern, SearchOption.AllDirectories))
                {
                    errors.Add($"Potential release secret is inside the repository: {path}");
                }
            }
        }
    }

    private static void ValidateLandscapeStoreAssets(ICollection<string> errors)
    {
        foreach (var fileName in LandscapeStoreScreenshots)
        {
            var path = Path.Combine(StoreAssetFolder, fileName);
            if (!TryReadPngSize(path, out var width, out var height))
            {
                errors.Add($"E15 landscape store screenshot is missing or invalid: {path}");
                continue;
            }

            if (width != 1920 || height != 1080)
            {
                errors.Add($"E15 store screenshot must be 1920x1080: {path} is {width}x{height}.");
            }
        }
    }

    private static void ValidateOwnerApproval(ICollection<string> errors)
    {
        var decision = ReadText(DecisionPath);
        if (!HasStatus(decision, "approved"))
        {
            errors.Add("E15 release decision is not approved by the owner.");
        }

        var readiness = ReadText(ReadinessPath);
        if (!HasStatus(readiness, "completed"))
        {
            errors.Add("E15 readiness document is not completed.");
        }

        var report = ReadText(ReportPath);
        if (!HasStatus(report, "completed"))
        {
            errors.Add("E15 expansion release report is not completed.");
        }

        if (!HasFieldValue(report, "Technical gate status", "passed"))
        {
            errors.Add("E15 expansion release report does not record a passed technical gate.");
        }

        if (!HasFieldValue(report, "Remote verification", "develop == origin/develop"))
        {
            errors.Add("E15 expansion release report does not confirm develop == origin/develop.");
        }

        var backlog = ReadText(ExternalBacklogPath);
        foreach (var id in new[] { "PLAYTEST-001", "DEVICE-QA-001", "STORE-ACCOUNT-001" })
        {
            var section = ReadSection(backlog, $"ID: `{id}`");
            if (!HasStatus(section, "Completed"))
            {
                errors.Add($"External E15 P0 block is not complete: {id}.");
            }
        }

        var privacy = ReadText("docs/store/PRIVACY_POLICY_DRAFT.md");
        foreach (var placeholder in new[] { "[developer legal/display name]", "[privacy contact", "[publish date]" })
        {
            if (privacy.Contains(placeholder, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"Privacy policy owner placeholder remains unresolved: {placeholder}");
            }
        }
    }

    private static bool HasStatus(string content, string expectedStatus)
    {
        var statusLine = content
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .FirstOrDefault(line => line.StartsWith("Status:", StringComparison.OrdinalIgnoreCase));

        if (statusLine == null)
        {
            return false;
        }

        var status = statusLine.Substring("Status:".Length).Trim().Trim('`');
        return string.Equals(status, expectedStatus, StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasFieldValue(string content, string fieldName, string expectedValue)
    {
        var prefix = fieldName + ":";
        var fieldLine = content
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .FirstOrDefault(line => line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

        if (fieldLine == null)
        {
            return false;
        }

        var value = fieldLine.Substring(prefix.Length).Trim().Trim('`');
        return string.Equals(value, expectedValue, StringComparison.OrdinalIgnoreCase);
    }

    private static void ValidateFileContains(string path, IEnumerable<string> tokens, ICollection<string> errors)
    {
        if (!File.Exists(path))
        {
            errors.Add($"E15 required file is missing: {path}");
            return;
        }

        var content = ReadText(path);
        foreach (var token in tokens)
        {
            if (!content.Contains(token, StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"E15 required token '{token}' is missing from {path}.");
            }
        }
    }

    private static string ReadText(string path)
    {
        return File.Exists(path) ? File.ReadAllText(path) : string.Empty;
    }

    private static string ReadSection(string content, string marker)
    {
        var start = content.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0)
        {
            return string.Empty;
        }

        var end = content.IndexOf("\n## ", start + marker.Length, StringComparison.Ordinal);
        return end < 0 ? content.Substring(start) : content.Substring(start, end - start);
    }

    private static bool TryReadPngSize(string path, out int width, out int height)
    {
        width = 0;
        height = 0;
        if (!File.Exists(path))
        {
            return false;
        }

        var bytes = File.ReadAllBytes(path);
        if (bytes.Length < 24
            || bytes[0] != 0x89
            || bytes[1] != 0x50
            || bytes[2] != 0x4E
            || bytes[3] != 0x47)
        {
            return false;
        }

        width = ReadBigEndianInt32(bytes, 16);
        height = ReadBigEndianInt32(bytes, 20);
        return width > 0 && height > 0;
    }

    private static int ReadBigEndianInt32(byte[] bytes, int offset)
    {
        return (bytes[offset] << 24)
               | (bytes[offset + 1] << 16)
               | (bytes[offset + 2] << 8)
               | bytes[offset + 3];
    }
}
