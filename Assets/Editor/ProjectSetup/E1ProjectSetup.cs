using System.Collections.Generic;
using System.IO;
using System.Linq;
using CatGuard.Core.Localization;
using CatGuard.UI.Layout;
using UnityEditor;
using UnityEngine;

public static class E1ProjectSetup
{
    private const string BootstrapPath = "Assets/_Project/Scripts/Core/Bootstrap/GameBootstrap.cs";
    private const string OrientationPolicyPath = "Assets/_Project/Scripts/Core/Bootstrap/OrientationPolicy.cs";
    private const string MainMenuPath = "Assets/_Project/Scripts/UI/Screens/MainMenuController.cs";
    private const string HudPath = "Assets/_Project/Scripts/UI/HUD/PrototypeHud.cs";

    private static readonly string[] RequiredScenes =
    {
        "Assets/_Project/Scenes/Boot.unity",
        "Assets/_Project/Scenes/MainMenu.unity",
        "Assets/_Project/Scenes/Level.unity"
    };

    private static readonly string[] RequiredLocalizationKeys =
    {
        "menu.landscapeHint",
        "hud.waveState",
        "hud.statePreparing",
        "hud.stateRunning"
    };

    public static void Run()
    {
        AndroidOrientationSettings.ConfigureLandscapeAutoRotation();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        ValidateAndExit();
    }

    public static void Validate()
    {
        ValidateAndExit();
    }

    private static void ValidateAndExit()
    {
        var errors = new List<string>();
        AndroidOrientationSettings.ValidateLandscapeAutoRotation(errors);
        ValidateLayoutContract(errors);
        ValidateRuntimeOrientationBoundary(errors);
        ValidateBuildScenes(errors);
        ValidateLocalization(errors);

        if (errors.Count > 0)
        {
            foreach (var error in errors)
            {
                Debug.LogError(error);
            }

            EditorApplication.Exit(1);
            return;
        }

        Debug.Log(
            "E1 validation passed: landscape Auto Rotation, landscape-only directions, 1920x1080 layout, 1280x720 minimum viewport, runtime orientation boundary, scenes, and RU/EN UI strings are configured.");
        EditorApplication.Exit(0);
    }

    private static void ValidateLayoutContract(ICollection<string> errors)
    {
        if (!Mathf.Approximately(LandscapeLayout.ReferenceWidth, 1920f)
            || !Mathf.Approximately(LandscapeLayout.ReferenceHeight, 1080f))
        {
            errors.Add("E1 landscape reference surface must be 1920 x 1080.");
        }

        if (!Mathf.Approximately(LandscapeLayout.MinimumViewportWidth, 1280f)
            || !Mathf.Approximately(LandscapeLayout.MinimumViewportHeight, 720f))
        {
            errors.Add("E1 minimum logical viewport must be 1280 x 720.");
        }

        foreach (var sourcePath in new[] { MainMenuPath, HudPath })
        {
            if (!File.Exists(sourcePath))
            {
                errors.Add($"Required E1 UI source is missing: {sourcePath}");
                continue;
            }

            var source = File.ReadAllText(sourcePath);
            if (source.Contains("DesignWidth = 540") || source.Contains("DesignHeight = 1200"))
            {
                errors.Add($"Active E1 UI source still contains a portrait 540 x 1200 layout assumption: {sourcePath}");
            }
        }
    }

    private static void ValidateRuntimeOrientationBoundary(ICollection<string> errors)
    {
        if (!File.Exists(BootstrapPath) || !File.Exists(OrientationPolicyPath))
        {
            errors.Add("E1 runtime orientation boundary sources are missing.");
            return;
        }

        var bootstrapSource = File.ReadAllText(BootstrapPath);
        if (!bootstrapSource.Contains("OrientationPolicy.Apply()"))
        {
            errors.Add("GameBootstrap must apply the centralized OrientationPolicy before loading MainMenu.");
        }

        var policySource = File.ReadAllText(OrientationPolicyPath);
        foreach (var requiredToken in new[]
                 {
                     "Screen.autorotateToLandscapeLeft = true",
                     "Screen.autorotateToLandscapeRight = true",
                     "Screen.autorotateToPortrait = false",
                     "Screen.autorotateToPortraitUpsideDown = false",
                     "Screen.orientation = ScreenOrientation.AutoRotation"
                 })
        {
            if (!policySource.Contains(requiredToken))
            {
                errors.Add($"OrientationPolicy is missing required runtime rule: {requiredToken}");
            }
        }
    }

    private static void ValidateBuildScenes(ICollection<string> errors)
    {
        var enabledScenes = EditorBuildSettings.scenes
            .Where(scene => scene.enabled)
            .Select(scene => scene.path)
            .ToArray();
        if (!enabledScenes.SequenceEqual(RequiredScenes))
        {
            errors.Add("E1 Android build scenes must remain Boot, MainMenu, Level in that order.");
        }

        foreach (var scenePath in RequiredScenes)
        {
            if (!File.Exists(scenePath))
            {
                errors.Add($"Required E1 scene is missing: {scenePath}");
            }
        }
    }

    private static void ValidateLocalization(ICollection<string> errors)
    {
        var originalLanguage = LocalizationService.CurrentLanguageCode;
        try
        {
            foreach (var languageCode in new[] { LocalizationService.English, LocalizationService.Russian })
            {
                LocalizationService.SetLanguage(languageCode);
                foreach (var key in RequiredLocalizationKeys)
                {
                    var value = LocalizationService.Text(key);
                    if (string.IsNullOrWhiteSpace(value) || value == key)
                    {
                        errors.Add($"E1 landscape localization is missing for {languageCode}: {key}.");
                    }
                }
            }
        }
        finally
        {
            LocalizationService.SetLanguage(originalLanguage);
        }
    }
}
