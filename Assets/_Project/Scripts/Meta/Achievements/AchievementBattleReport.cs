using System;
using System.Collections.Generic;

namespace CatGuard.Meta.Achievements
{
    public sealed class AchievementBattleReport
    {
        private readonly HashSet<string> ultimateIds = new(StringComparer.Ordinal);
        private readonly HashSet<string> defeatedBossIds = new(StringComparer.Ordinal);

        public AchievementBattleReport(
            string eventId,
            bool won,
            int livesLost,
            int defeatedEnemies,
            int routeCount,
            int highestTowerTier,
            IEnumerable<string> usedUltimateIds,
            IEnumerable<string> bossIds)
        {
            EventId = eventId ?? string.Empty;
            Won = won;
            LivesLost = Math.Max(0, livesLost);
            DefeatedEnemies = Math.Max(0, defeatedEnemies);
            RouteCount = Math.Max(0, routeCount);
            HighestTowerTier = Math.Max(0, highestTowerTier);
            CopyIds(usedUltimateIds, ultimateIds);
            CopyIds(bossIds, defeatedBossIds);
        }

        public string EventId { get; }
        public bool Won { get; }
        public int LivesLost { get; }
        public int DefeatedEnemies { get; }
        public int RouteCount { get; }
        public int HighestTowerTier { get; }
        public IReadOnlyCollection<string> UltimateIds => ultimateIds;
        public IReadOnlyCollection<string> DefeatedBossIds => defeatedBossIds;

        private static void CopyIds(IEnumerable<string> source, ISet<string> destination)
        {
            if (source == null)
            {
                return;
            }

            foreach (var id in source)
            {
                if (!string.IsNullOrWhiteSpace(id))
                {
                    destination.Add(id);
                }
            }
        }
    }

    public sealed class AchievementProgressUpdate
    {
        public AchievementProgressUpdate(AchievementConfig config, int previousProgress, int currentProgress, bool completedNow)
        {
            Config = config;
            PreviousProgress = Math.Max(0, previousProgress);
            CurrentProgress = Math.Max(0, currentProgress);
            CompletedNow = completedNow;
        }

        public AchievementConfig Config { get; }
        public int PreviousProgress { get; }
        public int CurrentProgress { get; }
        public bool CompletedNow { get; }
    }

    public sealed class AchievementProgressBatch
    {
        public static readonly AchievementProgressBatch Empty = new(string.Empty, Array.Empty<AchievementProgressUpdate>(), false);

        public AchievementProgressBatch(string eventId, AchievementProgressUpdate[] updates, bool duplicate)
        {
            EventId = eventId ?? string.Empty;
            Updates = updates ?? Array.Empty<AchievementProgressUpdate>();
            Duplicate = duplicate;
        }

        public string EventId { get; }
        public AchievementProgressUpdate[] Updates { get; }
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
