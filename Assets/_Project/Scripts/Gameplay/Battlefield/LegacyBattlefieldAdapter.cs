using System;
using CatGuard.Gameplay.Levels;
using UnityEngine;

namespace CatGuard.Gameplay.Battlefield
{
    public static class LegacyBattlefieldAdapter
    {
        public static BattlefieldDefinition Create(LevelConfig level)
        {
            if (level == null || level.LegacyPathPoints == null || level.LegacyPathPoints.Length < 2)
            {
                return null;
            }

            var path = level.LegacyPathPoints;
            var cellSize = level.LegacyCellSize;
            var gridWidth = Mathf.Max(cellSize, (level.LegacyGridColumns - 1) * cellSize);
            var gridHeight = Mathf.Max(cellSize, (level.LegacyGridRows - 1) * cellSize);
            var gridBounds = new Rect(
                level.LegacyGridOrigin.x - (cellSize * 0.5f),
                level.LegacyGridOrigin.y - (cellSize * 0.5f),
                gridWidth + cellSize,
                gridHeight + cellSize);

            var minX = gridBounds.xMin;
            var maxX = gridBounds.xMax;
            var minY = gridBounds.yMin;
            var maxY = gridBounds.yMax;
            foreach (var point in path)
            {
                minX = Mathf.Min(minX, point.x);
                maxX = Mathf.Max(maxX, point.x);
                minY = Mathf.Min(minY, point.y);
                maxY = Mathf.Max(maxY, point.y);
            }

            const float padding = 0.7f;
            var worldBounds = Rect.MinMaxRect(minX - padding, minY - padding, maxX + padding, maxY + padding);
            var placementZone = new BattlefieldZone("legacy_grid", gridBounds);
            var route = new PathRouteDefinition(
                "main",
                "Main Route",
                path,
                path[0],
                path[^1],
                "legacy_main",
                0.42f,
                1f,
                new[] { "ground", "legacy" },
                0f,
                "LevelConfig legacy geometry adapter.",
                false);
            return new BattlefieldDefinition(
                $"legacy_{level.LevelId}",
                $"Legacy {level.DisplayName}",
                "garden_day",
                "legacy_garden",
                BattlefieldCameraMode.FixedOverview,
                worldBounds,
                worldBounds,
                worldBounds.center,
                worldBounds.height,
                new[] { route },
                cellSize,
                new[] { placementZone },
                Array.Empty<BattlefieldZone>(),
                Array.Empty<BattlefieldDecorationAnchor>(),
                new BattlefieldPresentationPalette(),
                true,
                true);
        }
    }
}
