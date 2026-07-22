using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CatGuard.EditorTools.MapAuthoring;
using CatGuard.Gameplay.Battlefield;
using CatGuard.Gameplay.Levels;
using CatGuard.Gameplay.Waves;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class E4ProjectSetup
{
    private const string BattlefieldFolder = "Assets/_Project/ScriptableObjects/Battlefields";
    private const string LevelFolder = "Assets/_Project/ScriptableObjects/Levels";
    private const string AuthoringFolder = "Assets/_Project/ScriptableObjects/Authoring";
    private const string LevelCatalogPath = LevelFolder + "/LevelCatalog.asset";
    private const string GardenPath = BattlefieldFolder + "/GardenGateWide.asset";
    private const string WellPath = BattlefieldFolder + "/OldWellCrossing.asset";
    private const string RooftopPath = BattlefieldFolder + "/RooftopMoonline.asset";
    private const string SandboxMapPath = AuthoringFolder + "/E4AuthoringSandboxBattlefield.asset";
    private const string SandboxLevelPath = AuthoringFolder + "/E4AuthoringSandboxLevel.asset";
    private const string WorkflowPath = "docs/planning/MAP_AUTHORING_WORKFLOW.md";

    public static void Run()
    {
        var garden = LoadRequired<BattlefieldConfig>(GardenPath);
        var well = LoadRequired<BattlefieldConfig>(WellPath);
        var rooftop = LoadRequired<BattlefieldConfig>(RooftopPath);
        ConfigureDesignCards(garden, well, rooftop);

        var template = LoadRequired<LevelCatalogConfig>(LevelCatalogPath).FindById("level_01");
        var sandbox = MapAuthoringAssetFactory.CreateOrUpdateStarterLevel(
            AuthoringFolder,
            "E4AuthoringSandbox",
            "e4_authoring_sandbox",
            "level_e4_authoring_sandbox",
            "E4 Authoring Sandbox",
            template);
        MapAuthoringPreviewSceneService.EnsurePreviewScene(sandbox.Battlefield);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        ValidateAndExit();
    }

    public static void Validate()
    {
        ValidateAndExit();
    }

    public static void CapturePreviewEvidence()
    {
        var outputFolder = "Builds/Android/qa-device/e4-authoring-preview";
        Directory.CreateDirectory(outputFolder);
        foreach (var path in new[] { GardenPath, WellPath, RooftopPath, SandboxMapPath })
        {
            var battlefield = LoadRequired<BattlefieldConfig>(path);
            var outputPath = Path.Combine(outputFolder, $"{battlefield.BattlefieldId}.png");
            MapAuthoringPreviewExporter.Export(battlefield, outputPath);
            Debug.Log($"E4 authoring preview exported: {outputPath}");
        }

        EditorApplication.Exit(0);
    }

    private static void ConfigureDesignCards(
        BattlefieldConfig garden,
        BattlefieldConfig well,
        BattlefieldConfig rooftop)
    {
        garden.ConfigureDesignCard(
            "Intro / low",
            "Single readable route used as the control layout for onboarding and regression.",
            new[] { "Short-range coverage at bends", "Long-range coverage across the center" },
            "A single predictable stream that teaches route progress.",
            "Future ultimates can cover the central bend without requiring precision.",
            "One high-contrast route, large placement pockets, and stable fixed camera framing.");
        well.ConfigureDesignCard(
            "Campaign / medium-high",
            "Two separated lanes plus a central boss route on a horizontally scrollable map.",
            new[] { "Lane-specialized towers", "Long-range cross-lane support", "Strong targeting on the boss lane" },
            "Simultaneous pressure at distant goals with a durable central threat.",
            "Future map-scale control rewards timing while both lane fronts are visible.",
            "Distinct vertical lanes, numbered endpoints, route colors, and bounded camera panning.");
        rooftop.ConfigureDesignCard(
            "Campaign / medium",
            "Shared spawn and goal split into visually distinct forked routes around rooftop props.",
            new[] { "Splash near shared endpoints", "Long-range coverage across the fork", "Last targeting for leak control" },
            "Uneven route lengths create timing offsets before enemies reconverge.",
            "Future ultimates are legible at the fork center or shared goal.",
            "Shared endpoint markers are deduplicated and route colors remain distinct at crossings.");

        EditorUtility.SetDirty(garden);
        EditorUtility.SetDirty(well);
        EditorUtility.SetDirty(rooftop);
    }

    private static void ValidateAndExit()
    {
        var errors = new List<string>();
        var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalogConfig>(LevelCatalogPath);
        var garden = AssetDatabase.LoadAssetAtPath<BattlefieldConfig>(GardenPath);
        var well = AssetDatabase.LoadAssetAtPath<BattlefieldConfig>(WellPath);
        var rooftop = AssetDatabase.LoadAssetAtPath<BattlefieldConfig>(RooftopPath);
        var sandboxMap = AssetDatabase.LoadAssetAtPath<BattlefieldConfig>(SandboxMapPath);
        var sandboxLevel = AssetDatabase.LoadAssetAtPath<LevelConfig>(SandboxLevelPath);

        ValidateVerticalSlices(catalog, garden, well, rooftop, sandboxMap, sandboxLevel, errors);
        ValidateNegativeFixtures(catalog, garden, errors);
        ValidateMigrationHelper(catalog, errors);
        ValidatePreviewContract(sandboxMap, errors);
        ValidateRuntimeBoundary(errors);
        ValidateDocumentation(errors);

        if (errors.Count > 0)
        {
            foreach (var error in errors)
            {
                Debug.LogError(error);
            }

            EditorApplication.Exit(1);
            return;
        }

        Debug.Log("E4 validation passed: three vertical-slice maps and a factory-built sandbox are valid; asset-path diagnostics, critical negative fixtures, preview/handles, legacy migration, and map-independent runtime boundaries are configured.");
        EditorApplication.Exit(0);
    }

    private static void ValidateVerticalSlices(
        LevelCatalogConfig catalog,
        BattlefieldConfig garden,
        BattlefieldConfig well,
        BattlefieldConfig rooftop,
        BattlefieldConfig sandboxMap,
        LevelConfig sandboxLevel,
        ICollection<string> errors)
    {
        if (catalog == null || garden == null || well == null || rooftop == null)
        {
            errors.Add("E4 requires the E3 catalog and all three vertical-slice battlefields.");
            return;
        }

        var representativeLevels = new[]
        {
            catalog.FindById("level_01"),
            catalog.FindById("level_08"),
            catalog.FindById("level_07")
        };
        var expectedMaps = new[] { garden, well, rooftop };
        var expandedCampaign = catalog.Levels.Length > 10;
        for (var index = 0; index < representativeLevels.Length; index++)
        {
            var level = representativeLevels[index];
            if (!expandedCampaign && level?.BattlefieldConfig != expectedMaps[index])
            {
                errors.Add($"E4 representative level {index} does not reference the expected vertical-slice map.");
                continue;
            }

            AppendUnexpectedIssues(
                expandedCampaign
                    ? MapAuthoringValidator.ValidateBattlefield(expectedMaps[index])
                    : MapAuthoringValidator.ValidateLevel(level),
                errors);
        }

        if (sandboxMap == null || sandboxLevel == null || sandboxLevel.BattlefieldConfig != sandboxMap)
        {
            errors.Add("E4 starter factory must persist its sandbox battlefield, wave, and level assets.");
        }
        else
        {
            AppendUnexpectedIssues(MapAuthoringValidator.ValidateLevel(sandboxLevel), errors);
            if (catalog.Levels.Contains(sandboxLevel))
            {
                errors.Add("The E4 authoring sandbox must not alter the ten-level player campaign catalog.");
            }
        }
    }

    private static void ValidateNegativeFixtures(
        LevelCatalogConfig catalog,
        BattlefieldConfig baselineMap,
        ICollection<string> errors)
    {
        var baselineLevel = catalog?.FindById("level_01");
        if (baselineLevel == null || baselineMap == null)
        {
            errors.Add("E4 negative fixtures require the level_01 baseline.");
            return;
        }

        AssertBattlefieldCode(
            baselineMap,
            "route-point-count",
            MapAuthoringValidator.RoutePointCountCode,
            map => map.ConfigureRoutes(new[]
            {
                CreateRoute("main", new[] { baselineMap.RouteConfigs[0].Points[0] })
            }),
            errors);

        AssertBattlefieldCode(
            baselineMap,
            "duplicate-route-id",
            MapAuthoringValidator.RouteDuplicateIdCode,
            map => map.ConfigureRoutes(new[]
            {
                baselineMap.RouteConfigs[0],
                baselineMap.RouteConfigs[0]
            }),
            errors);

        AssertBattlefieldCode(
            baselineMap,
            "route-world-bounds",
            MapAuthoringValidator.RouteWorldBoundsCode,
            map =>
            {
                var points = (Vector2[])baselineMap.RouteConfigs[0].Points.Clone();
                points[1] = new Vector2(baselineMap.WorldBounds.xMax + 2f, points[1].y);
                map.ConfigureRoutes(new[] { CreateRoute("main", points) });
            },
            errors);

        AssertBattlefieldCode(
            baselineMap,
            "route-exception-reason",
            MapAuthoringValidator.RouteExceptionCode,
            map =>
            {
                var baselineRoute = baselineMap.RouteConfigs[0];
                var points = (Vector2[])baselineRoute.Points.Clone();
                points[1] = new Vector2(baselineMap.WorldBounds.xMax + 2f, points[1].y);
                map.ConfigureRoutes(new[]
                {
                    new PathRouteConfig(
                        "main",
                        "Undocumented Exception Route",
                        points,
                        baselineRoute.SpawnAnchor,
                        baselineRoute.GoalAnchor,
                        "main",
                        baselineRoute.VisualWidth,
                        1f,
                        new[] { "ground" },
                        0f,
                        string.Empty,
                        true)
                });
            },
            errors);

        AssertBattlefieldCode(
            baselineMap,
            "route-spawn-reachability",
            MapAuthoringValidator.RouteSpawnReachabilityCode,
            map =>
            {
                var baselineRoute = baselineMap.RouteConfigs[0];
                var points = (Vector2[])baselineRoute.Points.Clone();
                points[0] += Vector2.right * 1.5f;
                map.ConfigureRoutes(new[]
                {
                    new PathRouteConfig(
                        "main",
                        "Broken Spawn Route",
                        points,
                        baselineRoute.SpawnAnchor,
                        baselineRoute.GoalAnchor,
                        "main",
                        baselineRoute.VisualWidth,
                        1f,
                        new[] { "ground" })
                });
            },
            errors);

        AssertBattlefieldCode(
            baselineMap,
            "route-goal-reachability",
            MapAuthoringValidator.RouteGoalReachabilityCode,
            map =>
            {
                var points = (Vector2[])baselineMap.RouteConfigs[0].Points.Clone();
                points[^1] += Vector2.left * 1.5f;
                map.ConfigureRoutes(new[]
                {
                    new PathRouteConfig(
                        "main",
                        "Broken Goal Route",
                        points,
                        points[0],
                        baselineMap.RouteConfigs[0].GoalAnchor,
                        "main",
                        0.48f,
                        1f,
                        new[] { "ground" })
                });
            },
            errors);

        AssertBattlefieldCode(
            baselineMap,
            "route-degenerate-segment",
            MapAuthoringValidator.RouteDegenerateSegmentCode,
            map =>
            {
                var points = (Vector2[])baselineMap.RouteConfigs[0].Points.Clone();
                points[1] = points[0];
                map.ConfigureRoutes(new[] { CreateRoute("main", points) });
            },
            errors);

        AssertBattlefieldCode(
            baselineMap,
            "placement-path",
            MapAuthoringValidator.PlacementPathCode,
            map =>
            {
                ReconfigureGeometry(
                    map,
                    baselineMap.CameraBounds,
                    new[]
                    {
                        new BattlefieldZone("route_cell", new Rect(-6.8f, 1.9f, 1.1f, 1.1f))
                    },
                    Array.Empty<BattlefieldZone>());
            },
            errors);

        AssertBattlefieldCode(
            baselineMap,
            "placement-blocked",
            MapAuthoringValidator.PlacementBlockedCode,
            map =>
            {
                ReconfigureGeometry(
                    map,
                    baselineMap.CameraBounds,
                    new[]
                    {
                        new BattlefieldZone("near_blocked", new Rect(-4.55f, -3.85f, 1.1f, 1.1f))
                    },
                    new[]
                    {
                        new BattlefieldZone("edge_block", new Rect(-3.99f, -3.4f, 0.2f, 0.2f))
                    });
            },
            errors);

        AssertBattlefieldCode(
            baselineMap,
            "placement-capacity",
            MapAuthoringValidator.PlacementCapacityCode,
            map => ReconfigureGeometry(
                map,
                baselineMap.CameraBounds,
                new[]
                {
                    new BattlefieldZone("single_cell", new Rect(-4.5f, -3.8f, 0.5f, 0.5f))
                },
                Array.Empty<BattlefieldZone>()),
            errors);

        AssertBattlefieldCode(
            baselineMap,
            "camera-coverage",
            MapAuthoringValidator.CameraCoverageCode,
            map => ReconfigureGeometry(
                map,
                new Rect(-1f, -1f, 2f, 2f),
                baselineMap.PlacementZones,
                baselineMap.BlockedZones),
            errors);

        AssertBattlefieldCode(
            baselineMap,
            "design-card",
            MapAuthoringValidator.DesignCardCode,
            map => map.ConfigureDesignCard(string.Empty, string.Empty, Array.Empty<string>(), string.Empty, string.Empty, string.Empty),
            errors);

        AssertBattlefieldCode(
            baselineMap,
            "battlefield-core",
            MapAuthoringValidator.BattlefieldCoreCode,
            map => ReconfigureIdentity(map, string.Empty),
            errors);

        var missingLevelPath = "Assets/__E4Negative__/missing-level.asset";
        var missingLevelResult = MapAuthoringValidator.ValidateLevel(null, missingLevelPath);
        var missingLevelIssue = missingLevelResult.Issues.FirstOrDefault(issue => issue.Code == MapAuthoringValidator.MissingLevelCode);
        if (missingLevelIssue == null || missingLevelIssue.AssetPath != missingLevelPath)
        {
            errors.Add("E4 negative fixture 'missing-level' did not produce [reference.level] with its exact asset path.");
        }

        AssertLevelCode(
            baselineLevel,
            "missing-battlefield",
            MapAuthoringValidator.MissingBattlefieldCode,
            level => level.ConfigureBattlefield(null),
            errors);
        AssertLevelCode(
            baselineLevel,
            "missing-tower",
            MapAuthoringValidator.MissingTowerCode,
            level => ReconfigureLevel(level, baselineLevel, baselineMap, new[] { baselineLevel.AvailableTowers[0], null }, baselineLevel.WaveConfig),
            errors);
        AssertLevelCode(
            baselineLevel,
            "missing-wave",
            MapAuthoringValidator.MissingWaveCode,
            level => ReconfigureLevel(level, baselineLevel, baselineMap, baselineLevel.AvailableTowers, null),
            errors);

        var missingEnemyWave = ScriptableObject.CreateInstance<WaveConfig>();
        missingEnemyWave.Configure(new[] { new WaveEnemyGroup(null, 1, 1f, 0f, 1f, 1f, "main") });
        AssertLevelCode(
            baselineLevel,
            "missing-enemy",
            MapAuthoringValidator.MissingEnemyCode,
            level => ReconfigureLevel(level, baselineLevel, baselineMap, baselineLevel.AvailableTowers, missingEnemyWave),
            errors);
        UnityEngine.Object.DestroyImmediate(missingEnemyWave);

        var missingRouteWave = UnityEngine.Object.Instantiate(baselineLevel.WaveConfig);
        missingRouteWave.Groups[0].ConfigureRoute("missing_route");
        AssertLevelCode(
            baselineLevel,
            "missing-wave-route",
            MapAuthoringValidator.WaveRouteReferenceCode,
            level => ReconfigureLevel(level, baselineLevel, baselineMap, baselineLevel.AvailableTowers, missingRouteWave),
            errors);
        UnityEngine.Object.DestroyImmediate(missingRouteWave);
    }

    private static void ValidateMigrationHelper(LevelCatalogConfig catalog, ICollection<string> errors)
    {
        var legacy = catalog?.FindById("level_03");
        if (legacy == null)
        {
            errors.Add("E4 migration rehearsal requires level_03 source geometry.");
            return;
        }

        BattlefieldConfig migrated = null;
        LevelConfig syntheticLegacy = null;
        try
        {
            var migrationSource = legacy;
            if (!legacy.UsesLegacyBattlefield)
            {
                syntheticLegacy = UnityEngine.Object.Instantiate(legacy);
                syntheticLegacy.ConfigureBattlefield(null);
                migrationSource = syntheticLegacy;
            }

            migrated = MapAuthoringMigrationService.CreateMigratedBattlefieldInMemory(migrationSource);
            var result = MapAuthoringValidator.ValidateBattlefield(migrated, "Assets/__E4Negative__/migrated-level03.asset");
            if (!result.IsValid
                || migrated.RouteConfigs.Length != 1
                || migrated.RouteConfigs[0].RouteId != "main"
                || migrated.CreateDefinition().UsesLegacyRoute)
            {
                errors.Add("E4 legacy migration helper must produce a valid explicit 'main' route without mutating the source level.");
                AppendUnexpectedIssues(result, errors);
            }
        }
        finally
        {
            if (migrated != null)
            {
                UnityEngine.Object.DestroyImmediate(migrated);
            }

            if (syntheticLegacy != null)
            {
                UnityEngine.Object.DestroyImmediate(syntheticLegacy);
            }
        }
    }

    private static void ValidatePreviewContract(BattlefieldConfig sandboxMap, ICollection<string> errors)
    {
        if (!File.Exists(MapAuthoringPreviewSceneService.PreviewScenePath))
        {
            errors.Add("E4 preview scene is missing.");
            return;
        }

        if (EditorBuildSettings.scenes.Any(scene => string.Equals(scene.path, MapAuthoringPreviewSceneService.PreviewScenePath, StringComparison.Ordinal)))
        {
            errors.Add("The editor-only map preview scene must not be included in player Build Settings.");
        }

        var scene = EditorSceneManager.OpenScene(MapAuthoringPreviewSceneService.PreviewScenePath, OpenSceneMode.Single);
        var host = UnityEngine.Object.FindFirstObjectByType<BattlefieldAuthoringPreview>();
        if (!scene.IsValid() || host == null || host.BattlefieldConfig != sandboxMap || Camera.main == null)
        {
            errors.Add("E4 preview scene must contain a configured preview host and an authoring camera.");
        }

        var runtimeSource = File.ReadAllText("Assets/_Project/Scripts/Gameplay/Levels/PrototypeLevelController.cs");
        var previewSource = File.ReadAllText("Assets/_Project/Scripts/Gameplay/Battlefield/BattlefieldAuthoringPreview.cs");
        var exporterSource = File.ReadAllText("Assets/Editor/MapAuthoring/MapAuthoringPreviewExporter.cs");
        if (!runtimeSource.Contains("BattlefieldRouteVisualStyle.GetColors")
            || !previewSource.Contains("BattlefieldRouteVisualStyle.GetColors")
            || !previewSource.Contains("battlefield.Routes")
            || !exporterSource.Contains("BattlefieldRouteVisualStyle.GetColors")
            || !exporterSource.Contains("battlefield.Routes"))
        {
            errors.Add("Editor preview and runtime routes must share route geometry and the same visual-style resolver.");
        }
    }

    private static void ValidateRuntimeBoundary(ICollection<string> errors)
    {
        var runtimeFiles = new[]
        {
            "Assets/_Project/Scripts/Gameplay/Levels/PrototypeLevelController.cs",
            "Assets/_Project/Scripts/Gameplay/Grid/TowerGrid.cs",
            "Assets/_Project/Scripts/Gameplay/Camera/BattlefieldCameraController.cs",
            "Assets/_Project/Scripts/Gameplay/Battlefield/BattlefieldDefinition.cs"
        };
        foreach (var path in runtimeFiles)
        {
            var source = File.ReadAllText(path);
            if (source.Contains("level_01")
                || source.Contains("level_07")
                || source.Contains("level_08")
                || source.Contains("e4_authoring_sandbox"))
            {
                errors.Add($"Runtime geometry must not contain map-specific level ids: {path}");
            }
        }

        if (!File.Exists("Assets/Editor/MapAuthoring/MapAuthoringWindow.cs")
            || !File.Exists("Assets/Editor/MapAuthoring/BattlefieldAuthoringPreviewEditor.cs")
            || !File.Exists("Assets/Editor/MapAuthoring/MapAuthoringMigrationService.cs"))
        {
            errors.Add("E4 authoring window, route handles, or migration helper is missing.");
        }
    }

    private static void ValidateDocumentation(ICollection<string> errors)
    {
        if (!File.Exists(WorkflowPath))
        {
            errors.Add("E4 repeatable map-authoring workflow is missing.");
            return;
        }

        var workflow = File.ReadAllText(WorkflowPath);
        foreach (var required in new[]
                 {
                     "Create Or Update Starter Assets",
                     "Open Scene/Game Preview",
                     "Validate Authoring Data",
                     "Migrate Selected Legacy Level",
                     "run-emulator-route-qa.ps1"
                 })
        {
            if (!workflow.Contains(required))
            {
                errors.Add($"E4 authoring workflow is missing required instruction: {required}");
            }
        }
    }

    private static void AssertBattlefieldCode(
        BattlefieldConfig baseline,
        string fixtureName,
        string expectedCode,
        Action<BattlefieldConfig> damage,
        ICollection<string> errors)
    {
        var clone = UnityEngine.Object.Instantiate(baseline);
        clone.name = $"Broken_{fixtureName}";
        try
        {
            damage(clone);
            var path = $"Assets/__E4Negative__/{fixtureName}.asset";
            var result = MapAuthoringValidator.ValidateBattlefield(clone, path);
            var matching = result.Issues.FirstOrDefault(issue => issue.Code == expectedCode);
            if (matching == null || matching.AssetPath != path)
            {
                errors.Add($"E4 negative fixture '{fixtureName}' did not produce [{expectedCode}] with its exact asset path.");
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(clone);
        }
    }

    private static void AssertLevelCode(
        LevelConfig baseline,
        string fixtureName,
        string expectedCode,
        Action<LevelConfig> damage,
        ICollection<string> errors)
    {
        var clone = UnityEngine.Object.Instantiate(baseline);
        clone.name = $"Broken_{fixtureName}";
        try
        {
            damage(clone);
            var path = $"Assets/__E4Negative__/{fixtureName}.asset";
            var result = MapAuthoringValidator.ValidateLevel(clone, path);
            var matching = result.Issues.FirstOrDefault(issue => issue.Code == expectedCode);
            if (matching == null || (matching.AssetPath != path && expectedCode != MapAuthoringValidator.MissingEnemyCode && expectedCode != MapAuthoringValidator.WaveRouteReferenceCode))
            {
                errors.Add($"E4 negative fixture '{fixtureName}' did not produce [{expectedCode}] with actionable path diagnostics.");
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(clone);
        }
    }

    private static PathRouteConfig CreateRoute(string id, Vector2[] points)
    {
        var start = points.Length == 0 ? Vector2.zero : points[0];
        var goal = points.Length == 0 ? Vector2.zero : points[^1];
        return new PathRouteConfig(id, id, points, start, goal, id, 0.48f, 1f, new[] { "ground" });
    }

    private static void ReconfigureGeometry(
        BattlefieldConfig target,
        Rect cameraBounds,
        BattlefieldZone[] placementZones,
        BattlefieldZone[] blockedZones)
    {
        var originalRoutes = target.RouteConfigs.ToArray();
        var primary = originalRoutes[0];
        target.Configure(
            target.BattlefieldId,
            target.DisplayName,
            target.BackgroundId,
            target.BiomeId,
            target.CameraMode,
            target.WorldBounds,
            cameraBounds,
            target.InitialCameraFocus,
            target.ScrollableViewHeight,
            primary.Points,
            primary.VisualWidth,
            primary.SpawnAnchor,
            primary.GoalAnchor,
            target.PlacementCellSize,
            placementZones,
            blockedZones,
            target.DecorationAnchors);
        target.ConfigureRoutes(originalRoutes);
    }

    private static void ReconfigureIdentity(BattlefieldConfig target, string backgroundId)
    {
        var originalRoutes = target.RouteConfigs.ToArray();
        var primary = originalRoutes[0];
        target.Configure(
            target.BattlefieldId,
            target.DisplayName,
            backgroundId,
            target.BiomeId,
            target.CameraMode,
            target.WorldBounds,
            target.CameraBounds,
            target.InitialCameraFocus,
            target.ScrollableViewHeight,
            primary.Points,
            primary.VisualWidth,
            primary.SpawnAnchor,
            primary.GoalAnchor,
            target.PlacementCellSize,
            target.PlacementZones,
            target.BlockedZones,
            target.DecorationAnchors);
        target.ConfigureRoutes(originalRoutes);
    }

    private static void ReconfigureLevel(
        LevelConfig target,
        LevelConfig baseline,
        BattlefieldConfig battlefield,
        CatGuard.Gameplay.Towers.TowerConfig[] towers,
        WaveConfig wave)
    {
        target.Configure(
            baseline.LevelId,
            baseline.DisplayName,
            baseline.BaseLives,
            baseline.LegacyGridColumns,
            baseline.LegacyGridRows,
            baseline.LegacyCellSize,
            baseline.LegacyGridOrigin,
            baseline.LegacyPathPoints,
            towers,
            wave,
            baseline.FirstClearRewardCoins,
            baseline.ReplayRewardCoins,
            baseline.UnlocksLevelIds,
            baseline.TutorialTextKey,
            baseline.StartingBattleFish);
        target.ConfigureBattlefield(battlefield);
    }

    private static void AppendUnexpectedIssues(MapAuthoringValidationResult result, ICollection<string> errors)
    {
        if (result.IsValid)
        {
            return;
        }

        foreach (var issue in result.Issues)
        {
            errors.Add(issue.ToString());
        }
    }

    private static T LoadRequired<T>(string path)
        where T : UnityEngine.Object
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            throw new InvalidDataException($"Required E4 asset is missing: {path}");
        }

        return asset;
    }
}
