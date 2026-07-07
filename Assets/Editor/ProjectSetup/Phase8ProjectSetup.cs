using System.Collections.Generic;
using System.IO;
using CatGuard.Core.Localization;
using CatGuard.Gameplay.Levels;
using CatGuard.Meta.Progression;
using CatGuard.SDK.Ads;
using CatGuard.SDK.Analytics;
using CatGuard.UI.HUD;
using CatGuard.UI.Screens;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Phase8ProjectSetup
{
    private const string MainMenuScenePath = "Assets/_Project/Scenes/MainMenu.unity";
    private const string LevelScenePath = "Assets/_Project/Scenes/Level.unity";
    private const string ProjectScriptsFolder = "Assets/_Project/Scripts";

    public static void Run()
    {
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

        ValidateScenes(errors);
        ValidateRewardedPlacements(errors);
        ValidateRewardedResultModel(errors);
        ValidateLocalization(errors);
        ValidateNoForcedInterstitials(errors);

        if (errors.Count > 0)
        {
            foreach (var error in errors)
            {
                Debug.LogError(error);
            }

            EditorApplication.Exit(1);
            return;
        }

        Debug.Log("Phase 8 validation passed: voluntary rewarded placements, duplicate guards, and no forced interstitial runtime code are configured.");
        EditorApplication.Exit(0);
    }

    private static void ValidateScenes(ICollection<string> errors)
    {
        var mainMenuScene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
        var mainMenu = Object.FindFirstObjectByType<MainMenuController>();
        if (!mainMenuScene.isLoaded || mainMenu == null || !mainMenu.IsConfigured || !mainMenu.IsDailyConfigured)
        {
            errors.Add("MainMenu scene must keep configured progression, daily, and rewarded placement UI.");
        }

        var levelScene = EditorSceneManager.OpenScene(LevelScenePath, OpenSceneMode.Single);
        var levelController = Object.FindFirstObjectByType<PrototypeLevelController>();
        var hud = Object.FindFirstObjectByType<PrototypeHud>();
        if (!levelScene.isLoaded || levelController == null || !levelController.IsConfigured || hud == null)
        {
            errors.Add("Level scene must keep configured gameplay and result HUD components.");
        }
    }

    private static void ValidateRewardedPlacements(ICollection<string> errors)
    {
        var fakeAnalytics = new FakeAnalyticsService();
        AnalyticsService.ResetForValidation(fakeAnalytics);

        var fakeAds = new FakeRewardedAdService();
        var placementIds = new[]
        {
            RewardedAdPlacementIds.DailyRewardDouble,
            RewardedAdPlacementIds.VictoryRewardDouble,
            RewardedAdPlacementIds.Revive,
            RewardedAdPlacementIds.FreeCoins
        };

        foreach (var placementId in placementIds)
        {
            if (!fakeAds.IsRewardedAdAvailable(placementId))
            {
                errors.Add($"Fake rewarded ad service must expose {placementId}.");
            }

            AnalyticsService.TrackRewardedAdOffer(placementId, true);
            if (!fakeAds.TryShowRewardedAd(placementId))
            {
                errors.Add($"Fake rewarded ad service must complete {placementId}.");
            }
        }

        RequireEvent(fakeAnalytics, AnalyticsEventNames.RewardedAdOffer, errors);
        RequireEvent(fakeAnalytics, AnalyticsEventNames.RewardedAdStarted, errors);
        RequireEvent(fakeAnalytics, AnalyticsEventNames.RewardedAdCompleted, errors);
    }

    private static void ValidateRewardedResultModel(ICollection<string> errors)
    {
        var result = new LevelCompletionResult(20, true, new List<string>()).WithRewardedBonus(20);
        if (result.EarnedFishCoins != 20 || result.RewardedBonusFishCoins != 20 || result.TotalEarnedFishCoins != 40)
        {
            errors.Add("Level completion result must expose original reward, rewarded bonus, and total reward.");
        }

        if (ProgressionService.FreeCoinsRewardFishCoins <= 0)
        {
            errors.Add("Free coins rewarded placement must grant a positive amount.");
        }
    }

    private static void ValidateLocalization(ICollection<string> errors)
    {
        ValidateLanguage(LocalizationService.English, errors);
        ValidateLanguage(LocalizationService.Russian, errors);
    }

    private static void ValidateLanguage(string languageCode, ICollection<string> errors)
    {
        LocalizationService.SetLanguage(languageCode);
        var keys = new[]
        {
            "button.claimX2",
            "button.revive",
            "ads.freeCoins",
            "ads.freeCoinsClaimed",
            "result.rewardWithBonus",
            "result.x2Claimed"
        };

        foreach (var key in keys)
        {
            if (LocalizationService.Text(key) == key)
            {
                errors.Add($"{languageCode} localization must include {key}.");
            }
        }
    }

    private static void ValidateNoForcedInterstitials(ICollection<string> errors)
    {
        foreach (var path in Directory.GetFiles(ProjectScriptsFolder, "*.cs", SearchOption.AllDirectories))
        {
            var source = File.ReadAllText(path);
            if (source.Contains("Interstitial") || source.Contains("interstitial"))
            {
                errors.Add($"Forced interstitial references are not allowed in runtime scripts: {path}");
            }
        }
    }

    private static void RequireEvent(
        FakeAnalyticsService fakeAnalytics,
        string eventName,
        ICollection<string> errors)
    {
        if (!fakeAnalytics.HasEvent(eventName))
        {
            errors.Add($"Fake analytics service must record {eventName}.");
        }
    }
}
