using System;
using System.Collections.Generic;
using UnityEngine;

namespace CatGuard.Meta.Achievements
{
    public enum AchievementCategory
    {
        Campaign,
        TowerMastery,
        RouteControl,
        Ultimates,
        PerfectDefense,
        Collection,
        SecretsAndHumor,
        LongTermTotals
    }

    public enum AchievementPresentationTier
    {
        Bronze,
        Silver,
        Gold
    }

    public enum AchievementProgressRule
    {
        CompletedMaps,
        PerfectVictories,
        TierThreeTowerBattles,
        UniqueUltimatesUsed,
        EnemiesDefeated,
        MultiRouteVictories,
        DailyRewardsClaimed,
        ContractsCompleted,
        InitialEnemiesDiscovered,
        BossesDefeated,
        GardenInteractions
    }

    [Serializable]
    public sealed class AchievementRewardBundle
    {
        [Min(0)] [SerializeField] private int fishCoins = 20;
        [Min(0)] [SerializeField] private int playerExperience = 10;

        public int FishCoins => Math.Max(0, fishCoins);
        public int PlayerExperience => Math.Max(0, playerExperience);

        public AchievementRewardBundle()
        {
        }

        public AchievementRewardBundle(int rewardFishCoins, int rewardPlayerExperience)
        {
            fishCoins = rewardFishCoins;
            playerExperience = rewardPlayerExperience;
        }

        public bool IsValid()
        {
            return FishCoins > 0 || PlayerExperience > 0;
        }
    }

    [Serializable]
    public sealed class AchievementConfig
    {
        [SerializeField] private string achievementId = "achievement";
        [SerializeField] private string titleLocalizationKey = "achievement.title.achievement";
        [SerializeField] private string descriptionLocalizationKey = "achievement.description.achievement";
        [SerializeField] private AchievementCategory category;
        [SerializeField] private AchievementPresentationTier presentationTier;
        [SerializeField] private AchievementProgressRule progressRule;
        [Min(1)] [SerializeField] private int progressTarget = 1;
        [SerializeField] private bool hidden;
        [SerializeField] private AchievementRewardBundle reward = new();

        public string AchievementId => achievementId ?? string.Empty;
        public string TitleLocalizationKey => titleLocalizationKey ?? string.Empty;
        public string DescriptionLocalizationKey => descriptionLocalizationKey ?? string.Empty;
        public AchievementCategory Category => category;
        public AchievementPresentationTier PresentationTier => presentationTier;
        public AchievementProgressRule ProgressRule => progressRule;
        public int ProgressTarget => Math.Max(1, progressTarget);
        public bool Hidden => hidden;
        public AchievementRewardBundle Reward => reward ?? new AchievementRewardBundle();

        public AchievementConfig()
        {
        }

        public AchievementConfig(
            string id,
            string titleKey,
            string descriptionKey,
            AchievementCategory configuredCategory,
            AchievementPresentationTier tier,
            AchievementProgressRule rule,
            int target,
            bool isHidden,
            AchievementRewardBundle rewardBundle)
        {
            achievementId = id ?? string.Empty;
            titleLocalizationKey = titleKey ?? string.Empty;
            descriptionLocalizationKey = descriptionKey ?? string.Empty;
            category = configuredCategory;
            presentationTier = tier;
            progressRule = rule;
            progressTarget = target;
            hidden = isHidden;
            reward = rewardBundle ?? new AchievementRewardBundle();
        }

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(AchievementId)
                && !string.IsNullOrWhiteSpace(TitleLocalizationKey)
                && !string.IsNullOrWhiteSpace(DescriptionLocalizationKey)
                && ProgressTarget > 0
                && Reward.IsValid();
        }
    }

    [CreateAssetMenu(fileName = "AchievementCatalog", menuName = "Cat Guard/Achievement Catalog")]
    public sealed class AchievementCatalogConfig : ScriptableObject
    {
        public const string ResourcesPath = "Achievements/AchievementCatalog";

        [SerializeField] private AchievementConfig[] achievements = Array.Empty<AchievementConfig>();
        [SerializeField] private string[] initialEnemyIds = Array.Empty<string>();

        public AchievementConfig[] Achievements => achievements ?? Array.Empty<AchievementConfig>();
        public string[] InitialEnemyIds => initialEnemyIds ?? Array.Empty<string>();

        public static AchievementCatalogConfig LoadDefault()
        {
            return Resources.Load<AchievementCatalogConfig>(ResourcesPath);
        }

        public AchievementConfig FindById(string achievementId)
        {
            return string.IsNullOrWhiteSpace(achievementId)
                ? null
                : Array.Find(Achievements, item => item != null && item.AchievementId == achievementId);
        }

        public bool IsValid(out string error)
        {
            if (Achievements.Length < 12)
            {
                error = "Achievement catalog must contain at least 12 entries.";
                return false;
            }

            var ids = new HashSet<string>(StringComparer.Ordinal);
            var rules = new HashSet<AchievementProgressRule>();
            var hasHidden = false;
            foreach (var achievement in Achievements)
            {
                if (achievement?.IsValid() != true || !ids.Add(achievement.AchievementId))
                {
                    error = "Achievement catalog contains an invalid or duplicate entry.";
                    return false;
                }

                rules.Add(achievement.ProgressRule);
                hasHidden |= achievement.Hidden;
            }

            if (rules.Count != Enum.GetValues(typeof(AchievementProgressRule)).Length || !hasHidden)
            {
                error = "Achievement catalog does not cover every E11 progress rule and hidden presentation.";
                return false;
            }

            var enemyIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var enemyId in InitialEnemyIds)
            {
                if (string.IsNullOrWhiteSpace(enemyId) || !enemyIds.Add(enemyId))
                {
                    error = "Initial enemy ids are missing or duplicated.";
                    return false;
                }
            }

            if (enemyIds.Count != 5)
            {
                error = "E11 requires exactly five initial enemies for the collection achievement.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        public void Configure(AchievementConfig[] configuredAchievements, string[] configuredInitialEnemyIds)
        {
            achievements = configuredAchievements ?? Array.Empty<AchievementConfig>();
            initialEnemyIds = configuredInitialEnemyIds ?? Array.Empty<string>();
        }
    }
}
