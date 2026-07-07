using System.Collections.Generic;
using CatGuard.Core.Save;
using CatGuard.Gameplay.Levels;
using CatGuard.Meta.Upgrades;

namespace CatGuard.Meta.Progression
{
    public static class ProgressionService
    {
        private static LevelCatalogConfig levelCatalog;
        private static UpgradeCatalogConfig upgradeCatalog;
        private static GameSaveData saveData;
        private static LevelConfig selectedLevel;

        public static bool IsInitialized => saveData != null;
        public static int FishCoins => EnsureSave().fishCoins;
        public static string SavePath => GameSaveService.SavePath;

        public static void Initialize(LevelCatalogConfig levels, UpgradeCatalogConfig upgrades)
        {
            levelCatalog = levels;
            upgradeCatalog = upgrades;
            selectedLevel = null;
            saveData = GameSaveService.LoadOrCreate(levelCatalog?.FirstLevel?.LevelId);
            EnsureDefaults();
            ResolveSelectedLevel();
            Save();
        }

        public static GameSaveData EnsureSave()
        {
            if (saveData == null)
            {
                saveData = GameSaveService.LoadOrCreate(levelCatalog?.FirstLevel?.LevelId);
                EnsureDefaults();
            }

            return saveData;
        }

        public static void Save()
        {
            GameSaveService.Save(EnsureSave());
        }

        public static void ResetProgress()
        {
            GameSaveService.DeleteSave();
            saveData = GameSaveData.CreateDefault(levelCatalog?.FirstLevel?.LevelId);
            selectedLevel = levelCatalog?.FirstLevel;
            Save();
        }

        public static bool IsLevelUnlocked(LevelConfig level)
        {
            return level != null && EnsureSave().unlockedLevelIds.Contains(level.LevelId);
        }

        public static bool IsLevelCompleted(LevelConfig level)
        {
            return level != null && EnsureSave().completedLevelIds.Contains(level.LevelId);
        }

        public static void SelectLevel(LevelConfig level)
        {
            if (level == null || !IsLevelUnlocked(level))
            {
                return;
            }

            selectedLevel = level;
            EnsureSave().selectedLevelId = level.LevelId;
            Save();
        }

        public static LevelConfig GetSelectedLevelOrDefault(LevelConfig fallback)
        {
            ResolveSelectedLevel();
            return selectedLevel != null && IsLevelUnlocked(selectedLevel) ? selectedLevel : fallback;
        }

        public static LevelCompletionResult CompleteLevel(LevelConfig level)
        {
            if (level == null)
            {
                return new LevelCompletionResult(0, false, new List<string>());
            }

            var data = EnsureSave();
            var firstClear = !data.completedLevelIds.Contains(level.LevelId);
            var earnedCoins = firstClear ? level.FirstClearRewardCoins : level.ReplayRewardCoins;
            var unlockedNames = new List<string>();

            data.fishCoins += earnedCoins;

            if (firstClear)
            {
                data.completedLevelIds.Add(level.LevelId);
            }

            foreach (var nextLevelId in level.UnlocksLevelIds)
            {
                if (string.IsNullOrWhiteSpace(nextLevelId) || data.unlockedLevelIds.Contains(nextLevelId))
                {
                    continue;
                }

                data.unlockedLevelIds.Add(nextLevelId);
                unlockedNames.Add(levelCatalog?.FindById(nextLevelId)?.DisplayName ?? nextLevelId);
            }

            Save();
            return new LevelCompletionResult(earnedCoins, firstClear, unlockedNames);
        }

        public static int GetUpgradeLevel(UpgradeConfig upgrade)
        {
            if (upgrade == null)
            {
                return 0;
            }

            foreach (var entry in EnsureSave().upgrades)
            {
                if (entry.upgradeId == upgrade.UpgradeId)
                {
                    return entry.level;
                }
            }

            return 0;
        }

        public static bool CanBuyUpgrade(UpgradeConfig upgrade)
        {
            if (upgrade == null)
            {
                return false;
            }

            var currentLevel = GetUpgradeLevel(upgrade);
            if (currentLevel >= upgrade.MaxLevel)
            {
                return false;
            }

            return FishCoins >= upgrade.GetCostForLevel(currentLevel + 1);
        }

        public static bool BuyUpgrade(UpgradeConfig upgrade)
        {
            if (!CanBuyUpgrade(upgrade))
            {
                return false;
            }

            var data = EnsureSave();
            var currentLevel = GetUpgradeLevel(upgrade);
            var nextLevel = currentLevel + 1;
            data.fishCoins -= upgrade.GetCostForLevel(nextLevel);
            SetUpgradeLevel(data, upgrade.UpgradeId, nextLevel);
            Save();

            return true;
        }

        public static float GetTowerDamageMultiplier()
        {
            return 1f + GetSummedUpgradeEffect(UpgradeEffectType.TowerDamageMultiplier);
        }

        public static float GetTowerRangeMultiplier()
        {
            return 1f + GetSummedUpgradeEffect(UpgradeEffectType.TowerRangeMultiplier);
        }

        public static int GetBaseLivesBonus()
        {
            return (int)GetSummedUpgradeEffect(UpgradeEffectType.BaseLivesBonus);
        }

        private static void EnsureDefaults()
        {
            var data = EnsureSaveWithoutDefaults();
            if (data.unlockedLevelIds == null)
            {
                data.unlockedLevelIds = new List<string>();
            }

            if (data.completedLevelIds == null)
            {
                data.completedLevelIds = new List<string>();
            }

            if (data.upgrades == null)
            {
                data.upgrades = new List<UpgradeSaveEntry>();
            }

            var firstLevelId = levelCatalog?.FirstLevel?.LevelId;
            if (!string.IsNullOrWhiteSpace(firstLevelId) && !data.unlockedLevelIds.Contains(firstLevelId))
            {
                data.unlockedLevelIds.Add(firstLevelId);
            }

            if (string.IsNullOrWhiteSpace(data.selectedLevelId))
            {
                data.selectedLevelId = firstLevelId;
            }
        }

        private static GameSaveData EnsureSaveWithoutDefaults()
        {
            return saveData ??= GameSaveService.LoadOrCreate(levelCatalog?.FirstLevel?.LevelId);
        }

        private static void ResolveSelectedLevel()
        {
            if (selectedLevel != null)
            {
                return;
            }

            var data = EnsureSave();
            selectedLevel = levelCatalog?.FindById(data.selectedLevelId) ?? levelCatalog?.FirstLevel;
        }

        private static void SetUpgradeLevel(GameSaveData data, string upgradeId, int level)
        {
            foreach (var entry in data.upgrades)
            {
                if (entry.upgradeId == upgradeId)
                {
                    entry.level = level;
                    return;
                }
            }

            data.upgrades.Add(new UpgradeSaveEntry(upgradeId, level));
        }

        private static float GetSummedUpgradeEffect(UpgradeEffectType effectType)
        {
            var result = 0f;
            if (upgradeCatalog?.Upgrades == null)
            {
                return result;
            }

            foreach (var upgrade in upgradeCatalog.Upgrades)
            {
                if (upgrade == null || upgrade.EffectType != effectType)
                {
                    continue;
                }

                result += GetUpgradeLevel(upgrade) * upgrade.EffectPerLevel;
            }

            return result;
        }
    }
}
