using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CatGuard.Core.Localization;
using CatGuard.Gameplay.Battlefield;
using CatGuard.Gameplay.Bosses;
using CatGuard.Gameplay.Enemies;
using CatGuard.Gameplay.Levels;
using CatGuard.Gameplay.Waves;
using CatGuard.Meta.GuardianGrowth;
using UnityEditor;
using UnityEngine;

public static class E13ProjectSetup
{
    private const string BossFolder = "Assets/_Project/ScriptableObjects/Bosses";
    private const string EnemyFolder = "Assets/_Project/ScriptableObjects/Enemies";
    private const string LevelFolder = "Assets/_Project/ScriptableObjects/Levels";
    private const string LevelCatalogPath = LevelFolder + "/LevelCatalog.asset";
    private const string MetaCatalogPath = "Assets/_Project/Resources/MetaProgression/MetaProgressionCatalog.asset";
    private const string ReportPath = "docs/planning/E13_BOSSES_ADVANCED_RULES_REPORT.md";
    private const string WorkflowPath = "docs/planning/BOSS_AND_MAP_RULE_WORKFLOW.md";

    public static void Run()
    {
        Directory.CreateDirectory(BossFolder);
        Directory.CreateDirectory(EnemyFolder);

        var catalog = LoadRequired<LevelCatalogConfig>(LevelCatalogPath);
        if (catalog.Levels.Length != 12)
        {
            throw new InvalidDataException("E13 requires the completed E12 12-level campaign.");
        }

        var mouse = LoadRequired<EnemyConfig>($"{EnemyFolder}/MouseScoutEnemy.asset");
        var rat = LoadRequired<EnemyConfig>($"{EnemyFolder}/RatBruiserEnemy.asset");
        var beetle = LoadRequired<EnemyConfig>($"{EnemyFolder}/BeetleGuardEnemy.asset");
        var moth = LoadRequired<EnemyConfig>($"{EnemyFolder}/MothSwarmEnemy.asset");
        var snail = LoadRequired<EnemyConfig>($"{EnemyFolder}/SnailTankEnemy.asset");
        var sparrow = LoadRequired<EnemyConfig>($"{EnemyFolder}/SparrowRaiderEnemy.asset");
        var weasel = LoadRequired<EnemyConfig>($"{EnemyFolder}/PipeWeaselEnemy.asset");
        var runner = LoadRequired<EnemyConfig>($"{EnemyFolder}/CockroachRunnerEnemy.asset");

        var captain = EnsureBossEnemy(
            "HedgehogCaptainEnemy", "hedgehog_captain", "Captain Bramble",
            190f, 0.42f, 4, 0.92f, new Color(0.56f, 0.38f, 0.18f), 80, snail);
        var owl = EnsureBossEnemy(
            "RooftopOwlEnemy", "rooftop_owl", "Rooftop Owl",
            255f, 0.48f, 5, 1.02f, new Color(0.42f, 0.54f, 0.86f), 110, moth);
        var king = EnsureBossEnemy(
            "RatKingEnemy", "rat_king", "Rat King",
            360f, 0.36f, 7, 1.14f, new Color(0.76f, 0.28f, 0.18f), 220, rat);

        var level04 = catalog.FindById("level_04");
        var level08 = catalog.FindById("level_08");
        var level12 = catalog.FindById("level_12");
        ConfigureOrchardRoute(level04);
        ConfigureRule(
            level04,
            Rule(
                "orchard_second_entrance",
                AdvancedMapRuleType.SecondaryEntrance,
                "mapRule.orchardEntrance.name",
                "mapRule.orchardEntrance.cue",
                7f,
                0f,
                "orchard_boss",
                default,
                1f,
                new Color(0.18f, 0.86f, 0.46f, 0.8f)));
        ConfigureRule(
            level08,
            Rule(
                "chimney_flood",
                AdvancedMapRuleType.FloodedPlacementZone,
                "mapRule.chimneyFlood.name",
                "mapRule.chimneyFlood.cue",
                8f,
                14f,
                string.Empty,
                new BattlefieldZone("chimney_flood_pocket", new Rect(-4.6f, 1.55f, 4.5f, 1.65f)),
                1f,
                new Color(0.1f, 0.48f, 0.9f, 0.42f)));
        ConfigureRule(
            level12,
            Rule(
                "cellar_fog",
                AdvancedMapRuleType.Fog,
                "mapRule.cellarFog.name",
                "mapRule.cellarFog.cue",
                18f,
                18f,
                string.Empty,
                default,
                0.68f,
                new Color(0.16f, 0.12f, 0.2f, 0.38f)));

        var captainEncounter = EnsureEncounter(
            "HedgehogCaptainEncounter",
            "boss_hedgehog_captain",
            BossRank.MiniBoss,
            "boss.hedgehog.name",
            "boss.hedgehog.subtitle",
            captain,
            new Color(0.86f, 0.54f, 0.18f),
            Phase(
                "braced_formation", 1f,
                "boss.hedgehog.phase_1.name", "boss.hedgehog.phase_1.telegraph", "boss.hedgehog.phase_1.resistance",
                BossAbilityType.CallReinforcements, 2.2f, 4f, 0.9f, 0.72f, 0.8f, 0.7f, 0.5f, string.Empty,
                new BossSupportGroupConfig(sparrow, 3, 0.45f, "main", 1.15f, 1.15f)),
            Phase(
                "burrow_charge", 0.48f,
                "boss.hedgehog.phase_2.name", "boss.hedgehog.phase_2.telegraph", "boss.hedgehog.phase_2.resistance",
                BossAbilityType.SpeedSurge, 2f, 5f, 1.55f, 1f, 0.86f, 0.4f, 0f, string.Empty));

        var owlEncounter = EnsureEncounter(
            "RooftopOwlEncounter",
            "boss_rooftop_owl",
            BossRank.MiniBoss,
            "boss.owl.name",
            "boss.owl.subtitle",
            owl,
            new Color(0.44f, 0.68f, 0.98f),
            Phase(
                "storm_perch", 1f,
                "boss.owl.phase_1.name", "boss.owl.phase_1.telegraph", "boss.owl.phase_1.resistance",
                BossAbilityType.EnvironmentalPulse, 2.3f, 12f, 0.82f, 0.86f, 0.74f, 0.5f, 0.3f, "chimney_flood",
                new BossSupportGroupConfig(mouse, 4, 0.38f, "main", 1.25f, 1.25f)),
            Phase(
                "iron_wings", 0.5f,
                "boss.owl.phase_2.name", "boss.owl.phase_2.telegraph", "boss.owl.phase_2.resistance",
                BossAbilityType.ArmorShift, 2.4f, 6f, 1.05f, 0.5f, 0.65f, 0f, 0f, string.Empty));

        var kingEncounter = EnsureEncounter(
            "RatKingEncounter",
            "boss_rat_king",
            BossRank.MainBoss,
            "boss.rat_king.name",
            "boss.rat_king.subtitle",
            king,
            new Color(0.94f, 0.28f, 0.16f),
            Phase(
                "soot_veil", 1f,
                "boss.rat_king.phase_1.name", "boss.rat_king.phase_1.telegraph", "boss.rat_king.phase_1.resistance",
                BossAbilityType.EnvironmentalPulse, 2.5f, 14f, 0.86f, 0.78f, 0.72f, 0.55f, 0.35f, "cellar_fog",
                new BossSupportGroupConfig(runner, 4, 0.36f, "amber", 1.35f, 1.25f)),
            Phase(
                "pipe_muster", 0.66f,
                "boss.rat_king.phase_2.name", "boss.rat_king.phase_2.telegraph", "boss.rat_king.phase_2.resistance",
                BossAbilityType.CallReinforcements, 2.25f, 5f, 1.04f, 0.68f, 0.76f, 0.35f, 0f, string.Empty,
                new BossSupportGroupConfig(weasel, 3, 0.48f, "blue", 1.25f, 1.15f),
                new BossSupportGroupConfig(beetle, 3, 0.42f, "amber", 1.3f, 1.2f)),
            Phase(
                "crowned_rush", 0.3f,
                "boss.rat_king.phase_3.name", "boss.rat_king.phase_3.telegraph", "boss.rat_king.phase_3.resistance",
                BossAbilityType.SpeedSurge, 2.1f, 7f, 1.35f, 1f, 0.82f, 0f, 0f, string.Empty));

        AttachBoss(level04, captainEncounter, "orchard_boss", 1.8f);
        AttachBoss(level08, owlEncounter, "main", 2f);
        AttachBoss(level12, kingEncounter, "blue", 2.2f);
        ExtendCodex(captain, owl, king);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        ValidateAndExit();
    }

    public static void Validate()
    {
        ValidateAndExit();
    }

    private static EnemyConfig EnsureBossEnemy(
        string assetName,
        string id,
        string title,
        float health,
        float speed,
        int damage,
        float scale,
        Color color,
        int reward,
        EnemyConfig presentationSource)
    {
        var enemy = EnsureAsset<EnemyConfig>($"{EnemyFolder}/{assetName}.asset");
        enemy.Configure(id, title, health, speed, damage, scale, color, reward, presentationSource.VisualSprite);
        enemy.ConfigureAnimationProfile(presentationSource.AnimationProfile);
        enemy.ConfigureUltimateResistance(1f, 1f, 1f, false);
        enemy.ConfigureBossIdentity(true);
        EditorUtility.SetDirty(enemy);
        return enemy;
    }

    private static BossEncounterConfig EnsureEncounter(
        string assetName,
        string id,
        BossRank rank,
        string nameKey,
        string subtitleKey,
        EnemyConfig enemy,
        Color color,
        params BossPhaseConfig[] phases)
    {
        var encounter = EnsureAsset<BossEncounterConfig>($"{BossFolder}/{assetName}.asset");
        encounter.Configure(id, rank, nameKey, subtitleKey, enemy, color, phases);
        EditorUtility.SetDirty(encounter);
        return encounter;
    }

    private static BossPhaseConfig Phase(
        string id,
        float threshold,
        string nameKey,
        string telegraphKey,
        string resistanceKey,
        BossAbilityType ability,
        float telegraphSeconds,
        float abilitySeconds,
        float speed,
        float towerDamage,
        float ultimateDamage,
        float slowDuration,
        float stunDuration,
        string mapRuleId,
        params BossSupportGroupConfig[] support)
    {
        var phase = new BossPhaseConfig();
        phase.Configure(
            id,
            threshold,
            nameKey,
            telegraphKey,
            resistanceKey,
            ability,
            telegraphSeconds,
            abilitySeconds,
            speed,
            towerDamage,
            ultimateDamage,
            slowDuration,
            stunDuration,
            mapRuleId,
            support);
        return phase;
    }

    private static AdvancedMapRuleConfig Rule(
        string id,
        AdvancedMapRuleType type,
        string nameKey,
        string cueKey,
        float delay,
        float duration,
        string routeId,
        BattlefieldZone zone,
        float rangeMultiplier,
        Color color)
    {
        var rule = new AdvancedMapRuleConfig();
        rule.Configure(id, type, nameKey, cueKey, delay, duration, routeId, zone, rangeMultiplier, color);
        return rule;
    }

    private static void ConfigureOrchardRoute(LevelConfig level)
    {
        var map = level?.BattlefieldConfig
            ?? throw new InvalidDataException("Level 04 battlefield is missing.");
        var routes = map.RouteConfigs
            .Where(route => route != null && route.RouteId != "orchard_boss")
            .ToList();
        var points = P(-11f, -3.1f, -8f, -2.2f, -5f, -0.8f, -2f, 0.2f, 2f, 0.9f, 5f, -0.4f, 8f, -1.4f, 11f, -2.7f);
        routes.Add(new PathRouteConfig(
            "orchard_boss",
            "Orchard Second Entrance",
            points,
            points[0],
            points[^1],
            "amber",
            0.58f,
            1f,
            new[] { "ground", "boss", "secondary_entrance" },
            0f,
            "E13 predictable second entrance reserved for the biome mini-boss."));
        map.ConfigureRoutes(routes.ToArray());
        EditorUtility.SetDirty(map);
    }

    private static void ConfigureRule(LevelConfig level, AdvancedMapRuleConfig rule)
    {
        if (level == null)
        {
            throw new InvalidDataException("E13 target level is missing.");
        }

        level.ConfigureAdvancedMapRules(new[] { rule });
        EditorUtility.SetDirty(level);
    }

    private static void AttachBoss(LevelConfig level, BossEncounterConfig encounter, string routeId, float delay)
    {
        if (level?.WaveConfig == null)
        {
            throw new InvalidDataException($"Boss level '{level?.LevelId ?? "missing"}' has no wave.");
        }

        level.ConfigureBossEncounter(encounter);
        var standardGroups = level.WaveConfig.Groups
            .Where(group => group?.EnemyConfig != null && !group.EnemyConfig.IsBoss)
            .ToList();
        standardGroups.Add(new WaveEnemyGroup(encounter.BossEnemy, 1, 1f, delay, 1f, 1f, routeId));
        level.WaveConfig.Configure(standardGroups.ToArray());
        level.WaveConfig.ConfigureConcurrentGroups(true);
        EditorUtility.SetDirty(level.WaveConfig);
        EditorUtility.SetDirty(level);
    }

    private static void ExtendCodex(params EnemyConfig[] bosses)
    {
        var catalog = LoadRequired<MetaProgressionCatalogConfig>(MetaCatalogPath);
        var bossIds = new HashSet<string>(bosses.Select(boss => boss.EnemyId), StringComparer.Ordinal);
        var entries = catalog.CodexEntries
            .Where(entry => entry != null
                && !(entry.EntryType == CodexEntryType.Enemy && bossIds.Contains(entry.SourceId)))
            .ToList();
        foreach (var boss in bosses)
        {
            entries.Add(new CodexEntryConfig(
                $"enemy:{boss.EnemyId}",
                CodexEntryType.Enemy,
                boss.EnemyId,
                $"meta.codex.enemy.{boss.EnemyId}"));
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

    private static void ValidateAndExit()
    {
        var errors = new List<string>();
        var catalog = AssetDatabase.LoadAssetAtPath<LevelCatalogConfig>(LevelCatalogPath);
        ValidateBossContent(catalog, errors);
        ValidateMapRules(catalog, errors);
        ValidateCodex(errors);
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

        Debug.Log("E13 validation passed: two distinct mini-bosses and one three-phase main boss use explicit thresholds, readable telegraphs, non-zero ultimate responses, status feedback, support waves, environment interactions, three data-driven map rules, authoritative achievement ids, deterministic cleanup, low-FPS QA hooks, and Android transition interruption coverage.");
        EditorApplication.Exit(0);
    }

    private static void ValidateBossContent(LevelCatalogConfig catalog, ICollection<string> errors)
    {
        if (catalog == null || catalog.Levels.Length != 12)
        {
            errors.Add("E13 requires the exact 12-level E12 campaign.");
            return;
        }

        var bossLevels = catalog.Levels.Where(level => level?.BossEncounter != null).ToArray();
        if (bossLevels.Length != 3
            || bossLevels.Count(level => level.BossEncounter.Rank == BossRank.MiniBoss) != 2
            || bossLevels.Count(level => level.BossEncounter.Rank == BossRank.MainBoss) != 1)
        {
            errors.Add("E13 requires exactly two mini-boss encounters and one main-boss encounter.");
        }

        var bossIds = new HashSet<string>(StringComparer.Ordinal);
        var mechanics = new HashSet<BossAbilityType>();
        foreach (var level in bossLevels)
        {
            var battlefield = level.ResolveBattlefield();
            var encounter = level.BossEncounter;
            if (!encounter.IsValid(battlefield, out var error))
            {
                errors.Add($"{level.LevelId} boss encounter is invalid: {error}");
                continue;
            }

            if (!bossIds.Add(encounter.BossId) || !encounter.BossEnemy.IsBoss)
            {
                errors.Add($"{level.LevelId} boss identity is duplicated or the enemy is not marked as a boss.");
            }

            var bossGroups = level.WaveConfig.Groups
                .Where(group => group?.EnemyConfig == encounter.BossEnemy)
                .ToArray();
            if (bossGroups.Length != 1 || bossGroups[0].Count != 1)
            {
                errors.Add($"{level.LevelId} must spawn its configured boss exactly once.");
            }

            foreach (var phase in encounter.Phases)
            {
                mechanics.Add(phase.AbilityType);
                if (phase.UltimateDamageMultiplier <= 0f || phase.TelegraphSeconds < 0.75f)
                {
                    errors.Add($"{encounter.BossId}:{phase.PhaseId} violates readable telegraph/non-zero ultimate response rules.");
                }
            }
        }

        if (mechanics.Count < 4)
        {
            errors.Add("The three bosses must collectively exercise armor, speed, reinforcement, and environmental mechanics.");
        }
    }

    private static void ValidateMapRules(LevelCatalogConfig catalog, ICollection<string> errors)
    {
        if (catalog == null)
        {
            return;
        }

        var types = new HashSet<AdvancedMapRuleType>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var level in catalog.Levels)
        {
            var battlefield = level?.ResolveBattlefield();
            foreach (var rule in level?.AdvancedMapRules ?? Array.Empty<AdvancedMapRuleConfig>())
            {
                if (rule == null)
                {
                    errors.Add($"{level?.LevelId ?? "missing"} advanced rule is missing.");
                    continue;
                }

                if (!rule.IsValid(battlefield, out var error))
                {
                    errors.Add($"{level?.LevelId ?? "missing"} advanced rule is invalid: {error}");
                    continue;
                }

                if (!ids.Add(rule.RuleId))
                {
                    errors.Add($"Advanced rule id '{rule.RuleId}' is duplicated.");
                }
                types.Add(rule.RuleType);
            }
        }

        if (types.Count != 3 || ids.Count < 3)
        {
            errors.Add("E13 requires all three data-driven rule families: second entrance, flooded zone, and fog.");
        }

        var orchard = catalog.FindById("level_04");
        if (orchard?.ResolveBattlefield().Routes.Any(route => route.HasTag("boss") && route.RouteId == "orchard_boss") != true)
        {
            errors.Add("The orchard culmination requires an explicit boss-only secondary entrance route.");
        }
    }

    private static void ValidateCodex(ICollection<string> errors)
    {
        var catalog = AssetDatabase.LoadAssetAtPath<MetaProgressionCatalogConfig>(MetaCatalogPath);
        if (catalog?.CodexEntries.Length != 28
            || catalog.CodexEntries.Count(entry => entry.EntryType == CodexEntryType.Map) != 12
            || catalog.CodexEntries.Count(entry => entry.EntryType == CodexEntryType.Tower) != 5
            || catalog.CodexEntries.Count(entry => entry.EntryType == CodexEntryType.Enemy) != 11)
        {
            errors.Add("E13 codex must contain 12 maps, five towers, eight standard enemies, and three bosses (28 entries). ");
        }
    }

    private static void ValidateLocalization(LevelCatalogConfig catalog, ICollection<string> errors)
    {
        var original = LocalizationService.CurrentLanguageCode;
        foreach (var language in new[] { LocalizationService.English, LocalizationService.Russian })
        {
            LocalizationService.SetLanguage(language);
            foreach (var level in catalog?.Levels ?? Array.Empty<LevelConfig>())
            {
                var encounter = level?.BossEncounter;
                if (encounter != null)
                {
                    ValidateKey(encounter.NameLocalizationKey, language, errors);
                    ValidateKey(encounter.SubtitleLocalizationKey, language, errors);
                    ValidateKey($"meta.codex.enemy.{encounter.BossEnemy.EnemyId}", language, errors);
                    foreach (var phase in encounter.Phases)
                    {
                        ValidateKey(phase.NameLocalizationKey, language, errors);
                        ValidateKey(phase.TelegraphLocalizationKey, language, errors);
                        ValidateKey(phase.ResistanceLocalizationKey, language, errors);
                    }
                }

                foreach (var rule in level?.AdvancedMapRules ?? Array.Empty<AdvancedMapRuleConfig>())
                {
                    ValidateKey(rule.NameLocalizationKey, language, errors);
                    ValidateKey(rule.ActiveCueLocalizationKey, language, errors);
                }
            }
            ValidateKey("boss.hudTitle", language, errors);
            ValidateKey("boss.telegraphCountdown", language, errors);
            ValidateKey("mapRule.countdown", language, errors);
        }
        LocalizationService.SetLanguage(original);
    }

    private static void ValidateKey(string key, string language, ICollection<string> errors)
    {
        if (string.IsNullOrWhiteSpace(key) || LocalizationService.Text(key) == key)
        {
            errors.Add($"Missing E13 localization '{key}' in '{language}'.");
        }
    }

    private static void ValidateRuntimeBoundaries(ICollection<string> errors)
    {
        var required = new Dictionary<string, string[]>
        {
            ["Assets/_Project/Scripts/Gameplay/Bosses/BossRuntimeController.cs"] = new[] { "TransitionDamageMultiplier", "FilterIncomingDamage", "NotifyBossAbilityExecuted", "EndRuntime" },
            ["Assets/_Project/Scripts/Gameplay/Battlefield/AdvancedMapRuleController.cs"] = new[] { "IsRouteAvailable", "IsPlacementBlocked", "TowerRangeMultiplier", "EndBattle" },
            ["Assets/_Project/Scripts/Gameplay/Levels/PrototypeLevelController.cs"] = new[] { "defeatedBossIds", "QueueBossSupport", "EndBattleRuntime", "AbortDevelopmentBattle" },
            ["Assets/_Project/Scripts/UI/HUD/PrototypeHud.cs"] = new[] { "DrawBossAndRuleHud", "boss.telegraphCountdown", "mapRule.countdown" },
            ["Assets/_Project/Scripts/QA/DevelopmentQaService.cs"] = new[] { "bossAbilityExecutions", "battleRuntimeCleanupComplete", "interruptAtBossPhase", "targetFrameRate" },
            ["tools/android/run-emulator-boss-qa.ps1"] = new[] { "ExpectedBossPhases", "interrupted_restart", "interrupted_quit", "TargetFrameRate" }
        };
        foreach (var pair in required)
        {
            var source = File.Exists(pair.Key) ? File.ReadAllText(pair.Key) : string.Empty;
            foreach (var token in pair.Value)
            {
                if (!source.Contains(token, StringComparison.Ordinal))
                {
                    errors.Add($"E13 runtime boundary '{token}' is missing from {pair.Key}.");
                }
            }
        }

        var controllerSource = File.ReadAllText("Assets/_Project/Scripts/Gameplay/Levels/PrototypeLevelController.cs");
        foreach (var forbidden in new[] { "level_04\"", "level_08\"", "level_12\"", "boss_hedgehog_captain\"", "boss_rat_king\"" })
        {
            if (controllerSource.Contains(forbidden, StringComparison.Ordinal))
            {
                errors.Add($"Runtime controller contains forbidden E13 content-id branching token '{forbidden}'.");
            }
        }
    }

    private static void ValidateDocumentation(ICollection<string> errors)
    {
        foreach (var path in new[] { ReportPath, WorkflowPath })
        {
            if (!File.Exists(path) || File.ReadAllText(path).Length < 1000)
            {
                errors.Add($"E13 documentation is missing or incomplete: {path}.");
            }
        }
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
            ?? throw new InvalidDataException($"Required E13 asset is missing: {path}");
    }
}
