using System.Collections.Generic;
using CatGuard.Gameplay.Levels;
using CatGuard.Gameplay.Towers;
using CatGuard.Meta.DailyRewards;
using CatGuard.Meta.Progression;
using CatGuard.Meta.Upgrades;
using CatGuard.SDK.Firebase;
using UnityEngine;

namespace CatGuard.SDK.Analytics
{
    public static class AnalyticsService
    {
        private static IAnalyticsService current;
        private static bool appStartTracked;

        public static IAnalyticsService Current => current ??= CreateDefaultImplementation();

        public static void Initialize(IAnalyticsService service = null)
        {
            current = service ?? current ?? CreateDefaultImplementation();
            TrackAppStart();
        }

        public static void ResetForValidation(IAnalyticsService service = null)
        {
            current = service ?? new FakeAnalyticsService();
            appStartTracked = false;
        }

        public static void TrackEvent(string eventName, IReadOnlyDictionary<string, object> parameters = null)
        {
            if (string.IsNullOrWhiteSpace(eventName) || !Current.IsEnabled)
            {
                return;
            }

            Current.Track(new AnalyticsEventRecord(eventName, parameters));
        }

        public static void TrackAppStart()
        {
            if (appStartTracked)
            {
                return;
            }

            appStartTracked = true;
            TrackEvent(AnalyticsEventNames.AppStart);
        }

        public static void TrackLevelStart(LevelConfig level, int baseLives)
        {
            var parameters = CreateLevelParameters(level);
            parameters[AnalyticsParameterNames.BaseLives] = baseLives;
            TrackEvent(AnalyticsEventNames.LevelStart, parameters);
        }

        public static void TrackLevelComplete(
            LevelConfig level,
            LevelCompletionResult result,
            int remainingLives,
            int defeatedEnemies,
            int escapedEnemies,
            int towerCount)
        {
            var parameters = CreateLevelResultParameters(level, remainingLives, defeatedEnemies, escapedEnemies, towerCount);
            parameters[AnalyticsParameterNames.EarnedFishCoins] = result?.EarnedFishCoins ?? 0;
            parameters[AnalyticsParameterNames.FirstClear] = result?.FirstClear ?? false;
            parameters[AnalyticsParameterNames.UnlockedLevelCount] = result?.UnlockedLevelNames?.Count ?? 0;
            TrackEvent(AnalyticsEventNames.LevelComplete, parameters);
        }

        public static void TrackLevelFail(
            LevelConfig level,
            int defeatedEnemies,
            int escapedEnemies,
            int towerCount,
            string failReason)
        {
            var parameters = CreateLevelResultParameters(level, 0, defeatedEnemies, escapedEnemies, towerCount);
            parameters[AnalyticsParameterNames.FailReason] = string.IsNullOrWhiteSpace(failReason) ? "unknown" : failReason;
            TrackEvent(AnalyticsEventNames.LevelFail, parameters);
        }

        public static void TrackTowerPlace(LevelConfig level, TowerConfig tower, int towerCount, Vector2 position)
        {
            var parameters = CreateLevelParameters(level);
            parameters[AnalyticsParameterNames.TowerId] = tower == null ? "unknown" : tower.TowerId;
            parameters[AnalyticsParameterNames.TowerName] = tower == null ? "Unknown" : tower.DisplayName;
            parameters[AnalyticsParameterNames.TowerCount] = towerCount;
            parameters[AnalyticsParameterNames.PositionX] = position.x;
            parameters[AnalyticsParameterNames.PositionY] = position.y;
            TrackEvent(AnalyticsEventNames.TowerPlace, parameters);
        }

        public static void TrackTowerUpgrade(UpgradeConfig upgrade, int nextLevel, int costFishCoins)
        {
            var parameters = CreateUpgradeParameters(upgrade, nextLevel, costFishCoins);
            TrackEvent(AnalyticsEventNames.TowerUpgrade, parameters);
        }

        public static void TrackDailyRewardClaim(DailyRewardClaimResult result)
        {
            var parameters = new Dictionary<string, object>
            {
                [AnalyticsParameterNames.EarnedFishCoins] = result?.EarnedFishCoins ?? 0,
                [AnalyticsParameterNames.DayNumber] = result?.DayNumber ?? 0,
                [AnalyticsParameterNames.UsedRewardedDouble] = result?.UsedRewardedDouble ?? false
            };

            TrackEvent(AnalyticsEventNames.DailyRewardClaim, parameters);
        }

        public static void TrackRewardedAdOffer(string placementId, bool available)
        {
            TrackEvent(AnalyticsEventNames.RewardedAdOffer, new Dictionary<string, object>
            {
                [AnalyticsParameterNames.PlacementId] = NormalizePlacementId(placementId),
                [AnalyticsParameterNames.Available] = available
            });
        }

        public static void TrackRewardedAdStarted(string placementId)
        {
            TrackEvent(AnalyticsEventNames.RewardedAdStarted, new Dictionary<string, object>
            {
                [AnalyticsParameterNames.PlacementId] = NormalizePlacementId(placementId)
            });
        }

        public static void TrackRewardedAdCompleted(string placementId, bool success)
        {
            TrackEvent(AnalyticsEventNames.RewardedAdCompleted, new Dictionary<string, object>
            {
                [AnalyticsParameterNames.PlacementId] = NormalizePlacementId(placementId),
                [AnalyticsParameterNames.Success] = success
            });
        }

        public static void TrackShopOpen(string shopId)
        {
            TrackEvent(AnalyticsEventNames.ShopOpen, new Dictionary<string, object>
            {
                [AnalyticsParameterNames.ShopId] = string.IsNullOrWhiteSpace(shopId) ? "unknown" : shopId
            });
        }

        public static void TrackUpgradePurchase(
            UpgradeConfig upgrade,
            int nextLevel,
            int costFishCoins,
            int remainingFishCoins)
        {
            var parameters = CreateUpgradeParameters(upgrade, nextLevel, costFishCoins);
            parameters[AnalyticsParameterNames.RemainingFishCoins] = remainingFishCoins;
            TrackEvent(AnalyticsEventNames.UpgradePurchase, parameters);
        }

        private static IAnalyticsService CreateDefaultImplementation()
        {
#if CATGUARD_FIREBASE_ANALYTICS
            return new FirebaseAnalyticsService();
#else
            return new FakeAnalyticsService();
#endif
        }

        private static Dictionary<string, object> CreateLevelParameters(LevelConfig level)
        {
            return new Dictionary<string, object>
            {
                [AnalyticsParameterNames.LevelId] = level == null ? "unknown" : level.LevelId,
                [AnalyticsParameterNames.LevelName] = level == null ? "Unknown" : level.DisplayName
            };
        }

        private static Dictionary<string, object> CreateLevelResultParameters(
            LevelConfig level,
            int remainingLives,
            int defeatedEnemies,
            int escapedEnemies,
            int towerCount)
        {
            var parameters = CreateLevelParameters(level);
            parameters[AnalyticsParameterNames.RemainingLives] = remainingLives;
            parameters[AnalyticsParameterNames.DefeatedEnemies] = defeatedEnemies;
            parameters[AnalyticsParameterNames.EscapedEnemies] = escapedEnemies;
            parameters[AnalyticsParameterNames.TowerCount] = towerCount;
            return parameters;
        }

        private static Dictionary<string, object> CreateUpgradeParameters(
            UpgradeConfig upgrade,
            int nextLevel,
            int costFishCoins)
        {
            return new Dictionary<string, object>
            {
                [AnalyticsParameterNames.UpgradeId] = upgrade == null ? "unknown" : upgrade.UpgradeId,
                [AnalyticsParameterNames.UpgradeName] = upgrade == null ? "Unknown" : upgrade.DisplayName,
                [AnalyticsParameterNames.UpgradeLevel] = nextLevel,
                [AnalyticsParameterNames.CostFishCoins] = costFishCoins
            };
        }

        private static string NormalizePlacementId(string placementId)
        {
            return string.IsNullOrWhiteSpace(placementId) ? "unknown" : placementId;
        }
    }
}
