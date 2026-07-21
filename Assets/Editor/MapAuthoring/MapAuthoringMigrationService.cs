using System;
using System.IO;
using CatGuard.Gameplay.Battlefield;
using CatGuard.Gameplay.Levels;
using UnityEditor;
using UnityEngine;

namespace CatGuard.EditorTools.MapAuthoring
{
    public static class MapAuthoringMigrationService
    {
        public static BattlefieldConfig CreateMigratedBattlefieldInMemory(LevelConfig legacyLevel)
        {
            var target = ScriptableObject.CreateInstance<BattlefieldConfig>();
            target.name = legacyLevel == null ? "MigratedBattlefield" : $"{legacyLevel.name}Battlefield";
            ConfigureMigratedBattlefield(target, legacyLevel);
            return target;
        }

        public static BattlefieldConfig MigrateLegacyLevelToAsset(LevelConfig legacyLevel, string assetPath)
        {
            if (legacyLevel == null)
            {
                throw new ArgumentNullException(nameof(legacyLevel));
            }

            if (!legacyLevel.UsesLegacyBattlefield)
            {
                throw new InvalidOperationException($"Level '{legacyLevel.LevelId}' already references a BattlefieldConfig.");
            }

            if (string.IsNullOrWhiteSpace(assetPath)
                || !assetPath.Replace('\\', '/').StartsWith("Assets/", StringComparison.Ordinal)
                || !assetPath.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Migration target must be an .asset path inside Assets/.", nameof(assetPath));
            }

            var existing = AssetDatabase.LoadAssetAtPath<BattlefieldConfig>(assetPath);
            if (existing != null)
            {
                throw new IOException($"Migration target already exists: {assetPath}");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(assetPath) ?? "Assets");
            var target = ScriptableObject.CreateInstance<BattlefieldConfig>();
            target.name = Path.GetFileNameWithoutExtension(assetPath);
            ConfigureMigratedBattlefield(target, legacyLevel);
            AssetDatabase.CreateAsset(target, assetPath);
            legacyLevel.ConfigureBattlefield(target);
            EditorUtility.SetDirty(legacyLevel);
            AssetDatabase.SaveAssets();
            return target;
        }

        public static bool MigrateLegacyRouteInPlace(BattlefieldConfig battlefield)
        {
            if (battlefield == null || battlefield.RouteConfigs.Length > 0 || battlefield.PathPoints.Length < 2)
            {
                return false;
            }

            battlefield.ConfigureRoutes(new[]
            {
                new PathRouteConfig(
                    "main",
                    "Main Route",
                    battlefield.PathPoints,
                    battlefield.SpawnPresentationAnchor,
                    battlefield.GoalPresentationAnchor,
                    "legacy_main",
                    battlefield.PathVisualWidth,
                    1f,
                    new[] { "ground", "legacy" },
                    0f,
                    "Migrated explicitly by the E4 authoring helper.")
            });
            EditorUtility.SetDirty(battlefield);
            return true;
        }

        private static void ConfigureMigratedBattlefield(BattlefieldConfig target, LevelConfig legacyLevel)
        {
            var source = legacyLevel?.ResolveBattlefield();
            if (source == null || source.PrimaryRoute == null)
            {
                throw new InvalidDataException("Legacy level does not contain valid grid/path geometry.");
            }

            var route = source.PrimaryRoute;
            target.Configure(
                $"migrated_{legacyLevel.LevelId}",
                $"Migrated {legacyLevel.DisplayName}",
                source.BackgroundId,
                source.BiomeId,
                source.CameraMode,
                source.WorldBounds,
                source.CameraBounds,
                source.InitialCameraFocus,
                source.ScrollableViewHeight,
                route.Points,
                route.VisualWidth,
                route.SpawnAnchor,
                route.GoalAnchor,
                source.PlacementCellSize,
                source.PlacementZones,
                source.BlockedZones,
                source.DecorationAnchors);
            target.ConfigureRoutes(new[]
            {
                new PathRouteConfig(
                    "main",
                    "Main Route",
                    route.Points,
                    route.SpawnAnchor,
                    route.GoalAnchor,
                    "legacy_main",
                    route.VisualWidth,
                    1f,
                    new[] { "ground", "legacy" },
                    0f,
                    $"Migrated from LevelConfig '{legacyLevel.LevelId}' by the E4 authoring helper.")
            });
            target.ConfigureDesignCard(
                "Migration review required",
                "Legacy single route migrated to explicit route 'main'.",
                new[] { "Verify legacy placement coverage" },
                "Preserves the original legacy wave and route pressure.",
                "Review future ultimate coverage after migration.",
                "Verify contrast and safe placement after migration.");
        }
    }
}
