using System;
using System.Collections.Generic;

namespace CatGuard.Core.Save
{
    public static class GameSaveMigrationService
    {
        public const int CurrentSchemaVersion = 2;

        public static bool TryMigrate(GameSaveData data, string firstLevelId, out bool changed, out string error)
        {
            changed = false;
            error = string.Empty;
            if (data == null)
            {
                error = "Save data is missing.";
                return false;
            }

            if (data.schemaVersion < 0 || data.schemaVersion > CurrentSchemaVersion)
            {
                error = $"Unsupported save schema version {data.schemaVersion}.";
                return false;
            }

            while (data.schemaVersion < CurrentSchemaVersion)
            {
                switch (data.schemaVersion)
                {
                    case 0:
                        MigrateLegacyToVersion1(data, firstLevelId);
                        data.schemaVersion = 1;
                        changed = true;
                        break;
                    case 1:
                        MigrateVersion1ToVersion2(data);
                        data.schemaVersion = 2;
                        changed = true;
                        break;
                    default:
                        error = $"No migration step exists for schema version {data.schemaVersion}.";
                        return false;
                }
            }

            NormalizeCollections(data, firstLevelId);
            return true;
        }

        private static void MigrateLegacyToVersion1(GameSaveData data, string firstLevelId)
        {
            NormalizeCollections(data, firstLevelId);
        }

        private static void MigrateVersion1ToVersion2(GameSaveData data)
        {
            data.playerExperience = Math.Max(0, data.playerExperience);
            data.towerMasteries ??= new List<TowerMasterySaveEntry>();
            data.workshopResearch ??= new List<ResearchSaveEntry>();
            data.unlockedUltimateIds ??= new List<string>();
            data.unlockedGuardianPerkIds ??= new List<string>();
            data.equippedUltimateIds ??= new List<string>();
            data.equippedGuardianPerkId ??= string.Empty;
            data.discoveredCodexEntryIds ??= new List<string>();
            data.processedMetaBattleEventIds ??= new List<string>();
            MigrateLegacyUpgrade(data, "claw_training", "starting_supplies");
            MigrateLegacyUpgrade(data, "whisker_focus", "guardian_focus");
            MigrateLegacyUpgrade(data, "cozy_cushions", "barrel_reinforcement");
        }

        private static void MigrateLegacyUpgrade(GameSaveData data, string upgradeId, string researchId)
        {
            var legacyLevel = 0;
            foreach (var entry in data.upgrades)
            {
                if (entry != null && string.Equals(entry.upgradeId, upgradeId, StringComparison.Ordinal))
                {
                    legacyLevel = Math.Max(0, entry.level);
                    break;
                }
            }

            if (legacyLevel <= 0)
            {
                return;
            }

            foreach (var entry in data.workshopResearch)
            {
                if (entry != null && string.Equals(entry.researchId, researchId, StringComparison.Ordinal))
                {
                    entry.level = Math.Max(entry.level, legacyLevel);
                    return;
                }
            }

            data.workshopResearch.Add(new ResearchSaveEntry(researchId, legacyLevel));
        }

        private static void NormalizeCollections(GameSaveData data, string firstLevelId)
        {
            data.unlockedLevelIds ??= new List<string>();
            data.completedLevelIds ??= new List<string>();
            data.upgrades ??= new List<UpgradeSaveEntry>();
            data.dailyMissions ??= new List<DailyMissionSaveEntry>();
            data.quests ??= new List<QuestProgressSaveEntry>();
            data.activeQuestIds ??= new List<string>();
            data.processedQuestEventIds ??= new List<string>();
            data.towerMasteries ??= new List<TowerMasterySaveEntry>();
            data.workshopResearch ??= new List<ResearchSaveEntry>();
            data.unlockedUltimateIds ??= new List<string>();
            data.unlockedGuardianPerkIds ??= new List<string>();
            data.equippedUltimateIds ??= new List<string>();
            data.discoveredCodexEntryIds ??= new List<string>();
            data.processedMetaBattleEventIds ??= new List<string>();
            data.selectedLevelId ??= string.Empty;
            data.lastDailyRewardClaimDateKey ??= string.Empty;
            data.dailyMissionDateKey ??= string.Empty;
            data.languageCode ??= "ru";
            data.lastFreeCoinsRewardDateKey ??= string.Empty;
            data.equippedGuardianPerkId ??= string.Empty;

            if (!string.IsNullOrWhiteSpace(firstLevelId) && !data.unlockedLevelIds.Contains(firstLevelId))
            {
                data.unlockedLevelIds.Add(firstLevelId);
            }

            if (string.IsNullOrWhiteSpace(data.selectedLevelId))
            {
                data.selectedLevelId = firstLevelId ?? string.Empty;
            }
        }
    }
}
