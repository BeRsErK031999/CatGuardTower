using System;

namespace CatGuard.Meta.Quests
{
    [Serializable]
    public sealed class QuestObjectiveConfig
    {
        public QuestObjectiveType objectiveType;
        public int targetAmount = 1;
        public int threshold;
        public string requiredLevelId = string.Empty;
        public string requiredTowerId = string.Empty;

        public QuestObjectiveType ObjectiveType => objectiveType;
        public int TargetAmount => Math.Max(1, targetAmount);
        public int Threshold => Math.Max(0, threshold);
        public string RequiredLevelId => requiredLevelId ?? string.Empty;
        public string RequiredTowerId => requiredTowerId ?? string.Empty;

        public QuestObjectiveConfig()
        {
        }

        public QuestObjectiveConfig(
            QuestObjectiveType type,
            int target,
            int objectiveThreshold = 0,
            string levelId = "",
            string towerId = "")
        {
            objectiveType = type;
            targetAmount = target;
            threshold = objectiveThreshold;
            requiredLevelId = levelId ?? string.Empty;
            requiredTowerId = towerId ?? string.Empty;
        }

        public bool IsValid()
        {
            if (TargetAmount <= 0)
            {
                return false;
            }

            return ObjectiveType switch
            {
                QuestObjectiveType.ReachTowerTier => Threshold > 0,
                QuestObjectiveType.WinWithMaxTowers => Threshold > 0,
                QuestObjectiveType.WinOnLevel => !string.IsNullOrWhiteSpace(RequiredLevelId),
                _ => true
            };
        }
    }

    [Serializable]
    public sealed class QuestRewardConfig
    {
        public int fishCoins = 10;

        public int FishCoins => Math.Max(0, fishCoins);

        public QuestRewardConfig()
        {
        }

        public QuestRewardConfig(int rewardFishCoins)
        {
            fishCoins = rewardFishCoins;
        }

        public bool IsValid()
        {
            return FishCoins > 0;
        }
    }

    [Serializable]
    public sealed class QuestConfig
    {
        public string questId = "quest";
        public QuestCategory category = QuestCategory.Contract;
        public string nameLocalizationKey = "quest.name.quest";
        public string descriptionLocalizationKey = "quest.description.quest";
        public QuestObjectiveConfig objective = new();
        public QuestRewardConfig reward = new();

        public string QuestId => questId ?? string.Empty;
        public QuestCategory Category => category;
        public string NameLocalizationKey => nameLocalizationKey ?? string.Empty;
        public string DescriptionLocalizationKey => descriptionLocalizationKey ?? string.Empty;
        public QuestObjectiveConfig Objective => objective;
        public QuestRewardConfig Reward => reward;

        public QuestConfig()
        {
        }

        public QuestConfig(
            string id,
            QuestCategory questCategory,
            string nameKey,
            string descriptionKey,
            QuestObjectiveConfig questObjective,
            QuestRewardConfig questReward)
        {
            questId = id ?? string.Empty;
            category = questCategory;
            nameLocalizationKey = nameKey ?? string.Empty;
            descriptionLocalizationKey = descriptionKey ?? string.Empty;
            objective = questObjective;
            reward = questReward;
        }

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(QuestId)
                && !string.IsNullOrWhiteSpace(NameLocalizationKey)
                && !string.IsNullOrWhiteSpace(DescriptionLocalizationKey)
                && Objective?.IsValid() == true
                && Reward?.IsValid() == true;
        }
    }
}
