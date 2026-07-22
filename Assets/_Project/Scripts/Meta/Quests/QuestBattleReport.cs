using System;
using System.Collections.Generic;

namespace CatGuard.Meta.Quests
{
    public sealed class QuestBattleReport
    {
        private readonly Dictionary<string, int> highestTowerTiers;

        public QuestBattleReport(
            string eventId,
            string levelId,
            bool won,
            int startingLives,
            int remainingLives,
            int livesLost,
            int defeatedEnemies,
            int escapedEnemies,
            int towersPlaced,
            int towersSold,
            int controlledEnemies,
            int ultimatesUsed,
            IReadOnlyDictionary<string, int> towerTiers)
        {
            EventId = eventId ?? string.Empty;
            LevelId = levelId ?? string.Empty;
            Won = won;
            StartingLives = Math.Max(0, startingLives);
            RemainingLives = Math.Max(0, remainingLives);
            LivesLost = Math.Max(0, livesLost);
            DefeatedEnemies = Math.Max(0, defeatedEnemies);
            EscapedEnemies = Math.Max(0, escapedEnemies);
            TowersPlaced = Math.Max(0, towersPlaced);
            TowersSold = Math.Max(0, towersSold);
            ControlledEnemies = Math.Max(0, controlledEnemies);
            UltimatesUsed = Math.Max(0, ultimatesUsed);
            highestTowerTiers = new Dictionary<string, int>(StringComparer.Ordinal);
            if (towerTiers == null)
            {
                return;
            }

            foreach (var pair in towerTiers)
            {
                if (!string.IsNullOrWhiteSpace(pair.Key))
                {
                    highestTowerTiers[pair.Key] = Math.Max(0, pair.Value);
                }
            }
        }

        public string EventId { get; }
        public string LevelId { get; }
        public bool Won { get; }
        public int StartingLives { get; }
        public int RemainingLives { get; }
        public int LivesLost { get; }
        public int DefeatedEnemies { get; }
        public int EscapedEnemies { get; }
        public int TowersPlaced { get; }
        public int TowersSold { get; }
        public int ControlledEnemies { get; }
        public int UltimatesUsed { get; }

        public int GetHighestTier(string towerId)
        {
            if (!string.IsNullOrWhiteSpace(towerId)
                && highestTowerTiers.TryGetValue(towerId, out var tier))
            {
                return tier;
            }

            var highest = 0;
            foreach (var pair in highestTowerTiers)
            {
                highest = Math.Max(highest, pair.Value);
            }

            return highest;
        }
    }

    public sealed class QuestProgressUpdate
    {
        public QuestProgressUpdate(QuestConfig quest, int previousProgress, int currentProgress)
        {
            Quest = quest;
            PreviousProgress = Math.Max(0, previousProgress);
            CurrentProgress = Math.Max(0, currentProgress);
        }

        public QuestConfig Quest { get; }
        public int PreviousProgress { get; }
        public int CurrentProgress { get; }
        public bool CompletedNow => Quest != null
            && PreviousProgress < Quest.Objective.TargetAmount
            && CurrentProgress >= Quest.Objective.TargetAmount;
    }

    public sealed class QuestProgressBatch
    {
        public static readonly QuestProgressBatch Empty = new(string.Empty, Array.Empty<QuestProgressUpdate>(), false);

        public QuestProgressBatch(string eventId, QuestProgressUpdate[] updates, bool duplicate)
        {
            EventId = eventId ?? string.Empty;
            Updates = updates ?? Array.Empty<QuestProgressUpdate>();
            Duplicate = duplicate;
        }

        public string EventId { get; }
        public QuestProgressUpdate[] Updates { get; }
        public bool Duplicate { get; }
        public bool HasUpdates => Updates.Length > 0;
        public int CompletedCount
        {
            get
            {
                var count = 0;
                foreach (var update in Updates)
                {
                    if (update?.CompletedNow == true)
                    {
                        count++;
                    }
                }

                return count;
            }
        }
    }
}
