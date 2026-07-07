using System.Collections.Generic;
using System.IO;
using CatGuard.Gameplay.Levels;
using CatGuard.Meta.DailyRewards;
using CatGuard.Meta.Upgrades;
using CatGuard.SDK.Ads;
using CatGuard.UI.Screens;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Phase5ProjectSetup
{
    private const string MainMenuScenePath = "Assets/_Project/Scenes/MainMenu.unity";
    private const string LevelConfigFolder = "Assets/_Project/ScriptableObjects/Levels";
    private const string EconomyConfigFolder = "Assets/_Project/ScriptableObjects/Economy";

    private const string LevelCatalogPath = LevelConfigFolder + "/LevelCatalog.asset";
    private const string UpgradeCatalogPath = EconomyConfigFolder + "/UpgradeCatalog.asset";
    private const string DailyRewardChainPath = EconomyConfigFolder + "/DailyRewardChain.asset";
    private const string DailyMissionCatalogPath = EconomyConfigFolder + "/DailyMissionCatalog.asset";

    public static void Run()
    {
        EnsureFolders();

        var dailyRewardChain = EnsureDailyRewardChain();
        var dailyMissionCatalog = EnsureDailyMissionCatalog();
        ConfigureMainMenuScene(dailyRewardChain, dailyMissionCatalog);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        ValidateAndExit();
    }

    public static void Validate()
    {
        ValidateAndExit();
    }

    private static void EnsureFolders()
    {
        Directory.CreateDirectory(EconomyConfigFolder);
    }

    private static DailyRewardChainConfig EnsureDailyRewardChain()
    {
        var chain = EnsureAsset<DailyRewardChainConfig>(DailyRewardChainPath);
        chain.Configure(new[]
        {
            new DailyRewardConfig(1, 20),
            new DailyRewardConfig(2, 25),
            new DailyRewardConfig(3, 30),
            new DailyRewardConfig(4, 35),
            new DailyRewardConfig(5, 45),
            new DailyRewardConfig(6, 55),
            new DailyRewardConfig(7, 75)
        });

        EditorUtility.SetDirty(chain);
        return chain;
    }

    private static DailyMissionCatalogConfig EnsureDailyMissionCatalog()
    {
        var catalog = EnsureAsset<DailyMissionCatalogConfig>(DailyMissionCatalogPath);
        catalog.Configure(new[]
        {
            new DailyMissionConfig("win_level", "Win 1 Level", DailyMissionType.CompleteLevels, 1, 15),
            new DailyMissionConfig("place_towers", "Place 3 Towers", DailyMissionType.PlaceTowers, 3, 10),
            new DailyMissionConfig("claim_daily", "Claim Daily Reward", DailyMissionType.ClaimDailyReward, 1, 8)
        });

        EditorUtility.SetDirty(catalog);
        return catalog;
    }

    private static void ConfigureMainMenuScene(
        DailyRewardChainConfig dailyRewardChain,
        DailyMissionCatalogConfig dailyMissionCatalog)
    {
        var levelCatalog = AssetDatabase.LoadAssetAtPath<LevelCatalogConfig>(LevelCatalogPath);
        var upgradeCatalog = AssetDatabase.LoadAssetAtPath<UpgradeCatalogConfig>(UpgradeCatalogPath);
        var scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
        var controller = Object.FindFirstObjectByType<MainMenuController>();

        if (controller == null)
        {
            var controllerObject = new GameObject("MainMenuController");
            controller = controllerObject.AddComponent<MainMenuController>();
        }

        controller.Configure(levelCatalog, upgradeCatalog, dailyRewardChain, dailyMissionCatalog);
        EditorUtility.SetDirty(controller);
        EditorSceneManager.SaveScene(scene);
    }

    private static TAsset EnsureAsset<TAsset>(string assetPath)
        where TAsset : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<TAsset>(assetPath);
        if (asset != null)
        {
            return asset;
        }

        asset = ScriptableObject.CreateInstance<TAsset>();
        asset.name = Path.GetFileNameWithoutExtension(assetPath);
        AssetDatabase.CreateAsset(asset, assetPath);
        return asset;
    }

    private static void ValidateAndExit()
    {
        var errors = new List<string>();

        var dailyRewardChain = AssetDatabase.LoadAssetAtPath<DailyRewardChainConfig>(DailyRewardChainPath);
        var dailyMissionCatalog = AssetDatabase.LoadAssetAtPath<DailyMissionCatalogConfig>(DailyMissionCatalogPath);

        if (dailyRewardChain == null || !dailyRewardChain.IsValid())
        {
            errors.Add("DailyRewardChain must contain exactly seven valid rewards.");
        }

        if (dailyMissionCatalog == null || !dailyMissionCatalog.IsValid() || dailyMissionCatalog.Missions.Length < 3)
        {
            errors.Add("DailyMissionCatalog must contain at least three valid missions.");
        }

        ValidateRewardedHook(errors);
        ValidateMainMenu(errors);

        if (errors.Count > 0)
        {
            foreach (var error in errors)
            {
                Debug.LogError(error);
            }

            EditorApplication.Exit(1);
            return;
        }

        Debug.Log("Phase 5 validation passed: daily rewards, daily missions, and fake rewarded x2 hook are configured.");
        EditorApplication.Exit(0);
    }

    private static void ValidateRewardedHook(ICollection<string> errors)
    {
        var fakeAds = new FakeRewardedAdService();
        if (!fakeAds.IsRewardedAdAvailable(RewardedAdPlacementIds.DailyRewardDouble))
        {
            errors.Add("Fake rewarded ad service must expose the daily reward x2 placement.");
        }
    }

    private static void ValidateMainMenu(ICollection<string> errors)
    {
        var scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
        var controller = Object.FindFirstObjectByType<MainMenuController>();

        if (!scene.isLoaded)
        {
            errors.Add($"{MainMenuScenePath} did not load for validation.");
        }

        if (controller == null || !controller.IsConfigured)
        {
            errors.Add("MainMenuController must remain configured with level and upgrade catalogs.");
        }

        if (controller == null || !controller.IsDailyConfigured)
        {
            errors.Add("MainMenuController must be configured with daily reward and daily mission catalogs.");
        }
    }
}
