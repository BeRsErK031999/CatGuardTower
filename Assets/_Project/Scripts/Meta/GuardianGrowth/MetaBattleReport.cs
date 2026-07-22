using System;
using System.Collections.Generic;

namespace CatGuard.Meta.GuardianGrowth
{
    public sealed class MetaBattleReport
    {
        private readonly Dictionary<string, int> towerPlacements = new(StringComparer.Ordinal);
        private readonly Dictionary<string, int> highestTowerTiers = new(StringComparer.Ordinal);
        private readonly HashSet<string> encounteredEnemyIds = new(StringComparer.Ordinal);

        public MetaBattleReport(
            string eventId,
            string levelId,
            bool won,
            IReadOnlyDictionary<string, int> placements,
            IReadOnlyDictionary<string, int> towerTiers,
            IEnumerable<string> enemyIds)
        {
            EventId = eventId ?? string.Empty;
            LevelId = levelId ?? string.Empty;
            Won = won;
            CopyPositiveValues(placements, towerPlacements);
            CopyPositiveValues(towerTiers, highestTowerTiers);
            if (enemyIds == null)
            {
                return;
            }

            foreach (var enemyId in enemyIds)
            {
                if (!string.IsNullOrWhiteSpace(enemyId))
                {
                    encounteredEnemyIds.Add(enemyId);
                }
            }
        }

        public string EventId { get; }
        public string LevelId { get; }
        public bool Won { get; }
        public IReadOnlyDictionary<string, int> TowerPlacements => towerPlacements;
        public IReadOnlyDictionary<string, int> HighestTowerTiers => highestTowerTiers;
        public IReadOnlyCollection<string> EncounteredEnemyIds => encounteredEnemyIds;

        public int GetHighestTier(string towerId)
        {
            return !string.IsNullOrWhiteSpace(towerId) && highestTowerTiers.TryGetValue(towerId, out var tier)
                ? tier
                : 0;
        }

        private static void CopyPositiveValues(
            IReadOnlyDictionary<string, int> source,
            IDictionary<string, int> destination)
        {
            if (source == null)
            {
                return;
            }

            foreach (var pair in source)
            {
                if (!string.IsNullOrWhiteSpace(pair.Key) && pair.Value > 0)
                {
                    destination[pair.Key] = pair.Value;
                }
            }
        }
    }

    public sealed class TowerMasteryProgressUpdate
    {
        public TowerMasteryProgressUpdate(string towerId, int previousExperience, int currentExperience, int previousLevel, int currentLevel)
        {
            TowerId = towerId ?? string.Empty;
            PreviousExperience = Math.Max(0, previousExperience);
            CurrentExperience = Math.Max(0, currentExperience);
            PreviousLevel = Math.Max(1, previousLevel);
            CurrentLevel = Math.Max(1, currentLevel);
        }

        public string TowerId { get; }
        public int PreviousExperience { get; }
        public int CurrentExperience { get; }
        public int PreviousLevel { get; }
        public int CurrentLevel { get; }
        public bool LeveledUp => CurrentLevel > PreviousLevel;
    }

    public sealed class MetaProgressionBattleResult
    {
        public static readonly MetaProgressionBattleResult Empty = new(
            string.Empty,
            0,
            1,
            1,
            Array.Empty<TowerMasteryProgressUpdate>(),
            Array.Empty<string>(),
            Array.Empty<string>(),
            false);

        public MetaProgressionBattleResult(
            string eventId,
            int experienceGained,
            int previousRank,
            int currentRank,
            TowerMasteryProgressUpdate[] masteryUpdates,
            string[] discoveries,
            string[] unlocks,
            bool duplicate)
        {
            EventId = eventId ?? string.Empty;
            ExperienceGained = Math.Max(0, experienceGained);
            PreviousRank = Math.Max(1, previousRank);
            CurrentRank = Math.Max(1, currentRank);
            MasteryUpdates = masteryUpdates ?? Array.Empty<TowerMasteryProgressUpdate>();
            Discoveries = discoveries ?? Array.Empty<string>();
            Unlocks = unlocks ?? Array.Empty<string>();
            Duplicate = duplicate;
        }

        public string EventId { get; }
        public int ExperienceGained { get; }
        public int PreviousRank { get; }
        public int CurrentRank { get; }
        public TowerMasteryProgressUpdate[] MasteryUpdates { get; }
        public string[] Discoveries { get; }
        public string[] Unlocks { get; }
        public bool Duplicate { get; }
        public bool HasUpdates => ExperienceGained > 0 || MasteryUpdates.Length > 0 || Discoveries.Length > 0 || Unlocks.Length > 0;
    }
}
