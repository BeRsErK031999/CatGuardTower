using System;
using CatGuard.Gameplay.Levels;
using CatGuard.Meta.Progression;
using CatGuard.Meta.Quests;
using CatGuard.Meta.GuardianGrowth;
using CatGuard.Meta.Achievements;

namespace CatGuard.Meta.HomeHub
{
    public sealed class HomeHubBattleSummary
    {
        public bool Won { get; }
        public string LevelId { get; }
        public string LevelDisplayName { get; }
        public int EarnedFishCoins { get; }
        public bool FirstClear { get; }
        public string[] UnlockedLevelNames { get; }
        public int RemainingLives { get; }
        public int DefeatedEnemies { get; }
        public int EscapedEnemies { get; }
        public QuestProgressBatch QuestProgress { get; }
        public MetaProgressionBattleResult MetaProgress { get; }
        public AchievementProgressBatch AchievementProgress { get; }

        public HomeHubBattleSummary(
            bool won,
            string levelId,
            string levelDisplayName,
            int earnedFishCoins,
            bool firstClear,
            string[] unlockedLevelNames,
            int remainingLives,
            int defeatedEnemies,
            int escapedEnemies,
            QuestProgressBatch questProgress,
            MetaProgressionBattleResult metaProgress,
            AchievementProgressBatch achievementProgress)
        {
            Won = won;
            LevelId = levelId ?? string.Empty;
            LevelDisplayName = levelDisplayName ?? string.Empty;
            EarnedFishCoins = Math.Max(0, earnedFishCoins);
            FirstClear = firstClear;
            UnlockedLevelNames = unlockedLevelNames ?? Array.Empty<string>();
            RemainingLives = Math.Max(0, remainingLives);
            DefeatedEnemies = Math.Max(0, defeatedEnemies);
            EscapedEnemies = Math.Max(0, escapedEnemies);
            QuestProgress = questProgress ?? QuestProgressBatch.Empty;
            MetaProgress = metaProgress ?? MetaProgressionBattleResult.Empty;
            AchievementProgress = achievementProgress ?? AchievementProgressBatch.Empty;
        }
    }

    public static class HomeHubNavigationService
    {
        private static HomeHubBattleSummary pendingBattleSummary;

        public static HomeHubBattleSummary PendingBattleSummary => pendingBattleSummary;

        public static void BeginBattle(LevelConfig level)
        {
            pendingBattleSummary = null;
        }

        public static void RecordVictory(
            LevelConfig level,
            LevelCompletionResult result,
            int remainingLives,
            int defeatedEnemies,
            int escapedEnemies,
            QuestProgressBatch questProgress = null,
            MetaProgressionBattleResult metaProgress = null,
            AchievementProgressBatch achievementProgress = null)
        {
            var unlockedNames = result?.UnlockedLevelNames == null
                ? Array.Empty<string>()
                : new string[result.UnlockedLevelNames.Count];
            if (result?.UnlockedLevelNames != null)
            {
                for (var index = 0; index < result.UnlockedLevelNames.Count; index++)
                {
                    unlockedNames[index] = result.UnlockedLevelNames[index] ?? string.Empty;
                }
            }

            pendingBattleSummary = new HomeHubBattleSummary(
                true,
                level?.LevelId,
                level?.DisplayName,
                result?.TotalEarnedFishCoins ?? 0,
                result?.FirstClear == true,
                unlockedNames,
                remainingLives,
                defeatedEnemies,
                escapedEnemies,
                questProgress ?? pendingBattleSummary?.QuestProgress,
                metaProgress ?? pendingBattleSummary?.MetaProgress,
                achievementProgress ?? pendingBattleSummary?.AchievementProgress);
        }

        public static void RecordDefeat(
            LevelConfig level,
            int defeatedEnemies,
            int escapedEnemies,
            QuestProgressBatch questProgress = null,
            MetaProgressionBattleResult metaProgress = null,
            AchievementProgressBatch achievementProgress = null)
        {
            pendingBattleSummary = new HomeHubBattleSummary(
                false,
                level?.LevelId,
                level?.DisplayName,
                0,
                false,
                Array.Empty<string>(),
                0,
                defeatedEnemies,
                escapedEnemies,
                questProgress,
                metaProgress,
                achievementProgress);
        }

        public static HomeHubBattleSummary ConsumeBattleSummary()
        {
            var result = pendingBattleSummary;
            pendingBattleSummary = null;
            return result;
        }

        public static void ClearPendingBattleSummary()
        {
            pendingBattleSummary = null;
        }
    }
}
