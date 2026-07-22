using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CatGuard.Core.Localization;
using CatGuard.Gameplay.Levels;
using CatGuard.Meta.DailyRewards;
using CatGuard.Meta.HomeHub;
using CatGuard.Meta.Progression;
using CatGuard.Meta.Upgrades;
using CatGuard.SDK.Ads;
using CatGuard.UI.Screens;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class E8ProjectSetup
{
    private const string MainMenuScenePath = "Assets/_Project/Scenes/MainMenu.unity";
    private const string LevelCatalogPath = "Assets/_Project/ScriptableObjects/Levels/LevelCatalog.asset";
    private const string UpgradeCatalogPath = "Assets/_Project/ScriptableObjects/Economy/UpgradeCatalog.asset";
    private const string DailyRewardChainPath = "Assets/_Project/ScriptableObjects/Economy/DailyRewardChain.asset";
    private const string DailyMissionCatalogPath = "Assets/_Project/ScriptableObjects/Economy/DailyMissionCatalog.asset";
    private const string MainMenuSourcePath = "Assets/_Project/Scripts/UI/Screens/MainMenuController.cs";
    private const string LevelSourcePath = "Assets/_Project/Scripts/Gameplay/Levels/PrototypeLevelController.cs";
    private const string ExternalBacklogPath = "docs/planning/EXTERNAL_PRODUCTION_BACKLOG.md";
    private const string ReportPath = "docs/planning/E8_HOME_HUB_REPORT.md";
    private const string WorkflowPath = "docs/planning/HOME_HUB_WORKFLOW.md";

    private static readonly HomeHubRoute[] ZoneRoutes =
    {
        HomeHubRoute.CampaignGate,
        HomeHubRoute.Workshop,
        HomeHubRoute.QuestBoard,
        HomeHubRoute.AchievementWall,
        HomeHubRoute.GuardianLodge,
        HomeHubRoute.DailyBasket,
        HomeHubRoute.SettingsCorner
    };

    public static void Run()
    {
        ValidateAndExit();
    }

    public static void Validate()
    {
        ValidateAndExit();
    }

    private static void ValidateAndExit()
    {
        var errors = new List<string>();
        var levelCatalog = AssetDatabase.LoadAssetAtPath<LevelCatalogConfig>(LevelCatalogPath);
        var upgradeCatalog = AssetDatabase.LoadAssetAtPath<UpgradeCatalogConfig>(UpgradeCatalogPath);
        var dailyRewardChain = AssetDatabase.LoadAssetAtPath<DailyRewardChainConfig>(DailyRewardChainPath);
        var dailyMissionCatalog = AssetDatabase.LoadAssetAtPath<DailyMissionCatalogConfig>(DailyMissionCatalogPath);

        ValidateCatalogs(levelCatalog, upgradeCatalog, dailyRewardChain, dailyMissionCatalog, errors);
        ValidateRouteModel(errors);
        ValidateMainMenuScene(
            levelCatalog,
            upgradeCatalog,
            dailyRewardChain,
            dailyMissionCatalog,
            errors);
        ValidateInitializationBoundary(
            levelCatalog,
            upgradeCatalog,
            dailyRewardChain,
            dailyMissionCatalog,
            errors);
        ValidateBattleSummary(errors);
        ValidateBadges(levelCatalog, upgradeCatalog, dailyMissionCatalog, errors);
        ValidateLocalization(errors);
        ValidateSourceBoundaries(errors);
        ValidateExternalArtBoundary(errors);
        ValidateDocumentation(errors);

        if (errors.Count > 0)
        {
            foreach (var error in errors)
            {
                UnityEngine.Debug.LogError(error);
            }

            EditorApplication.Exit(1);
            return;
        }

        UnityEngine.Debug.Log(
            "E8 validation passed: the state-driven garden hub exposes seven routes, centralized badges, panel-first Back navigation, idempotent progression initialization, post-battle summaries, preserved campaign state, RU/EN copy, landscape layout, settings/daily/reset access, and an explicit ART-HUB-001 placeholder boundary.");
        EditorApplication.Exit(0);
    }

    private static void ValidateCatalogs(
        LevelCatalogConfig levelCatalog,
        UpgradeCatalogConfig upgradeCatalog,
        DailyRewardChainConfig dailyRewardChain,
        DailyMissionCatalogConfig dailyMissionCatalog,
        ICollection<string> errors)
    {
        if (levelCatalog == null || !levelCatalog.IsValid())
        {
            errors.Add("E8 requires the valid campaign level catalog.");
        }

        if (upgradeCatalog == null || !upgradeCatalog.IsValid())
        {
            errors.Add("E8 requires the valid permanent upgrade catalog.");
        }

        if (dailyRewardChain == null || !dailyRewardChain.IsValid())
        {
            errors.Add("E8 requires the valid daily reward chain.");
        }

        if (dailyMissionCatalog == null || !dailyMissionCatalog.IsValid())
        {
            errors.Add("E8 requires the valid daily mission catalog.");
        }
    }

    private static void ValidateRouteModel(ICollection<string> errors)
    {
        var routes = Enum.GetValues(typeof(HomeHubRoute)).Cast<HomeHubRoute>().ToArray();
        if (routes.Length != 8 || routes.Distinct().Count() != routes.Length)
        {
            errors.Add("E8 route model must contain Home plus seven unique hub zones.");
        }

        if (ZoneRoutes.Length != 7 || ZoneRoutes.Contains(HomeHubRoute.Home))
        {
            errors.Add("E8 must expose exactly seven interactive zone routes outside Home.");
        }
    }

    private static void ValidateMainMenuScene(
        LevelCatalogConfig levelCatalog,
        UpgradeCatalogConfig upgradeCatalog,
        DailyRewardChainConfig dailyRewardChain,
        DailyMissionCatalogConfig dailyMissionCatalog,
        ICollection<string> errors)
    {
        var scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
        var controller = UnityEngine.Object.FindFirstObjectByType<MainMenuController>();
        if (!scene.IsValid() || controller == null || !controller.IsConfigured || !controller.IsDailyConfigured)
        {
            errors.Add("MainMenu must remain configured as the E8 home hub with campaign, upgrades, and daily content.");
            return;
        }

        ProgressionService.Initialize(
            levelCatalog,
            upgradeCatalog,
            dailyRewardChain,
            dailyMissionCatalog,
            new FakeRewardedAdService());

        controller.NavigateTo(HomeHubRoute.Home);
        foreach (var route in ZoneRoutes)
        {
            controller.NavigateTo(route);
            if (controller.CurrentRoute != route)
            {
                errors.Add($"Hub route '{route}' did not open from Home.");
            }

            if (!controller.NavigateBack() || controller.CurrentRoute != HomeHubRoute.Home)
            {
                errors.Add($"Hub route '{route}' did not return to Home through the shared Back contract.");
            }
        }
    }

    private static void ValidateInitializationBoundary(
        LevelCatalogConfig levelCatalog,
        UpgradeCatalogConfig upgradeCatalog,
        DailyRewardChainConfig dailyRewardChain,
        DailyMissionCatalogConfig dailyMissionCatalog,
        ICollection<string> errors)
    {
        if (levelCatalog == null || upgradeCatalog == null)
        {
            return;
        }

        ProgressionService.Initialize(
            levelCatalog,
            upgradeCatalog,
            dailyRewardChain,
            dailyMissionCatalog,
            new FakeRewardedAdService());
        var firstCount = ProgressionService.InitializationCount;
        var selectedBefore = ProgressionService.GetSelectedLevelOrDefault(levelCatalog.FirstLevel)?.LevelId;
        var fishBefore = ProgressionService.FishCoins;
        var audioBefore = ProgressionService.IsAudioMuted;
        var languageBefore = ProgressionService.LanguageCode;
        var shakeBefore = ProgressionService.CameraShakeLevel;
        var flashBefore = ProgressionService.ReducedFlash;

        ProgressionService.Initialize(
            levelCatalog,
            upgradeCatalog,
            dailyRewardChain,
            dailyMissionCatalog,
            new FakeRewardedAdService());

        if (ProgressionService.InitializationCount != firstCount)
        {
            errors.Add("Reloading MainMenu with identical catalogs duplicated progression initialization.");
        }

        var selectedAfter = ProgressionService.GetSelectedLevelOrDefault(levelCatalog.FirstLevel)?.LevelId;
        if (!string.Equals(selectedBefore, selectedAfter, StringComparison.Ordinal)
            || fishBefore != ProgressionService.FishCoins)
        {
            errors.Add("Idempotent hub initialization changed selected map or saved rewards.");
        }

        if (audioBefore != ProgressionService.IsAudioMuted
            || languageBefore != ProgressionService.LanguageCode
            || shakeBefore != ProgressionService.CameraShakeLevel
            || flashBefore != ProgressionService.ReducedFlash)
        {
            errors.Add("Idempotent hub initialization changed saved settings.");
        }
    }

    private static void ValidateBattleSummary(ICollection<string> errors)
    {
        HomeHubNavigationService.ClearPendingBattleSummary();
        var completion = new LevelCompletionResult(37, true, new[] { "Map B" }).WithRewardedBonus(11);
        HomeHubNavigationService.RecordVictory(null, completion, 4, 12, 1);
        var victory = HomeHubNavigationService.ConsumeBattleSummary();
        if (victory == null
            || !victory.Won
            || victory.EarnedFishCoins != 48
            || !victory.FirstClear
            || victory.UnlockedLevelNames.Length != 1
            || victory.RemainingLives != 4)
        {
            errors.Add("Victory summary did not preserve reward, first clear, unlock, and remaining-life state.");
        }

        if (HomeHubNavigationService.PendingBattleSummary != null)
        {
            errors.Add("Consumed battle summary was shown more than once.");
        }

        HomeHubNavigationService.RecordDefeat(null, 8, 3);
        var defeat = HomeHubNavigationService.ConsumeBattleSummary();
        if (defeat == null || defeat.Won || defeat.DefeatedEnemies != 8 || defeat.EscapedEnemies != 3)
        {
            errors.Add("Defeat summary did not preserve the clear post-round result.");
        }
    }

    private static void ValidateBadges(
        LevelCatalogConfig levelCatalog,
        UpgradeCatalogConfig upgradeCatalog,
        DailyMissionCatalogConfig dailyMissionCatalog,
        ICollection<string> errors)
    {
        var watch = Stopwatch.StartNew();
        HomeHubBadgeSnapshot snapshot = null;
        for (var index = 0; index < 50000; index++)
        {
            snapshot = HomeHubBadgeService.CreateSnapshot(levelCatalog, upgradeCatalog, dailyMissionCatalog);
        }
        watch.Stop();

        if (snapshot == null
            || ZoneRoutes.Any(route => snapshot.GetCount(route) < 0)
            || snapshot.TotalClaimable != ZoneRoutes.Sum(snapshot.GetCount))
        {
            errors.Add("Centralized E8 badge snapshot returned an invalid claimable-action count.");
        }

        if (watch.ElapsedMilliseconds > 3000)
        {
            errors.Add($"E8 badge snapshot is too expensive: 50,000 evaluations took {watch.ElapsedMilliseconds} ms.");
        }
    }

    private static void ValidateLocalization(ICollection<string> errors)
    {
        var originalLanguage = LocalizationService.CurrentLanguageCode;
        var keys = new[]
        {
            "hub.title",
            "hub.home",
            "hub.back",
            "hub.campaignGate",
            "hub.workshop",
            "hub.questBoard",
            "hub.achievementWall",
            "hub.guardianLodge",
            "hub.dailyBasket",
            "hub.settingsCorner",
            "hub.resultVictory",
            "hub.resultDefeat",
            "hub.quickPlay"
        };

        foreach (var language in new[] { LocalizationService.English, LocalizationService.Russian })
        {
            LocalizationService.SetLanguage(language);
            foreach (var key in keys)
            {
                var text = LocalizationService.Text(key);
                if (string.IsNullOrWhiteSpace(text) || text == key || text.Length > 42)
                {
                    errors.Add($"E8 localized label '{key}' is missing or too long for landscape UI in '{language}'.");
                }
            }
        }

        LocalizationService.SetLanguage(originalLanguage);
    }

    private static void ValidateSourceBoundaries(ICollection<string> errors)
    {
        var mainMenuSource = ReadProjectFile(MainMenuSourcePath);
        var levelSource = ReadProjectFile(LevelSourcePath);
        var requiredMenuTokens = new[]
        {
            "HomeHubBadgeService.CreateSnapshot",
            "HomeHubNavigationService.ConsumeBattleSummary",
            "Input.GetKeyDown(KeyCode.Escape)",
            "NavigateBack()",
            "ProgressionService.ResetProgress()",
            "LandscapeLayout.Begin",
            "UltimateCatalogConfig.LoadDefault"
        };
        foreach (var token in requiredMenuTokens)
        {
            if (!mainMenuSource.Contains(token, StringComparison.Ordinal))
            {
                errors.Add($"MainMenu E8 source is missing required contract '{token}'.");
            }
        }

        foreach (var route in ZoneRoutes)
        {
            if (!mainMenuSource.Contains($"HomeHubRoute.{route}", StringComparison.Ordinal))
            {
                errors.Add($"MainMenu E8 source is missing zone '{route}'.");
            }
        }

        if (mainMenuSource.Contains("GameSaveData", StringComparison.Ordinal)
            || mainMenuSource.Contains("GameSaveService", StringComparison.Ordinal))
        {
            errors.Add("Hub widgets must read and mutate state through meta services, not the save model directly.");
        }

        if (!levelSource.Contains("HomeHubNavigationService.RecordVictory", StringComparison.Ordinal)
            || !levelSource.Contains("HomeHubNavigationService.RecordDefeat", StringComparison.Ordinal))
        {
            errors.Add("Level result flow must publish both victory and defeat summaries to the hub contract.");
        }
    }

    private static void ValidateExternalArtBoundary(ICollection<string> errors)
    {
        var backlog = ReadProjectFile(ExternalBacklogPath);
        if (!backlog.Contains("ID: `ART-HUB-001`", StringComparison.Ordinal)
            || !backlog.Contains("Status: `Not started`", StringComparison.Ordinal)
            || !backlog.Contains("Functional code-authored E8 placeholder", StringComparison.Ordinal))
        {
            errors.Add("ART-HUB-001 must remain explicitly not started with the temporary functional E8 placeholder recorded.");
        }
    }

    private static void ValidateDocumentation(ICollection<string> errors)
    {
        foreach (var path in new[] { ReportPath, WorkflowPath })
        {
            if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), path)))
            {
                errors.Add($"E8 documentation is missing: {path}.");
            }
        }
    }

    private static string ReadProjectFile(string relativePath)
    {
        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), relativePath);
        return File.Exists(fullPath) ? File.ReadAllText(fullPath) : string.Empty;
    }
}
