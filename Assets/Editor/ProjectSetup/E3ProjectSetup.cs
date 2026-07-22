using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CatGuard.Core.Localization;
using CatGuard.Gameplay.Battlefield;
using CatGuard.Gameplay.Levels;
using CatGuard.Gameplay.Towers;
using CatGuard.Gameplay.Waves;
using CatGuard.SDK.Analytics;
using UnityEditor;
using UnityEngine;

public static class E3ProjectSetup
{
    private const string BattlefieldFolder = "Assets/_Project/ScriptableObjects/Battlefields";
    private const string LevelFolder = "Assets/_Project/ScriptableObjects/Levels";
    private const string LevelCatalogPath = LevelFolder + "/LevelCatalog.asset";
    private const string GardenPath = BattlefieldFolder + "/GardenGateWide.asset";
    private const string WellPath = BattlefieldFolder + "/OldWellCrossing.asset";
    private const string RooftopPath = BattlefieldFolder + "/RooftopMoonline.asset";

    public static void Run()
    {
        var garden = LoadRequired<BattlefieldConfig>(GardenPath);
        var well = LoadRequired<BattlefieldConfig>(WellPath);
        var rooftop = LoadRequired<BattlefieldConfig>(RooftopPath);
        ConfigureGardenRoutes(garden);
        ConfigureOldWellRoutes(well);
        ConfigureRooftopRoutes(rooftop);
        ConfigureWaveRoutes();
        ConfigureTowerTargetPriorities();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        ValidateAndExit();
    }

    public static void Validate()
    {
        ValidateAndExit();
    }

    private static void ConfigureGardenRoutes(BattlefieldConfig map)
    {
        var points = map.PathPoints;
        map.ConfigureRoutes(new[]
        {
            new PathRouteConfig(
                "main",
                "Garden Main",
                points,
                points[0],
                points[^1],
                "garden_main",
                map.PathVisualWidth,
                1f,
                new[] { "ground", "control" },
                0f,
                "E3 single-route regression control.")
        });
        EditorUtility.SetDirty(map);
    }

    private static void ConfigureOldWellRoutes(BattlefieldConfig map)
    {
        map.ConfigureRoutes(new[]
        {
            new PathRouteConfig(
                "north_lane",
                "North Lane",
                new[]
                {
                    new Vector2(-11f, 2.55f),
                    new Vector2(-7.7f, 1.45f),
                    new Vector2(-4.4f, 2.35f),
                    new Vector2(-1.9f, 1.75f),
                    new Vector2(2.1f, 1.75f),
                    new Vector2(5.1f, 2.3f),
                    new Vector2(8.2f, 1.45f),
                    new Vector2(11f, 2.55f)
                },
                new Vector2(-11f, 2.55f),
                new Vector2(11f, 2.55f),
                "well_north",
                0.48f,
                1f,
                new[] { "ground", "lane" },
                0f,
                "Separate northern lane."),
            new PathRouteConfig(
                "south_lane",
                "South Lane",
                new[]
                {
                    new Vector2(-11f, -2.55f),
                    new Vector2(-8f, -1.25f),
                    new Vector2(-4.8f, -2.05f),
                    new Vector2(-2f, -2.45f),
                    new Vector2(2f, -2.45f),
                    new Vector2(5f, -1.3f),
                    new Vector2(8.1f, -1.7f),
                    new Vector2(11f, -2.55f)
                },
                new Vector2(-11f, -2.55f),
                new Vector2(11f, -2.55f),
                "well_south",
                0.48f,
                1f,
                new[] { "ground", "lane" },
                0f,
                "Separate southern lane."),
            new PathRouteConfig(
                "well_boss",
                "Well Boss Route",
                new[]
                {
                    new Vector2(-11f, 0f),
                    new Vector2(-7f, 0.45f),
                    new Vector2(-3f, 0.55f),
                    new Vector2(-1.85f, 1.85f),
                    new Vector2(1.85f, 1.85f),
                    new Vector2(3.2f, 0.35f),
                    new Vector2(7.1f, 0.2f),
                    new Vector2(11f, 0f)
                },
                new Vector2(-11f, 0f),
                new Vector2(11f, 0f),
                "well_boss",
                0.6f,
                0.45f,
                new[] { "ground", "boss" },
                0.35f,
                "Boss-only route around the old well.")
        });
        EditorUtility.SetDirty(map);
    }

    private static void ConfigureRooftopRoutes(BattlefieldConfig map)
    {
        var sharedSpawn = new Vector2(-7.2f, 2.8f);
        var sharedGoal = new Vector2(7.15f, -2.75f);
        map.ConfigureRoutes(new[]
        {
            new PathRouteConfig(
                "west_branch",
                "West Roof Branch",
                new[]
                {
                    sharedSpawn,
                    new Vector2(-5.3f, -0.1f),
                    new Vector2(-2.1f, -0.6f),
                    new Vector2(0.2f, 1.55f),
                    new Vector2(3.25f, 0.35f),
                    new Vector2(5.1f, -1.25f),
                    sharedGoal
                },
                sharedSpawn,
                sharedGoal,
                "roof_west",
                0.44f,
                1f,
                new[] { "ground", "fork" },
                0f,
                "Shared spawn and goal; lower western branch."),
            new PathRouteConfig(
                "moon_branch",
                "Moon Roof Branch",
                new[]
                {
                    sharedSpawn,
                    new Vector2(-5.3f, -0.1f),
                    new Vector2(-2.2f, 1.9f),
                    new Vector2(1f, 2.45f),
                    new Vector2(3.55f, 1.05f),
                    new Vector2(5.1f, -1.25f),
                    sharedGoal
                },
                sharedSpawn,
                sharedGoal,
                "roof_moon",
                0.44f,
                1f,
                new[] { "ground", "fork" },
                0f,
                "Shared spawn and goal; upper moon branch."),
            new PathRouteConfig(
                "chimney_boss",
                "Chimney Boss Route",
                new[]
                {
                    sharedSpawn,
                    new Vector2(-5.3f, -0.1f),
                    new Vector2(-2.4f, -1.35f),
                    new Vector2(0f, -1.75f),
                    new Vector2(3.2f, -1.7f),
                    new Vector2(5.1f, -1.25f),
                    sharedGoal
                },
                sharedSpawn,
                sharedGoal,
                "chimney_boss",
                0.58f,
                0.4f,
                new[] { "ground", "boss", "fork" },
                0.3f,
                "Boss-only central chimney branch.")
        });
        EditorUtility.SetDirty(map);
    }

    private static void ConfigureWaveRoutes()
    {
        var catalog = LoadRequired<LevelCatalogConfig>(LevelCatalogPath);
        foreach (var level in catalog.Levels)
        {
            if (level?.WaveConfig == null)
            {
                continue;
            }

            switch (level.BattlefieldConfig?.BattlefieldId)
            {
                case "old_well_crossing":
                    ConfigureMultiRouteWave(level.WaveConfig, new[] { "north_lane", "south_lane" }, "well_boss");
                    break;
                case "rooftop_moonline":
                    ConfigureMultiRouteWave(level.WaveConfig, new[] { "west_branch", "moon_branch" }, "chimney_boss");
                    break;
                default:
                    ConfigureControlWave(level.WaveConfig);
                    break;
            }

            EditorUtility.SetDirty(level.WaveConfig);
        }
    }

    private static void ConfigureControlWave(WaveConfig wave)
    {
        wave.ConfigureConcurrentGroups(false);
        foreach (var group in wave.Groups)
        {
            group?.ConfigureRoute("main");
        }
    }

    private static void ConfigureMultiRouteWave(WaveConfig wave, IReadOnlyList<string> lanes, string bossRouteId)
    {
        wave.ConfigureConcurrentGroups(true);
        var laneIndex = 0;
        foreach (var group in wave.Groups)
        {
            if (group?.EnemyConfig == null)
            {
                continue;
            }

            if (group.EnemyConfig.EnemyId == "snail_tank")
            {
                group.ConfigureRoute(bossRouteId, 0.4f);
                continue;
            }

            var routeId = lanes[laneIndex % lanes.Count];
            var delay = laneIndex < 2 ? 0f : (laneIndex - 1) * 0.35f;
            group.ConfigureRoute(routeId, delay);
            laneIndex++;
        }
    }

    private static void ConfigureTowerTargetPriorities()
    {
        foreach (var tower in LoadAssets<TowerConfig>())
        {
            var priority = tower.TowerId switch
            {
                "yarn_cannon" => TowerTargetPriority.Last,
                "bell_sniper" => TowerTargetPriority.Strong,
                "blanket_boom" => TowerTargetPriority.Strong,
                _ => TowerTargetPriority.First
            };
            tower.ConfigureTargetPriority(priority);
            EditorUtility.SetDirty(tower);
        }
    }

    private static void ValidateAndExit()
    {
        var errors = new List<string>();
        var garden = AssetDatabase.LoadAssetAtPath<BattlefieldConfig>(GardenPath);
        var well = AssetDatabase.LoadAssetAtPath<BattlefieldConfig>(WellPath);
        var rooftop = AssetDatabase.LoadAssetAtPath<BattlefieldConfig>(RooftopPath);
        ValidateRouteLayouts(garden, well, rooftop, errors);
        ValidateWaveReferences(errors);
        ValidateTargeting(errors);
        ValidateAnalytics(errors);
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

        Debug.Log("E3 validation passed: explicit multi-route waves, normalized lane-agnostic targeting, route analytics, legacy main-route migration, and control/two-lane/forked content are configured.");
        EditorApplication.Exit(0);
    }

    private static void ValidateRouteLayouts(
        BattlefieldConfig garden,
        BattlefieldConfig well,
        BattlefieldConfig rooftop,
        ICollection<string> errors)
    {
        var maps = new[] { garden, well, rooftop };
        if (maps.Any(map => map == null))
        {
            errors.Add("E3 requires all three E2 battlefield assets.");
            return;
        }

        foreach (var map in maps)
        {
            var definition = map.CreateDefinition();
            if (!definition.IsValid(out var error))
            {
                errors.Add($"Battlefield {map.name} route validation failed: {error}");
            }

            if (map.RouteConfigs.Length == 0 || definition.UsesLegacyRoute)
            {
                errors.Add($"Battlefield {map.name} must persist explicit E3 route configs.");
            }
        }

        if (garden.CreateDefinition().Routes.Length != 1 || garden.CreateDefinition().PrimaryRoute.RouteId != "main")
        {
            errors.Add("Garden Gate Wide must remain the single-route main regression control.");
        }

        var wellRoutes = well.CreateDefinition().Routes;
        if (wellRoutes.Length < 3
            || wellRoutes[0].SpawnAnchor == wellRoutes[1].SpawnAnchor
            || wellRoutes[0].GoalAnchor == wellRoutes[1].GoalAnchor)
        {
            errors.Add("Old Well Crossing must expose separate north/south spawns and goals plus a boss route.");
        }

        var roofRoutes = rooftop.CreateDefinition().Routes;
        if (roofRoutes.Length < 3
            || roofRoutes[0].SpawnAnchor != roofRoutes[1].SpawnAnchor
            || roofRoutes[0].GoalAnchor != roofRoutes[1].GoalAnchor)
        {
            errors.Add("Rooftop Moonline must expose forked routes with a shared spawn and goal.");
        }

        if (!wellRoutes.Any(route => route.HasTag("boss")) || !roofRoutes.Any(route => route.HasTag("boss")))
        {
            errors.Add("Both multi-route slices require an explicit boss-only tagged route.");
        }
    }

    private static void ValidateWaveReferences(ICollection<string> errors)
    {
        var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalogConfig>(LevelCatalogPath);
        if (catalog == null || catalog.Levels.Length < 10)
        {
            errors.Add("E3 migration requires the original ten campaign levels.");
            return;
        }

        var expandedCampaign = catalog.Levels.Length > 10;

        var simultaneousMultiRouteWave = false;
        var bossRouteReferences = 0;
        foreach (var level in catalog.Levels)
        {
            var battlefield = level?.ResolveBattlefield();
            var routeError = "Level, battlefield, or wave is missing.";
            if (level == null
                || battlefield == null
                || level.WaveConfig == null
                || !level.WaveConfig.IsValid(battlefield, out routeError))
            {
                errors.Add($"Level {level?.LevelId ?? "missing"} has invalid route references: {routeError}");
                continue;
            }

            var referencedRoutes = level.WaveConfig.GetReferencedRouteIds();
            simultaneousMultiRouteWave |= level.WaveConfig.ConcurrentGroups && referencedRoutes.Count >= 2;
            foreach (var group in level.WaveConfig.Groups)
            {
                var routeId = group.GetConfiguredRouteId(0);
                if (!battlefield.TryGetRoute(routeId, out var route))
                {
                    continue;
                }

                if (route.HasTag("boss"))
                {
                    bossRouteReferences++;
                    var isOriginalE3Route = route.RouteId == "well_boss" || route.RouteId == "chimney_boss";
                    if (isOriginalE3Route && group.EnemyConfig.EnemyId != "snail_tank")
                    {
                        errors.Add($"Boss-only route {route.RouteId} must only reference snail_tank in the E3 slice.");
                    }
                }
            }
        }

        if (!simultaneousMultiRouteWave)
        {
            errors.Add("At least one E3 wave must run groups concurrently on multiple explicit routes.");
        }

        if (!expandedCampaign && bossRouteReferences < 2)
        {
            errors.Add("E3 content must reference boss-only routes on both multi-route layouts.");
        }
    }

    private static void ValidateTargeting(ICollection<string> errors)
    {
        var rear = new EnemyTargetingMetrics("short", 0.2f, 10f, 2f, 1);
        var front = new EnemyTargetingMetrics("long", 0.8f, 8f, 3f, 2);
        var strong = new EnemyTargetingMetrics("boss", 0.45f, 90f, 4f, 3);
        if (!EnemyTargeting.IsBetter(TowerTargetPriority.First, front, rear)
            || !EnemyTargeting.IsBetter(TowerTargetPriority.Last, rear, front)
            || !EnemyTargeting.IsBetter(TowerTargetPriority.Strong, strong, front))
        {
            errors.Add("First/Last/Strong targeting must compare normalized progress or strength across route lengths.");
        }

        var priorities = new HashSet<TowerTargetPriority>(LoadAssets<TowerConfig>().Select(tower => tower.TargetPriority));
        if (!priorities.Contains(TowerTargetPriority.First)
            || !priorities.Contains(TowerTargetPriority.Last)
            || !priorities.Contains(TowerTargetPriority.Strong))
        {
            errors.Add("The existing tower roster must exercise First, Last, and Strong targeting priorities.");
        }
    }

    private static void ValidateAnalytics(ICollection<string> errors)
    {
        var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalogConfig>(LevelCatalogPath);
        var level = catalog?.Levels.FirstOrDefault(candidate => candidate?.ResolveBattlefield()?.Routes.Length > 1)
            ?? catalog?.Levels.FirstOrDefault(candidate => candidate?.BattlefieldConfig?.BattlefieldId == "old_well_crossing");
        var route = level?.ResolveBattlefield()?.Routes.FirstOrDefault();
        var enemy = level?.WaveConfig?.Groups.FirstOrDefault()?.EnemyConfig;
        if (level == null || route == null || enemy == null)
        {
            errors.Add("E3 analytics validation cannot resolve route content.");
            return;
        }

        var fake = new FakeAnalyticsService();
        AnalyticsService.ResetForValidation(fake);
        AnalyticsService.TrackEnemySpawn(level, enemy, route, 1);
        var record = fake.Events.LastOrDefault(item => item.Name == AnalyticsEventNames.EnemySpawn);
        if (record == null
            || !record.Parameters.TryGetValue(AnalyticsParameterNames.RouteId, out var routeValue)
            || !string.Equals(routeValue?.ToString(), route.RouteId, StringComparison.Ordinal))
        {
            errors.Add("Enemy route id must be present in the fake analytics payload.");
        }
    }

    private static void ValidateRuntimeBoundaries(ICollection<string> errors)
    {
        var controller = File.ReadAllText("Assets/_Project/Scripts/Gameplay/Levels/PrototypeLevelController.cs");
        var enemy = File.ReadAllText("Assets/_Project/Scripts/Gameplay/Enemies/BasicEnemy.cs");
        var spawner = File.ReadAllText("Assets/_Project/Scripts/Gameplay/Waves/PrototypeWaveSpawner.cs");
        if (controller.Contains("Battlefield.PathPoints") || controller.Contains("FindNearestEnemy"))
        {
            errors.Add("E3 runtime must not read a single battlefield path or use nearest-only targeting.");
        }

        if (!enemy.Contains("PathRouteDefinition") || !enemy.Contains("NormalizedProgress"))
        {
            errors.Add("BasicEnemy must retain its assigned route and expose normalized progress.");
        }

        if (!spawner.Contains("GetConfiguredRouteId") || !spawner.Contains("RegisterIncomingRoute"))
        {
            errors.Add("Wave spawner must resolve explicit/deterministic routes and emit route warnings.");
        }

        if (!File.Exists("tools/android/run-emulator-route-qa.ps1"))
        {
            errors.Add("E3 Android route QA runner is missing.");
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
                if (LocalizationService.Text("hud.waveIncoming") == "hud.waveIncoming")
                {
                    errors.Add($"E3 localization is missing hud.waveIncoming for {language}.");
                }
            }
        }
        finally
        {
            LocalizationService.SetLanguage(originalLanguage);
        }
    }

    private static T LoadRequired<T>(string path)
        where T : UnityEngine.Object
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            throw new InvalidDataException($"Required E3 asset is missing: {path}");
        }

        return asset;
    }

    private static T[] LoadAssets<T>()
        where T : UnityEngine.Object
    {
        return AssetDatabase.FindAssets($"t:{typeof(T).Name}")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<T>)
            .Where(asset => asset != null)
            .ToArray();
    }
}
