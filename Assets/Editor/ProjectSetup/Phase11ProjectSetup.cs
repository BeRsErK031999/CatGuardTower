using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CatGuard.Core.Localization;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class Phase11ProjectSetup
{
    private const string StoreApplicationIdentifier = "com.berserk031999.catguardtower";
    private const string QaApplicationIdentifier = "com.catguard.towerdefense.qa";
    private const string StoreVersionName = "0.1.0";
    private const int StoreVersionCode = 1;
    private const string BuildFolder = "Builds/Android";
    private const string StoreAabPath = BuildFolder + "/CatGuardTowerDefense-store.aab";
    private const string KeystorePathVariable = "CATGUARD_ANDROID_KEYSTORE_PATH";
    private const string KeystorePasswordVariable = "CATGUARD_ANDROID_KEYSTORE_PASSWORD";
    private const string KeyAliasVariable = "CATGUARD_ANDROID_KEY_ALIAS";
    private const string KeyPasswordVariable = "CATGUARD_ANDROID_KEY_PASSWORD";

    private static readonly string[] RequiredScenes =
    {
        "Assets/_Project/Scenes/Boot.unity",
        "Assets/_Project/Scenes/MainMenu.unity",
        "Assets/_Project/Scenes/Level.unity"
    };

    private static readonly string[] RequiredPrivacyLocalizationKeys =
    {
        "privacy.button",
        "privacy.title",
        "privacy.updated",
        "privacy.close",
        "privacy.body"
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

    public static void BuildSignedAab()
    {
        var originalSettings = AndroidBuildSettingsSnapshot.Capture();
        var exitCode = 1;

        try
        {
            ConfigureStoreBuildSettings();

            var signingConfiguration = AndroidSigningConfiguration.FromEnvironment();
            var errors = new List<string>();
            ValidateStoreIdentity(errors);
            ValidateAndroidSettings(errors);
            ValidateBuildScenes(errors);
            ValidateSigningConfiguration(signingConfiguration, errors);

            if (errors.Count > 0)
            {
                throw new InvalidOperationException(string.Join(" ", errors));
            }

            signingConfiguration.Apply();
            if (!BuildStoreAab(StoreAabPath))
            {
                throw new InvalidOperationException("Signed store AAB build did not complete successfully.");
            }

            exitCode = 0;
        }
        catch (Exception exception)
        {
            Debug.LogError($"Signed store AAB build failed: {exception.Message}");
        }
        finally
        {
            originalSettings.Restore();
            AssetDatabase.SaveAssets();
            EditorApplication.Exit(exitCode);
        }
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
        ValidatePrivacyPolicy(errors);

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

    private static void ValidatePrivacyPolicy(ICollection<string> errors)
    {
        var originalLanguage = LocalizationService.CurrentLanguageCode;
        try
        {
            foreach (var languageCode in new[] { LocalizationService.English, LocalizationService.Russian })
            {
                LocalizationService.SetLanguage(languageCode);
                foreach (var key in RequiredPrivacyLocalizationKeys)
                {
                    var value = LocalizationService.Text(key);
                    if (string.IsNullOrWhiteSpace(value) || value == key)
                    {
                        errors.Add($"Privacy policy localization is missing for {languageCode}: {key}.");
                    }
                }
            }
        }
        finally
        {
            LocalizationService.SetLanguage(originalLanguage);
        }
    }

    private static void ValidateSigningConfiguration(
        AndroidSigningConfiguration signingConfiguration,
        ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(signingConfiguration.KeystorePath))
        {
            errors.Add($"Missing {KeystorePathVariable}.");
        }
        else
        {
            try
            {
                signingConfiguration.KeystorePath = Path.GetFullPath(
                    Environment.ExpandEnvironmentVariables(signingConfiguration.KeystorePath.Trim()));

                if (!File.Exists(signingConfiguration.KeystorePath))
                {
                    errors.Add("Android keystore file does not exist.");
                }

                var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                if (IsPathInsideDirectory(signingConfiguration.KeystorePath, projectRoot))
                {
                    errors.Add("Android keystore must be stored outside the Git repository.");
                }
            }
            catch (Exception exception)
            {
                errors.Add($"Android keystore path is invalid: {exception.Message}");
            }
        }

        if (string.IsNullOrEmpty(signingConfiguration.KeystorePassword))
        {
            errors.Add($"Missing {KeystorePasswordVariable}.");
        }

        if (string.IsNullOrWhiteSpace(signingConfiguration.KeyAlias))
        {
            errors.Add($"Missing {KeyAliasVariable}.");
        }
        else
        {
            signingConfiguration.KeyAlias = signingConfiguration.KeyAlias.Trim();
        }

        if (string.IsNullOrEmpty(signingConfiguration.KeyPassword))
        {
            errors.Add($"Missing {KeyPasswordVariable}.");
        }
    }

    private static bool IsPathInsideDirectory(string candidatePath, string directoryPath)
    {
        var normalizedDirectory = Path.GetFullPath(directoryPath)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        var normalizedCandidate = Path.GetFullPath(candidatePath);

        return normalizedCandidate.StartsWith(normalizedDirectory, StringComparison.OrdinalIgnoreCase);
    }

    private static bool BuildStoreAab(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? BuildFolder);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        EditorUserBuildSettings.buildAppBundle = true;
        EditorUserBuildSettings.development = false;

        var report = BuildPipeline.BuildPlayer(RequiredScenes, path, BuildTarget.Android, BuildOptions.None);
        if (report.summary.result != BuildResult.Succeeded)
        {
            Debug.LogError($"Signed store AAB build failed: {report.summary.result}.");
            return false;
        }

        if (!File.Exists(path))
        {
            Debug.LogError($"Signed store AAB build succeeded but output was not created: {path}");
            return false;
        }

        var fileInfo = new FileInfo(path);
        var sizeMiB = fileInfo.Length / 1024f / 1024f;
        Debug.Log($"Signed store AAB created at {path} ({sizeMiB:F2} MiB).");
        return true;
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

    private sealed class AndroidSigningConfiguration
    {
        public string KeystorePath { get; set; }
        public string KeystorePassword { get; set; }
        public string KeyAlias { get; set; }
        public string KeyPassword { get; set; }

        public static AndroidSigningConfiguration FromEnvironment()
        {
            return new AndroidSigningConfiguration
            {
                KeystorePath = Environment.GetEnvironmentVariable(KeystorePathVariable),
                KeystorePassword = Environment.GetEnvironmentVariable(KeystorePasswordVariable),
                KeyAlias = Environment.GetEnvironmentVariable(KeyAliasVariable),
                KeyPassword = Environment.GetEnvironmentVariable(KeyPasswordVariable)
            };
        }

        public void Apply()
        {
            PlayerSettings.Android.useCustomKeystore = true;
            PlayerSettings.Android.keystoreName = KeystorePath;
            PlayerSettings.Android.keystorePass = KeystorePassword;
            PlayerSettings.Android.keyaliasName = KeyAlias;
            PlayerSettings.Android.keyaliasPass = KeyPassword;
        }
    }

    private sealed class AndroidBuildSettingsSnapshot
    {
        private readonly AndroidBuildSystem androidBuildSystem;
        private readonly bool exportAsGoogleAndroidProject;
        private readonly bool buildAppBundle;
        private readonly bool development;
        private readonly string productName;
        private readonly string companyName;
        private readonly string bundleVersion;
        private readonly UIOrientation interfaceOrientation;
        private readonly bool autorotatePortrait;
        private readonly bool autorotatePortraitUpsideDown;
        private readonly bool autorotateLandscapeLeft;
        private readonly bool autorotateLandscapeRight;
        private readonly string applicationIdentifier;
        private readonly ScriptingImplementation scriptingBackend;
        private readonly int bundleVersionCode;
        private readonly AndroidSdkVersions minSdkVersion;
        private readonly AndroidSdkVersions targetSdkVersion;
        private readonly AndroidArchitecture targetArchitectures;
        private readonly bool forceInternetPermission;
        private readonly bool forceSdCardPermission;
        private readonly bool useCustomKeystore;
        private readonly string keystoreName;
        private readonly string keystorePassword;
        private readonly string keyAliasName;
        private readonly string keyAliasPassword;
        private readonly EditorBuildSettingsScene[] scenes;

        private AndroidBuildSettingsSnapshot()
        {
            androidBuildSystem = EditorUserBuildSettings.androidBuildSystem;
            exportAsGoogleAndroidProject = EditorUserBuildSettings.exportAsGoogleAndroidProject;
            buildAppBundle = EditorUserBuildSettings.buildAppBundle;
            development = EditorUserBuildSettings.development;
            productName = PlayerSettings.productName;
            companyName = PlayerSettings.companyName;
            bundleVersion = PlayerSettings.bundleVersion;
            interfaceOrientation = PlayerSettings.defaultInterfaceOrientation;
            autorotatePortrait = PlayerSettings.allowedAutorotateToPortrait;
            autorotatePortraitUpsideDown = PlayerSettings.allowedAutorotateToPortraitUpsideDown;
            autorotateLandscapeLeft = PlayerSettings.allowedAutorotateToLandscapeLeft;
            autorotateLandscapeRight = PlayerSettings.allowedAutorotateToLandscapeRight;
            applicationIdentifier = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);
            scriptingBackend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android);
            bundleVersionCode = PlayerSettings.Android.bundleVersionCode;
            minSdkVersion = PlayerSettings.Android.minSdkVersion;
            targetSdkVersion = PlayerSettings.Android.targetSdkVersion;
            targetArchitectures = PlayerSettings.Android.targetArchitectures;
            forceInternetPermission = PlayerSettings.Android.forceInternetPermission;
            forceSdCardPermission = PlayerSettings.Android.forceSDCardPermission;
            useCustomKeystore = PlayerSettings.Android.useCustomKeystore;
            keystoreName = useCustomKeystore ? PlayerSettings.Android.keystoreName : string.Empty;
            keystorePassword = PlayerSettings.Android.keystorePass;
            keyAliasName = useCustomKeystore ? PlayerSettings.Android.keyaliasName : string.Empty;
            keyAliasPassword = PlayerSettings.Android.keyaliasPass;
            scenes = EditorBuildSettings.scenes;
        }

        public static AndroidBuildSettingsSnapshot Capture()
        {
            return new AndroidBuildSettingsSnapshot();
        }

        public void Restore()
        {
            EditorUserBuildSettings.androidBuildSystem = androidBuildSystem;
            EditorUserBuildSettings.exportAsGoogleAndroidProject = exportAsGoogleAndroidProject;
            EditorUserBuildSettings.buildAppBundle = buildAppBundle;
            EditorUserBuildSettings.development = development;
            PlayerSettings.productName = productName;
            PlayerSettings.companyName = companyName;
            PlayerSettings.bundleVersion = bundleVersion;
            PlayerSettings.defaultInterfaceOrientation = interfaceOrientation;
            PlayerSettings.allowedAutorotateToPortrait = autorotatePortrait;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = autorotatePortraitUpsideDown;
            PlayerSettings.allowedAutorotateToLandscapeLeft = autorotateLandscapeLeft;
            PlayerSettings.allowedAutorotateToLandscapeRight = autorotateLandscapeRight;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, applicationIdentifier);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, scriptingBackend);
            PlayerSettings.Android.bundleVersionCode = bundleVersionCode;
            PlayerSettings.Android.minSdkVersion = minSdkVersion;
            PlayerSettings.Android.targetSdkVersion = targetSdkVersion;
            PlayerSettings.Android.targetArchitectures = targetArchitectures;
            PlayerSettings.Android.forceInternetPermission = forceInternetPermission;
            PlayerSettings.Android.forceSDCardPermission = forceSdCardPermission;
            PlayerSettings.Android.useCustomKeystore = useCustomKeystore;
            PlayerSettings.Android.keystoreName = keystoreName;
            PlayerSettings.Android.keystorePass = keystorePassword;
            PlayerSettings.Android.keyaliasName = keyAliasName;
            PlayerSettings.Android.keyaliasPass = keyAliasPassword;
            EditorBuildSettings.scenes = scenes;
        }
    }
}
