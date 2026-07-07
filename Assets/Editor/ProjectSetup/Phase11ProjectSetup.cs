using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

public static class Phase11ProjectSetup
{
    private const string StoreApplicationIdentifier = "com.berserk031999.catguardtower";
    private const string QaApplicationIdentifier = "com.catguard.towerdefense.qa";
    private const string StoreVersionName = "0.1.0";
    private const int StoreVersionCode = 1;
    private const string BuildFolder = "Builds/Android";

    private static readonly string[] RequiredScenes =
    {
        "Assets/_Project/Scenes/Boot.unity",
        "Assets/_Project/Scenes/MainMenu.unity",
        "Assets/_Project/Scenes/Level.unity"
    };

    public static void Run()
    {
        ConfigureStoreBuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        ValidateAndExit();
    }

    public static void Validate()
    {
        ValidateAndExit();
    }

    private static void ConfigureStoreBuildSettings()
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
        EditorUserBuildSettings.buildAppBundle = true;
        EditorUserBuildSettings.development = false;

        PlayerSettings.productName = "Cat Guard: Tower Defense";
        PlayerSettings.companyName = "CatGuard";
        PlayerSettings.bundleVersion = StoreVersionName;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, StoreApplicationIdentifier);
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);

        PlayerSettings.Android.bundleVersionCode = StoreVersionCode;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.forceInternetPermission = false;
        PlayerSettings.Android.forceSDCardPermission = false;

        ConfigureBuildScenes();
        AssetDatabase.SaveAssets();
    }

    private static void ConfigureBuildScenes()
    {
        EditorBuildSettings.scenes = RequiredScenes
            .Select(scenePath => new EditorBuildSettingsScene(scenePath, true))
            .ToArray();
    }

    private static void ValidateAndExit()
    {
        var errors = new System.Collections.Generic.List<string>();
        ConfigureStoreBuildSettings();
        ValidateStoreIdentity(errors);
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
        Debug.Log($"Phase 11 validation passed: store package {StoreApplicationIdentifier}, version {StoreVersionName} ({StoreVersionCode}), AAB-ready Android settings, and scenes are configured.");
        EditorApplication.Exit(0);
    }

    private static void ValidateStoreIdentity(System.Collections.Generic.ICollection<string> errors)
    {
        var currentIdentifier = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
        if (currentIdentifier != StoreApplicationIdentifier)
        {
            errors.Add($"Android store application id must be {StoreApplicationIdentifier}.");
        }

        if (currentIdentifier == QaApplicationIdentifier)
        {
            errors.Add("Android store application id must not use the Phase 10 QA package name.");
        }

        if (!Regex.IsMatch(StoreApplicationIdentifier, "^[a-z][a-z0-9_]*(\\.[a-z][a-z0-9_]*)+$"))
        {
            errors.Add("Android store application id must use a valid reverse-DNS package format.");
        }

        if (PlayerSettings.bundleVersion != StoreVersionName)
        {
            errors.Add($"Android store versionName must be {StoreVersionName}.");
        }

        if (PlayerSettings.Android.bundleVersionCode != StoreVersionCode)
        {
            errors.Add($"Android store versionCode must be {StoreVersionCode}.");
        }
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

        if (!EditorUserBuildSettings.buildAppBundle)
        {
            errors.Add("Phase 11 store build settings must prefer Android App Bundle output.");
        }

        if (PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) != ScriptingImplementation.IL2CPP)
        {
            errors.Add("Android scripting backend must be IL2CPP.");
        }

        if (PlayerSettings.Android.targetArchitectures != AndroidArchitecture.ARM64)
        {
            errors.Add("Android target architecture must be ARM64 for Google Play readiness.");
        }

        if (PlayerSettings.defaultInterfaceOrientation != UIOrientation.Portrait)
        {
            errors.Add("Android build must use portrait orientation.");
        }

        if (PlayerSettings.Android.forceInternetPermission)
        {
            errors.Add("Android build must not force Internet permission before SDK/data safety decisions are final.");
        }

        if (PlayerSettings.Android.forceSDCardPermission)
        {
            errors.Add("Android build must not force external storage permission.");
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
