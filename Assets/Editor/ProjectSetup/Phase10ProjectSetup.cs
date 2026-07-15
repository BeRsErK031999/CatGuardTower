using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class Phase10ProjectSetup
{
    private const string QaApplicationIdentifier = "com.catguard.towerdefense.qa";
    private const string BuildFolder = "Builds/Android";
    private const string ApkPath = BuildFolder + "/CatGuardTowerDefense-qa.apk";
    private const string DebugApkPath = BuildFolder + "/CatGuardTowerDefense-qa-debug.apk";
    private const string EmulatorApkPath = BuildFolder + "/CatGuardTowerDefense-emulator.apk";
    private const string AabPath = BuildFolder + "/CatGuardTowerDefense-qa.aab";

    private static readonly string[] RequiredScenes =
    {
        "Assets/_Project/Scenes/Boot.unity",
        "Assets/_Project/Scenes/MainMenu.unity",
        "Assets/_Project/Scenes/Level.unity"
    };

    public static void Run()
    {
        ConfigureAndroidBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        ValidateAndExit();
    }

    public static void Validate()
    {
        ValidateAndExit();
    }

    public static void BuildApk()
    {
        ConfigureAndroidBuildSettings();
        var succeeded = BuildAndroidArtifact(ApkPath, false, false);
        EditorApplication.Exit(succeeded ? 0 : 1);
    }

    public static void BuildDebugApk()
    {
        ConfigureAndroidBuildSettings();
        var succeeded = BuildAndroidArtifact(DebugApkPath, false, true);
        EditorApplication.Exit(succeeded ? 0 : 1);
    }

    public static void BuildEmulatorApk()
    {
        ConfigureAndroidBuildSettings();
        var succeeded = false;

        try
        {
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.X86_64;
            AssetDatabase.SaveAssets();
            succeeded = BuildAndroidArtifact(EmulatorApkPath, false, true);
        }
        finally
        {
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            EditorUserBuildSettings.development = false;
            EditorUserBuildSettings.buildAppBundle = false;
            AssetDatabase.SaveAssets();
        }

        EditorApplication.Exit(succeeded ? 0 : 1);
    }

    public static void BuildAab()
    {
        ConfigureAndroidBuildSettings();
        var succeeded = BuildAndroidArtifact(AabPath, true, false);
        EditorApplication.Exit(succeeded ? 0 : 1);
    }

    public static void BuildAll()
    {
        ConfigureAndroidBuildSettings();
        var apkSucceeded = BuildAndroidArtifact(ApkPath, false, false);
        var aabSucceeded = BuildAndroidArtifact(AabPath, true, false);
        var debugApkSucceeded = BuildAndroidArtifact(DebugApkPath, false, true);
        EditorUserBuildSettings.development = false;
        EditorUserBuildSettings.buildAppBundle = false;
        EditorApplication.Exit(apkSucceeded && aabSucceeded && debugApkSucceeded ? 0 : 1);
    }

    private static void ConfigureAndroidBuildSettings()
    {
        Directory.CreateDirectory(BuildFolder);

        var androidSelected = EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        if (!androidSelected)
        {
            Debug.LogError("Android build target could not be selected.");
            return;
        }

        EditorUserBuildSettings.androidBuildSystem = AndroidBuildSystem.Gradle;
        EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
        EditorUserBuildSettings.buildAppBundle = false;
        EditorUserBuildSettings.development = false;

        PlayerSettings.productName = "Cat Guard: Tower Defense";
        PlayerSettings.companyName = "CatGuard";
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, QaApplicationIdentifier);
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);

        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.forceInternetPermission = false;
        PlayerSettings.Android.forceSDCardPermission = false;

        QualitySettings.vSyncCount = 0;
        ConfigureBuildScenes();
        AssetDatabase.SaveAssets();
    }

    private static void ConfigureBuildScenes()
    {
        EditorBuildSettings.scenes = RequiredScenes
            .Select(scenePath => new EditorBuildSettingsScene(scenePath, true))
            .ToArray();
    }

    private static bool BuildAndroidArtifact(string path, bool appBundle, bool development)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? BuildFolder);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        EditorUserBuildSettings.buildAppBundle = appBundle;
        EditorUserBuildSettings.development = development;
        var options = development ? BuildOptions.Development : BuildOptions.None;
        var report = BuildPipeline.BuildPlayer(RequiredScenes, path, BuildTarget.Android, options);
        var summary = report.summary;
        var label = development ? "Debug APK" : appBundle ? "AAB" : "APK";

        if (summary.result != BuildResult.Succeeded)
        {
            Debug.LogError($"{label} build failed: {summary.result}.");
            return false;
        }

        if (!File.Exists(path))
        {
            Debug.LogError($"{label} build succeeded but output was not created: {path}");
            return false;
        }

        var fileInfo = new FileInfo(path);
        var sizeMiB = fileInfo.Length / 1024f / 1024f;
        Debug.Log($"{label} build created at {path} ({sizeMiB:F2} MiB).");
        return true;
    }

    private static void ValidateAndExit()
    {
        var errors = new System.Collections.Generic.List<string>();
        ConfigureAndroidBuildSettings();
        ValidateAndroidSettings(errors);
        ValidateBuildScenes(errors);

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
        Debug.Log("Phase 10 validation passed: Android QA build settings, scenes, IL2CPP ARM64 target, and offline-safe permissions are configured.");
        EditorApplication.Exit(0);
    }

    private static void ValidateAndroidSettings(System.Collections.Generic.ICollection<string> errors)
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
        {
            errors.Add("Android build target is not supported by this Unity installation.");
        }

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            errors.Add("Active build target must be Android.");
        }

        if (PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android) != QaApplicationIdentifier)
        {
            errors.Add($"Android QA application id must be {QaApplicationIdentifier}.");
        }

        if (PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) != ScriptingImplementation.IL2CPP)
        {
            errors.Add("Android scripting backend must be IL2CPP.");
        }

        if (PlayerSettings.Android.targetArchitectures != AndroidArchitecture.ARM64)
        {
            errors.Add("Android target architecture must be ARM64 for QA/App Bundle readiness.");
        }

        if (PlayerSettings.defaultInterfaceOrientation != UIOrientation.Portrait)
        {
            errors.Add("Android build must use portrait orientation.");
        }

        if (PlayerSettings.Android.forceInternetPermission)
        {
            errors.Add("Android build must not force Internet permission for offline QA.");
        }

        if (PlayerSettings.Android.forceSDCardPermission)
        {
            errors.Add("Android build must not force external storage permission for save QA.");
        }
    }

    private static void ValidateBuildScenes(System.Collections.Generic.ICollection<string> errors)
    {
        var enabledScenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();

        if (!enabledScenes.SequenceEqual(RequiredScenes))
        {
            errors.Add("Android build scenes must be Boot, MainMenu, Level in that order.");
        }

        foreach (var scenePath in RequiredScenes)
        {
            if (!File.Exists(scenePath))
            {
                errors.Add($"Required scene is missing: {scenePath}");
            }
        }
    }
}
