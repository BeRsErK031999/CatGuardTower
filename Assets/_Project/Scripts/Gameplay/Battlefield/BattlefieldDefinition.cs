using System;
using System.Collections.Generic;
using UnityEngine;

namespace CatGuard.Gameplay.Battlefield
{
    public sealed class BattlefieldDefinition
    {
        private readonly Vector2[] buildCellCenters;

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
            Vector2[] route,
            float routeVisualWidth,
            Vector2 spawnAnchor,
            Vector2 goalAnchor,
            float cellSize,
            BattlefieldZone[] buildZones,
            BattlefieldZone[] noBuildZones,
            BattlefieldDecorationAnchor[] decorations,
            bool legacy)
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
            PathPoints = route ?? Array.Empty<Vector2>();
            PathVisualWidth = routeVisualWidth;
            SpawnPresentationAnchor = spawnAnchor;
            GoalPresentationAnchor = goalAnchor;
            PlacementCellSize = cellSize;
            PlacementZones = buildZones ?? Array.Empty<BattlefieldZone>();
            BlockedZones = noBuildZones ?? Array.Empty<BattlefieldZone>();
            DecorationAnchors = decorations ?? Array.Empty<BattlefieldDecorationAnchor>();
            IsLegacy = legacy;
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
        public Vector2[] PathPoints { get; }
        public float PathVisualWidth { get; }
        public Vector2 SpawnPresentationAnchor { get; }
        public Vector2 GoalPresentationAnchor { get; }
        public float PlacementCellSize { get; }
        public BattlefieldZone[] PlacementZones { get; }
        public BattlefieldZone[] BlockedZones { get; }
        public BattlefieldDecorationAnchor[] DecorationAnchors { get; }
        public bool IsLegacy { get; }
        public IReadOnlyList<Vector2> BuildCellCenters => buildCellCenters;
        public bool CanPan => CameraMode == BattlefieldCameraMode.ScrollableLarge;

        public static BattlefieldDefinition FromConfig(BattlefieldConfig config)
        {
            if (config == null)
            {
                return null;
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
                config.PathPoints,
                config.PathVisualWidth,
                config.SpawnPresentationAnchor,
                config.GoalPresentationAnchor,
                config.PlacementCellSize,
                config.PlacementZones,
                config.BlockedZones,
                config.DecorationAnchors,
                false);
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

            if (PathPoints.Length < 2 || PathVisualWidth <= 0f)
            {
                error = "Battlefield route requires at least two points and a positive visual width.";
                return false;
            }

            foreach (var point in PathPoints)
            {
                if (!Contains(WorldBounds, point))
                {
                    error = "Battlefield route points must stay inside world bounds.";
                    return false;
                }
            }

            if (!Contains(WorldBounds, SpawnPresentationAnchor) || !Contains(WorldBounds, GoalPresentationAnchor))
            {
                error = "Battlefield spawn and goal presentation anchors must stay inside world bounds.";
                return false;
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
                        if (IsBlocked(center))
                        {
                            continue;
                        }

                        var key = new Vector2Int(
                            Mathf.RoundToInt(center.x * 1000f),
                            Mathf.RoundToInt(center.y * 1000f));
                        if (unique.Add(key))
                        {
                            result.Add(center);
                        }
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
