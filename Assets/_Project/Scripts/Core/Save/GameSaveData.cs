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
}
