using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CatGuard.Core.Localization;
using CatGuard.Core.Save;
using CatGuard.Meta.Achievements;
using CatGuard.Meta.Quests;
using CatGuard.SDK.Analytics;
using UnityEditor;
using UnityEngine;

public static class E11ProjectSetup
{
    private const string AchievementFolder = "Assets/_Project/Resources/Achievements";
    private const string AchievementCatalogPath = AchievementFolder + "/AchievementCatalog.asset";
    private const string QuestCatalogPath = "Assets/_Project/Resources/Quests/QuestCatalog.asset";
    private const string ReportPath = "docs/planning/E11_ACHIEVEMENTS_REPORT.md";
    private const string WorkflowPath = "docs/planning/ACHIEVEMENT_WORKFLOW.md";

    public static void Run()
    {
        Directory.CreateDirectory(AchievementFolder);
        var catalog = AssetDatabase.LoadAssetAtPath<AchievementCatalogConfig>(AchievementCatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<AchievementCatalogConfig>();
            AssetDatabase.CreateAsset(catalog, AchievementCatalogPath);
        }

        catalog.Configure(CreateAchievements(), InitialEnemyIds());
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        ValidateAndExit();
    }

    public static void Validate()
    {
        ValidateAndExit();
    }

    private static AchievementConfig[] CreateAchievements()
    {
        return new[]
        {
            Achievement(
                "first_clear", "first_clear", AchievementCategory.Campaign, AchievementPresentationTier.Bronze,
                AchievementProgressRule.CompletedMaps, 1, false, 20, 15),
            Achievement(
                "ten_maps", "ten_maps", AchievementCategory.Campaign, AchievementPresentationTier.Gold,
                AchievementProgressRule.CompletedMaps, 10, false, 100, 60),
            Achievement(
                "perfect_defense", "perfect_defense", AchievementCategory.PerfectDefense, AchievementPresentationTier.Silver,
                AchievementProgressRule.PerfectVictories, 1, false, 40, 25),
            Achievement(
                "first_tier_three", "first_tier_three", AchievementCategory.TowerMastery, AchievementPresentationTier.Silver,
                AchievementProgressRule.TierThreeTowerBattles, 1, false, 45, 25),
            Achievement(
                "guardian_triad", "guardian_triad", AchievementCategory.Ultimates, AchievementPresentationTier.Gold,
                AchievementProgressRule.UniqueUltimatesUsed, 3, false, 75, 40),
            Achievement(
                "thousand_enemies", "thousand_enemies", AchievementCategory.LongTermTotals, AchievementPresentationTier.Gold,
                AchievementProgressRule.EnemiesDefeated, 1000, false, 120, 60),
            Achievement(
                "multi_route_win", "multi_route_win", AchievementCategory.RouteControl, AchievementPresentationTier.Silver,
                AchievementProgressRule.MultiRouteVictories, 1, false, 50, 30),
            Achievement(
                "seven_daily", "seven_daily", AchievementCategory.LongTermTotals, AchievementPresentationTier.Silver,
                AchievementProgressRule.DailyRewardsClaimed, 7, false, 70, 35),
            Achievement(
                "ten_contracts", "ten_contracts", AchievementCategory.LongTermTotals, AchievementPresentationTier.Silver,
                AchievementProgressRule.ContractsCompleted, 10, false, 65, 35),
            Achievement(
                "initial_bestiary", "initial_bestiary", AchievementCategory.Collection, AchievementPresentationTier.Silver,
                AchievementProgressRule.InitialEnemiesDiscovered, 5, false, 60, 30),
            Achievement(
                "first_boss", "first_boss", AchievementCategory.Campaign, AchievementPresentationTier.Gold,
                AchievementProgressRule.BossesDefeated, 1, false, 120, 75),
            Achievement(
                AchievementService.GardenSecretAchievementId, "garden_secret", AchievementCategory.SecretsAndHumor,
                AchievementPresentationTier.Bronze, AchievementProgressRule.GardenInteractions, 1, true, 30, 20)
        };
    }

    private static AchievementConfig Achievement(
        string id,
        string localizationSuffix,
        AchievementCategory category,
        AchievementPresentationTier tier,
        AchievementProgressRule rule,
        int target,
        bool hidden,
        int fishCoins,
        int experience)
    {
        return new AchievementConfig(
            id,
            $"achievement.title.{localizationSuffix}",
            $"achievement.description.{localizationSuffix}",
            category,
            tier,
            rule,
            target,
            hidden,
            new AchievementRewardBundle(fishCoins, experience));
    }

    private static string[] InitialEnemyIds()
    {
        return new[] { "mouse_scout", "moth_swarm", "rat_bruiser", "beetle_guard", "snail_tank" };
    }

    private static void ValidateAndExit()
    {
        var errors = new List<string>();
        var catalog = AssetDatabase.LoadAssetAtPath<AchievementCatalogConfig>(AchievementCatalogPath);
        var quests = AssetDatabase.LoadAssetAtPath<QuestCatalogConfig>(QuestCatalogPath);
        ValidateCatalog(catalog, errors);
        ValidateAllConditions(catalog, quests, errors);
        ValidatePartialDuplicateRollbackAndReset(catalog, quests, errors);
        ValidateRestartAndClaims(catalog, quests, errors);
        ValidateMigration(errors);
        ValidateAnalyticsBoundary(catalog, errors);
        ValidateLocalization(catalog, errors);
        ValidateRuntimeBoundaries(errors);
        ValidateDocumentation(errors);

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
            "E11 validation passed: 12 stable localized achievements cover incremental, one-shot, reliable retroactive, hidden, boss-ready, daily, contract, collection, and battle rules; multiple completions, restart-before-claim, one-time Fish/XP claims, rewarded-revive rollback, reset, hub badges, and non-PII analytics are protected by schema v3 persistence.");
        EditorApplication.Exit(0);
    }

    private static void ValidateCatalog(AchievementCatalogConfig catalog, ICollection<string> errors)
    {
        var error = "Catalog is missing.";
        if (catalog == null || !catalog.IsValid(out error))
        {
            errors.Add($"E11 achievement catalog is invalid: {error}");
            return;
        }

        var expectedIds = new[]
        {
            "first_clear", "ten_maps", "perfect_defense", "first_tier_three", "guardian_triad",
            "thousand_enemies", "multi_route_win", "seven_daily", "ten_contracts", "initial_bestiary",
            "first_boss", AchievementService.GardenSecretAchievementId
        };
        foreach (var id in expectedIds)
        {
            if (catalog.FindById(id) == null)
            {
                errors.Add($"E11 required achievement '{id}' is missing.");
            }
        }

        if (catalog.FindById(AchievementService.GardenSecretAchievementId)?.Hidden != true)
        {
            errors.Add("Garden interaction achievement must remain hidden before completion.");
        }
    }

    private static void ValidateAllConditions(
        AchievementCatalogConfig catalog,
        QuestCatalogConfig quests,
        ICollection<string> errors)
    {
        if (catalog == null || quests == null)
        {
            errors.Add("E11 production catalogs are unavailable for condition validation.");
            return;
        }

        var save = GameSaveData.CreateDefault("level_01");
        for (var index = 1; index <= 10; index++)
        {
            save.completedLevelIds.Add($"level_{index:00}");
        }

        foreach (var enemyId in catalog.InitialEnemyIds)
        {
            save.discoveredCodexEntryIds.Add($"enemy:{enemyId}");
        }

        foreach (var quest in quests.Quests.Take(10))
        {
            save.quests.Add(new QuestProgressSaveEntry(quest.QuestId) { progress = quest.Objective.TargetAmount });
        }

        var machine = new AchievementStateMachine(catalog, quests, save, null, null, () => "2026-07-22");
        var retroactive = machine.CreateSnapshot();
        if (retroactive.CompletedCount != 4
            || !machine.IsCompleted("first_clear")
            || !machine.IsCompleted("ten_maps")
            || !machine.IsCompleted("ten_contracts")
            || !machine.IsCompleted("initial_bestiary"))
        {
            errors.Add("Reliable map, contract, or codex progress was not calculated retroactively.");
        }

        var battle = machine.RecordBattle(new AchievementBattleReport(
            "battle-all-rules",
            true,
            0,
            1000,
            2,
            3,
            new[] { "yarn_meteor_shower", "catnip_moon", "nine_lives_ward" },
            new[] { "boss_future_01" }));
        if (battle.CompletedCount != 6)
        {
            errors.Add("One battle result did not complete all six simultaneous battle achievement conditions.");
        }

        for (var day = 1; day <= 7; day++)
        {
            machine.RecordDailyRewardClaim($"2026-07-{day:00}");
        }

        machine.RecordGardenInteraction();
        var final = machine.CreateSnapshot();
        if (final.CompletedCount != 12 || final.ClaimableCount != 12)
        {
            errors.Add($"All 12 E11 conditions did not complete exactly once; completed={final.CompletedCount}, claimable={final.ClaimableCount}.");
        }
    }

    private static void ValidatePartialDuplicateRollbackAndReset(
        AchievementCatalogConfig catalog,
        QuestCatalogConfig quests,
        ICollection<string> errors)
    {
        if (catalog == null)
        {
            return;
        }

        var partialSave = GameSaveData.CreateDefault("level_01");
        var partial = new AchievementStateMachine(catalog, quests, partialSave, null, null, () => "2026-07-22");
        partial.RecordBattle(new AchievementBattleReport(
            "partial", false, 2, 999, 1, 2, new[] { "yarn_meteor_shower", "catnip_moon" }, Array.Empty<string>()));
        var enemyState = partial.CreateSnapshot().States.First(item => item.Config.AchievementId == "thousand_enemies");
        var ultimateState = partial.CreateSnapshot().States.First(item => item.Config.AchievementId == "guardian_triad");
        if (enemyState.Progress != 999 || enemyState.Completed || ultimateState.Progress != 2 || ultimateState.Completed)
        {
            errors.Add("Incremental achievement partial progress was clamped, lost, or completed early.");
        }

        var duplicate = partial.RecordBattle(new AchievementBattleReport(
            "partial", false, 2, 999, 1, 2, new[] { "nine_lives_ward" }, Array.Empty<string>()));
        if (!duplicate.Duplicate || partial.CreateSnapshot().States.First(item => item.Config.AchievementId == "thousand_enemies").Progress != 999)
        {
            errors.Add("Duplicate battle event advanced E11 achievement progress.");
        }

        if (!partial.RollbackBattleEvent("partial")
            || partial.CreateSnapshot().States.First(item => item.Config.AchievementId == "thousand_enemies").Progress != 0
            || partialSave.processedAchievementEventIds.Contains("partial"))
        {
            errors.Add("Rewarded-revive rollback did not restore E11 progress and event state.");
        }

        var reset = new AchievementStateMachine(catalog, quests, GameSaveData.CreateDefault("level_01"));
        if (reset.CreateSnapshot().CompletedCount != 0 || reset.CreateSnapshot().ClaimableCount != 0)
        {
            errors.Add("Clean/reset save contains completed or claimable achievements.");
        }

        var hidden = reset.CreateSnapshot().States.First(item => item.Config.AchievementId == AchievementService.GardenSecretAchievementId);
        if (!hidden.Concealed)
        {
            errors.Add("Hidden achievement exposes its condition before completion.");
        }

        reset.RecordGardenInteraction();
        if (reset.CreateSnapshot().States.First(item => item.Config.AchievementId == AchievementService.GardenSecretAchievementId).Concealed)
        {
            errors.Add("Hidden achievement did not reveal itself after completion.");
        }
    }

    private static void ValidateRestartAndClaims(
        AchievementCatalogConfig catalog,
        QuestCatalogConfig quests,
        ICollection<string> errors)
    {
        if (catalog == null)
        {
            return;
        }

        var save = GameSaveData.CreateDefault("level_01");
        save.completedLevelIds.Add("level_01");
        var machine = new AchievementStateMachine(catalog, quests, save, null, null, () => "2026-07-22");
        var restartedData = JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(save));
        var restarted = new AchievementStateMachine(catalog, quests, restartedData, null, null, () => "2026-07-22");
        var beforeCoins = restartedData.fishCoins;
        var beforeExperience = restartedData.playerExperience;
        var reward = catalog.FindById("first_clear").Reward;
        var firstClaim = restarted.ClaimReward("first_clear");
        var secondClaim = restarted.ClaimReward("first_clear");
        if (!firstClaim.Claimed
            || secondClaim.Claimed
            || restartedData.fishCoins != beforeCoins + reward.FishCoins
            || restartedData.playerExperience != beforeExperience + reward.PlayerExperience
            || restarted.CreateSnapshot().ClaimableCount != 0)
        {
            errors.Add("Restart-before-claim or repeated claim protection failed to award Fish/XP exactly once.");
        }
    }

    private static void ValidateMigration(ICollection<string> errors)
    {
        var directory = Path.Combine("Temp", "E11SaveMigrationGate");
        Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "schema-v2.json");
            File.WriteAllText(path,
                "{\"schemaVersion\":2,\"fishCoins\":73,\"selectedLevelId\":\"level_02\",\"unlockedLevelIds\":[\"level_01\",\"level_02\"],\"completedLevelIds\":[\"level_01\"],\"playerExperience\":105,\"languageCode\":\"en\"}");
            var migrated = GameSaveService.LoadOrCreateAtPath(path, "level_01", true);
            if (migrated.schemaVersion != 3
                || migrated.fishCoins != 73
                || migrated.playerExperience != 105
                || migrated.languageCode != "en"
                || !migrated.completedLevelIds.Contains("level_01")
                || migrated.achievements == null
                || migrated.processedAchievementEventIds == null
                || GameSaveService.LastLoadStatus != GameSaveLoadStatus.Migrated
                || !File.Exists(GameSaveService.LastBackupPath))
            {
                errors.Add("Schema v2 to v3 migration did not preserve E10 progress or create E11 collections and backup evidence.");
            }

            GameSaveService.SaveAtPath(path, migrated);
            var reloaded = GameSaveService.LoadOrCreateAtPath(path, "level_01", true);
            if (GameSaveService.LastLoadStatus != GameSaveLoadStatus.Loaded
                || JsonUtility.ToJson(reloaded) != JsonUtility.ToJson(migrated))
            {
                errors.Add("Schema v3 achievement save is not idempotent after restart.");
            }
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }

    private static void ValidateAnalyticsBoundary(AchievementCatalogConfig catalog, ICollection<string> errors)
    {
        if (catalog == null)
        {
            return;
        }

        var fake = new FakeAnalyticsService();
        AnalyticsService.ResetForValidation(fake);
        var config = catalog.FindById("first_clear");
        AnalyticsService.TrackAchievementCompleted(config);
        AnalyticsService.TrackAchievementClaim(config, new AchievementClaimResult(true, config.AchievementId, 20, 15));
        var allowed = new HashSet<string>(StringComparer.Ordinal)
        {
            AnalyticsParameterNames.AchievementId,
            AnalyticsParameterNames.AchievementCategory,
            AnalyticsParameterNames.AchievementTier,
            AnalyticsParameterNames.EarnedFishCoins,
            AnalyticsParameterNames.EarnedPlayerExperience
        };
        if (fake.Events.Count != 2 || fake.Events.Any(item => item.Parameters.Keys.Any(key => !allowed.Contains(key))))
        {
            errors.Add("Achievement analytics contains unexpected or potentially personal parameters.");
        }
    }

    private static void ValidateLocalization(AchievementCatalogConfig catalog, ICollection<string> errors)
    {
        if (catalog == null)
        {
            return;
        }

        var keys = new List<string>
        {
            "achievement.wall.title", "achievement.wall.progress", "achievement.postRoundSummary",
            "achievement.hidden.title", "achievement.hidden.description", "achievement.claimReward",
            "achievement.category.campaign", "achievement.category.towerMastery", "achievement.category.routeControl",
            "achievement.category.ultimates", "achievement.category.perfectDefense", "achievement.category.collection",
            "achievement.category.secrets", "achievement.category.longTerm"
        };
        keys.AddRange(catalog.Achievements.SelectMany(item => new[] { item.TitleLocalizationKey, item.DescriptionLocalizationKey }));
        var original = LocalizationService.CurrentLanguageCode;
        foreach (var language in new[] { "en", "ru" })
        {
            LocalizationService.SetLanguage(language);
            foreach (var key in keys)
            {
                if (LocalizationService.Text(key) == key)
                {
                    errors.Add($"Missing {language} achievement localization '{key}'.");
                }
            }
        }

        LocalizationService.SetLanguage(original);
    }

    private static void ValidateRuntimeBoundaries(ICollection<string> errors)
    {
        var required = new Dictionary<string, string[]>
        {
            ["Assets/_Project/Scripts/UI/Screens/MainMenuController.cs"] = new[]
            {
                "AchievementService.Initialize", "DrawAchievements", "AchievementService.ClaimReward",
                "AchievementService.RecordGardenInteraction", "DrawCodexPanel"
            },
            ["Assets/_Project/Scripts/Gameplay/Levels/PrototypeLevelController.cs"] = new[]
            {
                "AchievementService.RecordBattle", "AchievementService.RollbackBattleEvent", "ActivatedUltimateIds"
            },
            ["Assets/_Project/Scripts/Meta/Progression/ProgressionService.cs"] = new[]
            {
                "AchievementService.RecordDailyRewardClaim"
            },
            ["Assets/_Project/Scripts/Meta/HomeHub/HomeHubBadgeService.cs"] = new[]
            {
                "AchievementService.ClaimableCount"
            },
            ["Assets/_Project/Scripts/SDK/Analytics/AnalyticsService.cs"] = new[]
            {
                "TrackAchievementCompleted", "TrackAchievementClaim"
            }
        };

        foreach (var pair in required)
        {
            var source = File.Exists(pair.Key) ? File.ReadAllText(pair.Key) : string.Empty;
            foreach (var token in pair.Value)
            {
                if (!source.Contains(token, StringComparison.Ordinal))
                {
                    errors.Add($"E11 runtime boundary '{token}' is missing from {pair.Key}.");
                }
            }
        }
    }

    private static void ValidateDocumentation(ICollection<string> errors)
    {
        foreach (var path in new[] { ReportPath, WorkflowPath, "docs/planning/SAVE_SCHEMA_AND_META_PROGRESSION.md" })
        {
            if (!File.Exists(path) || File.ReadAllText(path).Length < 600)
            {
                errors.Add($"E11 documentation is missing or incomplete: {path}.");
            }
        }
    }
}
