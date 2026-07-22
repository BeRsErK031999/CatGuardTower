using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CatGuard.Core.Localization;
using CatGuard.Core.Save;
using CatGuard.Gameplay.Levels;
using CatGuard.Meta.DailyRewards;
using CatGuard.Meta.Quests;
using UnityEditor;
using UnityEngine;

public static class E9ProjectSetup
{
    private const string QuestFolder = "Assets/_Project/Resources/Quests";
    private const string QuestCatalogPath = QuestFolder + "/QuestCatalog.asset";
    private const string LevelCatalogPath = "Assets/_Project/ScriptableObjects/Levels/LevelCatalog.asset";
    private const string MainMenuSourcePath = "Assets/_Project/Scripts/UI/Screens/MainMenuController.cs";
    private const string LevelSourcePath = "Assets/_Project/Scripts/Gameplay/Levels/PrototypeLevelController.cs";
    private const string HudSourcePath = "Assets/_Project/Scripts/UI/HUD/PrototypeHud.cs";
    private const string SaveSourcePath = "Assets/_Project/Scripts/Core/Save/GameSaveData.cs";
    private const string ReportPath = "docs/planning/E9_QUEST_BOARD_REPORT.md";
    private const string WorkflowPath = "docs/planning/QUEST_CONTRACT_WORKFLOW.md";

    public static void Run()
    {
        Directory.CreateDirectory(QuestFolder);
        var catalog = AssetDatabase.LoadAssetAtPath<QuestCatalogConfig>(QuestCatalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<QuestCatalogConfig>();
            AssetDatabase.CreateAsset(catalog, QuestCatalogPath);
        }

        catalog.Configure(CreateProductionQuests(), 4);
        EditorUtility.SetDirty(catalog);
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
        var catalog = AssetDatabase.LoadAssetAtPath<QuestCatalogConfig>(QuestCatalogPath);
        var levelCatalog = AssetDatabase.LoadAssetAtPath<LevelCatalogConfig>(LevelCatalogPath);

        ValidateCatalog(catalog, levelCatalog, errors);
        ValidateEveryObjective(catalog, errors);
        ValidateStateMachine(catalog, levelCatalog, errors);
        ValidateLockedContentFiltering(catalog, levelCatalog, errors);
        ValidateDailyRollover(errors);
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
            "E9 validation passed: 12 config-driven contracts and 10 objective types support four active slots, simultaneous battle progress, active/completed/claimed Quest Board states, post-round summaries, daily mission adaptation, unlocked-content filtering, UTC daily rollover policy, persisted restart state, revive rollback, and duplicate event/reward protection.");
        EditorApplication.Exit(0);
    }

    private static QuestConfig[] CreateProductionQuests()
    {
        return new[]
        {
            CreateQuest("tutorial_hold_line", QuestCategory.Tutorial, QuestObjectiveType.WinBattles, 1, 15),
            CreateQuest("pest_patrol", QuestCategory.Contract, QuestObjectiveType.DefeatEnemies, 20, 12),
            CreateQuest("build_circle", QuestCategory.Contract, QuestObjectiveType.PlaceTowers, 8, 10),
            CreateQuest("guardian_signal", QuestCategory.Tutorial, QuestObjectiveType.UseUltimates, 1, 14),
            CreateQuest(
                "careful_craftsman",
                QuestCategory.Mastery,
                QuestObjectiveType.ReachTowerTier,
                1,
                18,
                threshold: 3,
                towerId: "cat_dart"),
            CreateQuest("control_school", QuestCategory.Mastery, QuestObjectiveType.ControlEnemies, 15, 16),
            CreateQuest("untouched_barrel", QuestCategory.Contract, QuestObjectiveType.WinWithoutLifeLoss, 1, 22),
            CreateQuest(
                "small_paw_squad",
                QuestCategory.Contract,
                QuestObjectiveType.WinWithMaxTowers,
                1,
                25,
                threshold: 3),
            CreateQuest("no_scrap", QuestCategory.Contract, QuestObjectiveType.WinWithoutSelling, 1, 20),
            CreateQuest(
                "old_well_watch",
                QuestCategory.MapChallenge,
                QuestObjectiveType.WinOnLevel,
                1,
                28,
                levelId: "level_08"),
            CreateQuest(
                "rooftop_watch",
                QuestCategory.MapChallenge,
                QuestObjectiveType.WinOnLevel,
                1,
                30,
                levelId: "level_07"),
            CreateQuest("veteran_guard", QuestCategory.Mastery, QuestObjectiveType.WinBattles, 3, 35)
        };
    }

    private static QuestConfig CreateQuest(
        string id,
        QuestCategory category,
        QuestObjectiveType type,
        int target,
        int reward,
        int threshold = 0,
        string levelId = "",
        string towerId = "")
    {
        return new QuestConfig(
            id,
            category,
            $"quest.name.{id}",
            $"quest.description.{id}",
            new QuestObjectiveConfig(type, target, threshold, levelId, towerId),
            new QuestRewardConfig(reward));
    }

    private static void ValidateCatalog(
        QuestCatalogConfig catalog,
        LevelCatalogConfig levelCatalog,
        ICollection<string> errors)
    {
        var error = "Quest catalog asset is missing.";
        if (catalog == null || !catalog.IsValid(out error))
        {
            errors.Add($"E9 catalog is invalid: {error}");
            return;
        }

        if (catalog.ActiveContractLimit != 4)
        {
            errors.Add("E9 production catalog must expose exactly four active contract slots.");
        }

        foreach (var quest in catalog.Quests)
        {
            var objective = quest.Objective;
            if (!string.IsNullOrWhiteSpace(objective.RequiredLevelId)
                && levelCatalog?.FindById(objective.RequiredLevelId) == null)
            {
                errors.Add($"Quest '{quest.QuestId}' references missing level '{objective.RequiredLevelId}'.");
            }

            if (string.IsNullOrWhiteSpace(objective.RequiredTowerId))
            {
                continue;
            }

            var towerExists = levelCatalog?.Levels.Any(level => level != null
                && level.AvailableTowers.Any(tower => tower != null && tower.TowerId == objective.RequiredTowerId)) == true;
            if (!towerExists)
            {
                errors.Add($"Quest '{quest.QuestId}' references missing tower '{objective.RequiredTowerId}'.");
            }
        }
    }

    private static void ValidateEveryObjective(QuestCatalogConfig catalog, ICollection<string> errors)
    {
        if (catalog == null)
        {
            return;
        }

        foreach (var quest in catalog.Quests)
        {
            var objective = quest.Objective;
            var towerTiers = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                [string.IsNullOrWhiteSpace(objective.RequiredTowerId) ? "cat_dart" : objective.RequiredTowerId] = Math.Max(3, objective.Threshold)
            };
            var report = new QuestBattleReport(
                $"objective-{quest.QuestId}",
                string.IsNullOrWhiteSpace(objective.RequiredLevelId) ? "level_01" : objective.RequiredLevelId,
                true,
                20,
                20,
                0,
                Math.Max(25, objective.TargetAmount),
                0,
                objective.ObjectiveType == QuestObjectiveType.WinWithMaxTowers
                    ? objective.Threshold
                    : Math.Max(8, objective.TargetAmount),
                0,
                Math.Max(15, objective.TargetAmount),
                Math.Max(1, objective.TargetAmount),
                towerTiers);
            if (QuestStateMachine.EvaluateDelta(objective, report) <= 0)
            {
                errors.Add($"Objective evaluator did not progress '{quest.QuestId}' ({objective.ObjectiveType}).");
            }
        }

        var restoredLivesReport = new QuestBattleReport(
            "restored-lives",
            "level_01",
            true,
            20,
            20,
            1,
            8,
            0,
            3,
            0,
            0,
            0,
            new Dictionary<string, int>(StringComparer.Ordinal));
        var flawless = catalog.FindById("untouched_barrel");
        if (flawless == null
            || QuestStateMachine.EvaluateDelta(flawless.Objective, restoredLivesReport) != 0)
        {
            errors.Add("Life-loss contract incorrectly passed after healing restored the final life total.");
        }
    }

    private static void ValidateStateMachine(
        QuestCatalogConfig catalog,
        LevelCatalogConfig levelCatalog,
        ICollection<string> errors)
    {
        if (catalog == null || levelCatalog == null)
        {
            return;
        }

        var save = CreateUnlockedSave(levelCatalog);
        var machine = new QuestStateMachine(catalog, levelCatalog, save);
        var initial = machine.CreateSnapshot();
        if (initial.Active.Length != 4 || initial.Completed.Length != 0 || initial.Claimed.Length != 0)
        {
            errors.Add("Fresh E9 state did not create four clear active contracts.");
        }

        var report = CreateFullProgressReport("simultaneous-event");
        var batch = machine.ProcessBattle(report);
        if (batch.Updates.Length != 4 || batch.CompletedCount != 4)
        {
            errors.Add("One battle did not progress all four simultaneous active contracts.");
        }

        var progressBeforeDuplicate = initial.Active.ToDictionary(
            state => state.Quest.QuestId,
            state => machine.GetProgress(state.Quest.QuestId),
            StringComparer.Ordinal);
        var duplicate = machine.ProcessBattle(report);
        if (!duplicate.Duplicate || duplicate.HasUpdates
            || progressBeforeDuplicate.Any(pair => machine.GetProgress(pair.Key) != pair.Value))
        {
            errors.Add("Duplicate battle event protection changed quest progress.");
        }

        var restarted = new QuestStateMachine(catalog, levelCatalog, save);
        var restartSnapshot = restarted.CreateSnapshot();
        if (restartSnapshot.Completed.Length != 4
            || !restarted.ProcessBattle(report).Duplicate)
        {
            errors.Add("Quest completion or processed event ids did not survive an app-style service restart.");
        }

        var claim = restartSnapshot.Completed[0];
        var coinsBefore = save.fishCoins;
        if (!restarted.ClaimReward(claim.Quest.QuestId)
            || save.fishCoins != coinsBefore + claim.Quest.Reward.FishCoins)
        {
            errors.Add("Completed quest did not grant its configured reward exactly once.");
        }

        var coinsAfter = save.fishCoins;
        if (restarted.ClaimReward(claim.Quest.QuestId) || save.fishCoins != coinsAfter)
        {
            errors.Add("Repeated quest claim granted a duplicate reward.");
        }

        var afterClaim = new QuestStateMachine(catalog, levelCatalog, save).CreateSnapshot();
        if (afterClaim.Claimed.All(state => state.Quest.QuestId != claim.Quest.QuestId)
            || afterClaim.Active.Length + afterClaim.Completed.Length != catalog.ActiveContractLimit)
        {
            errors.Add("Claimed state or active contract refill did not survive restart.");
        }

        var rollbackSave = CreateUnlockedSave(levelCatalog);
        var rollbackMachine = new QuestStateMachine(catalog, levelCatalog, rollbackSave);
        var rollbackQuestId = rollbackMachine.CreateSnapshot().Active[0].Quest.QuestId;
        var progressBeforeRollback = rollbackMachine.GetProgress(rollbackQuestId);
        rollbackMachine.ProcessBattle(CreateFullProgressReport("revive-event"));
        if (!rollbackMachine.RollbackBattleEvent("revive-event")
            || rollbackMachine.GetProgress(rollbackQuestId) != progressBeforeRollback
            || rollbackSave.processedQuestEventIds.Contains("revive-event"))
        {
            errors.Add("Rewarded revive rollback did not restore pre-result quest state.");
        }
    }

    private static void ValidateLockedContentFiltering(
        QuestCatalogConfig catalog,
        LevelCatalogConfig levelCatalog,
        ICollection<string> errors)
    {
        if (catalog == null || levelCatalog?.FirstLevel == null)
        {
            return;
        }

        var save = GameSaveData.CreateDefault(levelCatalog.FirstLevel.LevelId);
        var machine = new QuestStateMachine(catalog, levelCatalog, save);
        var oldWell = catalog.FindById("old_well_watch");
        if (machine.IsEligible(oldWell))
        {
            errors.Add("Locked Old Well contract was offered before its map unlocked.");
        }

        save.unlockedLevelIds.Add("level_08");
        if (!machine.IsEligible(oldWell))
        {
            errors.Add("Old Well contract remained impossible after its map unlocked.");
        }
    }

    private static void ValidateDailyRollover(ICollection<string> errors)
    {
        var today = new DateTime(2026, 7, 22, 8, 0, 0, DateTimeKind.Utc);
        var key = DailyMissionRotationPolicy.GetUtcDateKey(today);
        if (DailyMissionRotationPolicy.RequiresRollover(key, today)
            || !DailyMissionRotationPolicy.RequiresRollover("2026-07-21", today)
            || !DailyMissionRotationPolicy.TryParseUtcDateKey(key, out var parsed)
            || parsed.Date != today.Date)
        {
            errors.Add("UTC daily mission rollover policy is not deterministic across a date boundary.");
        }
    }

    private static void ValidateLocalization(QuestCatalogConfig catalog, ICollection<string> errors)
    {
        if (catalog == null)
        {
            return;
        }

        var originalLanguage = LocalizationService.CurrentLanguageCode;
        foreach (var language in new[] { LocalizationService.English, LocalizationService.Russian })
        {
            LocalizationService.SetLanguage(language);
            foreach (var quest in catalog.Quests)
            {
                foreach (var key in new[] { quest.NameLocalizationKey, quest.DescriptionLocalizationKey })
                {
                    var text = LocalizationService.Text(key);
                    if (string.IsNullOrWhiteSpace(text) || text == key || text.Length > 90)
                    {
                        errors.Add($"Quest localization '{key}' is missing or too long in '{language}'.");
                    }
                }
            }
        }

        LocalizationService.SetLanguage(originalLanguage);
    }

    private static void ValidateRuntimeBoundaries(ICollection<string> errors)
    {
        var mainMenu = ReadProjectFile(MainMenuSourcePath);
        var level = ReadProjectFile(LevelSourcePath);
        var hud = ReadProjectFile(HudSourcePath);
        var save = ReadProjectFile(SaveSourcePath);
        var requiredTokens = new Dictionary<string, string[]>
        {
            [MainMenuSourcePath] = new[]
            {
                "QuestService.Initialize",
                "DailyMissionQuestAdapter.CreateSnapshot",
                "QuestService.ClaimReward",
                "QuestStatus.Completed"
            },
            [LevelSourcePath] = new[]
            {
                "QuestService.ProcessBattle",
                "QuestService.RollbackBattleEvent",
                "QuestBattleReport"
            },
            [HudSourcePath] = new[] { "quest.postRoundSummary" },
            [SaveSourcePath] = new[] { "activeQuestIds", "processedQuestEventIds", "QuestProgressSaveEntry" }
        };
        var sources = new Dictionary<string, string>
        {
            [MainMenuSourcePath] = mainMenu,
            [LevelSourcePath] = level,
            [HudSourcePath] = hud,
            [SaveSourcePath] = save
        };
        foreach (var pair in requiredTokens)
        {
            foreach (var token in pair.Value)
            {
                if (!sources[pair.Key].Contains(token, StringComparison.Ordinal))
                {
                    errors.Add($"E9 runtime boundary '{pair.Key}' is missing '{token}'.");
                }
            }
        }

        if (mainMenu.Contains("GameSaveData", StringComparison.Ordinal)
            || mainMenu.Contains("GameSaveService", StringComparison.Ordinal))
        {
            errors.Add("Quest Board UI must not edit the save model directly.");
        }
    }

    private static void ValidateDocumentation(ICollection<string> errors)
    {
        foreach (var path in new[] { ReportPath, WorkflowPath })
        {
            if (!File.Exists(Path.Combine(Directory.GetCurrentDirectory(), path)))
            {
                errors.Add($"E9 documentation is missing: {path}.");
            }
        }
    }

    private static GameSaveData CreateUnlockedSave(LevelCatalogConfig levelCatalog)
    {
        var save = GameSaveData.CreateDefault(levelCatalog.FirstLevel.LevelId);
        foreach (var level in levelCatalog.Levels)
        {
            if (level != null && !save.unlockedLevelIds.Contains(level.LevelId))
            {
                save.unlockedLevelIds.Add(level.LevelId);
            }
        }

        return save;
    }

    private static QuestBattleReport CreateFullProgressReport(string eventId)
    {
        return new QuestBattleReport(
            eventId,
            "level_01",
            true,
            20,
            20,
            0,
            30,
            0,
            8,
            0,
            20,
            2,
            new Dictionary<string, int>(StringComparer.Ordinal) { ["cat_dart"] = 3 });
    }

    private static string ReadProjectFile(string relativePath)
    {
        var fullPath = Path.Combine(Directory.GetCurrentDirectory(), relativePath);
        return File.Exists(fullPath) ? File.ReadAllText(fullPath) : string.Empty;
    }
}
