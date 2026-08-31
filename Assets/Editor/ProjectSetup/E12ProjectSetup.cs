using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CatGuard.Core.Localization;
using CatGuard.Core.Save;
using CatGuard.EditorTools.MapAuthoring;
using CatGuard.Gameplay.Battlefield;
using CatGuard.Gameplay.Enemies;
using CatGuard.Gameplay.Levels;
using CatGuard.Gameplay.Presentation;
using CatGuard.Gameplay.Towers;
using CatGuard.Gameplay.Waves;
using CatGuard.Meta.GuardianGrowth;
using UnityEditor;
using UnityEngine;

public static class E12ProjectSetup
{
    private const string BattlefieldFolder = "Assets/_Project/ScriptableObjects/Battlefields/Campaign";
    private const string EnemyFolder = "Assets/_Project/ScriptableObjects/Enemies";
    private const string LevelFolder = "Assets/_Project/ScriptableObjects/Levels";
    private const string LevelCatalogPath = LevelFolder + "/LevelCatalog.asset";
    private const string MetaCatalogPath = "Assets/_Project/Resources/MetaProgression/MetaProgressionCatalog.asset";
    private const string ReportPath = "docs/planning/E12_EXPANDED_CAMPAIGN_REPORT.md";
    private const string WorkflowPath = "docs/planning/CAMPAIGN_CONTENT_WORKFLOW.md";
    private const string InternalAssetManifest = "Internal code-authored layout; existing Cat Guard prototype sprites and animation profiles only; no external asset or license dependency.";

    private static readonly string[] QuestHooks =
    {
        "tutorial_hold_line", "pest_patrol", "build_circle", "guardian_signal",
        "careful_craftsman", "control_school", "untouched_barrel", "small_paw_squad",
        "no_scrap", "old_well_watch", "rooftop_watch", "veteran_guard"
    };

    public static void Run()
    {
        Directory.CreateDirectory(BattlefieldFolder);
        Directory.CreateDirectory(EnemyFolder);
        Directory.CreateDirectory(LevelFolder);

        var towers = LoadAssets<TowerConfig>("Assets/_Project/ScriptableObjects/Towers");
        var enemies = EnsureEnemyRoster();
        var mapSpecs = BuildMapSpecs();
        var maps = mapSpecs.Select(EnsureBattlefield).ToArray();
        var waves = EnsureWaves(enemies, mapSpecs);
        var levels = EnsureLevels(towers, maps, waves, mapSpecs);
        var catalog = EnsureAsset<LevelCatalogConfig>(LevelCatalogPath);
        catalog.Configure(levels);
        EditorUtility.SetDirty(catalog);
        ExtendCodex(enemies);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        ValidateAndExit();
    }

    public static void Validate()
    {
        ValidateAndExit();
    }

    private static EnemyConfig[] EnsureEnemyRoster()
    {
        var existing = new[]
        {
            LoadRequired<EnemyConfig>($"{EnemyFolder}/MouseScoutEnemy.asset"),
            LoadRequired<EnemyConfig>($"{EnemyFolder}/RatBruiserEnemy.asset"),
            LoadRequired<EnemyConfig>($"{EnemyFolder}/BeetleGuardEnemy.asset"),
            LoadRequired<EnemyConfig>($"{EnemyFolder}/MothSwarmEnemy.asset"),
            LoadRequired<EnemyConfig>($"{EnemyFolder}/SnailTankEnemy.asset")
        };

        var sparrow = EnsureEnemy(
            "SparrowRaiderEnemy", "sparrow_raider", "Sparrow Raider",
            3.4f, 1.42f, 1, 0.38f, new Color(0.92f, 0.68f, 0.26f), 7,
            existing[3], "Assets/_Project/ScriptableObjects/Presentation/MothSwarmAnimation.asset");
        var weasel = EnsureEnemy(
            "PipeWeaselEnemy", "pipe_weasel", "Pipe Weasel",
            8.5f, 0.72f, 2, 0.56f, new Color(0.66f, 0.42f, 0.3f), 15,
            existing[1], "Assets/_Project/ScriptableObjects/Presentation/RatBruiserAnimation.asset");
        var runner = EnsureEnemy(
            "CockroachRunnerEnemy", "cockroach_runner", "Cockroach Runner",
            5.2f, 1.18f, 1, 0.43f, new Color(0.52f, 0.34f, 0.18f), 10,
            existing[2], "Assets/_Project/ScriptableObjects/Presentation/BeetleGuardAnimation.asset");
        return existing.Concat(new[] { sparrow, weasel, runner }).ToArray();
    }

    private static EnemyConfig EnsureEnemy(
        string assetName,
        string id,
        string title,
        float health,
        float speed,
        int damage,
        float scale,
        Color color,
        int reward,
        EnemyConfig presentationSource,
        string animationPath)
    {
        var enemy = EnsureAsset<EnemyConfig>($"{EnemyFolder}/{assetName}.asset");
        enemy.Configure(id, title, health, speed, damage, scale, color, reward, presentationSource.VisualSprite);
        enemy.ConfigureAnimationProfile(LoadRequired<UnitAnimationConfig>(animationPath));
        EditorUtility.SetDirty(enemy);
        return enemy;
    }

    private static BattlefieldConfig EnsureBattlefield(MapSpec spec)
    {
        var map = EnsureAsset<BattlefieldConfig>($"{BattlefieldFolder}/{spec.AssetName}.asset");
        var primary = spec.Routes[0];
        map.Configure(
            spec.BattlefieldId,
            spec.Title,
            $"campaign_{spec.BiomeId}",
            spec.BiomeId,
            spec.CameraMode,
            spec.Bounds,
            spec.Bounds,
            spec.CameraMode == BattlefieldCameraMode.ScrollableLarge
                ? new Vector2(spec.Bounds.xMin + spec.Bounds.width * 0.3f, 0f)
                : spec.Bounds.center,
            spec.CameraMode == BattlefieldCameraMode.ScrollableLarge ? 6.2f : 8.2f,
            primary.Points,
            primary.VisualWidth,
            primary.SpawnAnchor,
            primary.GoalAnchor,
            1.05f,
            CreatePlacementZones(spec.Bounds),
            CreateBlockedZones(spec.Bounds, spec.Index),
            CreateDecorations(spec.Bounds, spec.BiomeId));
        map.ConfigureRoutes(spec.Routes);
        map.ConfigureDesignCard(
            $"{spec.Index + 1:00}/12",
            spec.RouteConcept,
            new[] { "2-4 high-value crossfire pockets", "long-range coverage", "splash choke", "economy-safe fallback" },
            spec.DominantThreat,
            "Route convergence and long sight lines create deliberate area and global ultimate windows.",
            "Distinct spawn/goal anchors, high-contrast paths, readable no-build obstacles, and camera-safe tactical points.");
        map.ConfigurePresentationPalette(CreatePalette(spec.BiomeId));
        EditorUtility.SetDirty(map);
        return map;
    }

    private static BattlefieldZone[] CreatePlacementZones(Rect bounds)
    {
        var margin = 0.9f;
        var pocketWidth = bounds.width * 0.22f;
        return new[]
        {
            new BattlefieldZone("north_west", new Rect(bounds.xMin + margin, bounds.yMax - 2.05f, pocketWidth, 1.35f)),
            new BattlefieldZone("north_center", new Rect(bounds.center.x - pocketWidth * 0.5f, bounds.yMax - 1.8f, pocketWidth, 1.15f)),
            new BattlefieldZone("north_east", new Rect(bounds.xMax - pocketWidth - margin, bounds.yMax - 2.05f, pocketWidth, 1.35f)),
            new BattlefieldZone("south_west", new Rect(bounds.xMin + margin, bounds.yMin + 0.7f, pocketWidth, 1.35f)),
            new BattlefieldZone("south_center", new Rect(bounds.center.x - pocketWidth * 0.5f, bounds.yMin + 0.65f, pocketWidth, 1.15f)),
            new BattlefieldZone("south_east", new Rect(bounds.xMax - pocketWidth - margin, bounds.yMin + 0.7f, pocketWidth, 1.35f))
        };
    }

    private static BattlefieldZone[] CreateBlockedZones(Rect bounds, int index)
    {
        var offset = (index % 3 - 1) * 1.35f;
        return new[]
        {
            new BattlefieldZone("landmark", new Rect(bounds.center.x + offset - 0.65f, bounds.center.y - 0.65f, 1.3f, 1.3f)),
            new BattlefieldZone("service_edge", new Rect(bounds.xMax - 2.2f, bounds.yMin + 1.8f, 1.2f, 1.05f))
        };
    }

    private static BattlefieldDecorationAnchor[] CreateDecorations(Rect bounds, string biomeId)
    {
        return new[]
        {
            new BattlefieldDecorationAnchor($"{biomeId}_landmark", bounds.center, 1.3f, false),
            new BattlefieldDecorationAnchor($"{biomeId}_light_west", new Vector2(bounds.xMin + 1.3f, bounds.yMax - 0.8f), 0.75f, true),
            new BattlefieldDecorationAnchor($"{biomeId}_light_east", new Vector2(bounds.xMax - 1.3f, bounds.yMax - 0.8f), 0.75f, true)
        };
    }

    private static BattlefieldPresentationPalette CreatePalette(string biomeId)
    {
        return biomeId switch
        {
            "backyard_dawn" => new BattlefieldPresentationPalette(
                new Color(0.86f, 0.92f, 0.75f), new Color(0.1f, 0.24f, 0.14f),
                new Color(0.24f, 0.5f, 0.2f, 0.28f), new Color(0.36f, 0.2f, 0.08f, 0.36f)),
            "moonlit_rooftops" => new BattlefieldPresentationPalette(
                new Color(0.48f, 0.58f, 0.78f), new Color(0.07f, 0.09f, 0.17f),
                new Color(0.25f, 0.22f, 0.42f, 0.38f), new Color(0.18f, 0.14f, 0.24f, 0.52f)),
            _ => new BattlefieldPresentationPalette(
                new Color(0.76f, 0.63f, 0.45f), new Color(0.18f, 0.1f, 0.07f),
                new Color(0.42f, 0.24f, 0.12f, 0.36f), new Color(0.3f, 0.16f, 0.08f, 0.5f))
        };
    }

    private static WaveConfig[] EnsureWaves(IReadOnlyList<EnemyConfig> enemies, IReadOnlyList<MapSpec> maps)
    {
        var result = new WaveConfig[maps.Count];
        var previousThreat = 0f;
        for (var index = 0; index < maps.Count; index++)
        {
            var routes = maps[index].Routes.Select(route => route.RouteId).ToArray();
            var isFinalMap = index == maps.Count - 1;
            var speedScale = 1f + index * 0.035f;
            var scoutCount = 8 + index * 2;
            var tankCount = 2 + index;
            var specialistCount = 4 + index;
            var specialist = enemies[(index + 2) % enemies.Count];
            var unscaledThreat = speedScale * (
                enemies[0].Health * scoutCount
                + enemies[4].Health * tankCount
                + specialist.Health * specialistCount);
            var healthScale = index == 0
                ? 1f
                : Mathf.Max(1f + index * 0.12f, previousThreat * 1.14f / unscaledThreat);
            // Preserve level 12's final threat while giving default-economy towers time to earn reinvestment Fish.
            var scoutSpawnInterval = isFinalMap ? 0.42f : Mathf.Max(0.2f, 0.62f - index * 0.025f);
            var scoutHealthScale = isFinalMap ? 1.8f : healthScale;
            var scoutSpeedScale = isFinalMap ? 1.25f : speedScale;
            var tankStartDelay = isFinalMap ? 6f : 0.35f;
            var specialistStartDelay = isFinalMap ? 10f : 0.45f;
            var reinforcementHealthScale = isFinalMap ? 3.4f : healthScale;
            var reinforcementSpeedScale = isFinalMap ? 1.35f : speedScale;
            var groups = new[]
            {
                new WaveEnemyGroup(enemies[0], scoutCount, scoutSpawnInterval, 0f, scoutHealthScale, scoutSpeedScale, routes[0]),
                new WaveEnemyGroup(enemies[4], tankCount, Mathf.Max(0.32f, 1f - index * 0.04f), tankStartDelay, reinforcementHealthScale, reinforcementSpeedScale, routes[^1]),
                new WaveEnemyGroup(specialist, specialistCount, Mathf.Max(0.22f, 0.74f - index * 0.03f), specialistStartDelay, reinforcementHealthScale, reinforcementSpeedScale, routes[0])
            };
            if (routes.Length > 1)
            {
                groups[0].ConfigureRoundRobin(routes);
            }

            var wave = EnsureAsset<WaveConfig>($"{LevelFolder}/E12Level{index + 1:00}Wave.asset");
            wave.Configure(groups);
            wave.ConfigureConcurrentGroups(index >= 4);
            EditorUtility.SetDirty(wave);
            result[index] = wave;
            previousThreat = unscaledThreat * healthScale;
        }

        return result;
    }

    private static LevelConfig[] EnsureLevels(
        IReadOnlyList<TowerConfig> towers,
        IReadOnlyList<BattlefieldConfig> maps,
        IReadOnlyList<WaveConfig> waves,
        IReadOnlyList<MapSpec> specs)
    {
        if (towers.Count != 5)
        {
            throw new InvalidDataException($"E12 requires the five production towers; found {towers.Count}.");
        }

        var result = new LevelConfig[12];
        for (var index = 0; index < result.Length; index++)
        {
            var level = EnsureAsset<LevelConfig>($"{LevelFolder}/Level{index + 1:00}Config.asset");
            var next = index == result.Length - 1 ? Array.Empty<string>() : new[] { $"level_{index + 2:00}" };
            level.Configure(
                $"level_{index + 1:00}",
                specs[index].Title,
                7 + index / 3,
                4,
                3,
                1.15f,
                new Vector2(-1.75f, -2.8f),
                specs[index].Routes[0].Points,
                towers.ToArray(),
                waves[index],
                35 + index * 18,
                8 + index * 2,
                next,
                index == 0 ? "tutorial.level_01" : string.Empty,
                110 + index * 10);
            level.ConfigureBattlefield(maps[index]);
            level.ConfigureCampaignMetadata(CreateMetadata(index, specs[index]));
            EditorUtility.SetDirty(level);
            result[index] = level;
        }

        return result;
    }

    private static CampaignMapMetadata CreateMetadata(int index, MapSpec spec)
    {
        var challenge = new CampaignChallengeConfig();
        var livesDelta = 0;
        var fish = 1f;
        var health = 1f;
        var speed = 1f;
        var description = "campaign.challenge.combined";
        switch (index % 5)
        {
            case 0:
                fish = 0.72f;
                description = "campaign.challenge.supplies";
                break;
            case 1:
                livesDelta = -2;
                description = "campaign.challenge.lives";
                break;
            case 2:
                speed = 1.2f;
                description = "campaign.challenge.speed";
                break;
            case 3:
                health = 1.28f;
                description = "campaign.challenge.health";
                break;
            default:
                health = 1.18f;
                speed = 1.14f;
                break;
        }

        challenge.Configure(
            $"challenge_level_{index + 1:00}",
            $"campaign.challenge.level_{index + 1:00}.name",
            description,
            livesDelta,
            fish,
            health,
            speed,
            30 + index * 8,
            6 + index);
        var metadata = new CampaignMapMetadata();
        metadata.Configure(
            spec.BiomeId switch
            {
                "backyard_dawn" => "campaign.biome.backyard",
                "moonlit_rooftops" => "campaign.biome.rooftops",
                _ => "campaign.biome.pantry"
            },
            $"campaign.map.level_{index + 1:00}.summary",
            challenge,
            QuestHooks[index],
            index is 0 ? "first_clear" : index is 9 ? "ten_maps" : string.Empty,
            96,
            InternalAssetManifest);
        return metadata;
    }

    private static void ExtendCodex(IReadOnlyCollection<EnemyConfig> enemies)
    {
        var catalog = LoadRequired<MetaProgressionCatalogConfig>(MetaCatalogPath);
        var entries = new List<CodexEntryConfig>();
        for (var index = 1; index <= 12; index++)
        {
            var levelId = $"level_{index:00}";
            entries.Add(new CodexEntryConfig($"map:{levelId}", CodexEntryType.Map, levelId, $"level.{levelId}"));
        }

        foreach (var mastery in catalog.TowerMasteries)
        {
            entries.Add(new CodexEntryConfig($"tower:{mastery.TowerId}", CodexEntryType.Tower, mastery.TowerId, $"tower.{mastery.TowerId}"));
        }

        foreach (var enemy in enemies)
        {
            entries.Add(new CodexEntryConfig($"enemy:{enemy.EnemyId}", CodexEntryType.Enemy, enemy.EnemyId, $"meta.codex.enemy.{enemy.EnemyId}"));
        }

        catalog.Configure(
            catalog.PlayerRank,
            catalog.TowerMasteries,
            catalog.WorkshopResearch,
            catalog.UltimateUnlocks,
            catalog.GuardianPerks,
            entries.ToArray());
        EditorUtility.SetDirty(catalog);
    }

    private static MapSpec[] BuildMapSpecs()
    {
        var fixedBounds = new Rect(-8f, -4.5f, 16f, 9f);
        var wideBounds = new Rect(-12f, -4.8f, 24f, 9.6f);
        return new[]
        {
            Spec(0, "GardenGateDawn", "garden_gate_dawn", "Garden Gate", "backyard_dawn", fixedBounds, "Readable S-bend with four overlapping placement pockets.", "Mouse scouts teach basic coverage.", R("main", P(-7,2.5f,-4,0.7f,-1,1.6f,2,-0.5f,5,-1.5f,7,-2.5f))),
            Spec(1, "GlasshouseBend", "glasshouse_bend", "Glasshouse Bend", "backyard_dawn", fixedBounds, "Tight glasshouse bend creates a high-value range corner.", "Guards exploit short recovery gaps.", R("main", P(-7,-2.5f,-4,-0.7f,-2,1.7f,1,2.1f,3,0.2f,7,1.4f))),
            Spec(2, "OldWellFork", "old_well_fork", "Old Well Fork", "backyard_dawn", fixedBounds, "Two entrances merge after the well landmark.", "Alternating lanes punish one-sided placement.", R("upper", P(-7,2.7f,-4,1.4f,-1,0.5f,2,-0.8f,7,-2.2f)), R("lower", P(-7,-2.7f,-4,-1.4f,-1,0.5f,2,-0.8f,7,-2.2f))),
            Spec(3, "OrchardSwitchback", "orchard_switchback", "Orchard Switchback", "backyard_dawn", wideBounds, "Long scrollable switchback revisits the same defense bands.", "Mixed armor and speed stretch the full line.", R("main", P(-11,2.7f,-8,0.7f,-5,2.2f,-2,-1.8f,2,1.8f,5,-2.2f,8,-0.5f,11,-2.7f))),
            Spec(4, "MoonlitFence", "moonlit_fence", "Moonlit Fence", "moonlit_rooftops", fixedBounds, "Single roofline exposes clear long-range sight lines.", "Sparrow raiders raise the speed check.", R("main", P(-7,2.8f,-5,0.2f,-2,-0.8f,0,1.4f,3,0.4f,5,-1.3f,7,-2.7f))),
            Spec(5, "DrainpipeDivide", "drainpipe_divide", "Drainpipe Divide", "moonlit_rooftops", fixedBounds, "Independent drainpipe lanes split early and finish apart.", "Concurrent groups demand balanced investment.", R("north", P(-7,2.8f,-4,1.4f,-1,2.1f,3,1.1f,7,2.4f)), R("south", P(-7,-2.8f,-4,-1.2f,-1,-2.1f,3,-1.0f,7,-2.4f))),
            Spec(6, "LanternRoofs", "lantern_roofs", "Lantern Roofs", "moonlit_rooftops", fixedBounds, "Three roof entrances converge beneath the lanterns.", "Three-way warning pressure tests priorities.", R("high", P(-7,3f,-4,2.1f,-1,0.6f,3,-0.2f,7,-1.7f)), R("mid", P(-7,0f,-4,0.4f,-1,0.6f,3,-0.2f,7,-1.7f)), R("low", P(-7,-3f,-4,-1.8f,-1,0.6f,3,-0.2f,7,-1.7f))),
            Spec(7, "ChimneyRun", "chimney_run", "Chimney Run", "moonlit_rooftops", wideBounds, "Scrollable roof run alternates close and distant firing windows.", "Tank and runner pairs stress reposition planning.", R("main", P(-11,-2.8f,-8,-0.8f,-5,-2f,-2,1.9f,2,-1.7f,5,2f,8,0.7f,11,2.7f))),
            Spec(8, "PantryThreshold", "pantry_threshold", "Pantry Threshold", "pantry_underpass", fixedBounds, "Compact pantry threshold funnels traffic past two kill zones.", "Cockroach runners open the underpass speed tier.", R("main", P(-7,0.4f,-5,2.4f,-2,1.4f,0,-1.7f,3,-2.2f,5,0.5f,7,-0.4f))),
            Spec(9, "CrateMaze", "crate_maze", "Crate Maze", "pantry_underpass", fixedBounds, "Crates create staggered upper and lower lanes.", "Pipe weasels arrive behind fast screens.", R("upper", P(-7,2.7f,-4,1.4f,-1,2.1f,2,0.2f,7,-1.8f)), R("lower", P(-7,-2.7f,-4,-1.6f,-1,-2.1f,2,0.2f,7,-1.8f))),
            Spec(10, "PipeJunction", "pipe_junction", "Pipe Junction", "pantry_underpass", fixedBounds, "Three pipes retain separate timings before a late merge.", "Dense concurrent arrivals reward saved ultimates.", R("steam", P(-7,3f,-4,1.8f,0,1f,3,0f,7,-2f)), R("water", P(-7,0f,-4,0f,0,1f,3,0f,7,-2f)), R("drain", P(-7,-3f,-4,-1.6f,0,1f,3,0f,7,-2f))),
            Spec(11, "CellarCrossroads", "cellar_crossroads", "Cellar Crossroads", "pantry_underpass", wideBounds, "Two long cellar routes cross twice and end at opposite goals.", "The heaviest mixed assault combines route, armor, and speed pressure.", R("amber", P(-11,3f,-6,2f,0,-1.2f,6,1.1f,11,-2.5f)), R("blue", P(-11,-3f,-6,-2f,0,1.2f,6,-1.1f,11,2.5f)))
        };
    }

    private static MapSpec Spec(int index, string asset, string id, string title, string biome, Rect bounds, string concept, string threat, params PathRouteConfig[] routes)
    {
        return new MapSpec(index, asset, id, title, biome, index is 3 or 7 or 11 ? BattlefieldCameraMode.ScrollableLarge : BattlefieldCameraMode.FixedOverview, bounds, concept, threat, routes);
    }

    private static PathRouteConfig R(string id, Vector2[] points)
    {
        return new PathRouteConfig(id, id, points, points[0], points[^1], id, 0.5f, 1f, new[] { "ground", "campaign" }, 0f, "E12 authored campaign route.");
    }

    private static Vector2[] P(params float[] coordinates)
    {
        var points = new Vector2[coordinates.Length / 2];
        for (var index = 0; index < points.Length; index++)
        {
            points[index] = new Vector2(coordinates[index * 2], coordinates[index * 2 + 1]);
        }

        return points;
    }

    private static void ValidateAndExit()
    {
        var errors = new List<string>();
        var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalogConfig>(LevelCatalogPath);
        ValidateCatalog(catalog, errors);
        ValidateMigration(errors);
        ValidateLocalization(catalog, errors);
        ValidateRuntimeBoundaries(errors);
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

        Debug.Log("E12 validation passed: 12 authored maps across three four-map biomes, eight standard enemy families, 12 unlock-gated challenges, strict difficulty/reward progression, schema v4 persistence, 25 codex entries, localization, licensing, and normal/challenge traversal contracts are complete; bosses remain reserved for E13.");
        EditorApplication.Exit(0);
    }

    private static void ValidateCatalog(LevelCatalogConfig catalog, ICollection<string> errors)
    {
        if (catalog == null || !catalog.IsValid() || catalog.Levels.Length != 12)
        {
            errors.Add("E12 requires an exact valid 12-level catalog.");
            return;
        }

        var biomes = new Dictionary<string, int>(StringComparer.Ordinal);
        var enemies = new HashSet<string>(StringComparer.Ordinal);
        var mapIds = new HashSet<string>(StringComparer.Ordinal);
        var challengeIds = new HashSet<string>(StringComparer.Ordinal);
        var previousThreat = 0f;
        var previousReward = -1;
        for (var index = 0; index < catalog.Levels.Length; index++)
        {
            var level = catalog.Levels[index];
            var authoring = MapAuthoringValidator.ValidateLevel(level);
            foreach (var issue in authoring.Errors)
            {
                errors.Add($"{level?.LevelId}: {issue}");
            }

            if (level?.BattlefieldConfig == null || !level.HasCampaignMetadata)
            {
                errors.Add($"Level {index + 1} lacks explicit E12 battlefield or campaign metadata.");
                continue;
            }

            var definition = level.ResolveBattlefield();
            mapIds.Add(definition.BattlefieldId);
            biomes[definition.BiomeId] = biomes.TryGetValue(definition.BiomeId, out var count) ? count + 1 : 1;
            if (!challengeIds.Add(level.CampaignMetadata.Challenge.ChallengeId))
            {
                errors.Add($"Duplicate challenge id on {level.LevelId}.");
            }

            if (level.WaveConfig.TotalEnemyCount > level.CampaignMetadata.RecommendedMaxActiveEnemies)
            {
                errors.Add($"{level.LevelId} exceeds its declared performance budget.");
            }

            var threat = 0f;
            foreach (var group in level.WaveConfig.Groups)
            {
                if (group.EnemyConfig.IsBoss)
                {
                    continue;
                }
                enemies.Add(group.EnemyConfig.EnemyId);
                threat += group.EnemyConfig.Health * group.HealthMultiplier * group.SpeedMultiplier * group.Count;
            }

            if (index > 0 && (threat <= previousThreat || level.FirstClearRewardCoins <= previousReward))
            {
                errors.Add($"Difficulty or first-clear reward is not strictly increasing at {level.LevelId}.");
            }

            var expectedNext = index == 11 ? Array.Empty<string>() : new[] { $"level_{index + 2:00}" };
            if (!level.UnlocksLevelIds.SequenceEqual(expectedNext))
            {
                errors.Add($"Unlock graph is not the strict E12 chain at {level.LevelId}.");
            }

            previousThreat = threat;
            previousReward = level.FirstClearRewardCoins;
        }

        if (mapIds.Count != 12 || biomes.Count != 3 || biomes.Values.Any(value => value != 4))
        {
            errors.Add("E12 map identity must be 12 unique maps arranged as exactly 3 biomes x 4 maps.");
        }

        if (enemies.Count != 8)
        {
            errors.Add($"E12 requires exactly eight standard enemy families in campaign waves; found {enemies.Count}.");
        }

        var meta = AssetDatabase.LoadAssetAtPath<MetaProgressionCatalogConfig>(MetaCatalogPath);
        if (meta?.CodexEntries.Length < 25
            || meta.CodexEntries.Count(entry => entry.EntryType == CodexEntryType.Map) != 12
            || meta.CodexEntries.Count(entry => entry.EntryType == CodexEntryType.Enemy) < 8)
        {
            errors.Add("Expanded codex must contain 12 maps, five towers, and eight enemies (25 entries). ");
        }
    }

    private static void ValidateMigration(ICollection<string> errors)
    {
        var save = GameSaveData.CreateDefault("level_01");
        save.schemaVersion = 3;
        save.selectedChallengeId = null;
        save.completedChallengeIds = null;
        save.completedLevelIds.Add("level_01");
        if (!GameSaveMigrationService.TryMigrate(save, "level_01", out var changed, out var error)
            || !changed
            || !string.IsNullOrEmpty(error)
            || save.schemaVersion != GameSaveMigrationService.CurrentSchemaVersion
            || save.selectedChallengeId != string.Empty
            || save.completedChallengeIds == null
            || !save.completedLevelIds.Contains("level_01"))
        {
            errors.Add("Schema v3 to v4 campaign-challenge migration is not safe and preserving.");
        }
    }

    private static void ValidateLocalization(LevelCatalogConfig catalog, ICollection<string> errors)
    {
        if (catalog == null)
        {
            return;
        }

        var original = LocalizationService.CurrentLanguageCode;
        foreach (var language in new[] { LocalizationService.English, LocalizationService.Russian })
        {
            LocalizationService.SetLanguage(language);
            foreach (var level in catalog.Levels)
            {
                var metadata = level.CampaignMetadata;
                foreach (var key in new[]
                {
                    $"level.{level.LevelId}", metadata.BiomeNameLocalizationKey, metadata.TacticalSummaryLocalizationKey,
                    metadata.Challenge.NameLocalizationKey, metadata.Challenge.DescriptionLocalizationKey
                })
                {
                    if (LocalizationService.Text(key) == key)
                    {
                        errors.Add($"Missing E12 localization '{key}' in '{language}'.");
                    }
                }
            }

            foreach (var enemyId in new[] { "sparrow_raider", "pipe_weasel", "cockroach_runner" })
            {
                var key = $"meta.codex.enemy.{enemyId}";
                if (LocalizationService.Text(key) == key)
                {
                    errors.Add($"Missing E12 enemy localization '{key}' in '{language}'.");
                }
            }
        }

        LocalizationService.SetLanguage(original);
    }

    private static void ValidateRuntimeBoundaries(ICollection<string> errors)
    {
        var required = new Dictionary<string, string[]>
        {
            ["Assets/_Project/Scripts/Meta/Progression/ProgressionService.cs"] = new[] { "IsChallengeUnlocked", "CompleteChallenge", "completedChallengeIds" },
            ["Assets/_Project/Scripts/Gameplay/Levels/PrototypeLevelController.cs"] = new[] { "ActiveChallenge", "EnemyHealthMultiplier", "PresentationPalette" },
            ["Assets/_Project/Scripts/UI/Screens/MainMenuController.cs"] = new[] { "selectedCampaignChallenge", "campaign.challenge", "TacticalSummaryLocalizationKey" },
            ["Assets/_Project/Scripts/Core/Save/GameSaveMigrationService.cs"] = new[] { "CurrentSchemaVersion", "MigrateVersion3ToVersion4" },
            ["tools/android/run-emulator-campaign-qa.ps1"] = new[] { "ChallengeId", "level_12", "RequirePerformance" }
        };
        foreach (var pair in required)
        {
            var source = File.Exists(pair.Key) ? File.ReadAllText(pair.Key) : string.Empty;
            foreach (var token in pair.Value)
            {
                if (!source.Contains(token, StringComparison.Ordinal))
                {
                    errors.Add($"E12 runtime boundary '{token}' is missing from {pair.Key}.");
                }
            }
        }

        var controllerSource = File.ReadAllText("Assets/_Project/Scripts/Gameplay/Levels/PrototypeLevelController.cs");
        foreach (var forbidden in new[] { "BiomeId switch", "BiomeId ==", "level_11\"", "level_12\"", "challenge_level_" })
        {
            if (controllerSource.Contains(forbidden, StringComparison.Ordinal))
            {
                errors.Add($"Controller contains forbidden E12 content-id branching token '{forbidden}'.");
            }
        }
    }

    private static void ValidateDocumentation(ICollection<string> errors)
    {
        foreach (var path in new[] { ReportPath, WorkflowPath })
        {
            if (!File.Exists(path) || File.ReadAllText(path).Length < 900)
            {
                errors.Add($"E12 documentation is missing or incomplete: {path}.");
            }
        }
    }

    private static T EnsureAsset<T>(string path) where T : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null)
        {
            return asset;
        }

        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static T LoadRequired<T>(string path) where T : UnityEngine.Object
    {
        return AssetDatabase.LoadAssetAtPath<T>(path)
            ?? throw new InvalidDataException($"Required E12 asset is missing: {path}");
    }

    private static T[] LoadAssets<T>(string folder) where T : UnityEngine.Object
    {
        return AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { folder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<T>)
            .Where(asset => asset != null)
            .OrderBy(asset => asset.name, StringComparer.Ordinal)
            .ToArray();
    }

    private sealed class MapSpec
    {
        public MapSpec(int index, string assetName, string battlefieldId, string title, string biomeId, BattlefieldCameraMode cameraMode, Rect bounds, string routeConcept, string dominantThreat, PathRouteConfig[] routes)
        {
            Index = index;
            AssetName = assetName;
            BattlefieldId = battlefieldId;
            Title = title;
            BiomeId = biomeId;
            CameraMode = cameraMode;
            Bounds = bounds;
            RouteConcept = routeConcept;
            DominantThreat = dominantThreat;
            Routes = routes;
        }

        public int Index { get; }
        public string AssetName { get; }
        public string BattlefieldId { get; }
        public string Title { get; }
        public string BiomeId { get; }
        public BattlefieldCameraMode CameraMode { get; }
        public Rect Bounds { get; }
        public string RouteConcept { get; }
        public string DominantThreat { get; }
        public PathRouteConfig[] Routes { get; }
    }
}
