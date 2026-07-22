using System;
using System.Collections.Generic;

namespace CatGuard.Core.Save
{
    [Serializable]
    public sealed class GameSaveData
    {
        public int schemaVersion = GameSaveMigrationService.CurrentSchemaVersion;
        public int fishCoins;
        public string selectedLevelId;
        public List<string> unlockedLevelIds = new();
        public List<string> completedLevelIds = new();
        public List<UpgradeSaveEntry> upgrades = new();
        public string lastDailyRewardClaimDateKey;
        public int dailyRewardStreakIndex;
        public string dailyMissionDateKey;
        public List<DailyMissionSaveEntry> dailyMissions = new();
        public List<QuestProgressSaveEntry> quests = new();
        public List<string> activeQuestIds = new();
        public List<string> processedQuestEventIds = new();
        public int playerExperience;
        public List<TowerMasterySaveEntry> towerMasteries = new();
        public List<ResearchSaveEntry> workshopResearch = new();
        public List<string> unlockedUltimateIds = new();
        public List<string> unlockedGuardianPerkIds = new();
        public List<string> equippedUltimateIds = new();
        public string equippedGuardianPerkId = string.Empty;
        public List<string> discoveredCodexEntryIds = new();
        public List<string> processedMetaBattleEventIds = new();
        public List<AchievementProgressSaveEntry> achievements = new();
        public List<string> achievementUltimateIds = new();
        public List<string> achievementBossIds = new();
        public List<string> processedAchievementEventIds = new();
        public bool audioMuted;
        public string languageCode = "ru";
        public string lastFreeCoinsRewardDateKey;
        public int cameraShakeIntensity = 2;
        public bool reducedFlash;

        public static GameSaveData CreateDefault(string firstLevelId)
        {
            var data = new GameSaveData
            {
                schemaVersion = GameSaveMigrationService.CurrentSchemaVersion,
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

    [Serializable]
    public sealed class QuestProgressSaveEntry
    {
        public string questId;
        public int progress;
        public bool rewardClaimed;

        public QuestProgressSaveEntry()
        {
        }

        public QuestProgressSaveEntry(string id)
        {
            questId = id;
        }
    }

    [Serializable]
    public sealed class TowerMasterySaveEntry
    {
        public string towerId;
        public int experience;

        public TowerMasterySaveEntry()
        {
        }

        public TowerMasterySaveEntry(string id)
        {
            towerId = id;
        }
    }

    [Serializable]
    public sealed class ResearchSaveEntry
    {
        public string researchId;
        public int level;

        public ResearchSaveEntry()
        {
        }

        public ResearchSaveEntry(string id, int researchLevel = 0)
        {
            researchId = id;
            level = researchLevel;
        }
    }

    [Serializable]
    public sealed class AchievementProgressSaveEntry
    {
        public string achievementId;
        public int progress;
        public string completedDateKey = string.Empty;
        public bool claimed;

        public AchievementProgressSaveEntry()
        {
        }

        public AchievementProgressSaveEntry(string id)
        {
            achievementId = id;
        }
    }
}
