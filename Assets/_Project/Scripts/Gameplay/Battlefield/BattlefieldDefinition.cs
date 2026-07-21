using System;
using System.Collections.Generic;
using UnityEngine;

namespace CatGuard.Gameplay.Battlefield
{
    public sealed class BattlefieldDefinition
    {
        private readonly Vector2[] buildCellCenters;
        private readonly List<Vector2> pathRejectedCellCenters = new();
        private readonly List<Vector2> blockedRejectedCellCenters = new();
        private readonly Dictionary<string, PathRouteDefinition> routesById = new(StringComparer.Ordinal);

        internal BattlefieldDefinition(
            string id,
            string title,
            string background,
            string biome,
            BattlefieldCameraMode mode,
            Rect mapWorldBounds,
            Rect mapCameraBounds,
            Vector2 cameraFocus,
            float viewHeight,
            PathRouteDefinition[] routeDefinitions,
            float cellSize,
            BattlefieldZone[] buildZones,
            BattlefieldZone[] noBuildZones,
            BattlefieldDecorationAnchor[] decorations,
            bool legacyBattlefield,
            bool legacyRoute)
        {
            BattlefieldId = id ?? string.Empty;
            DisplayName = title ?? string.Empty;
            BackgroundId = background ?? string.Empty;
            BiomeId = biome ?? string.Empty;
            CameraMode = mode;
            WorldBounds = mapWorldBounds;
            CameraBounds = mapCameraBounds;
            InitialCameraFocus = cameraFocus;
            ScrollableViewHeight = viewHeight;
            Routes = routeDefinitions ?? Array.Empty<PathRouteDefinition>();
            PlacementCellSize = cellSize;
            PlacementZones = buildZones ?? Array.Empty<BattlefieldZone>();
            BlockedZones = noBuildZones ?? Array.Empty<BattlefieldZone>();
            DecorationAnchors = decorations ?? Array.Empty<BattlefieldDecorationAnchor>();
            IsLegacy = legacyBattlefield;
            UsesLegacyRoute = legacyRoute;

            foreach (var route in Routes)
            {
                if (route != null && !routesById.ContainsKey(route.RouteId))
                {
                    routesById.Add(route.RouteId, route);
                }
            }

            buildCellCenters = CreateBuildCellCenters();
        }

        public string BattlefieldId { get; }
        public string DisplayName { get; }
        public string BackgroundId { get; }
        public string BiomeId { get; }
        public BattlefieldCameraMode CameraMode { get; }
        public Rect WorldBounds { get; }
        public Rect CameraBounds { get; }
        public Vector2 InitialCameraFocus { get; }
        public float ScrollableViewHeight { get; }
        public PathRouteDefinition[] Routes { get; }
        public PathRouteDefinition PrimaryRoute => Routes.Length == 0 ? null : Routes[0];
        public Vector2[] PathPoints => PrimaryRoute?.Points ?? Array.Empty<Vector2>();
        public float PathVisualWidth => PrimaryRoute?.VisualWidth ?? 0.5f;
        public Vector2 SpawnPresentationAnchor => PrimaryRoute?.SpawnAnchor ?? WorldBounds.center;
        public Vector2 GoalPresentationAnchor => PrimaryRoute?.GoalAnchor ?? WorldBounds.center;
        public float PlacementCellSize { get; }
        public BattlefieldZone[] PlacementZones { get; }
        public BattlefieldZone[] BlockedZones { get; }
        public BattlefieldDecorationAnchor[] DecorationAnchors { get; }
        public bool IsLegacy { get; }
        public bool UsesLegacyRoute { get; }
        public IReadOnlyList<Vector2> BuildCellCenters => buildCellCenters;
        public IReadOnlyList<Vector2> PathRejectedCellCenters => pathRejectedCellCenters;
        public IReadOnlyList<Vector2> BlockedRejectedCellCenters => blockedRejectedCellCenters;
        public bool CanPan => CameraMode == BattlefieldCameraMode.ScrollableLarge;

        public static BattlefieldDefinition FromConfig(BattlefieldConfig config)
        {
            if (config == null)
            {
                return null;
            }

            var routeConfigs = config.RouteConfigs;
            var usesLegacyRoute = routeConfigs.Length == 0;
            PathRouteDefinition[] routeDefinitions;
            if (usesLegacyRoute)
            {
                routeDefinitions = config.PathPoints.Length < 2
                    ? Array.Empty<PathRouteDefinition>()
                    : new[]
                    {
                        new PathRouteDefinition(
                            "main",
                            "Main Route",
                            config.PathPoints,
                            config.SpawnPresentationAnchor,
                            config.GoalPresentationAnchor,
                            "main",
                            config.PathVisualWidth,
                            1f,
                            new[] { "ground", "legacy" },
                            0f,
                            "Runtime migration from legacy battlefield pathPoints.",
                            false)
                    };
            }
            else
            {
                routeDefinitions = new PathRouteDefinition[routeConfigs.Length];
                for (var index = 0; index < routeConfigs.Length; index++)
                {
                    routeDefinitions[index] = routeConfigs[index]?.CreateDefinition();
                }
            }

            return new BattlefieldDefinition(
                config.BattlefieldId,
                config.DisplayName,
                config.BackgroundId,
                config.BiomeId,
                config.CameraMode,
                config.WorldBounds,
                config.CameraBounds,
                config.InitialCameraFocus,
                config.ScrollableViewHeight,
                routeDefinitions,
                config.PlacementCellSize,
                config.PlacementZones,
                config.BlockedZones,
                config.DecorationAnchors,
                false,
                usesLegacyRoute);
        }

        public bool TryGetRoute(string routeId, out PathRouteDefinition route)
        {
            route = null;
            return !string.IsNullOrWhiteSpace(routeId) && routesById.TryGetValue(routeId, out route);
        }

        public bool IsValid(out string error)
        {
            if (string.IsNullOrWhiteSpace(BattlefieldId)
                || string.IsNullOrWhiteSpace(DisplayName)
                || string.IsNullOrWhiteSpace(BackgroundId)
                || string.IsNullOrWhiteSpace(BiomeId))
            {
                error = "Battlefield identity, display name, background id, and biome id are required.";
                return false;
            }

            if (WorldBounds.width <= 0f || WorldBounds.height <= 0f
                || CameraBounds.width <= 0f || CameraBounds.height <= 0f)
            {
                error = "Battlefield world and camera bounds must have positive size.";
                return false;
            }

            if (!Contains(WorldBounds, CameraBounds))
            {
                error = "Battlefield camera bounds must stay inside world bounds.";
                return false;
            }

            if (Routes.Length == 0)
            {
                error = "Battlefield requires at least one route.";
                return false;
            }

            var routeIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var route in Routes)
            {
                if (route == null
                    || string.IsNullOrWhiteSpace(route.RouteId)
                    || string.IsNullOrWhiteSpace(route.DisplayName)
                    || string.IsNullOrWhiteSpace(route.VisualStyleId)
                    || route.Points.Length < 2
                    || route.TotalLength <= 0.0001f
                    || route.VisualWidth <= 0f
                    || route.RouteWeight <= 0f)
                {
                    error = "Every route requires identity, presentation, at least two distinct points, and positive width/weight.";
                    return false;
                }

                if (!routeIds.Add(route.RouteId))
                {
                    error = $"Battlefield route id '{route.RouteId}' is duplicated.";
                    return false;
                }

                foreach (var point in route.Points)
                {
                    if (!route.AllowPointsOutsideWorldBounds && !Contains(WorldBounds, point))
                    {
                        error = $"Route '{route.RouteId}' contains a point outside world bounds.";
                        return false;
                    }
                }

                if (!route.AllowPointsOutsideWorldBounds
                    && (!Contains(WorldBounds, route.SpawnAnchor) || !Contains(WorldBounds, route.GoalAnchor)))
                {
                    error = $"Route '{route.RouteId}' endpoints must stay inside world bounds.";
                    return false;
                }
            }

            if (PlacementCellSize <= 0f || PlacementZones.Length == 0 || buildCellCenters.Length < 4)
            {
                error = "Battlefield placement zones must produce at least four world-space cells.";
                return false;
            }

            foreach (var zone in PlacementZones)
            {
                if (string.IsNullOrWhiteSpace(zone.ZoneId) || !Contains(WorldBounds, zone.Bounds))
                {
                    error = "Every placement zone requires an id and bounds inside the world.";
                    return false;
                }
            }

            foreach (var zone in BlockedZones)
            {
                if (string.IsNullOrWhiteSpace(zone.ZoneId) || !Contains(WorldBounds, zone.Bounds))
                {
                    error = "Every blocked zone requires an id and bounds inside the world.";
                    return false;
                }
            }

            foreach (var decoration in DecorationAnchors)
            {
                if (string.IsNullOrWhiteSpace(decoration.DecorationId) || !Contains(WorldBounds, decoration.Position))
                {
                    error = "Every decoration anchor requires an id and a position inside the world.";
                    return false;
                }
            }

            if (CameraMode == BattlefieldCameraMode.ScrollableLarge && ScrollableViewHeight <= 0f)
            {
                error = "Scrollable battlefields require a positive view height.";
                return false;
            }

            error = string.Empty;
            return true;
        }

        public bool IsBlocked(Vector2 point)
        {
            foreach (var zone in BlockedZones)
            {
                if (zone.Contains(point))
                {
                    return true;
                }
            }

            return false;
        }

        private Vector2[] CreateBuildCellCenters()
        {
            var result = new List<Vector2>();
            var unique = new HashSet<Vector2Int>();
            var cellSize = Mathf.Max(0.5f, PlacementCellSize);

            foreach (var zone in PlacementZones)
            {
                var bounds = zone.Bounds;
                var columns = Mathf.Max(1, Mathf.FloorToInt(bounds.width / cellSize));
                var rows = Mathf.Max(1, Mathf.FloorToInt(bounds.height / cellSize));
                var start = new Vector2(
                    bounds.center.x - ((columns - 1) * cellSize * 0.5f),
                    bounds.center.y - ((rows - 1) * cellSize * 0.5f));

                for (var row = 0; row < rows; row++)
                {
                    for (var column = 0; column < columns; column++)
                    {
                        var center = start + new Vector2(column * cellSize, row * cellSize);
                        var key = new Vector2Int(
                            Mathf.RoundToInt(center.x * 1000f),
                            Mathf.RoundToInt(center.y * 1000f));
                        if (!unique.Add(key))
                        {
                            continue;
                        }

                        if (IntersectsBlockedCell(center, cellSize))
                        {
                            blockedRejectedCellCenters.Add(center);
                            continue;
                        }

                        if (IntersectsRouteCell(center, cellSize))
                        {
                            pathRejectedCellCenters.Add(center);
                            continue;
                        }

                        result.Add(center);
                    }
                }
            }

            result.Sort((left, right) =>
            {
                var xComparison = left.x.CompareTo(right.x);
                return xComparison != 0 ? xComparison : left.y.CompareTo(right.y);
            });
            return result.ToArray();
        }

        private bool IntersectsBlockedCell(Vector2 center, float cellSize)
        {
            var margin = cellSize * 0.08f;
            foreach (var zone in BlockedZones)
            {
                var expanded = Rect.MinMaxRect(
                    zone.Bounds.xMin - margin,
                    zone.Bounds.yMin - margin,
                    zone.Bounds.xMax + margin,
                    zone.Bounds.yMax + margin);
                if (expanded.Contains(center))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IntersectsRouteCell(Vector2 center, float cellSize)
        {
            foreach (var route in Routes)
            {
                var clearance = (route.VisualWidth * 0.5f) + (cellSize * 0.16f);
                for (var index = 1; index < route.Points.Length; index++)
                {
                    if (DistanceToSegment(center, route.Points[index - 1], route.Points[index]) < clearance)
                    {
                        return true;
                    }
                }
            }

            return false;
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

        private static bool Contains(Rect outer, Rect inner)
        {
            const float tolerance = 0.001f;
            return inner.xMin >= outer.xMin - tolerance
                && inner.xMax <= outer.xMax + tolerance
                && inner.yMin >= outer.yMin - tolerance
                && inner.yMax <= outer.yMax + tolerance;
        }

        private static bool Contains(Rect rect, Vector2 point)
        {
            const float tolerance = 0.001f;
            return point.x >= rect.xMin - tolerance
                && point.x <= rect.xMax + tolerance
                && point.y >= rect.yMin - tolerance
                && point.y <= rect.yMax + tolerance;
        }
    }
}
