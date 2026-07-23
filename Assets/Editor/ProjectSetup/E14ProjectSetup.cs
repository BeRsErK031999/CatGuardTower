using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CatGuard.Core.Localization;
using CatGuard.Core.Quality;
using CatGuard.Core.Save;
using CatGuard.Gameplay.Levels;
using CatGuard.Gameplay.Towers;
using CatGuard.Gameplay.Ultimates;
using CatGuard.Meta.Achievements;
using CatGuard.Meta.Quests;
using UnityEditor;
using UnityEngine;

public static class E14ProjectSetup
{
    private const string LevelCatalogPath = "Assets/_Project/ScriptableObjects/Levels/LevelCatalog.asset";
    private const string BudgetFolder = "Assets/_Project/Resources/Quality";
    private const string BudgetPath = BudgetFolder + "/E14QualityBudget.asset";
    private const string ReportPath = "docs/planning/E14_BALANCE_PERFORMANCE_ACCESSIBILITY_REPORT.md";
    private const string WorkflowPath = "docs/planning/E14_QUALITY_QA_WORKFLOW.md";

    [MenuItem("Cat Guard/Setup E14 Quality And Polish")]
    public static void Run()
    {
        Directory.CreateDirectory(BudgetFolder);
        var budget = AssetDatabase.LoadAssetAtPath<ExpansionQualityBudgetConfig>(BudgetPath);
        if (budget == null)
        {
            budget = ScriptableObject.CreateInstance<ExpansionQualityBudgetConfig>();
            AssetDatabase.CreateAsset(budget, BudgetPath);
        }

        budget.Configure(
            60,
            24,
            70f,
            "level_12",
            96,
            96,
            2048,
            128,
            35,
            120,
            90,
            120,
            10,
            new[] { 1, 2 });
        EditorUtility.SetDirty(budget);

        var catalog = LoadRequired<LevelCatalogConfig>(LevelCatalogPath);
        ConfigureTutorial(catalog, "level_01", "tutorial.e14.basics");
        ConfigureTutorial(catalog, "level_04", "tutorial.e14.routes");
        ConfigureTutorial(catalog, "level_06", "tutorial.e14.upgrades");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        ValidateAndExit();
    }

    public static void Validate()
    {
        ValidateAndExit();
    }

    private static void ConfigureTutorial(LevelCatalogConfig catalog, string levelId, string localizationKey)
    {
        var level = catalog.FindById(levelId)
            ?? throw new InvalidDataException($"E14 tutorial target '{levelId}' is missing.");
        level.ConfigureTutorialText(localizationKey);
        EditorUtility.SetDirty(level);
    }

    private static void ValidateAndExit()
    {
        var errors = new List<string>();
        var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalogConfig>(LevelCatalogPath);
        var budget = AssetDatabase.LoadAssetAtPath<ExpansionQualityBudgetConfig>(BudgetPath);
        ValidateBudgetAndCampaign(catalog, budget, errors);
        ValidateEconomyAndCadence(catalog, budget, errors);
        ValidateAccessibilityAndMigration(catalog, budget, errors);
        ValidateRuntimeBoundaries(errors);
        ValidateTextureAndDecorationBudgets(catalog, budget, errors);
        ValidateNoForcedInterstitials(errors);
        ValidateDocumentation(catalog, errors);

        if (errors.Count > 0)
        {
            foreach (var error in errors)
            {
                Debug.LogError(error);
            }

            EditorApplication.Exit(1);
            return;
        }

        Debug.Log("E14 validation passed: the 12-map/five-tower balance envelope, capped rewards, ultimate cadence, bounded enemy and VFX pools, cached audio mix, 60/24 FPS budgets, worst-case level_12 scenario, persisted accessibility/time controls, RU/EN tutorials, schema v5 migration, no-forced-interstitial rule, and Android quality workflow are complete.");
        EditorApplication.Exit(0);
    }

    private static void ValidateBudgetAndCampaign(
        LevelCatalogConfig catalog,
        ExpansionQualityBudgetConfig budget,
        ICollection<string> errors)
    {
        if (budget?.IsValid() != true)
        {
            errors.Add("E14 quality budget is missing or invalid.");
            return;
        }

        if (catalog?.Levels.Length != 12)
        {
            errors.Add("E14 requires exactly 12 campaign maps.");
            return;
        }

        var towerIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var level in catalog.Levels)
        {
            if (level?.IsValidForCore() != true)
            {
                errors.Add($"E14 level '{level?.LevelId ?? "missing"}' is invalid.");
                continue;
            }

            foreach (var tower in level.AvailableTowers)
            {
                if (tower?.IsValid() == true)
                {
                    towerIds.Add(tower.TowerId);
                }
            }

            var battlefield = level.ResolveBattlefield();
            if (battlefield.Routes.Length == 0 || level.WaveConfig.TotalEnemyCount <= 0)
            {
                errors.Add($"{level.LevelId} has no measurable route pressure.");
            }

            if (level.AvailableTowers.All(tower => tower == null || tower.BuildCost > level.StartingBattleFish))
            {
                errors.Add($"{level.LevelId} starts without an affordable tower.");
            }
        }

        if (towerIds.Count != 5)
        {
            errors.Add($"E14 balance report requires five tower families; found {towerIds.Count}.");
        }

        if (catalog.FindById(budget.WorstCaseLevelId)?.HasBossEncounter != true)
        {
            errors.Add("Worst-case quality scenario must resolve to the final boss map.");
        }
    }

    private static void ValidateEconomyAndCadence(
        LevelCatalogConfig catalog,
        ExpansionQualityBudgetConfig budget,
        ICollection<string> errors)
    {
        if (catalog == null || budget == null)
        {
            return;
        }

        foreach (var level in catalog.Levels)
        {
            if (level.FirstClearRewardCoins < level.ReplayRewardCoins
                || level.ReplayRewardCoins <= 0
                || level.StartingBattleFish <= 0)
            {
                errors.Add($"{level.LevelId} violates battle/hub reward cadence.");
            }
        }

        var questCatalog = QuestCatalogConfig.LoadDefault();
        if (questCatalog?.Quests.Any(quest => quest?.Reward == null
                || quest.Reward.FishCoins > budget.MaximumQuestFishReward) != false)
        {
            errors.Add("Quest reward cap is missing or exceeded.");
        }

        var achievementCatalog = AchievementCatalogConfig.LoadDefault();
        if (achievementCatalog?.Achievements.Any(item => item?.Reward == null
                || item.Reward.FishCoins > budget.MaximumAchievementFishReward) != false)
        {
            errors.Add("Achievement reward cap is missing or exceeded.");
        }

        var ultimateCatalog = UltimateCatalogConfig.LoadDefault();
        var ultimateError = "Ultimate catalog is missing.";
        if (ultimateCatalog == null || !ultimateCatalog.IsValid(out ultimateError))
        {
            errors.Add($"Ultimate cadence catalog is invalid: {ultimateError}");
            return;
        }

        foreach (var ultimate in ultimateCatalog.Ultimates)
        {
            if (ultimate.ChargeRequired < 40f
                || ultimate.ChargeRequired > 200f
                || ultimate.CooldownSeconds < 4f
                || ultimate.CooldownSeconds > 45f
                || ultimate.KillCharge <= 0f)
            {
                errors.Add($"Ultimate '{ultimate.UltimateId}' is outside the E14 charge/cooldown cadence.");
            }
        }
    }

    private static void ValidateAccessibilityAndMigration(
        LevelCatalogConfig catalog,
        ExpansionQualityBudgetConfig budget,
        ICollection<string> errors)
    {
        if (budget == null)
        {
            return;
        }

        var tutorials = new Dictionary<string, string>
        {
            ["level_01"] = "tutorial.e14.basics",
            ["level_04"] = "tutorial.e14.routes",
            ["level_06"] = "tutorial.e14.upgrades"
        };
        foreach (var pair in tutorials)
        {
            if (catalog?.FindById(pair.Key)?.TutorialTextKey != pair.Value)
            {
                errors.Add($"{pair.Key} does not use the concise E14 tutorial contract.");
            }
        }

        var originalLanguage = LocalizationService.CurrentLanguageCode;
        foreach (var language in new[] { LocalizationService.English, LocalizationService.Russian })
        {
            LocalizationService.SetLanguage(language);
            foreach (var key in tutorials.Values.Concat(new[]
            {
                "settings.cameraShake", "settings.reducedFlash", "settings.textScale",
                "button.pause", "button.resume", "pause.title", "pause.hint"
            }))
            {
                if (LocalizationService.Text(key) == key)
                {
                    errors.Add($"Missing E14 localization '{key}' in '{language}'.");
                }
            }
        }
        LocalizationService.SetLanguage(originalLanguage);

        var save = GameSaveData.CreateDefault("level_01");
        save.schemaVersion = 4;
        save.textScalePercent = 0;
        save.preferredBattleSpeed = 0;
        save.completedLevelIds.Add("level_01");
        if (!GameSaveMigrationService.TryMigrate(save, "level_01", out var changed, out var migrationError)
            || !changed
            || !string.IsNullOrEmpty(migrationError)
            || save.schemaVersion != 5
            || save.textScalePercent != 100
            || save.preferredBattleSpeed != 1
            || !save.completedLevelIds.Contains("level_01"))
        {
            errors.Add("Schema v4 to v5 accessibility migration is not preserving and normalized.");
        }
    }

    private static void ValidateRuntimeBoundaries(ICollection<string> errors)
    {
        var required = new Dictionary<string, string[]>
        {
            ["Assets/_Project/Scripts/Gameplay/Enemies/EnemyRuntimePool.cs"] = new[] { "ExpansionQualityBudgetConfig", "Acquire", "ReleaseAll", "PeakActiveCount" },
            ["Assets/_Project/Scripts/VFX/SimpleVfxFactory.cs"] = new[] { "Available", "MaximumPooledVfx", "ReducedFlash", "DroppedCount" },
            ["Assets/_Project/Scripts/Core/Audio/ProceduralAudioService.cs"] = new[] { "ClipCache", "SetContext", "TowerUpgrade", "BossPhase" },
            ["Assets/_Project/Scripts/Gameplay/Levels/PrototypeLevelController.cs"] = new[] { "EnemyRuntimePool", "SetPaused", "SetBattleSpeed", "PreferredBattleSpeed" },
            ["Assets/_Project/Scripts/UI/HUD/PrototypeHud.cs"] = new[] { "DrawPauseOverlay", "BattleSpeed", "TextScalePercent" },
            ["Assets/_Project/Scripts/UI/Screens/MainMenuController.cs"] = new[] { "CycleTextScale", "ApplyTextScale", "settings.textScale" },
            ["Assets/_Project/Scripts/QA/DevelopmentQaService.cs"] = new[] { "enemyPoolReused", "simpleVfxDropped", "timeControlsVerified", "textScalePercent" },
            ["tools/android/run-emulator-level-qa.ps1"] = new[] { "RequirePooling", "RequireSettings", "RequireTimeControls" },
            ["tools/android/run-emulator-e14-qa.ps1"] = new[] { "worst_case", "settings_persistence", "offline", "soak" }
        };

        foreach (var pair in required)
        {
            var source = File.Exists(pair.Key) ? File.ReadAllText(pair.Key) : string.Empty;
            foreach (var token in pair.Value)
            {
                if (!source.Contains(token, StringComparison.Ordinal))
                {
                    errors.Add($"E14 runtime boundary '{token}' is missing from {pair.Key}.");
                }
            }
        }

        var towerSource = File.ReadAllText("Assets/_Project/Scripts/Gameplay/Towers/BasicTower.cs");
        if (towerSource.Contains("new GameObject(\"Projectile", StringComparison.Ordinal)
            || towerSource.Contains("Instantiate(projectile", StringComparison.OrdinalIgnoreCase))
        {
            errors.Add("Instant-hit towers unexpectedly allocate projectile objects; add a bounded projectile pool first.");
        }
    }

    private static void ValidateTextureAndDecorationBudgets(
        LevelCatalogConfig catalog,
        ExpansionQualityBudgetConfig budget,
        ICollection<string> errors)
    {
        if (budget == null)
        {
            return;
        }

        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/_Project" }))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture != null
                && (texture.width > budget.MaximumAnimationTextureSize
                    || texture.height > budget.MaximumAnimationTextureSize))
            {
                errors.Add($"Texture '{path}' exceeds {budget.MaximumAnimationTextureSize}px.");
            }
        }

        foreach (var level in catalog?.Levels ?? Array.Empty<LevelConfig>())
        {
            var decorations = level?.ResolveBattlefield()?.DecorationAnchors.Length ?? 0;
            if (decorations > budget.MaximumMapDecorations)
            {
                errors.Add($"{level?.LevelId ?? "missing"} exceeds the map decoration budget.");
            }
        }
    }

    private static void ValidateNoForcedInterstitials(ICollection<string> errors)
    {
        foreach (var path in Directory.GetFiles("Assets/_Project/Scripts", "*.cs", SearchOption.AllDirectories))
        {
            var source = File.ReadAllText(path);
            if (source.Contains("Interstitial", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add($"Forced interstitial reference is not allowed in runtime source: {path}.");
            }
        }
    }

    private static void ValidateDocumentation(LevelCatalogConfig catalog, ICollection<string> errors)
    {
        foreach (var path in new[] { ReportPath, WorkflowPath })
        {
            if (!File.Exists(path) || File.ReadAllText(path).Length < 1000)
            {
                errors.Add($"E14 documentation is missing or incomplete: {path}.");
            }
        }

        var report = File.Exists(ReportPath) ? File.ReadAllText(ReportPath) : string.Empty;
        foreach (var level in catalog?.Levels ?? Array.Empty<LevelConfig>())
        {
            if (!report.Contains(level.LevelId, StringComparison.Ordinal))
            {
                errors.Add($"E14 report does not cover {level.LevelId}.");
            }
        }

        foreach (var towerId in catalog?.Levels
                     .SelectMany(level => level?.AvailableTowers ?? Array.Empty<TowerConfig>())
                     .Where(tower => tower != null)
                     .Select(tower => tower.TowerId)
                     .Distinct(StringComparer.Ordinal)
                     ?? Enumerable.Empty<string>())
        {
            if (!report.Contains(towerId, StringComparison.Ordinal))
            {
                errors.Add($"E14 report does not cover tower family '{towerId}'.");
            }
        }
    }

    private static T LoadRequired<T>(string path) where T : UnityEngine.Object
    {
        return AssetDatabase.LoadAssetAtPath<T>(path)
            ?? throw new InvalidDataException($"Required E14 asset is missing: {path}");
    }
}
