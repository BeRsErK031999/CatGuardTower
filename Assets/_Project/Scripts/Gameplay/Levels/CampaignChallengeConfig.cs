using System;
using UnityEngine;

namespace CatGuard.Gameplay.Levels
{
    [Serializable]
    public sealed class CampaignChallengeConfig
    {
        [SerializeField] private string challengeId = string.Empty;
        [SerializeField] private string nameLocalizationKey = string.Empty;
        [SerializeField] private string descriptionLocalizationKey = string.Empty;
        [SerializeField] private int maximumLivesDelta;
        [Min(0.1f)]
        [SerializeField] private float startingBattleFishMultiplier = 1f;
        [Min(0.1f)]
        [SerializeField] private float enemyHealthMultiplier = 1f;
        [Min(0.1f)]
        [SerializeField] private float enemySpeedMultiplier = 1f;
        [Min(0)]
        [SerializeField] private int firstClearRewardCoins;
        [Min(0)]
        [SerializeField] private int replayRewardCoins;

        public string ChallengeId => challengeId ?? string.Empty;
        public string NameLocalizationKey => nameLocalizationKey ?? string.Empty;
        public string DescriptionLocalizationKey => descriptionLocalizationKey ?? string.Empty;
        public int MaximumLivesDelta => maximumLivesDelta;
        public float StartingBattleFishMultiplier => Mathf.Max(0.1f, startingBattleFishMultiplier);
        public float EnemyHealthMultiplier => Mathf.Max(0.1f, enemyHealthMultiplier);
        public float EnemySpeedMultiplier => Mathf.Max(0.1f, enemySpeedMultiplier);
        public int FirstClearRewardCoins => Mathf.Max(0, firstClearRewardCoins);
        public int ReplayRewardCoins => Mathf.Max(0, replayRewardCoins);

        public bool IsValid()
        {
            var modifiesBattle = maximumLivesDelta != 0
                || !Mathf.Approximately(StartingBattleFishMultiplier, 1f)
                || !Mathf.Approximately(EnemyHealthMultiplier, 1f)
                || !Mathf.Approximately(EnemySpeedMultiplier, 1f);
            return !string.IsNullOrWhiteSpace(ChallengeId)
                && !string.IsNullOrWhiteSpace(NameLocalizationKey)
                && !string.IsNullOrWhiteSpace(DescriptionLocalizationKey)
                && FirstClearRewardCoins > 0
                && ReplayRewardCoins > 0
                && FirstClearRewardCoins > ReplayRewardCoins
                && modifiesBattle;
        }

        public void Configure(
            string id,
            string nameKey,
            string descriptionKey,
            int livesDelta,
            float startingFishMultiplier,
            float healthMultiplier,
            float speedMultiplier,
            int firstReward,
            int replayReward)
        {
            challengeId = id ?? string.Empty;
            nameLocalizationKey = nameKey ?? string.Empty;
            descriptionLocalizationKey = descriptionKey ?? string.Empty;
            maximumLivesDelta = livesDelta;
            startingBattleFishMultiplier = Mathf.Max(0.1f, startingFishMultiplier);
            enemyHealthMultiplier = Mathf.Max(0.1f, healthMultiplier);
            enemySpeedMultiplier = Mathf.Max(0.1f, speedMultiplier);
            firstClearRewardCoins = Mathf.Max(0, firstReward);
            replayRewardCoins = Mathf.Max(0, replayReward);
        }
    }

    [Serializable]
    public sealed class CampaignMapMetadata
    {
        [SerializeField] private string biomeNameLocalizationKey = string.Empty;
        [SerializeField] private string tacticalSummaryLocalizationKey = string.Empty;
        [SerializeField] private CampaignChallengeConfig challenge = new();
        [SerializeField] private string questHookId = string.Empty;
        [SerializeField] private string achievementHookId = string.Empty;
        [SerializeField] private int recommendedMaxActiveEnemies = 40;
        [SerializeField] private string externalAssetManifest = string.Empty;

        public string BiomeNameLocalizationKey => biomeNameLocalizationKey ?? string.Empty;
        public string TacticalSummaryLocalizationKey => tacticalSummaryLocalizationKey ?? string.Empty;
        public CampaignChallengeConfig Challenge => challenge;
        public string QuestHookId => questHookId ?? string.Empty;
        public string AchievementHookId => achievementHookId ?? string.Empty;
        public int RecommendedMaxActiveEnemies => Mathf.Max(1, recommendedMaxActiveEnemies);
        public string ExternalAssetManifest => externalAssetManifest ?? string.Empty;

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(BiomeNameLocalizationKey)
                && !string.IsNullOrWhiteSpace(TacticalSummaryLocalizationKey)
                && challenge != null
                && challenge.IsValid()
                && !string.IsNullOrWhiteSpace(QuestHookId)
                && RecommendedMaxActiveEnemies >= 20
                && !string.IsNullOrWhiteSpace(ExternalAssetManifest);
        }

        public void Configure(
            string biomeNameKey,
            string tacticalSummaryKey,
            CampaignChallengeConfig challengeConfig,
            string questId,
            string achievementId,
            int maxActiveEnemies,
            string assetManifest)
        {
            biomeNameLocalizationKey = biomeNameKey ?? string.Empty;
            tacticalSummaryLocalizationKey = tacticalSummaryKey ?? string.Empty;
            challenge = challengeConfig ?? new CampaignChallengeConfig();
            questHookId = questId ?? string.Empty;
            achievementHookId = achievementId ?? string.Empty;
            recommendedMaxActiveEnemies = Mathf.Max(1, maxActiveEnemies);
            externalAssetManifest = assetManifest ?? string.Empty;
        }
    }
}
