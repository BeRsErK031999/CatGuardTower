using System;
using System.Collections.Generic;
using System.Globalization;
using CatGuard.Core.Audio;
using CatGuard.Core.Localization;
using CatGuard.Core.Save;
using CatGuard.Gameplay.Levels;
using CatGuard.Meta.DailyRewards;
using CatGuard.Meta.Upgrades;
using CatGuard.SDK.Ads;
using CatGuard.SDK.Analytics;

namespace CatGuard.Meta.Progression
{
    public static class ProgressionService
    {
        private const string DateKeyFormat = "yyyy-MM-dd";
        private const int FreeCoinsRewardAmount = 15;

        private static LevelCatalogConfig levelCatalog;
        private static UpgradeCatalogConfig upgradeCatalog;
        private static DailyRewardChainConfig dailyRewardChain;
        private static DailyMissionCatalogConfig dailyMissionCatalog;
        private static IRewardedAdService rewardedAdService;
        private static GameSaveData saveData;
        private static LevelConfig selectedLevel;

        public static bool IsInitialized => saveData != null;
        public static int FishCoins => EnsureSave().fishCoins;
        public static string SavePath => GameSaveService.SavePath;
        public static bool HasDailyLoop => dailyRewardChain != null
            && dailyRewardChain.IsValid()
            && dailyMissionCatalog != null
            && dailyMissionCatalog.IsValid();
        public static bool IsAudioMuted => EnsureSave().audioMuted;
        public static string LanguageCode => LocalizationService.NormalizeLanguageCode(EnsureSave().languageCode);
        public static int CameraShakeLevel => Math.Clamp(EnsureSave().cameraShakeIntensity, 0, 2);
        public static float CameraShakeIntensity => CameraShakeLevel * 0.5f;
        public static bool ReducedFlash => EnsureSave().reducedFlash;
        public static bool IsDailyRewardDoubleAvailable => rewardedAdService != null
            && rewardedAdService.IsRewardedAdAvailable(RewardedAdPlacementIds.DailyRewardDouble);
        public static int FreeCoinsRewardFishCoins => FreeCoinsRewardAmount;

        public static DailyRewardConfig CurrentDailyReward
        {
            get
            {
                var data = EnsureSave();
                NormalizeDailyRewardState(data);
                return dailyRewardChain?.GetRewardForIndex(data.dailyRewardStreakIndex);
            }
        }

        public static void Initialize(LevelCatalogConfig levels, UpgradeCatalogConfig upgrades)
        {
            Initialize(levels, upgrades, null, null, new FakeRewardedAdService());
        }

        public static void Initialize(
            LevelCatalogConfig levels,
            UpgradeCatalogConfig upgrades,
            DailyRewardChainConfig dailyRewards,
            DailyMissionCatalogConfig dailyMissions,
            IRewardedAdService ads)
        {
            levelCatalog = levels;
            upgradeCatalog = upgrades;
            dailyRewardChain = dailyRewards;
            dailyMissionCatalog = dailyMissions;
            rewardedAdService = ads ?? new FakeRewardedAdService();
            selectedLevel = null;
            AnalyticsService.Initialize();
            saveData = GameSaveService.LoadOrCreate(levelCatalog?.FirstLevel?.LevelId);
            EnsureDefaults();
            ResolveSelectedLevel();
            ApplySettings();
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
            EnsureDefaults();
            ApplySettings();
            Save();
        }

        public static void SetAudioMuted(bool isMuted)
        {
            var data = EnsureSave();
            data.audioMuted = isMuted;
            ProceduralAudioService.SetMuted(isMuted);
            Save();
        }

        public static void ToggleAudioMuted()
        {
            SetAudioMuted(!IsAudioMuted);
        }

        public static void SetLanguage(string languageCode)
        {
            var data = EnsureSave();
            data.languageCode = LocalizationService.NormalizeLanguageCode(languageCode);
            LocalizationService.SetLanguage(data.languageCode);
            Save();
        }

        public static void ToggleLanguage()
        {
            SetLanguage(LocalizationService.NextLanguageCode());
        }

        public static void CycleCameraShakeIntensity()
        {
            var data = EnsureSave();
            data.cameraShakeIntensity = (CameraShakeLevel + 1) % 3;
            Save();
        }

        public static void ToggleReducedFlash()
        {
            var data = EnsureSave();
            data.reducedFlash = !data.reducedFlash;
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

            AddDailyMissionProgress(DailyMissionType.CompleteLevels, 1, false);
            Save();
            return new LevelCompletionResult(earnedCoins, firstClear, unlockedNames);
        }

        public static void RecordTowerPlaced()
        {
            AddDailyMissionProgress(DailyMissionType.PlaceTowers, 1, true);
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
            var cost = upgrade.GetCostForLevel(nextLevel);
            data.fishCoins -= cost;
            SetUpgradeLevel(data, upgrade.UpgradeId, nextLevel);
            AnalyticsService.TrackTowerUpgrade(upgrade, nextLevel, cost);
            AnalyticsService.TrackUpgradePurchase(upgrade, nextLevel, cost, data.fishCoins);
            Save();

            return true;
        }

        public static bool IsRewardedPlacementAvailable(string placementId)
        {
            return rewardedAdService != null
                && !string.IsNullOrWhiteSpace(placementId)
                && rewardedAdService.IsRewardedAdAvailable(placementId);
        }

        public static bool TryShowRewardedPlacement(string placementId)
        {
            var available = IsRewardedPlacementAvailable(placementId);
            AnalyticsService.TrackRewardedAdOffer(placementId, available);
            return available && rewardedAdService.TryShowRewardedAd(placementId);
        }

        public static int GrantRewardedFishCoins(string placementId, int amount)
        {
            if (string.IsNullOrWhiteSpace(placementId) || amount <= 0)
            {
                return 0;
            }

            var data = EnsureSave();
            data.fishCoins += amount;
            Save();
            return amount;
        }

        public static bool CanClaimFreeCoinsReward()
        {
            var data = EnsureSave();
            return data.lastFreeCoinsRewardDateKey != TodayDateKey()
                && IsRewardedPlacementAvailable(RewardedAdPlacementIds.FreeCoins);
        }

        public static int ClaimFreeCoinsReward()
        {
            if (!CanClaimFreeCoinsReward())
            {
                return 0;
            }

            if (!TryShowRewardedPlacement(RewardedAdPlacementIds.FreeCoins))
            {
                return 0;
            }

            var data = EnsureSave();
            data.fishCoins += FreeCoinsRewardAmount;
            data.lastFreeCoinsRewardDateKey = TodayDateKey();
            Save();
            return FreeCoinsRewardAmount;
        }

        public static DailyRewardClaimResult ClaimDailyReward(bool requestRewardedDouble)
        {
            if (!CanClaimDailyReward())
            {
                return new DailyRewardClaimResult(false, 0, 0, false);
            }

            var reward = CurrentDailyReward;
            if (reward == null)
            {
                return new DailyRewardClaimResult(false, 0, 0, false);
            }

            var usedRewardedDouble = requestRewardedDouble
                && TryShowRewardedPlacement(RewardedAdPlacementIds.DailyRewardDouble);
            var multiplier = usedRewardedDouble ? 2 : 1;
            var earnedCoins = reward.FishCoins * multiplier;
            var data = EnsureSave();

            data.fishCoins += earnedCoins;
            data.lastDailyRewardClaimDateKey = TodayDateKey();
            data.dailyRewardStreakIndex = GetNextDailyRewardIndex(data.dailyRewardStreakIndex);

            AddDailyMissionProgress(DailyMissionType.ClaimDailyReward, 1, false);
            Save();

            var result = new DailyRewardClaimResult(true, earnedCoins, reward.DayNumber, usedRewardedDouble);
            AnalyticsService.TrackDailyRewardClaim(result);
            return result;
        }

        public static bool CanClaimDailyReward()
        {
            if (dailyRewardChain == null || !dailyRewardChain.IsValid())
            {
                return false;
            }

            var data = EnsureSave();
            NormalizeDailyRewardState(data);

            if (string.IsNullOrWhiteSpace(data.lastDailyRewardClaimDateKey))
            {
                return true;
            }

            return GetDaysSinceDateKey(data.lastDailyRewardClaimDateKey) >= 1;
        }

        public static int GetDailyMissionProgress(DailyMissionConfig mission)
        {
            if (mission == null)
            {
                return 0;
            }

            var data = EnsureSave();
            EnsureDailyMissionsForToday(data);
            var entry = GetDailyMissionEntry(data, mission);
            return entry == null ? 0 : Math.Min(entry.progress, mission.TargetAmount);
        }

        public static bool IsDailyMissionComplete(DailyMissionConfig mission)
        {
            return mission != null && GetDailyMissionProgress(mission) >= mission.TargetAmount;
        }

        public static bool IsDailyMissionRewardClaimed(DailyMissionConfig mission)
        {
            if (mission == null)
            {
                return false;
            }

            var data = EnsureSave();
            EnsureDailyMissionsForToday(data);
            var entry = GetDailyMissionEntry(data, mission);
            return entry != null && entry.rewardClaimed;
        }

        public static bool CanClaimDailyMissionReward(DailyMissionConfig mission)
        {
            return mission != null
                && IsDailyMissionComplete(mission)
                && !IsDailyMissionRewardClaimed(mission);
        }

        public static bool ClaimDailyMissionReward(DailyMissionConfig mission)
        {
            if (!CanClaimDailyMissionReward(mission))
            {
                return false;
            }

            var data = EnsureSave();
            var entry = GetDailyMissionEntry(data, mission);
            if (entry == null)
            {
                return false;
            }

            data.fishCoins += mission.RewardFishCoins;
            entry.rewardClaimed = true;
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

            if (data.dailyMissions == null)
            {
                data.dailyMissions = new List<DailyMissionSaveEntry>();
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

            data.languageCode = LocalizationService.NormalizeLanguageCode(data.languageCode);
            data.cameraShakeIntensity = Math.Clamp(data.cameraShakeIntensity, 0, 2);
            NormalizeDailyRewardState(data);
            EnsureDailyMissionsForToday(data);
        }

        private static void ApplySettings()
        {
            var data = EnsureSaveWithoutDefaults();
            LocalizationService.SetLanguage(data.languageCode);
            ProceduralAudioService.Initialize(data.audioMuted);
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

        private static void AddDailyMissionProgress(DailyMissionType missionType, int amount, bool saveAfter)
        {
            if (dailyMissionCatalog == null || !dailyMissionCatalog.IsValid() || amount <= 0)
            {
                return;
            }

            var data = EnsureSave();
            EnsureDailyMissionsForToday(data);

            foreach (var mission in dailyMissionCatalog.Missions)
            {
                if (mission == null || mission.MissionType != missionType)
                {
                    continue;
                }

                var entry = GetDailyMissionEntry(data, mission);
                if (entry == null || entry.rewardClaimed)
                {
                    continue;
                }

                entry.progress = Math.Min(mission.TargetAmount, entry.progress + amount);
            }

            if (saveAfter)
            {
                Save();
            }
        }

        private static void EnsureDailyMissionsForToday(GameSaveData data)
        {
            if (dailyMissionCatalog == null || !dailyMissionCatalog.IsValid())
            {
                return;
            }

            if (data.dailyMissions == null)
            {
                data.dailyMissions = new List<DailyMissionSaveEntry>();
            }

            var today = TodayDateKey();
            if (data.dailyMissionDateKey != today)
            {
                data.dailyMissionDateKey = today;
                data.dailyMissions.Clear();
            }

            foreach (var mission in dailyMissionCatalog.Missions)
            {
                if (mission == null || GetDailyMissionEntry(data, mission, false) != null)
                {
                    continue;
                }

                data.dailyMissions.Add(new DailyMissionSaveEntry(mission.MissionId));
            }
        }

        private static DailyMissionSaveEntry GetDailyMissionEntry(GameSaveData data, DailyMissionConfig mission)
        {
            return GetDailyMissionEntry(data, mission, true);
        }

        private static DailyMissionSaveEntry GetDailyMissionEntry(
            GameSaveData data,
            DailyMissionConfig mission,
            bool createIfMissing)
        {
            if (data == null || mission == null)
            {
                return null;
            }

            if (data.dailyMissions == null)
            {
                data.dailyMissions = new List<DailyMissionSaveEntry>();
            }

            foreach (var entry in data.dailyMissions)
            {
                if (entry != null && entry.missionId == mission.MissionId)
                {
                    return entry;
                }
            }

            if (!createIfMissing)
            {
                return null;
            }

            var newEntry = new DailyMissionSaveEntry(mission.MissionId);
            data.dailyMissions.Add(newEntry);
            return newEntry;
        }

        private static void NormalizeDailyRewardState(GameSaveData data)
        {
            if (data == null || dailyRewardChain == null || dailyRewardChain.Rewards.Length == 0)
            {
                return;
            }

            if (data.dailyRewardStreakIndex < 0 || data.dailyRewardStreakIndex >= dailyRewardChain.Rewards.Length)
            {
                data.dailyRewardStreakIndex = 0;
            }

            if (string.IsNullOrWhiteSpace(data.lastDailyRewardClaimDateKey))
            {
                return;
            }

            if (!TryParseDateKey(data.lastDailyRewardClaimDateKey, out _))
            {
                data.lastDailyRewardClaimDateKey = string.Empty;
                data.dailyRewardStreakIndex = 0;
                return;
            }

            if (GetDaysSinceDateKey(data.lastDailyRewardClaimDateKey) > 1)
            {
                data.dailyRewardStreakIndex = 0;
            }
        }

        private static int GetNextDailyRewardIndex(int currentIndex)
        {
            var rewardCount = dailyRewardChain?.Rewards.Length ?? 0;
            if (rewardCount == 0)
            {
                return 0;
            }

            return (currentIndex + 1) % rewardCount;
        }

        private static string TodayDateKey()
        {
            return DateTime.UtcNow.ToString(DateKeyFormat, CultureInfo.InvariantCulture);
        }

        private static int GetDaysSinceDateKey(string dateKey)
        {
            if (!TryParseDateKey(dateKey, out var date))
            {
                return int.MaxValue;
            }

            return (DateTime.UtcNow.Date - date.Date).Days;
        }

        private static bool TryParseDateKey(string dateKey, out DateTime date)
        {
            return DateTime.TryParseExact(
                dateKey,
                DateKeyFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out date);
        }
    }
}
