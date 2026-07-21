using System;
using System.Collections.Generic;

namespace CatGuard.Core.Save
{
    [Serializable]
    public sealed class GameSaveData
    {
        public int fishCoins;
        public string selectedLevelId;
        public List<string> unlockedLevelIds = new();
        public List<string> completedLevelIds = new();
        public List<UpgradeSaveEntry> upgrades = new();
        public string lastDailyRewardClaimDateKey;
        public int dailyRewardStreakIndex;
        public string dailyMissionDateKey;
        public List<DailyMissionSaveEntry> dailyMissions = new();
        public bool audioMuted;
        public string languageCode = "ru";
        public string lastFreeCoinsRewardDateKey;
        public int cameraShakeIntensity = 2;
        public bool reducedFlash;

        public static GameSaveData CreateDefault(string firstLevelId)
        {
            var data = new GameSaveData
            {
                fishCoins = 0,
                selectedLevelId = firstLevelId
            };

            if (!string.IsNullOrWhiteSpace(firstLevelId))
            {
                data.unlockedLevelIds.Add(firstLevelId);
            }

            return data;
        }
    }

    [Serializable]
    public sealed class UpgradeSaveEntry
    {
        public string upgradeId;
        public int level;

        public UpgradeSaveEntry()
        {
        }

        public UpgradeSaveEntry(string id, int upgradeLevel)
        {
            upgradeId = id;
            level = upgradeLevel;
        }
    }

    [Serializable]
    public sealed class DailyMissionSaveEntry
    {
        public string missionId;
        public int progress;
        public bool rewardClaimed;

        public DailyMissionSaveEntry()
        {
        }

        public DailyMissionSaveEntry(string id)
        {
            missionId = id;
        }
    }
}
