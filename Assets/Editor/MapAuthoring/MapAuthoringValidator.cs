using System;
using System.Collections.Generic;
using System.Linq;
using CatGuard.Gameplay.Battlefield;
using CatGuard.Gameplay.Levels;
using UnityEditor;
using UnityEngine;

namespace CatGuard.EditorTools.MapAuthoring
{
    public static class MapAuthoringValidator
    {
        public const string MissingLevelCode = "reference.level";
        public const string MissingBattlefieldCode = "reference.battlefield";
        public const string MissingTowerCode = "reference.tower";
        public const string MissingWaveCode = "reference.wave";
        public const string MissingEnemyCode = "reference.enemy";
        public const string DesignCardCode = "design_card.incomplete";
        public const string RoutePointCountCode = "route.point_count";
        public const string RouteDuplicateIdCode = "route.duplicate_id";
        public const string RouteWorldBoundsCode = "route.world_bounds";
        public const string RouteExceptionCode = "route.exception_reason";
        public const string RouteSpawnReachabilityCode = "route.spawn_reachability";
        public const string RouteGoalReachabilityCode = "route.goal_reachability";
        public const string RouteDegenerateSegmentCode = "route.degenerate_segment";
        public const string WaveRouteReferenceCode = "wave.route_reference";
        public const string PlacementPathCode = "placement.path_intersection";
        public const string PlacementBlockedCode = "placement.blocked_intersection";
        public const string PlacementCapacityCode = "placement.capacity";
        public const string CameraCoverageCode = "camera.coverage";
        public const string BattlefieldCoreCode = "battlefield.core";

        public static MapAuthoringValidationResult ValidateLevel(LevelConfig level, string assetPathOverride = null)
        {
            var result = new MapAuthoringValidationResult();
            var levelPath = ResolvePath(level, assetPathOverride);
            if (level == null)
            {
                AddError(result, MissingLevelCode, levelPath, "level", "LevelConfig reference is missing.");
                return result;
            }

            if (level.BattlefieldConfig == null)
            {
                AddError(result, MissingBattlefieldCode, levelPath, level.LevelId, "Level must reference an explicit BattlefieldConfig for the E4 authoring pipeline.");
                return result;
            }

            ValidateBattlefieldInto(level.BattlefieldConfig, ResolvePath(level.BattlefieldConfig, null), result);
            ValidateContentReferences(level, levelPath, result);
            return result;
        }

        public static MapAuthoringValidationResult ValidateBattlefield(BattlefieldConfig battlefield, string assetPathOverride = null)
        {
            var result = new MapAuthoringValidationResult();
            ValidateBattlefieldInto(battlefield, ResolvePath(battlefield, assetPathOverride), result);
            return result;
        }

        private static void ValidateBattlefieldInto(
            BattlefieldConfig battlefield,
            string assetPath,
            MapAuthoringValidationResult result)
        {
            if (battlefield == null)
            {
                AddError(result, MissingBattlefieldCode, assetPath, "battlefield", "BattlefieldConfig reference is missing.");
                return;
            }

            if (!battlefield.DesignCard.IsComplete(out var designError))
            {
                AddError(result, DesignCardCode, assetPath, battlefield.BattlefieldId, designError);
            }

            var definition = battlefield.CreateDefinition();
            if (definition == null)
            {
                AddError(result, BattlefieldCoreCode, assetPath, battlefield.BattlefieldId, "Battlefield definition could not be created.");
                return;
            }

            if (!definition.IsValid(out var coreError))
            {
                AddError(result, BattlefieldCoreCode, assetPath, battlefield.BattlefieldId, coreError);
            }

            ValidateRoutes(battlefield, definition, assetPath, result);
            ValidatePlacement(definition, assetPath, result);
            ValidateCameraCoverage(definition, assetPath, result);
        }

        private static void ValidateRoutes(
            BattlefieldConfig battlefield,
            BattlefieldDefinition definition,
            string assetPath,
            MapAuthoringValidationResult result)
        {
            var routeIds = new HashSet<string>(StringComparer.Ordinal);
            var routeConfigs = battlefield.RouteConfigs;
            if (routeConfigs.Length == 0)
            {
                AddError(result, RoutePointCountCode, assetPath, battlefield.BattlefieldId, "Explicit route configs are required; migrate the legacy path to route 'main'.");
                return;
            }

            foreach (var route in routeConfigs)
            {
                var routeId = route?.RouteId ?? "<missing route>";
                if (route == null || route.Points.Length < 2)
                {
                    AddError(result, RoutePointCountCode, assetPath, routeId, "Route requires at least two points.");
                    continue;
                }

                if (!routeIds.Add(routeId))
                {
                    AddError(result, RouteDuplicateIdCode, assetPath, routeId, "Route id must be unique inside the battlefield.");
                }

                var outOfBoundsPoints = route.Points.Where(point => !Contains(definition.WorldBounds, point)).ToArray();
                var endpointOutside = !Contains(definition.WorldBounds, route.SpawnAnchor)
                    || !Contains(definition.WorldBounds, route.GoalAnchor);
                if ((outOfBoundsPoints.Length > 0 || endpointOutside) && !route.AllowPointsOutsideWorldBounds)
                {
                    AddError(result, RouteWorldBoundsCode, assetPath, routeId, "Route points and endpoints must stay inside world bounds or use an explicit documented exception.");
                }
                else if (route.AllowPointsOutsideWorldBounds && string.IsNullOrWhiteSpace(route.ValidationMetadata))
                {
                    AddError(result, RouteExceptionCode, assetPath, routeId, "An out-of-bounds exception requires validation metadata explaining the authoring decision.");
                }

                var reachabilityTolerance = Mathf.Max(0.08f, route.VisualWidth * 0.75f);
                if (Vector2.Distance(route.Points[0], route.SpawnAnchor) > reachabilityTolerance)
                {
                    AddError(result, RouteSpawnReachabilityCode, assetPath, routeId, "First route point must reach the configured spawn anchor.");
                }

                if (Vector2.Distance(route.Points[^1], route.GoalAnchor) > reachabilityTolerance)
                {
                    AddError(result, RouteGoalReachabilityCode, assetPath, routeId, "Last route point must reach the configured goal anchor.");
                }

                for (var index = 1; index < route.Points.Length; index++)
                {
                    if (Vector2.Distance(route.Points[index - 1], route.Points[index]) <= 0.001f)
                    {
                        AddError(result, RouteDegenerateSegmentCode, assetPath, $"{routeId}.points[{index}]", "Consecutive route points must be distinct.");
                    }
                }
            }
        }

        private static void ValidatePlacement(
            BattlefieldDefinition battlefield,
            string assetPath,
            MapAuthoringValidationResult result)
        {
            if (battlefield.BuildCellCenters.Count < 4)
            {
                AddError(result, PlacementCapacityCode, assetPath, battlefield.BattlefieldId, "Placement zones must produce at least four usable cells.");
                if (battlefield.PathRejectedCellCenters.Count > 0)
                {
                    AddError(result, PlacementPathCode, assetPath, battlefield.BattlefieldId, "Route clearance rejects too many requested placement cells.");
                }

                if (battlefield.BlockedRejectedCellCenters.Count > 0)
                {
                    AddError(result, PlacementBlockedCode, assetPath, battlefield.BattlefieldId, "No-build zones reject too many requested placement cells.");
                }
            }

            foreach (var center in battlefield.BuildCellCenters)
            {
                foreach (var route in battlefield.Routes)
                {
                    var minimumClearance = (route.VisualWidth * 0.5f) + (battlefield.PlacementCellSize * 0.16f);
                    if (DistanceToRoute(center, route.Points) < minimumClearance)
                    {
                        AddError(result, PlacementPathCode, assetPath, FormatPoint(center), $"Placement cell intersects route '{route.RouteId}'.");
                        break;
                    }
                }

                foreach (var blockedZone in battlefield.BlockedZones)
                {
                    var expanded = Expand(blockedZone.Bounds, battlefield.PlacementCellSize * 0.08f);
                    if (expanded.Contains(center))
                    {
                        AddError(result, PlacementBlockedCode, assetPath, FormatPoint(center), $"Placement cell intersects no-build zone '{blockedZone.ZoneId}'.");
                        break;
                    }
                }
            }
        }

        private static void ValidateCameraCoverage(
            BattlefieldDefinition battlefield,
            string assetPath,
            MapAuthoringValidationResult result)
        {
            var required = new List<(string Context, Vector2 Point)>();
            foreach (var route in battlefield.Routes)
            {
                if (route.AllowPointsOutsideWorldBounds)
                {
                    continue;
                }

                required.Add(($"{route.RouteId}.spawn", route.SpawnAnchor));
                required.Add(($"{route.RouteId}.goal", route.GoalAnchor));
                for (var index = 0; index < route.Points.Length; index++)
                {
                    required.Add(($"{route.RouteId}.points[{index}]", route.Points[index]));
                }
            }

            required.AddRange(battlefield.BuildCellCenters.Select((point, index) => ($"placement[{index}]", point)));
            required.AddRange(battlefield.DecorationAnchors.Select(anchor => ($"decoration.{anchor.DecorationId}", anchor.Position)));

            foreach (var item in required)
            {
                if (!Contains(battlefield.CameraBounds, item.Point))
                {
                    AddError(result, CameraCoverageCode, assetPath, item.Context, $"Required object at {FormatPoint(item.Point)} is outside camera bounds.");
                }
            }
        }

        private static void ValidateContentReferences(
            LevelConfig level,
            string assetPath,
            MapAuthoringValidationResult result)
        {
            if (level.AvailableTowers == null || level.AvailableTowers.Length == 0)
            {
                AddError(result, MissingTowerCode, assetPath, level.LevelId, "At least one tower reference is required.");
            }
            else
            {
                for (var index = 0; index < level.AvailableTowers.Length; index++)
                {
                    var tower = level.AvailableTowers[index];
                    if (tower == null || !tower.IsValid())
                    {
                        AddError(result, MissingTowerCode, assetPath, $"availableTowers[{index}]", "Tower reference is missing or invalid.");
                    }
                }
            }

            if (level.WaveConfig == null)
            {
                AddError(result, MissingWaveCode, assetPath, level.LevelId, "WaveConfig reference is missing.");
                return;
            }

            for (var index = 0; index < level.WaveConfig.Groups.Length; index++)
            {
                var group = level.WaveConfig.Groups[index];
                if (group?.EnemyConfig == null || !group.EnemyConfig.IsValid())
                {
                    AddError(result, MissingEnemyCode, ResolvePath(level.WaveConfig, null), $"groups[{index}]", "Enemy reference is missing or invalid.");
                }
            }

            var battlefield = level.BattlefieldConfig.CreateDefinition();
            if (!level.WaveConfig.IsValid(battlefield, out var routeError))
            {
                AddError(result, WaveRouteReferenceCode, ResolvePath(level.WaveConfig, null), level.LevelId, routeError);
            }
        }

        private static string ResolvePath(UnityEngine.Object asset, string overridePath)
        {
            if (!string.IsNullOrWhiteSpace(overridePath))
            {
                return overridePath;
            }

            var assetPath = asset == null ? string.Empty : AssetDatabase.GetAssetPath(asset);
            return string.IsNullOrWhiteSpace(assetPath) ? "<unsaved asset>" : assetPath;
        }

        private static void AddError(
            MapAuthoringValidationResult result,
            string code,
            string assetPath,
            string context,
            string message)
        {
            result.Add(new MapAuthoringValidationIssue(MapAuthoringIssueSeverity.Error, code, assetPath, context, message));
        }

        private static float DistanceToRoute(Vector2 point, IReadOnlyList<Vector2> routePoints)
        {
            var result = float.MaxValue;
            for (var index = 1; index < routePoints.Count; index++)
            {
                result = Mathf.Min(result, DistanceToSegment(point, routePoints[index - 1], routePoints[index]));
            }

            return result;
        }

        private static float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
        {
            var segment = end - start;
            var lengthSquared = segment.sqrMagnitude;
            if (lengthSquared <= 0.000001f)
            {
                return Vector2.Distance(point, start);
            }

            var projection = Mathf.Clamp01(Vector2.Dot(point - start, segment) / lengthSquared);
            return Vector2.Distance(point, start + (segment * projection));
        }

        private static Rect Expand(Rect rect, float amount)
        {
            return Rect.MinMaxRect(rect.xMin - amount, rect.yMin - amount, rect.xMax + amount, rect.yMax + amount);
        }

        private static bool Contains(Rect rect, Vector2 point)
        {
            const float tolerance = 0.001f;
            return point.x >= rect.xMin - tolerance
                && point.x <= rect.xMax + tolerance
                && point.y >= rect.yMin - tolerance
                && point.y <= rect.yMax + tolerance;
        }

        private static string FormatPoint(Vector2 point)
        {
            return $"{point.x:0.###},{point.y:0.###}";
        }
    }
}
