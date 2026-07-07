using System;

namespace CatGuard.Meta.DailyRewards
{
    [Serializable]
    public sealed class DailyMissionConfig
    {
        public string missionId = "mission";
        public string displayName = "Mission";
        public DailyMissionType missionType;
        public int targetAmount = 1;
        public int rewardFishCoins = 10;

        public string MissionId => string.IsNullOrWhiteSpace(missionId) ? displayName : missionId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? MissionId : displayName;
        public DailyMissionType MissionType => missionType;
        public int TargetAmount => Math.Max(1, targetAmount);
        public int RewardFishCoins => Math.Max(0, rewardFishCoins);

        public DailyMissionConfig()
        {
        }

        public DailyMissionConfig(
            string id,
            string title,
            DailyMissionType type,
            int target,
            int reward)
        {
            missionId = id;
            displayName = title;
            missionType = type;
            targetAmount = target;
            rewardFishCoins = reward;
        }

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(MissionId)
                && TargetAmount > 0;
        }
    }
}
