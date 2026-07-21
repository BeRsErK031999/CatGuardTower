using System;
using System.IO;
using CatGuard.Gameplay.Battlefield;
using UnityEngine;

namespace CatGuard.EditorTools.MapAuthoring
{
    public static class MapAuthoringPreviewExporter
    {
        private const int Padding = 42;

        public static void Export(BattlefieldConfig battlefieldConfig, string outputPath, int width = 1600, int height = 900)
        {
            var battlefield = battlefieldConfig?.CreateDefinition();
            if (battlefield == null)
            {
                throw new ArgumentNullException(nameof(battlefieldConfig));
            }

            width = Mathf.Max(640, width);
            height = Mathf.Max(360, height);
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var pixels = new Color[width * height];
            Array.Fill(pixels, new Color(0.025f, 0.045f, 0.055f, 1f));
            texture.SetPixels(pixels);

            DrawRect(texture, battlefield.WorldBounds, battlefield.WorldBounds, new Color(0.82f, 0.86f, 0.9f, 1f), 3);
            DrawRect(texture, battlefield.WorldBounds, battlefield.CameraBounds, new Color(0.2f, 0.82f, 0.96f, 1f), 3);
            foreach (var zone in battlefield.PlacementZones)
            {
                DrawRect(texture, battlefield.WorldBounds, zone.Bounds, new Color(0.18f, 0.72f, 0.4f, 1f), 2);
            }

            foreach (var zone in battlefield.BlockedZones)
            {
                DrawRect(texture, battlefield.WorldBounds, zone.Bounds, new Color(0.92f, 0.2f, 0.18f, 1f), 3);
            }

            var cellHalf = battlefield.PlacementCellSize * 0.34f;
            foreach (var center in battlefield.BuildCellCenters)
            {
                DrawRect(
                    texture,
                    battlefield.WorldBounds,
                    new Rect(center.x - cellHalf, center.y - cellHalf, cellHalf * 2f, cellHalf * 2f),
                    new Color(0.3f, 0.9f, 0.52f, 0.82f),
                    1);
            }

            for (var routeIndex = 0; routeIndex < battlefield.Routes.Length; routeIndex++)
            {
                var route = battlefield.Routes[routeIndex];
                var colors = BattlefieldRouteVisualStyle.GetColors(route.VisualStyleId, routeIndex);
                var thickness = Mathf.Max(3, Mathf.RoundToInt(route.VisualWidth / battlefield.WorldBounds.width * (width - (Padding * 2))));
                for (var pointIndex = 1; pointIndex < route.Points.Length; pointIndex++)
                {
                    DrawLine(
                        texture,
                        ToPixel(battlefield.WorldBounds, route.Points[pointIndex - 1], width, height),
                        ToPixel(battlefield.WorldBounds, route.Points[pointIndex], width, height),
                        colors.End,
                        thickness);
                }

                DrawCircle(texture, ToPixel(battlefield.WorldBounds, route.SpawnAnchor, width, height), 10, colors.Start);
                DrawSquare(texture, ToPixel(battlefield.WorldBounds, route.GoalAnchor, width, height), 9, colors.End);
            }

            foreach (var decoration in battlefield.DecorationAnchors)
            {
                DrawCircle(
                    texture,
                    ToPixel(battlefield.WorldBounds, decoration.Position, width, height),
                    Mathf.Max(4, Mathf.RoundToInt(4f * decoration.Scale)),
                    new Color(1f, 0.86f, 0.22f, 1f));
            }

            texture.Apply(false, false);
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath) ?? ".");
            File.WriteAllBytes(outputPath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }

        private static Vector2Int ToPixel(Rect world, Vector2 point, int width, int height)
        {
            var usableWidth = width - (Padding * 2);
            var usableHeight = height - (Padding * 2);
            return new Vector2Int(
                Padding + Mathf.RoundToInt((point.x - world.xMin) / world.width * usableWidth),
                Padding + Mathf.RoundToInt((point.y - world.yMin) / world.height * usableHeight));
        }

        private static void DrawRect(Texture2D texture, Rect world, Rect rect, Color color, int thickness)
        {
            var min = ToPixel(world, rect.min, texture.width, texture.height);
            var max = ToPixel(world, rect.max, texture.width, texture.height);
            DrawLine(texture, new Vector2Int(min.x, min.y), new Vector2Int(max.x, min.y), color, thickness);
            DrawLine(texture, new Vector2Int(max.x, min.y), new Vector2Int(max.x, max.y), color, thickness);
            DrawLine(texture, new Vector2Int(max.x, max.y), new Vector2Int(min.x, max.y), color, thickness);
            DrawLine(texture, new Vector2Int(min.x, max.y), new Vector2Int(min.x, min.y), color, thickness);
        }

        private static void DrawLine(Texture2D texture, Vector2Int start, Vector2Int end, Color color, int thickness)
        {
            var distance = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(start, end)));
            for (var step = 0; step <= distance; step++)
            {
                var point = Vector2.Lerp(start, end, step / (float)distance);
                DrawCircle(texture, new Vector2Int(Mathf.RoundToInt(point.x), Mathf.RoundToInt(point.y)), thickness, color);
            }
        }

        private static void DrawSquare(Texture2D texture, Vector2Int center, int halfSize, Color color)
        {
            for (var y = -halfSize; y <= halfSize; y++)
            {
                for (var x = -halfSize; x <= halfSize; x++)
                {
                    SetPixel(texture, center.x + x, center.y + y, color);
                }
            }
        }

        private static void DrawCircle(Texture2D texture, Vector2Int center, int radius, Color color)
        {
            var radiusSquared = radius * radius;
            for (var y = -radius; y <= radius; y++)
            {
                for (var x = -radius; x <= radius; x++)
                {
                    if ((x * x) + (y * y) <= radiusSquared)
                    {
                        SetPixel(texture, center.x + x, center.y + y, color);
                    }
                }
            }
        }

        private static void SetPixel(Texture2D texture, int x, int y, Color color)
        {
            if (x >= 0 && x < texture.width && y >= 0 && y < texture.height)
            {
                texture.SetPixel(x, y, color);
            }
        }
    }
}
