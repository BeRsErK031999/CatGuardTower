using System;
using System.Collections.Generic;
using CatGuard.Core.Save;
using CatGuard.Gameplay.Levels;
using CatGuard.Meta.GuardianGrowth;
using CatGuard.Meta.Progression;

namespace CatGuard.Meta.Quests
{
    public sealed class QuestViewState
    {
        public QuestViewState(QuestConfig quest, int progress, QuestStatus status)
        {
            Quest = quest;
            Progress = Math.Max(0, progress);
            Status = status;
        }

        public QuestConfig Quest { get; }
        public int Progress { get; }
        public QuestStatus Status { get; }
        public bool CanClaim => Status == QuestStatus.Completed;
    }

    public sealed class QuestBoardSnapshot
    {
        public static readonly QuestBoardSnapshot Empty = new(
            Array.Empty<QuestViewState>(),
            Array.Empty<QuestViewState>(),
            Array.Empty<QuestViewState>(),
            0);

        public QuestBoardSnapshot(
            QuestViewState[] active,
            QuestViewState[] completed,
            QuestViewState[] claimed,
            int activeLimit)
        {
            Active = active ?? Array.Empty<QuestViewState>();
            Completed = completed ?? Array.Empty<QuestViewState>();
            Claimed = claimed ?? Array.Empty<QuestViewState>();
            ActiveLimit = Math.Max(0, activeLimit);
        }

        public QuestViewState[] Active { get; }
        public QuestViewState[] Completed { get; }
        public QuestViewState[] Claimed { get; }
        public int ActiveLimit { get; }
        public int ClaimableCount => Completed.Length;
    }

    public sealed class QuestStateMachine
    {
        private const int ProcessedEventLimit = 64;

        private readonly QuestCatalogConfig catalog;
        private readonly LevelCatalogConfig levelCatalog;
        private readonly GameSaveData saveData;
        private readonly Action persist;
        private readonly Dictionary<string, QuestRollbackState[]> rollbackByEvent = new(StringComparer.Ordinal);

        public QuestStateMachine(
            QuestCatalogConfig questCatalog,
            LevelCatalogConfig levels,
            GameSaveData data,
            Action persistAction = null)
        {
            catalog = questCatalog;
            levelCatalog = levels;
            saveData = data ?? throw new ArgumentNullException(nameof(data));
            persist = persistAction;

            var changed = EnsureCollections();
            changed |= FillActiveContracts();
            if (changed)
            {
                persist?.Invoke();
            }
        }

        public GameSaveData SaveData => saveData;
        public int ActiveLimit => catalog?.ActiveContractLimit ?? 0;

        public QuestBoardSnapshot CreateSnapshot()
        {
            if (catalog == null)
            {
                return QuestBoardSnapshot.Empty;
            }

            var active = new List<QuestViewState>();
            var completed = new List<QuestViewState>();
            var claimed = new List<QuestViewState>();

            foreach (var quest in catalog.Quests)
            {
                if (quest == null)
                {
                    continue;
                }

                var entry = GetProgressEntry(quest.QuestId, false);
                if (entry?.rewardClaimed == true)
                {
                    claimed.Add(new QuestViewState(quest, quest.Objective.TargetAmount, QuestStatus.Claimed));
                    continue;
                }

                if (!saveData.activeQuestIds.Contains(quest.QuestId))
                {
                    continue;
                }

                var progress = Math.Min(entry?.progress ?? 0, quest.Objective.TargetAmount);
                if (progress >= quest.Objective.TargetAmount)
                {
                    completed.Add(new QuestViewState(quest, progress, QuestStatus.Completed));
                }
                else
                {
                    active.Add(new QuestViewState(quest, progress, QuestStatus.Active));
                }
            }

            return new QuestBoardSnapshot(
                active.ToArray(),
                completed.ToArray(),
                claimed.ToArray(),
                catalog.ActiveContractLimit);
        }

        public bool IsEligible(QuestConfig quest)
        {
            if (quest?.IsValid() != true)
            {
                return false;
            }

            var objective = quest.Objective;
            if (!string.IsNullOrWhiteSpace(objective.RequiredLevelId)
                && (levelCatalog?.FindById(objective.RequiredLevelId) == null
                    || saveData.unlockedLevelIds == null
                    || !saveData.unlockedLevelIds.Contains(objective.RequiredLevelId)))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(objective.RequiredTowerId))
            {
                return true;
            }

            if (levelCatalog?.Levels == null || saveData.unlockedLevelIds == null)
            {
                return false;
            }

            foreach (var level in levelCatalog.Levels)
            {
                if (level == null || !saveData.unlockedLevelIds.Contains(level.LevelId))
                {
                    continue;
                }

                foreach (var tower in level.AvailableTowers)
                {
                    if (tower != null
                        && string.Equals(tower.TowerId, objective.RequiredTowerId, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public QuestProgressBatch ProcessBattle(QuestBattleReport report)
        {
            if (catalog == null || report == null || string.IsNullOrWhiteSpace(report.EventId))
            {
                return QuestProgressBatch.Empty;
            }

            if (saveData.processedQuestEventIds.Contains(report.EventId))
            {
                return new QuestProgressBatch(report.EventId, Array.Empty<QuestProgressUpdate>(), true);
            }

            saveData.processedQuestEventIds.Add(report.EventId);
            TrimProcessedEvents();

            var updates = new List<QuestProgressUpdate>();
            var rollback = new List<QuestRollbackState>();
            foreach (var questId in saveData.activeQuestIds)
            {
                var quest = catalog.FindById(questId);
                if (quest == null)
                {
                    continue;
                }

                var entry = GetProgressEntry(quest.QuestId, true);
                if (entry.rewardClaimed || entry.progress >= quest.Objective.TargetAmount)
                {
                    continue;
                }

                var delta = EvaluateDelta(quest.Objective, report);
                if (delta <= 0)
                {
                    continue;
                }

                var previous = entry.progress;
                entry.progress = Math.Min(quest.Objective.TargetAmount, entry.progress + delta);
                rollback.Add(new QuestRollbackState(quest.QuestId, previous));
                updates.Add(new QuestProgressUpdate(quest, previous, entry.progress));
            }

            rollbackByEvent[report.EventId] = rollback.ToArray();
            persist?.Invoke();
            return new QuestProgressBatch(report.EventId, updates.ToArray(), false);
        }

        public bool RollbackBattleEvent(string eventId)
        {
            if (string.IsNullOrWhiteSpace(eventId)
                || !rollbackByEvent.TryGetValue(eventId, out var rollback))
            {
                return false;
            }

            foreach (var state in rollback)
            {
                var entry = GetProgressEntry(state.QuestId, false);
                if (entry != null && !entry.rewardClaimed)
                {
                    entry.progress = state.PreviousProgress;
                }
            }

            saveData.processedQuestEventIds.Remove(eventId);
            rollbackByEvent.Remove(eventId);
            persist?.Invoke();
            return true;
        }

        public bool ClaimReward(string questId)
        {
            var quest = catalog?.FindById(questId);
            var entry = GetProgressEntry(questId, false);
            if (quest == null
                || entry == null
                || entry.rewardClaimed
                || entry.progress < quest.Objective.TargetAmount
                || !saveData.activeQuestIds.Contains(questId))
            {
                return false;
            }

            entry.rewardClaimed = true;
            entry.progress = quest.Objective.TargetAmount;
            saveData.fishCoins += quest.Reward.FishCoins;
            saveData.activeQuestIds.Remove(questId);
            FillActiveContracts();
            persist?.Invoke();
            return true;
        }

        public int GetProgress(string questId)
        {
            var quest = catalog?.FindById(questId);
            var entry = GetProgressEntry(questId, false);
            return quest == null ? 0 : Math.Min(entry?.progress ?? 0, quest.Objective.TargetAmount);
        }

        public static int EvaluateDelta(QuestObjectiveConfig objective, QuestBattleReport report)
        {
            if (objective == null || report == null)
            {
                return 0;
            }

            return objective.ObjectiveType switch
            {
                QuestObjectiveType.WinBattles => report.Won ? 1 : 0,
                QuestObjectiveType.DefeatEnemies => report.DefeatedEnemies,
                QuestObjectiveType.PlaceTowers => report.TowersPlaced,
                QuestObjectiveType.ReachTowerTier => report.GetHighestTier(objective.RequiredTowerId) >= objective.Threshold ? 1 : 0,
                QuestObjectiveType.ControlEnemies => report.ControlledEnemies,
                QuestObjectiveType.WinWithoutLifeLoss => report.Won && report.LivesLost == 0 ? 1 : 0,
                QuestObjectiveType.WinWithMaxTowers => report.Won && report.TowersPlaced <= objective.Threshold ? 1 : 0,
                QuestObjectiveType.WinWithoutSelling => report.Won && report.TowersSold == 0 ? 1 : 0,
                QuestObjectiveType.UseUltimates => report.UltimatesUsed,
                QuestObjectiveType.WinOnLevel => report.Won
                    && string.Equals(report.LevelId, objective.RequiredLevelId, StringComparison.Ordinal) ? 1 : 0,
                _ => 0
            };
        }

        private bool EnsureCollections()
        {
            var changed = false;
            if (saveData.unlockedLevelIds == null)
            {
                saveData.unlockedLevelIds = new List<string>();
                changed = true;
            }

            if (saveData.quests == null)
            {
                saveData.quests = new List<QuestProgressSaveEntry>();
                changed = true;
            }

            if (saveData.activeQuestIds == null)
            {
                saveData.activeQuestIds = new List<string>();
                changed = true;
            }

            if (saveData.processedQuestEventIds == null)
            {
                saveData.processedQuestEventIds = new List<string>();
                changed = true;
            }

            return changed;
        }

        private bool FillActiveContracts()
        {
            if (catalog == null)
            {
                return false;
            }

            var changed = false;
            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (var index = saveData.activeQuestIds.Count - 1; index >= 0; index--)
            {
                var questId = saveData.activeQuestIds[index];
                var quest = catalog.FindById(questId);
                var entry = GetProgressEntry(questId, false);
                if (quest == null
                    || entry?.rewardClaimed == true
                    || !IsEligible(quest)
                    || !seen.Add(questId))
                {
                    saveData.activeQuestIds.RemoveAt(index);
                    changed = true;
                }
            }

            foreach (var quest in catalog.Quests)
            {
                if (saveData.activeQuestIds.Count >= catalog.ActiveContractLimit)
                {
                    break;
                }

                if (quest == null
                    || saveData.activeQuestIds.Contains(quest.QuestId)
                    || GetProgressEntry(quest.QuestId, false)?.rewardClaimed == true
                    || !IsEligible(quest))
                {
                    continue;
                }

                saveData.activeQuestIds.Add(quest.QuestId);
                GetProgressEntry(quest.QuestId, true);
                changed = true;
            }

            return changed;
        }

        private QuestProgressSaveEntry GetProgressEntry(string questId, bool createIfMissing)
        {
            if (string.IsNullOrWhiteSpace(questId))
            {
                return null;
            }

            foreach (var entry in saveData.quests)
            {
                if (entry != null && string.Equals(entry.questId, questId, StringComparison.Ordinal))
                {
                    return entry;
                }
            }

            if (!createIfMissing)
            {
                return null;
            }

            var result = new QuestProgressSaveEntry(questId);
            saveData.quests.Add(result);
            return result;
        }

        private void TrimProcessedEvents()
        {
            while (saveData.processedQuestEventIds.Count > ProcessedEventLimit)
            {
                var removed = saveData.processedQuestEventIds[0];
                saveData.processedQuestEventIds.RemoveAt(0);
                rollbackByEvent.Remove(removed);
            }
        }

        private readonly struct QuestRollbackState
        {
            public QuestRollbackState(string questId, int previousProgress)
            {
                QuestId = questId;
                PreviousProgress = previousProgress;
            }

            public string QuestId { get; }
            public int PreviousProgress { get; }
        }
    }

    public static class QuestService
    {
        private static QuestCatalogConfig catalog;
        private static LevelCatalogConfig levelCatalog;
        private static QuestStateMachine stateMachine;

        public static bool IsInitialized => stateMachine != null;
        public static QuestCatalogConfig Catalog => catalog;
        public static int ClaimableCount => CreateSnapshot().ClaimableCount;

        public static bool Initialize(QuestCatalogConfig questCatalog, LevelCatalogConfig levels)
        {
            var error = "Quest catalog is missing.";
            if (questCatalog == null || !questCatalog.IsValid(out error) || levels == null)
            {
                catalog = null;
                levelCatalog = null;
                stateMachine = null;
                return false;
            }

            catalog = questCatalog;
            levelCatalog = levels;
            stateMachine = new QuestStateMachine(
                catalog,
                levelCatalog,
                ProgressionService.EnsureSave(),
                ProgressionService.Save);
            return true;
        }

        public static QuestBoardSnapshot CreateSnapshot()
        {
            return stateMachine?.CreateSnapshot() ?? QuestBoardSnapshot.Empty;
        }

        public static QuestProgressBatch ProcessBattle(QuestBattleReport report)
        {
            return stateMachine?.ProcessBattle(report) ?? QuestProgressBatch.Empty;
        }

        public static bool RollbackBattleEvent(string eventId)
        {
            return stateMachine?.RollbackBattleEvent(eventId) == true;
        }

        public static bool ClaimReward(string questId)
        {
            var quest = catalog?.FindById(questId);
            if (quest == null || stateMachine?.ClaimReward(questId) != true)
            {
                return false;
            }

            MetaProgressionService.GrantQuestExperience(quest.Reward.PlayerExperience);
            return true;
        }

        public static bool IsEligible(QuestConfig quest)
        {
            return stateMachine?.IsEligible(quest) == true;
        }
    }
}
