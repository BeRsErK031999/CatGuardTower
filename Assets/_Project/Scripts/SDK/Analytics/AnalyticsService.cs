using System.Collections.Generic;
using CatGuard.Gameplay.Battlefield;
using CatGuard.Gameplay.Enemies;
using CatGuard.Gameplay.Levels;
using CatGuard.Gameplay.Towers;
using CatGuard.Gameplay.Towers.Upgrades;
using CatGuard.Gameplay.Ultimates;
using CatGuard.Meta.DailyRewards;
using CatGuard.Meta.Achievements;
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

        public static void TrackEnemySpawn(
            LevelConfig level,
            EnemyConfig enemy,
            PathRouteDefinition route,
            int spawnOrder)
        {
            var parameters = CreateEnemyRouteParameters(level, enemy, route, 0f);
            parameters[AnalyticsParameterNames.SpawnOrder] = spawnOrder;
            TrackEvent(AnalyticsEventNames.EnemySpawn, parameters);
        }

        public static void TrackEnemyDefeat(LevelConfig level, BasicEnemy enemy)
        {
            var parameters = CreateEnemyRouteParameters(
                level,
                null,
                enemy?.Route,
                enemy?.NormalizedProgress ?? 0f);
            parameters[AnalyticsParameterNames.EnemyId] = enemy?.EnemyId ?? "unknown";
            TrackEvent(AnalyticsEventNames.EnemyDefeat, parameters);
        }

        public static void TrackEnemyEscape(LevelConfig level, BasicEnemy enemy)
        {
            var parameters = CreateEnemyRouteParameters(
                level,
                null,
                enemy?.Route,
                enemy?.NormalizedProgress ?? 1f);
            parameters[AnalyticsParameterNames.EnemyId] = enemy?.EnemyId ?? "unknown";
            TrackEvent(AnalyticsEventNames.EnemyEscape, parameters);
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

        public static void TrackUltimateReady(LevelConfig level, UltimateConfig ultimate)
        {
            TrackEvent(AnalyticsEventNames.UltimateReady, CreateUltimateParameters(level, ultimate));
        }

        public static void TrackUltimateUse(LevelConfig level, UltimateConfig ultimate, Vector2 target)
        {
            var parameters = CreateUltimateParameters(level, ultimate);
            parameters[AnalyticsParameterNames.PositionX] = target.x;
            parameters[AnalyticsParameterNames.PositionY] = target.y;
            TrackEvent(AnalyticsEventNames.UltimateUse, parameters);
        }

        public static void TrackUltimateResult(
            LevelConfig level,
            UltimateConfig ultimate,
            string result,
            int hitCount,
            float damageDealt,
            int wardBlocks)
        {
            var parameters = CreateUltimateParameters(level, ultimate);
            parameters[AnalyticsParameterNames.UltimateResult] = string.IsNullOrWhiteSpace(result) ? "unknown" : result;
            parameters[AnalyticsParameterNames.HitCount] = Mathf.Max(0, hitCount);
            parameters[AnalyticsParameterNames.DamageDealt] = Mathf.Max(0f, damageDealt);
            parameters[AnalyticsParameterNames.WardBlocks] = Mathf.Max(0, wardBlocks);
            TrackEvent(AnalyticsEventNames.UltimateResult, parameters);
        }

        public static void TrackAchievementCompleted(AchievementConfig achievement)
        {
            TrackEvent(AnalyticsEventNames.AchievementCompleted, CreateAchievementParameters(achievement));
        }

        public static void TrackAchievementClaim(AchievementConfig achievement, AchievementClaimResult result)
        {
            var parameters = CreateAchievementParameters(achievement);
            parameters[AnalyticsParameterNames.EarnedFishCoins] = result?.FishCoins ?? 0;
            parameters[AnalyticsParameterNames.EarnedPlayerExperience] = result?.PlayerExperience ?? 0;
            TrackEvent(AnalyticsEventNames.AchievementClaim, parameters);
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
            var battlefield = level?.ResolveBattlefield();
            var routeIds = new List<string>();
            if (battlefield?.Routes != null)
            {
                foreach (var route in battlefield.Routes)
                {
                    if (route != null)
                    {
                        routeIds.Add(route.RouteId);
                    }
                }
            }

            return new Dictionary<string, object>
            {
                [AnalyticsParameterNames.LevelId] = level == null ? "unknown" : level.LevelId,
                [AnalyticsParameterNames.LevelName] = level == null ? "Unknown" : level.DisplayName,
                [AnalyticsParameterNames.BattlefieldId] = battlefield?.BattlefieldId ?? "unknown",
                [AnalyticsParameterNames.RouteCount] = routeIds.Count,
                [AnalyticsParameterNames.RouteIds] = string.Join(",", routeIds)
            };
        }

        public static void TrackBattleTowerUpgrade(
            LevelConfig level,
            BasicTower tower,
            TowerUpgradeBranchConfig branch,
            TowerUpgradeTierConfig tier,
            int remainingBattleFish)
        {
            var parameters = CreateBattleTowerParameters(level, tower);
            parameters[AnalyticsParameterNames.BranchId] = branch?.BranchId ?? "unknown";
            parameters[AnalyticsParameterNames.Tier] = tier?.Tier ?? 0;
            parameters[AnalyticsParameterNames.CostFishCoins] = tier?.Price ?? 0;
            parameters[AnalyticsParameterNames.RemainingFishCoins] = remainingBattleFish;
            TrackEvent(AnalyticsEventNames.BattleTowerUpgrade, parameters);
        }

        public static void TrackBattleTowerSell(
            LevelConfig level,
            BasicTower tower,
            int sellValue,
            int remainingBattleFish)
        {
            var parameters = CreateBattleTowerParameters(level, tower);
            parameters[AnalyticsParameterNames.SellValue] = sellValue;
            parameters[AnalyticsParameterNames.RemainingFishCoins] = remainingBattleFish;
            TrackEvent(AnalyticsEventNames.BattleTowerSell, parameters);
        }

        public static void TrackTowerTargetPriority(
            LevelConfig level,
            BasicTower tower,
            TowerTargetPriority priority)
        {
            var parameters = CreateBattleTowerParameters(level, tower);
            parameters[AnalyticsParameterNames.TargetPriority] = priority.ToString().ToLowerInvariant();
            TrackEvent(AnalyticsEventNames.TowerTargetPriority, parameters);
        }

        private static Dictionary<string, object> CreateEnemyRouteParameters(
            LevelConfig level,
            EnemyConfig enemy,
            PathRouteDefinition route,
            float normalizedProgress)
        {
            var parameters = CreateLevelParameters(level);
            parameters[AnalyticsParameterNames.EnemyId] = enemy?.EnemyId ?? "unknown";
            parameters[AnalyticsParameterNames.RouteId] = route?.RouteId ?? "unknown";
            parameters[AnalyticsParameterNames.RouteTags] = route == null ? string.Empty : string.Join(",", route.Tags);
            parameters[AnalyticsParameterNames.RouteProgress] = Mathf.Clamp01(normalizedProgress);
            return parameters;
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

        private static Dictionary<string, object> CreateBattleTowerParameters(LevelConfig level, BasicTower tower)
        {
            var parameters = CreateLevelParameters(level);
            parameters[AnalyticsParameterNames.TowerId] = tower?.Config?.TowerId ?? "unknown";
            parameters[AnalyticsParameterNames.BranchId] = tower?.SelectedBranchId ?? string.Empty;
            parameters[AnalyticsParameterNames.Tier] = tower?.CurrentTier ?? 0;
            parameters[AnalyticsParameterNames.InvestedCost] = tower?.InvestedUpgradeCost ?? 0;
            return parameters;
        }

        private static Dictionary<string, object> CreateUltimateParameters(LevelConfig level, UltimateConfig ultimate)
        {
            var parameters = CreateLevelParameters(level);
            parameters[AnalyticsParameterNames.UltimateId] = ultimate?.UltimateId ?? "unknown";
            parameters[AnalyticsParameterNames.UltimateTargetingMode] = ultimate == null
                ? "unknown"
                : ultimate.TargetingMode.ToString().ToLowerInvariant();
            return parameters;
        }

        private static Dictionary<string, object> CreateAchievementParameters(AchievementConfig achievement)
        {
            return new Dictionary<string, object>
            {
                [AnalyticsParameterNames.AchievementId] = achievement?.AchievementId ?? "unknown",
                [AnalyticsParameterNames.AchievementCategory] = achievement == null
                    ? "unknown"
                    : achievement.Category.ToString().ToLowerInvariant(),
                [AnalyticsParameterNames.AchievementTier] = achievement == null
                    ? "unknown"
                    : achievement.PresentationTier.ToString().ToLowerInvariant()
            };
        }

        private static string NormalizePlacementId(string placementId)
        {
            return string.IsNullOrWhiteSpace(placementId) ? "unknown" : placementId;
        }
    }
}
