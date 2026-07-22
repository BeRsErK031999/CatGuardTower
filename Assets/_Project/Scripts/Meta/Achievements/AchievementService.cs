using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using CatGuard.Core.Save;
using CatGuard.Meta.GuardianGrowth;
using CatGuard.Meta.Progression;
using CatGuard.Meta.Quests;
using CatGuard.SDK.Analytics;

namespace CatGuard.Meta.Achievements
{
    public sealed class AchievementViewState
    {
        public AchievementViewState(AchievementConfig config, AchievementProgressSaveEntry entry)
        {
            Config = config;
            Progress = Math.Max(0, entry?.progress ?? 0);
            CompletedDateKey = entry?.completedDateKey ?? string.Empty;
            Claimed = entry?.claimed == true;
        }

        public AchievementConfig Config { get; }
        public int Progress { get; }
        public int DisplayProgress => Math.Min(Progress, Config?.ProgressTarget ?? 0);
        public string CompletedDateKey { get; }
        public bool Completed => Config != null && Progress >= Config.ProgressTarget;
        public bool Claimed { get; }
        public bool CanClaim => Completed && !Claimed;
        public bool Concealed => Config?.Hidden == true && !Completed;
    }

    public sealed class AchievementSnapshot
    {
        public static readonly AchievementSnapshot Empty = new(Array.Empty<AchievementViewState>());

        public AchievementSnapshot(AchievementViewState[] states)
        {
            States = states ?? Array.Empty<AchievementViewState>();
        }

        public AchievementViewState[] States { get; }
        public int ClaimableCount => States.Count(item => item?.CanClaim == true);
        public int CompletedCount => States.Count(item => item?.Completed == true);
        public int ClaimedCount => States.Count(item => item?.Claimed == true);
    }

    public sealed class AchievementClaimResult
    {
        public static readonly AchievementClaimResult Failed = new(false, string.Empty, 0, 0);

        public AchievementClaimResult(bool claimed, string achievementId, int fishCoins, int playerExperience)
        {
            Claimed = claimed;
            AchievementId = achievementId ?? string.Empty;
            FishCoins = Math.Max(0, fishCoins);
            PlayerExperience = Math.Max(0, playerExperience);
        }

        public bool Claimed { get; }
        public string AchievementId { get; }
        public int FishCoins { get; }
        public int PlayerExperience { get; }
    }

    public sealed class AchievementStateMachine
    {
        private const int ProcessedEventLimit = 128;
        private readonly AchievementCatalogConfig catalog;
        private readonly QuestCatalogConfig questCatalog;
        private readonly GameSaveData saveData;
        private readonly Action persist;
        private readonly Action rewardApplied;
        private readonly Func<string> dateKeyProvider;
        private readonly Dictionary<string, AchievementRollbackState> rollbackByEvent = new(StringComparer.Ordinal);

        public AchievementStateMachine(
            AchievementCatalogConfig configuredCatalog,
            QuestCatalogConfig configuredQuestCatalog,
            GameSaveData data,
            Action persistAction = null,
            Action rewardAppliedAction = null,
            Func<string> completionDateKeyProvider = null)
        {
            catalog = configuredCatalog ?? throw new ArgumentNullException(nameof(configuredCatalog));
            questCatalog = configuredQuestCatalog;
            saveData = data ?? throw new ArgumentNullException(nameof(data));
            persist = persistAction;
            rewardApplied = rewardAppliedAction;
            dateKeyProvider = completionDateKeyProvider ?? TodayDateKey;
            EnsureCollections();
            var changed = EnsureCatalogEntries();
            changed |= SynchronizeReliableProgressInternal(null);
            if (changed)
            {
                persist?.Invoke();
            }
        }

        public GameSaveData SaveData => saveData;

        public AchievementSnapshot CreateSnapshot()
        {
            return new AchievementSnapshot(catalog.Achievements
                .Select(config => new AchievementViewState(config, GetEntry(config.AchievementId, true)))
                .ToArray());
        }

        public AchievementProgressBatch SynchronizeReliableProgress()
        {
            var updates = new List<AchievementProgressUpdate>();
            if (SynchronizeReliableProgressInternal(updates))
            {
                persist?.Invoke();
            }

            return new AchievementProgressBatch("retroactive", updates.ToArray(), false);
        }

        public AchievementProgressBatch RecordBattle(AchievementBattleReport report)
        {
            if (report == null || string.IsNullOrWhiteSpace(report.EventId))
            {
                return AchievementProgressBatch.Empty;
            }

            if (saveData.processedAchievementEventIds.Contains(report.EventId))
            {
                return new AchievementProgressBatch(report.EventId, Array.Empty<AchievementProgressUpdate>(), true);
            }

            rollbackByEvent[report.EventId] = AchievementRollbackState.Capture(saveData);
            saveData.processedAchievementEventIds.Add(report.EventId);
            TrimProcessedEvents();

            var updates = new List<AchievementProgressUpdate>();
            SynchronizeReliableProgressInternal(updates);
            if (report.Won && report.LivesLost == 0)
            {
                ApplyRule(AchievementProgressRule.PerfectVictories, 1, true, updates);
            }

            if (report.HighestTowerTier >= 3)
            {
                ApplyRule(AchievementProgressRule.TierThreeTowerBattles, 1, false, updates);
            }

            foreach (var ultimateId in report.UltimateIds)
            {
                AddUnique(saveData.achievementUltimateIds, ultimateId);
            }

            ApplyRule(AchievementProgressRule.UniqueUltimatesUsed, saveData.achievementUltimateIds.Count, false, updates);
            ApplyRule(AchievementProgressRule.EnemiesDefeated, report.DefeatedEnemies, true, updates);
            if (report.Won && report.RouteCount > 1)
            {
                ApplyRule(AchievementProgressRule.MultiRouteVictories, 1, true, updates);
            }

            foreach (var bossId in report.DefeatedBossIds)
            {
                AddUnique(saveData.achievementBossIds, bossId);
            }

            ApplyRule(AchievementProgressRule.BossesDefeated, saveData.achievementBossIds.Count, false, updates);
            persist?.Invoke();
            return new AchievementProgressBatch(report.EventId, updates.ToArray(), false);
        }

        public AchievementProgressBatch RecordDailyRewardClaim(string dateKey)
        {
            var eventId = string.IsNullOrWhiteSpace(dateKey) ? string.Empty : $"daily:{dateKey}";
            return RecordSimpleEvent(eventId, AchievementProgressRule.DailyRewardsClaimed);
        }

        public AchievementProgressBatch RecordGardenInteraction()
        {
            return RecordSimpleEvent("garden:lantern", AchievementProgressRule.GardenInteractions);
        }

        public bool RollbackBattleEvent(string eventId)
        {
            if (string.IsNullOrWhiteSpace(eventId) || !rollbackByEvent.TryGetValue(eventId, out var rollback))
            {
                return false;
            }

            rollback.Restore(saveData);
            rollbackByEvent.Remove(eventId);
            persist?.Invoke();
            return true;
        }

        public AchievementClaimResult ClaimReward(string achievementId)
        {
            var config = catalog.FindById(achievementId);
            var entry = GetEntry(achievementId, false);
            if (config == null || entry == null || entry.claimed || entry.progress < config.ProgressTarget)
            {
                return AchievementClaimResult.Failed;
            }

            entry.claimed = true;
            saveData.fishCoins += config.Reward.FishCoins;
            saveData.playerExperience += config.Reward.PlayerExperience;
            rewardApplied?.Invoke();
            persist?.Invoke();
            return new AchievementClaimResult(
                true,
                config.AchievementId,
                config.Reward.FishCoins,
                config.Reward.PlayerExperience);
        }

        public bool IsCompleted(string achievementId)
        {
            var config = catalog.FindById(achievementId);
            var entry = GetEntry(achievementId, false);
            return config != null && entry != null && entry.progress >= config.ProgressTarget;
        }

        private AchievementProgressBatch RecordSimpleEvent(string eventId, AchievementProgressRule rule)
        {
            if (string.IsNullOrWhiteSpace(eventId))
            {
                return AchievementProgressBatch.Empty;
            }

            if (saveData.processedAchievementEventIds.Contains(eventId))
            {
                return new AchievementProgressBatch(eventId, Array.Empty<AchievementProgressUpdate>(), true);
            }

            saveData.processedAchievementEventIds.Add(eventId);
            TrimProcessedEvents();
            var updates = new List<AchievementProgressUpdate>();
            ApplyRule(rule, 1, true, updates);
            persist?.Invoke();
            return new AchievementProgressBatch(eventId, updates.ToArray(), false);
        }

        private bool SynchronizeReliableProgressInternal(ICollection<AchievementProgressUpdate> updates)
        {
            var changed = false;
            changed |= ApplyRule(
                AchievementProgressRule.CompletedMaps,
                saveData.completedLevelIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.Ordinal).Count(),
                false,
                updates);
            changed |= ApplyRule(AchievementProgressRule.ContractsCompleted, CountCompletedContracts(), false, updates);
            changed |= ApplyRule(AchievementProgressRule.InitialEnemiesDiscovered, CountInitialEnemiesDiscovered(), false, updates);
            changed |= ApplyRule(AchievementProgressRule.UniqueUltimatesUsed, saveData.achievementUltimateIds.Count, false, updates);
            changed |= ApplyRule(AchievementProgressRule.BossesDefeated, saveData.achievementBossIds.Count, false, updates);
            return changed;
        }

        private int CountCompletedContracts()
        {
            if (questCatalog == null)
            {
                return 0;
            }

            var completed = 0;
            foreach (var entry in saveData.quests)
            {
                var quest = entry == null ? null : questCatalog.FindById(entry.questId);
                if (quest != null && entry.progress >= quest.Objective.TargetAmount)
                {
                    completed++;
                }
            }

            return completed;
        }

        private int CountInitialEnemiesDiscovered()
        {
            var discovered = 0;
            foreach (var enemyId in catalog.InitialEnemyIds)
            {
                if (saveData.discoveredCodexEntryIds.Contains($"enemy:{enemyId}"))
                {
                    discovered++;
                }
            }

            return discovered;
        }

        private bool ApplyRule(
            AchievementProgressRule rule,
            int value,
            bool additive,
            ICollection<AchievementProgressUpdate> updates)
        {
            if (value <= 0)
            {
                return false;
            }

            var changed = false;
            foreach (var config in catalog.Achievements)
            {
                if (config == null || config.ProgressRule != rule)
                {
                    continue;
                }

                var entry = GetEntry(config.AchievementId, true);
                var previous = Math.Max(0, entry.progress);
                var wasCompleted = previous >= config.ProgressTarget;
                entry.progress = additive ? previous + value : Math.Max(previous, value);
                var completedNow = !wasCompleted && entry.progress >= config.ProgressTarget;
                if (completedNow && string.IsNullOrWhiteSpace(entry.completedDateKey))
                {
                    entry.completedDateKey = dateKeyProvider() ?? string.Empty;
                }

                if (entry.progress != previous || completedNow)
                {
                    updates?.Add(new AchievementProgressUpdate(config, previous, entry.progress, completedNow));
                    changed = true;
                }
            }

            return changed;
        }

        private bool EnsureCatalogEntries()
        {
            var changed = false;
            foreach (var config in catalog.Achievements)
            {
                if (config != null && GetEntry(config.AchievementId, false) == null)
                {
                    saveData.achievements.Add(new AchievementProgressSaveEntry(config.AchievementId));
                    changed = true;
                }
            }

            return changed;
        }

        private AchievementProgressSaveEntry GetEntry(string achievementId, bool create)
        {
            var entry = saveData.achievements.FirstOrDefault(item => item != null && item.achievementId == achievementId);
            if (entry != null || !create || string.IsNullOrWhiteSpace(achievementId))
            {
                return entry;
            }

            entry = new AchievementProgressSaveEntry(achievementId);
            saveData.achievements.Add(entry);
            return entry;
        }

        private void EnsureCollections()
        {
            GameSaveMigrationService.TryMigrate(saveData, saveData.selectedLevelId, out _, out _);
            saveData.achievements = saveData.achievements
                .Where(item => item != null && !string.IsNullOrWhiteSpace(item.achievementId))
                .GroupBy(item => item.achievementId, StringComparer.Ordinal)
                .Select(group => group.OrderByDescending(item => item.claimed).ThenByDescending(item => item.progress).First())
                .ToList();
            foreach (var entry in saveData.achievements)
            {
                entry.progress = Math.Max(0, entry.progress);
                entry.completedDateKey ??= string.Empty;
            }

            saveData.achievementUltimateIds = NormalizeIds(saveData.achievementUltimateIds);
            saveData.achievementBossIds = NormalizeIds(saveData.achievementBossIds);
            saveData.processedAchievementEventIds = NormalizeIds(saveData.processedAchievementEventIds);
        }

        private void TrimProcessedEvents()
        {
            while (saveData.processedAchievementEventIds.Count > ProcessedEventLimit)
            {
                var removed = saveData.processedAchievementEventIds[0];
                saveData.processedAchievementEventIds.RemoveAt(0);
                rollbackByEvent.Remove(removed);
            }
        }

        private static List<string> NormalizeIds(IEnumerable<string> source)
        {
            return source?.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.Ordinal).ToList()
                ?? new List<string>();
        }

        private static void AddUnique(ICollection<string> destination, string id)
        {
            if (!string.IsNullOrWhiteSpace(id) && !destination.Contains(id))
            {
                destination.Add(id);
            }
        }

        private static string TodayDateKey()
        {
            return DateTime.UtcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        private sealed class AchievementRollbackState
        {
            private AchievementProgressSaveEntry[] achievements;
            private string[] ultimateIds;
            private string[] bossIds;
            private string[] processedEvents;

            public static AchievementRollbackState Capture(GameSaveData data)
            {
                return new AchievementRollbackState
                {
                    achievements = data.achievements.Select(CopyEntry).ToArray(),
                    ultimateIds = data.achievementUltimateIds.ToArray(),
                    bossIds = data.achievementBossIds.ToArray(),
                    processedEvents = data.processedAchievementEventIds.ToArray()
                };
            }

            public void Restore(GameSaveData data)
            {
                data.achievements = achievements.Select(CopyEntry).ToList();
                data.achievementUltimateIds = ultimateIds.ToList();
                data.achievementBossIds = bossIds.ToList();
                data.processedAchievementEventIds = processedEvents.ToList();
            }

            private static AchievementProgressSaveEntry CopyEntry(AchievementProgressSaveEntry source)
            {
                return new AchievementProgressSaveEntry(source.achievementId)
                {
                    progress = source.progress,
                    completedDateKey = source.completedDateKey,
                    claimed = source.claimed
                };
            }
        }
    }

    public static class AchievementService
    {
        public const string GardenSecretAchievementId = "garden_lantern_secret";

        private static AchievementCatalogConfig catalog;
        private static AchievementStateMachine stateMachine;

        public static bool IsInitialized => stateMachine != null;
        public static AchievementCatalogConfig Catalog => catalog;
        public static int ClaimableCount => CreateSnapshot().ClaimableCount;

        public static bool Initialize(AchievementCatalogConfig achievementCatalog, QuestCatalogConfig questCatalog)
        {
            var error = "Achievement catalog is missing.";
            if (achievementCatalog == null || !achievementCatalog.IsValid(out error))
            {
                catalog = null;
                stateMachine = null;
                return false;
            }

            catalog = achievementCatalog;
            stateMachine = new AchievementStateMachine(
                catalog,
                questCatalog,
                ProgressionService.EnsureSave(),
                ProgressionService.Save,
                MetaProgressionService.SynchronizeUnlocks);
            return true;
        }

        public static AchievementSnapshot CreateSnapshot() => stateMachine?.CreateSnapshot() ?? AchievementSnapshot.Empty;

        public static AchievementProgressBatch RecordBattle(AchievementBattleReport report)
        {
            return TrackCompletions(stateMachine?.RecordBattle(report) ?? AchievementProgressBatch.Empty);
        }

        public static AchievementProgressBatch RecordDailyRewardClaim(string dateKey)
        {
            return TrackCompletions(stateMachine?.RecordDailyRewardClaim(dateKey) ?? AchievementProgressBatch.Empty);
        }

        public static AchievementProgressBatch RecordGardenInteraction()
        {
            return TrackCompletions(stateMachine?.RecordGardenInteraction() ?? AchievementProgressBatch.Empty);
        }

        public static bool RollbackBattleEvent(string eventId) => stateMachine?.RollbackBattleEvent(eventId) == true;

        public static AchievementClaimResult ClaimReward(string achievementId)
        {
            var result = stateMachine?.ClaimReward(achievementId) ?? AchievementClaimResult.Failed;
            if (result.Claimed)
            {
                var config = catalog?.FindById(result.AchievementId);
                AnalyticsService.TrackAchievementClaim(config, result);
            }

            return result;
        }

        public static bool IsCompleted(string achievementId) => stateMachine?.IsCompleted(achievementId) == true;

        private static AchievementProgressBatch TrackCompletions(AchievementProgressBatch batch)
        {
            foreach (var update in batch.Updates)
            {
                if (update?.CompletedNow == true)
                {
                    AnalyticsService.TrackAchievementCompleted(update.Config);
                }
            }

            return batch;
        }
    }
}
