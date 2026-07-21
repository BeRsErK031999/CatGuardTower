using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CatGuard.Core.Localization;
using CatGuard.Gameplay.Battlefield;
using CatGuard.Gameplay.Levels;
using UnityEditor;
using UnityEngine;

public static class E2ProjectSetup
{
    private const string BattlefieldFolder = "Assets/_Project/ScriptableObjects/Battlefields";
    private const string LevelFolder = "Assets/_Project/ScriptableObjects/Levels";
    private const string LevelCatalogPath = LevelFolder + "/LevelCatalog.asset";
    private const string GardenPath = BattlefieldFolder + "/GardenGateWide.asset";
    private const string WellPath = BattlefieldFolder + "/OldWellCrossing.asset";
    private const string RooftopPath = BattlefieldFolder + "/RooftopMoonline.asset";

    public static void Run()
    {
        Directory.CreateDirectory(BattlefieldFolder);
        var garden = EnsureGardenGateWide();
        var well = EnsureOldWellCrossing();
        var rooftop = EnsureRooftopMoonline();
        AssignBattlefields(garden, well, rooftop);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        ValidateAndExit();
    }

    public static void Validate()
    {
        ValidateAndExit();
    }

    private static BattlefieldConfig EnsureGardenGateWide()
    {
        var map = EnsureAsset<BattlefieldConfig>(GardenPath);
        map.Configure(
            "garden_gate_wide",
            "Garden Gate Wide",
            "garden_day",
            "garden",
            BattlefieldCameraMode.FixedOverview,
            new Rect(-7f, -4.5f, 14f, 9f),
            new Rect(-7f, -4.5f, 14f, 9f),
            Vector2.zero,
            8.4f,
            new[]
            {
                new Vector2(-6.25f, 2.45f),
                new Vector2(-3.8f, 1.55f),
                new Vector2(-0.7f, 2.05f),
                new Vector2(2.35f, 0.75f),
                new Vector2(1.1f, -1.25f),
                new Vector2(6.15f, -2.45f)
            },
            0.48f,
            new Vector2(-6.25f, 2.45f),
            new Vector2(6.15f, -2.45f),
            1.1f,
            new[]
            {
                new BattlefieldZone("south_garden", new Rect(-5.9f, -3.95f, 11.8f, 2.25f)),
                new BattlefieldZone("west_pocket", new Rect(-6.25f, -1.2f, 2.35f, 2.55f)),
                new BattlefieldZone("east_pocket", new Rect(3.9f, -1.2f, 2.35f, 2.55f)),
                new BattlefieldZone("north_pocket", new Rect(-2.1f, 2.2f, 4.2f, 1.65f))
            },
            new[]
            {
                new BattlefieldZone("guardian_tree", new Rect(-0.75f, -3.65f, 1.5f, 1.45f))
            },
            new[]
            {
                new BattlefieldDecorationAnchor("hedge_west", new Vector2(-5.75f, 3.55f), 1.4f, false),
                new BattlefieldDecorationAnchor("guardian_tree", new Vector2(0f, -2.9f), 1.35f, true),
                new BattlefieldDecorationAnchor("lantern_gate", new Vector2(5.7f, -1.2f), 0.72f, true)
            });
        EditorUtility.SetDirty(map);
        return map;
    }

    private static BattlefieldConfig EnsureOldWellCrossing()
    {
        var map = EnsureAsset<BattlefieldConfig>(WellPath);
        map.Configure(
            "old_well_crossing",
            "Old Well Crossing",
            "old_well_twilight",
            "old_well",
            BattlefieldCameraMode.ScrollableLarge,
            new Rect(-12f, -4.8f, 24f, 9.6f),
            new Rect(-11.5f, -4.3f, 23f, 8.6f),
            new Vector2(-6f, 0f),
            6f,
            new[]
            {
                new Vector2(-11f, 2.5f),
                new Vector2(-8f, 1.2f),
                new Vector2(-4.7f, 1.8f),
                new Vector2(-2f, -0.9f),
                new Vector2(2.2f, -0.8f),
                new Vector2(5.1f, 1.5f),
                new Vector2(8.2f, 0.8f),
                new Vector2(11f, -2.5f)
            },
            0.58f,
            new Vector2(-11f, 2.5f),
            new Vector2(11f, -2.5f),
            1.05f,
            new[]
            {
                new BattlefieldZone("south_crossing", new Rect(-10.9f, -4.05f, 21.8f, 1.9f)),
                new BattlefieldZone("north_west", new Rect(-10.7f, 2.1f, 7.4f, 1.75f)),
                new BattlefieldZone("north_east", new Rect(3.3f, 2f, 7.4f, 1.85f)),
                new BattlefieldZone("west_bank", new Rect(-7.4f, -0.85f, 3.2f, 1.75f)),
                new BattlefieldZone("east_bank", new Rect(4.1f, -0.75f, 3.2f, 1.75f))
            },
            new[]
            {
                new BattlefieldZone("old_well", new Rect(-1.7f, -1.7f, 3.4f, 3.4f)),
                new BattlefieldZone("broken_cart", new Rect(7.55f, -3.55f, 1.65f, 1.25f))
            },
            new[]
            {
                new BattlefieldDecorationAnchor("old_well", Vector2.zero, 2.6f, false),
                new BattlefieldDecorationAnchor("lantern_west", new Vector2(-7.7f, 3.35f), 0.72f, true),
                new BattlefieldDecorationAnchor("lantern_east", new Vector2(7.4f, 3.2f), 0.72f, true),
                new BattlefieldDecorationAnchor("hedge_far_east", new Vector2(10.6f, 0.2f), 1.3f, true)
            });
        EditorUtility.SetDirty(map);
        return map;
    }

    private static BattlefieldConfig EnsureRooftopMoonline()
    {
        var map = EnsureAsset<BattlefieldConfig>(RooftopPath);
        map.Configure(
            "rooftop_moonline",
            "Rooftop Moonline",
            "rooftop_night",
            "rooftop",
            BattlefieldCameraMode.FixedOverview,
            new Rect(-8f, -4.5f, 16f, 9f),
            new Rect(-8f, -4.5f, 16f, 9f),
            new Vector2(0f, -0.1f),
            8.2f,
            new[]
            {
                new Vector2(-7.2f, 2.8f),
                new Vector2(-5.3f, -0.1f),
                new Vector2(-2.1f, -0.6f),
                new Vector2(0.2f, 1.55f),
                new Vector2(3.25f, 0.35f),
                new Vector2(5.1f, -1.25f),
                new Vector2(7.15f, -2.75f)
            },
            0.5f,
            new Vector2(-7.2f, 2.8f),
            new Vector2(7.15f, -2.75f),
            1.05f,
            new[]
            {
                new BattlefieldZone("south_roof", new Rect(-6.8f, -4f, 13.6f, 1.85f)),
                new BattlefieldZone("north_west_roof", new Rect(-6.5f, 1.55f, 3.5f, 2.1f)),
                new BattlefieldZone("north_east_roof", new Rect(2.5f, 1.75f, 3.9f, 1.9f)),
                new BattlefieldZone("center_roof", new Rect(-1.65f, -1.15f, 3.3f, 1.55f))
            },
            new[]
            {
                new BattlefieldZone("chimney", new Rect(-0.85f, -3.55f, 1.7f, 1.45f)),
                new BattlefieldZone("water_tank", new Rect(4.75f, 2.15f, 1.45f, 1.35f))
            },
            new[]
            {
                new BattlefieldDecorationAnchor("moon", new Vector2(6.65f, 3.55f), 1.05f, false),
                new BattlefieldDecorationAnchor("chimney", new Vector2(0f, -2.85f), 1.5f, true),
                new BattlefieldDecorationAnchor("roof_vent_west", new Vector2(-6.2f, -1.6f), 0.95f, true),
                new BattlefieldDecorationAnchor("roof_vent_east", new Vector2(5.6f, 1.1f), 0.9f, true)
            });
        EditorUtility.SetDirty(map);
        return map;
    }

    private static void AssignBattlefields(BattlefieldConfig garden, BattlefieldConfig well, BattlefieldConfig rooftop)
    {
        var assignments = new Dictionary<string, BattlefieldConfig>
        {
            ["level_01"] = garden,
            ["level_02"] = garden,
            ["level_03"] = null,
            ["level_04"] = garden,
            ["level_05"] = garden,
            ["level_06"] = rooftop,
            ["level_07"] = rooftop,
            ["level_08"] = well,
            ["level_09"] = well,
            ["level_10"] = rooftop
        };

        foreach (var level in LoadLevelCatalog().Levels)
        {
            if (level == null || !assignments.TryGetValue(level.LevelId, out var battlefield))
            {
                continue;
            }

            level.ConfigureBattlefield(battlefield);
            EditorUtility.SetDirty(level);
        }
    }

    private static void ValidateAndExit()
    {
        var errors = new List<string>();
        var maps = new[]
        {
            AssetDatabase.LoadAssetAtPath<BattlefieldConfig>(GardenPath),
            AssetDatabase.LoadAssetAtPath<BattlefieldConfig>(WellPath),
            AssetDatabase.LoadAssetAtPath<BattlefieldConfig>(RooftopPath)
        };

        ValidateMaps(maps, errors);
        ValidateLevelMigration(maps, errors);
        ValidateRuntimeBoundaries(errors);
        ValidateLocalization(errors);

        if (errors.Count > 0)
        {
            foreach (var error in errors)
            {
                Debug.LogError(error);
            }

            EditorApplication.Exit(1);
            return;
        }

        Debug.Log("E2 validation passed: three battlefield configs, fixed and scrollable cameras, world-space placement/blocked zones, legacy compatibility, layered presentation, and pan-safe input are configured.");
        EditorApplication.Exit(0);
    }

    private static void ValidateMaps(IReadOnlyCollection<BattlefieldConfig> maps, ICollection<string> errors)
    {
        if (maps.Count != 3 || maps.Any(map => map == null))
        {
            errors.Add("E2 requires Garden Gate Wide, Old Well Crossing, and Rooftop Moonline battlefield assets.");
            return;
        }

        var ids = new HashSet<string>();
        var fixedCount = 0;
        var scrollableCount = 0;
        foreach (var map in maps)
        {
            var definition = map.CreateDefinition();
            if (!definition.IsValid(out var error))
            {
                errors.Add($"Battlefield {map.name} is invalid: {error}");
                continue;
            }

            if (!ids.Add(definition.BattlefieldId))
            {
                errors.Add($"Duplicate battlefield id: {definition.BattlefieldId}.");
            }

            fixedCount += definition.CameraMode == BattlefieldCameraMode.FixedOverview ? 1 : 0;
            scrollableCount += definition.CameraMode == BattlefieldCameraMode.ScrollableLarge ? 1 : 0;
            if (definition.BlockedZones.Length == 0 || definition.DecorationAnchors.Length == 0)
            {
                errors.Add($"Battlefield {definition.BattlefieldId} must define blocked zones and decoration anchors.");
            }

            var cells = definition.BuildCellCenters;
            var minX = cells.Min(point => point.x);
            var maxX = cells.Max(point => point.x);
            var minY = cells.Min(point => point.y);
            var maxY = cells.Max(point => point.y);
            if (maxX - minX < definition.WorldBounds.width * 0.45f
                || maxY - minY < definition.WorldBounds.height * 0.35f)
            {
                errors.Add($"Battlefield {definition.BattlefieldId} must expose build cells near all map edges.");
            }
        }

        if (fixedCount < 1 || scrollableCount < 1)
        {
            errors.Add("E2 requires both Fixed Overview and Scrollable Large camera classes.");
        }

        var largeMap = maps.FirstOrDefault(map => map != null && map.CameraMode == BattlefieldCameraMode.ScrollableLarge);
        if (largeMap == null || largeMap.CameraBounds.width < largeMap.ScrollableViewHeight * (16f / 9f) * 1.35f)
        {
            errors.Add("The scrollable E2 map must be materially wider than a 16:9 gameplay viewport.");
        }
    }

    private static void ValidateLevelMigration(IReadOnlyCollection<BattlefieldConfig> maps, ICollection<string> errors)
    {
        var catalog = LoadLevelCatalog();
        if (catalog == null || catalog.Levels.Length != 10)
        {
            errors.Add("E2 migration requires all ten existing level configs.");
            return;
        }

        var referencedMaps = new HashSet<BattlefieldConfig>();
        var legacyLevels = new List<LevelConfig>();
        foreach (var level in catalog.Levels)
        {
            if (level == null || !level.IsValidForCore())
            {
                errors.Add("Every migrated or legacy level must remain valid for core gameplay.");
                continue;
            }

            if (level.BattlefieldConfig == null)
            {
                legacyLevels.Add(level);
                if (level.ResolveBattlefield()?.IsLegacy != true)
                {
                    errors.Add($"Legacy adapter failed for {level.LevelId}.");
                }
            }
            else
            {
                referencedMaps.Add(level.BattlefieldConfig);
            }
        }

        if (referencedMaps.Count != maps.Count)
        {
            errors.Add("All three E2 battlefield configs must be referenced by the existing campaign.");
        }

        if (legacyLevels.Count != 1 || legacyLevels[0].LevelId != "level_03")
        {
            errors.Add("Level 03 must remain the explicit single-level legacy compatibility path for E2.");
        }
    }

    private static void ValidateRuntimeBoundaries(ICollection<string> errors)
    {
        var levelControllerSource = File.ReadAllText("Assets/_Project/Scripts/Gameplay/Levels/PrototypeLevelController.cs");
        if (levelControllerSource.Contains("config.PathPoints")
            || levelControllerSource.Contains("config.GridOrigin")
            || levelControllerSource.Contains("config.GridColumns"))
        {
            errors.Add("PrototypeLevelController must consume BattlefieldDefinition instead of legacy LevelConfig geometry.");
        }

        var gridSource = File.ReadAllText("Assets/_Project/Scripts/Gameplay/Grid/TowerGrid.cs");
        if (!gridSource.Contains("battlefield.BuildCellCenters") || gridSource.Contains("private void Update()"))
        {
            errors.Add("TowerGrid must use world-space battlefield cells and delegate gestures to BattlefieldInputController.");
        }

        var inputSource = File.ReadAllText("Assets/_Project/Scripts/Gameplay/Camera/BattlefieldInputController.cs");
        if (!inputSource.Contains("pointerDragged") || !inputSource.Contains("PanByScreenDelta"))
        {
            errors.Add("BattlefieldInputController must suppress placement after camera drag.");
        }

        foreach (var layerName in new[]
                 {
                     BattlefieldLayerNames.Background,
                     BattlefieldLayerNames.Terrain,
                     BattlefieldLayerNames.Route,
                     BattlefieldLayerNames.PropsBelowUnits,
                     BattlefieldLayerNames.UnitsAndProjectiles,
                     BattlefieldLayerNames.PropsAboveUnits,
                     BattlefieldLayerNames.Vfx,
                     BattlefieldLayerNames.WorldIndicators
                 })
        {
            if (!levelControllerSource.Contains($"BattlefieldLayerNames.{GetLayerConstantName(layerName)}"))
            {
                errors.Add($"PrototypeLevelController is missing the {layerName} presentation layer.");
            }
        }
    }

    private static void ValidateLocalization(ICollection<string> errors)
    {
        var originalLanguage = LocalizationService.CurrentLanguageCode;
        try
        {
            foreach (var language in new[] { LocalizationService.English, LocalizationService.Russian })
            {
                LocalizationService.SetLanguage(language);
                foreach (var key in new[] { "hud.spawnShort", "hud.goalShort", "hud.cameraPan" })
                {
                    if (LocalizationService.Text(key) == key)
                    {
                        errors.Add($"E2 localization is missing {key} for {language}.");
                    }
                }
            }
        }
        finally
        {
            LocalizationService.SetLanguage(originalLanguage);
        }
    }

    private static string GetLayerConstantName(string layerName)
    {
        return layerName switch
        {
            "Background" => nameof(BattlefieldLayerNames.Background),
            "Terrain" => nameof(BattlefieldLayerNames.Terrain),
            "Route" => nameof(BattlefieldLayerNames.Route),
            "PropsBelowUnits" => nameof(BattlefieldLayerNames.PropsBelowUnits),
            "UnitsAndProjectiles" => nameof(BattlefieldLayerNames.UnitsAndProjectiles),
            "PropsAboveUnits" => nameof(BattlefieldLayerNames.PropsAboveUnits),
            "VFX" => nameof(BattlefieldLayerNames.Vfx),
            _ => nameof(BattlefieldLayerNames.WorldIndicators)
        };
    }

    private static LevelCatalogConfig LoadLevelCatalog()
    {
        return AssetDatabase.LoadAssetAtPath<LevelCatalogConfig>(LevelCatalogPath);
    }

    private static TAsset EnsureAsset<TAsset>(string path)
        where TAsset : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<TAsset>(path);
        if (asset != null)
        {
            return asset;
        }

        asset = ScriptableObject.CreateInstance<TAsset>();
        asset.name = Path.GetFileNameWithoutExtension(path);
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }
}
