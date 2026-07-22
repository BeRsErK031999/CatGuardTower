using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CatGuard.Core.Localization;
using CatGuard.Core.Save;
using CatGuard.Gameplay.Levels;
using CatGuard.Gameplay.Ultimates;
using CatGuard.Meta.GuardianGrowth;
using CatGuard.Meta.Quests;
using UnityEditor;
using UnityEngine;

public static class E10ProjectSetup
{
    private const string MetaFolder = "Assets/_Project/Resources/MetaProgression";
    private const string MetaCatalogPath = MetaFolder + "/MetaProgressionCatalog.asset";
    private const string LevelCatalogPath = "Assets/_Project/ScriptableObjects/Levels/LevelCatalog.asset";
    private const string UltimateCatalogPath = "Assets/_Project/Resources/Ultimates/UltimateCatalog.asset";
    private const string QuestCatalogPath = "Assets/_Project/Resources/Quests/QuestCatalog.asset";
    private const string ReportPath = "docs/planning/E10_META_PROGRESSION_REPORT.md";
    private const string MigrationPath = "docs/planning/SAVE_SCHEMA_AND_META_PROGRESSION.md";

    public static void Run()
    {
        Directory.CreateDirectory(MetaFolder);
        var catalog = AssetDatabase.LoadAssetAtPath<MetaProgressionCatalogConfig>(MetaCatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<MetaProgressionCatalogConfig>();
            AssetDatabase.CreateAsset(catalog, MetaCatalogPath);
        }

        catalog.Configure(
            new PlayerRankConfig(new[] { 0, 50, 150, 300, 520 }, 60, 20),
            CreateMasteries(),
            CreateResearch(),
            CreateUltimateUnlocks(),
            CreatePerks(),
            CreateCodex());
        EditorUtility.SetDirty(catalog);
        ConfigureQuestExperience();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        ValidateAndExit();
    }

    public static void Validate()
    {
        ValidateAndExit();
    }

    private static TowerMasteryConfig[] CreateMasteries()
    {
        return new[]
        {
            new TowerMasteryConfig("cat_dart", new[] { 0, 20, 60 }, "dart_precision", "dart_calico"),
            new TowerMasteryConfig("yarn_cannon", new[] { 0, 20, 60 }, "yarn_resonance", "yarn_moonspun"),
            new TowerMasteryConfig("bell_sniper", new[] { 0, 20, 60 }, "bell_hunter", "bell_silver"),
            new TowerMasteryConfig("laser_pointer", new[] { 0, 20, 60 }, "laser_chain", "laser_aurora"),
            new TowerMasteryConfig("blanket_boom", new[] { 0, 20, 60 }, "blanket_snare", "blanket_patchwork")
        };
    }

    private static WorkshopResearchConfig[] CreateResearch()
    {
        return new[]
        {
            new WorkshopResearchConfig(
                "starting_supplies",
                "meta.research.starting_supplies.name",
                "meta.research.starting_supplies.description",
                MetaResearchEffectType.StartingBattleFish,
                new[] { 20, 45, 80 },
                new[] { 8f, 14f, 18f }),
            new WorkshopResearchConfig(
                "barrel_reinforcement",
                "meta.research.barrel_reinforcement.name",
                "meta.research.barrel_reinforcement.description",
                MetaResearchEffectType.BaseLives,
                new[] { 30, 65, 110 },
                new[] { 1f, 2f, 3f },
                2),
            new WorkshopResearchConfig(
                "guardian_focus",
                "meta.research.guardian_focus.name",
                "meta.research.guardian_focus.description",
                MetaResearchEffectType.StartingUltimateChargePercent,
                new[] { 25, 55, 95 },
                new[] { 5f, 9f, 12f },
                2)
        };
    }

    private static GuardianUltimateUnlockConfig[] CreateUltimateUnlocks()
    {
        return new[]
        {
            new GuardianUltimateUnlockConfig("yarn_meteor_shower", 1),
            new GuardianUltimateUnlockConfig("catnip_moon", 2),
            new GuardianUltimateUnlockConfig("nine_lives_ward", 2, "cat_dart", 2)
        };
    }

    private static GuardianPerkConfig[] CreatePerks()
    {
        return new[]
        {
            new GuardianPerkConfig(
                "supply_pouch",
                "meta.perk.supply_pouch.name",
                "meta.perk.supply_pouch.description",
                GuardianPerkEffectType.StartingBattleFish,
                5f,
                1),
            new GuardianPerkConfig(
                "steady_heart",
                "meta.perk.steady_heart.name",
                "meta.perk.steady_heart.description",
                GuardianPerkEffectType.BaseLives,
                1f,
                2),
            new GuardianPerkConfig(
                "charged_whiskers",
                "meta.perk.charged_whiskers.name",
                "meta.perk.charged_whiskers.description",
                GuardianPerkEffectType.StartingUltimateChargePercent,
                8f,
                2,
                "guardian_focus",
                1)
        };
    }

    private static CodexEntryConfig[] CreateCodex()
    {
        var entries = new List<CodexEntryConfig>();
        for (var index = 1; index <= 10; index++)
        {
            var levelId = $"level_{index:00}";
            entries.Add(new CodexEntryConfig($"map:{levelId}", CodexEntryType.Map, levelId, $"level.{levelId}"));
        }

        foreach (var towerId in new[] { "cat_dart", "yarn_cannon", "bell_sniper", "laser_pointer", "blanket_boom" })
        {
            entries.Add(new CodexEntryConfig($"tower:{towerId}", CodexEntryType.Tower, towerId, $"tower.{towerId}"));
        }

        foreach (var enemyId in new[] { "mouse_scout", "moth_swarm", "rat_bruiser", "beetle_guard", "snail_tank" })
        {
            entries.Add(new CodexEntryConfig($"enemy:{enemyId}", CodexEntryType.Enemy, enemyId, $"meta.codex.enemy.{enemyId}"));
        }

        return entries.ToArray();
    }

    private static void ConfigureQuestExperience()
    {
        var questCatalog = AssetDatabase.LoadAssetAtPath<QuestCatalogConfig>(QuestCatalogPath);
        if (questCatalog == null)
        {
            return;
        }

        foreach (var quest in questCatalog.Quests)
        {
            if (quest?.Reward != null)
            {
                quest.Reward.playerExperience = 25;
            }
        }

        EditorUtility.SetDirty(questCatalog);
    }

    private static void ValidateAndExit()
    {
        var errors = new List<string>();
        var catalog = AssetDatabase.LoadAssetAtPath<MetaProgressionCatalogConfig>(MetaCatalogPath);
        var levels = AssetDatabase.LoadAssetAtPath<LevelCatalogConfig>(LevelCatalogPath);
        var ultimates = AssetDatabase.LoadAssetAtPath<UltimateCatalogConfig>(UltimateCatalogPath);
        ValidateCatalog(catalog, levels, ultimates, errors);
        ValidateCleanAndPartialState(catalog, ultimates, errors);
        ValidateBattleProgression(catalog, ultimates, errors);
        ValidateEconomyCaps(catalog, ultimates, errors);
        ValidateSaveMigration(errors);
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
            "E10 validation passed: current-schema backup/migration and corrupt/future recovery preserve legacy progress; rank, five tower masteries, capped workshop research, two-slot guardian loadout, perks, and 20-entry codex progress persist with duplicate-event and rewarded-revive rollback guards and without battle-upgrade leakage.");
        EditorApplication.Exit(0);
    }

    private static void ValidateCatalog(
        MetaProgressionCatalogConfig catalog,
        LevelCatalogConfig levels,
        UltimateCatalogConfig ultimates,
        ICollection<string> errors)
    {
        var error = "Catalog is missing.";
        if (catalog == null || !catalog.IsValid(out error))
        {
            errors.Add($"E10 catalog is invalid: {error}");
            return;
        }

        var towerIds = new HashSet<string>(
            levels?.Levels.SelectMany(level => level.AvailableTowers).Where(tower => tower != null).Select(tower => tower.TowerId)
                ?? Array.Empty<string>(),
            StringComparer.Ordinal);
        foreach (var mastery in catalog.TowerMasteries)
        {
            if (!towerIds.Contains(mastery.TowerId))
            {
                errors.Add($"Mastery references missing tower '{mastery.TowerId}'.");
            }
        }

        var ultimateIds = new HashSet<string>(ultimates?.Ultimates.Select(item => item.UltimateId) ?? Array.Empty<string>(), StringComparer.Ordinal);
        if (catalog.UltimateUnlocks.Any(item => !ultimateIds.Contains(item.UltimateId)))
        {
            errors.Add("Guardian loadout unlocks do not match the production ultimate catalog.");
        }
    }

    private static void ValidateCleanAndPartialState(
        MetaProgressionCatalogConfig catalog,
        UltimateCatalogConfig ultimates,
        ICollection<string> errors)
    {
        if (catalog == null || ultimates == null)
        {
            return;
        }

        var clean = GameSaveData.CreateDefault("level_01");
        var machine = new MetaProgressionStateMachine(catalog, ultimates, clean);
        var snapshot = machine.CreateSnapshot();
        if (snapshot.Rank != 1
            || snapshot.UnlockedUltimateIds.Length != 1
            || snapshot.EquippedUltimateIds.Length != 1
            || clean.unlockedGuardianPerkIds.Count != 1
            || string.IsNullOrWhiteSpace(snapshot.EquippedPerkId))
        {
            errors.Add("Clean E10 save did not create the intentional rank-1 guardian loadout.");
        }

        clean.playerExperience = 170;
        clean.fishCoins = 73;
        clean.workshopResearch.Add(new ResearchSaveEntry("starting_supplies", 1));
        clean.towerMasteries.Add(new TowerMasterySaveEntry("cat_dart") { experience = 24 });
        var restarted = new MetaProgressionStateMachine(catalog, ultimates, clean).CreateSnapshot();
        if (restarted.Rank != 3
            || restarted.Research.First(item => item.Config.ResearchId == "starting_supplies").Level != 1
            || restarted.Masteries.First(item => item.Config.TowerId == "cat_dart").Level != 2)
        {
            errors.Add("Partially complete E10 save did not survive service restart.");
        }
    }

    private static void ValidateBattleProgression(
        MetaProgressionCatalogConfig catalog,
        UltimateCatalogConfig ultimates,
        ICollection<string> errors)
    {
        if (catalog == null || ultimates == null)
        {
            return;
        }

        var save = GameSaveData.CreateDefault("level_01");
        var machine = new MetaProgressionStateMachine(catalog, ultimates, save);
        var report = new MetaBattleReport(
            "e10-win",
            "level_01",
            true,
            new Dictionary<string, int> { ["cat_dart"] = 1 },
            new Dictionary<string, int> { ["cat_dart"] = 2 },
            new[] { "mouse_scout", "moth_swarm" });
        var result = machine.RecordBattle(report);
        if (result.CurrentRank < 2
            || result.MasteryUpdates.Length != 1
            || !result.MasteryUpdates[0].LeveledUp
            || result.Discoveries.Length != 4
            || !save.unlockedUltimateIds.Contains("catnip_moon")
            || !save.unlockedUltimateIds.Contains("nine_lives_ward"))
        {
            errors.Add("One earned battle did not unlock rank, mastery, loadout, and codex progression together.");
        }

        var experience = save.playerExperience;
        var masteryExperience = save.towerMasteries[0].experience;
        if (!machine.RecordBattle(report).Duplicate
            || save.playerExperience != experience
            || save.towerMasteries[0].experience != masteryExperience)
        {
            errors.Add("Duplicate E10 battle event changed rank or mastery progress.");
        }

        var rollbackSave = GameSaveData.CreateDefault("level_01");
        var rollbackMachine = new MetaProgressionStateMachine(catalog, ultimates, rollbackSave);
        rollbackMachine.RecordBattle(report);
        if (!rollbackMachine.RollbackBattleEvent(report.EventId)
            || rollbackSave.playerExperience != 0
            || rollbackSave.processedMetaBattleEventIds.Contains(report.EventId)
            || rollbackSave.discoveredCodexEntryIds.Count != 0)
        {
            errors.Add("Rewarded-revive rollback did not restore E10 rank, mastery, codex, and event state.");
        }

        if (save.workshopResearch.Any(item => item.researchId.Contains("branch", StringComparison.OrdinalIgnoreCase)))
        {
            errors.Add("A runtime battle-upgrade branch leaked into persistent meta research.");
        }
    }

    private static void ValidateEconomyCaps(
        MetaProgressionCatalogConfig catalog,
        UltimateCatalogConfig ultimates,
        ICollection<string> errors)
    {
        if (catalog == null || ultimates == null)
        {
            return;
        }

        var save = GameSaveData.CreateDefault("level_01");
        var machine = new MetaProgressionStateMachine(catalog, ultimates, save);
        save.fishCoins = 19;
        if (machine.BuyResearch("starting_supplies") || save.fishCoins != 19)
        {
            errors.Add("Insufficient Fish Coins purchased workshop research.");
        }

        save.fishCoins = 500;
        var config = catalog.FindResearch("starting_supplies");
        for (var index = 0; index < config.MaxLevel; index++)
        {
            if (!machine.BuyResearch(config.ResearchId))
            {
                errors.Add("Affordable workshop research did not advance.");
                break;
            }
        }

        var coinsAtCap = save.fishCoins;
        if (machine.BuyResearch(config.ResearchId)
            || save.fishCoins != coinsAtCap
            || machine.CreateSnapshot().Research.First(item => item.Config == config).Level != config.MaxLevel)
        {
            errors.Add("Workshop research cap allowed an extra purchase or currency spend.");
        }
    }

    private static void ValidateSaveMigration(ICollection<string> errors)
    {
        var directory = Path.Combine("Temp", "E10SaveMigrationGate");
        Directory.CreateDirectory(directory);
        try
        {
            var legacyPath = Path.Combine(directory, "legacy.json");
            File.WriteAllText(legacyPath,
                "{\"fishCoins\":91,\"selectedLevelId\":\"level_02\",\"unlockedLevelIds\":[\"level_01\",\"level_02\"],\"completedLevelIds\":[\"level_01\"],\"upgrades\":[{\"upgradeId\":\"claw_training\",\"level\":2}],\"dailyMissionDateKey\":\"2026-07-21\",\"audioMuted\":true,\"languageCode\":\"en\"}");
            var migrated = GameSaveService.LoadOrCreateAtPath(legacyPath, "level_01", true);
            if (migrated.schemaVersion != GameSaveMigrationService.CurrentSchemaVersion
                || migrated.fishCoins != 91
                || migrated.selectedLevelId != "level_02"
                || !migrated.completedLevelIds.Contains("level_01")
                || !migrated.audioMuted
                || migrated.languageCode != "en"
                || migrated.workshopResearch.All(item => item.researchId != "starting_supplies" || item.level != 2)
                || GameSaveService.LastLoadStatus != GameSaveLoadStatus.Migrated
                || !File.Exists(GameSaveService.LastBackupPath))
            {
                errors.Add("Portrait-era save migration did not preserve progress, settings, legacy purchases, or backup evidence.");
            }

            GameSaveService.SaveAtPath(legacyPath, migrated);
            var reloaded = GameSaveService.LoadOrCreateAtPath(legacyPath, "level_01", true);
            if (GameSaveService.LastLoadStatus != GameSaveLoadStatus.Loaded
                || JsonUtility.ToJson(reloaded) != JsonUtility.ToJson(migrated))
            {
                errors.Add("Save migration is not idempotent after restart.");
            }

            var corruptPath = Path.Combine(directory, "corrupt.json");
            File.WriteAllText(corruptPath, "{not-json");
            var recovered = GameSaveService.LoadOrCreateAtPath(corruptPath, "level_01", true);
            if (recovered.schemaVersion != GameSaveMigrationService.CurrentSchemaVersion
                || GameSaveService.LastLoadStatus != GameSaveLoadStatus.RecoveredCorrupt
                || !File.Exists(GameSaveService.LastBackupPath))
            {
                errors.Add("Corrupted save recovery did not preserve a backup and return a safe current save.");
            }

            var futurePath = Path.Combine(directory, "future.json");
            File.WriteAllText(futurePath, "{\"schemaVersion\":99,\"fishCoins\":999}");
            var future = GameSaveService.LoadOrCreateAtPath(futurePath, "level_01", true);
            if (future.fishCoins != 0
                || GameSaveService.LastLoadStatus != GameSaveLoadStatus.RecoveredFutureVersion
                || !File.Exists(GameSaveService.LastBackupPath))
            {
                errors.Add("Unknown future save schema was not preserved before safe recovery.");
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

    private static void ValidateLocalization(MetaProgressionCatalogConfig catalog, ICollection<string> errors)
    {
        if (catalog == null)
        {
            return;
        }

        var original = LocalizationService.CurrentLanguageCode;
        var keys = catalog.WorkshopResearch.SelectMany(item => new[] { item.NameLocalizationKey, item.DescriptionLocalizationKey })
            .Concat(catalog.GuardianPerks.SelectMany(item => new[] { item.NameLocalizationKey, item.DescriptionLocalizationKey }))
            .Concat(catalog.CodexEntries.Select(item => item.NameLocalizationKey))
            .Distinct(StringComparer.Ordinal);
        foreach (var language in new[] { LocalizationService.English, LocalizationService.Russian })
        {
            LocalizationService.SetLanguage(language);
            foreach (var key in keys)
            {
                if (LocalizationService.Text(key) == key)
                {
                    errors.Add($"E10 localization '{key}' is missing in '{language}'.");
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
                "MetaProgressionService.Initialize",
                "DrawMetaProgression",
                "MetaProgressionService.BuyResearch",
                "MetaProgressionService.ToggleUltimateEquipped",
                "DrawCodex"
            },
            ["Assets/_Project/Scripts/Gameplay/Levels/PrototypeLevelController.cs"] = new[]
            {
                "MetaProgressionService.RecordBattle",
                "MetaProgressionService.RollbackBattleEvent",
                "GetStartingBattleFishBonus"
            },
            ["Assets/_Project/Scripts/Core/Save/GameSaveService.cs"] = new[]
            {
                "RecoveredFutureVersion",
                "CreateBackup",
                "GameSaveMigrationService.TryMigrate"
            },
            ["Assets/_Project/Scripts/Gameplay/Ultimates/GuardianUltimateController.cs"] = new[]
            {
                "GetEquippedUltimateIds",
                "GetStartingUltimateCharge01"
            }
        };

        foreach (var pair in required)
        {
            var source = File.Exists(pair.Key) ? File.ReadAllText(pair.Key) : string.Empty;
            foreach (var token in pair.Value)
            {
                if (!source.Contains(token, StringComparison.Ordinal))
                {
                    errors.Add($"E10 runtime boundary '{token}' is missing from {pair.Key}.");
                }
            }
        }
    }

    private static void ValidateDocumentation(ICollection<string> errors)
    {
        foreach (var path in new[] { ReportPath, MigrationPath })
        {
            if (!File.Exists(path) || File.ReadAllText(path).Length < 500)
            {
                errors.Add($"E10 documentation is missing or incomplete: {path}.");
            }
        }
    }
}
