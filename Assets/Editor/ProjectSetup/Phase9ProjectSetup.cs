using System.Collections.Generic;
using System.IO;
using CatGuard.Core.Localization;
using CatGuard.Gameplay.Enemies;
using CatGuard.Gameplay.Grid;
using CatGuard.Gameplay.Levels;
using CatGuard.Gameplay.Towers;
using CatGuard.Gameplay.Waves;
using CatGuard.Meta.DailyRewards;
using CatGuard.Meta.Upgrades;
using CatGuard.UI.HUD;
using CatGuard.UI.Screens;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Phase9ProjectSetup
{
    private const string MainMenuScenePath = "Assets/_Project/Scenes/MainMenu.unity";
    private const string LevelScenePath = "Assets/_Project/Scenes/Level.unity";
    private const string TowerConfigFolder = "Assets/_Project/ScriptableObjects/Towers";
    private const string EnemyConfigFolder = "Assets/_Project/ScriptableObjects/Enemies";
    private const string LevelConfigFolder = "Assets/_Project/ScriptableObjects/Levels";
    private const string EconomyConfigFolder = "Assets/_Project/ScriptableObjects/Economy";
    private const string TowerArtFolder = "Assets/_Project/Art/Units/Towers";
    private const string EnemyArtFolder = "Assets/_Project/Art/Units/Enemies";

    private const string LevelCatalogPath = LevelConfigFolder + "/LevelCatalog.asset";
    private const string UpgradeCatalogPath = EconomyConfigFolder + "/UpgradeCatalog.asset";
    private const string DamageUpgradePath = EconomyConfigFolder + "/ClawTrainingUpgrade.asset";
    private const string RangeUpgradePath = EconomyConfigFolder + "/WhiskerFocusUpgrade.asset";
    private const string LivesUpgradePath = EconomyConfigFolder + "/CozyCushionsUpgrade.asset";
    private const string DailyRewardChainPath = EconomyConfigFolder + "/DailyRewardChain.asset";
    private const string DailyMissionCatalogPath = EconomyConfigFolder + "/DailyMissionCatalog.asset";

    public static void Run()
    {
        EnsureFolders();

        var towers = EnsureTowerConfigs();
        var enemies = EnsureEnemyConfigs();
        var waves = EnsureWaveConfigs(enemies);
        var levels = EnsureLevelConfigs(towers, waves);
        var levelCatalog = EnsureLevelCatalog(levels);
        EnsureUpgradeCatalog(EnsureUpgradeConfigs());

        ConfigureMainMenuScene(levelCatalog);
        ConfigureLevelScene(levels[0]);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        ValidateAndExit();
    }

    public static void Validate()
    {
        ValidateAndExit();
    }

    private static void EnsureFolders()
    {
        Directory.CreateDirectory(TowerConfigFolder);
        Directory.CreateDirectory(EnemyConfigFolder);
        Directory.CreateDirectory(LevelConfigFolder);
        Directory.CreateDirectory(EconomyConfigFolder);
        Directory.CreateDirectory(TowerArtFolder);
        Directory.CreateDirectory(EnemyArtFolder);
    }

    private static TowerConfig[] EnsureTowerConfigs()
    {
        var dart = EnsureTower("CatDartTower", "cat_dart", "Dart", 2.7f, 1f, 0.28f, 0.56f, new Color(0.24f, 0.78f, 0.96f), 45, 0f, $"{TowerArtFolder}/cat_dart.png");
        var yarn = EnsureTower("YarnCannonTower", "yarn_cannon", "Yarn", 2.25f, 1.8f, 0.75f, 0.72f, new Color(0.96f, 0.72f, 0.28f), 50, 0.7f, $"{TowerArtFolder}/yarn_cannon.png");
        var bell = EnsureTower("BellSniperTower", "bell_sniper", "Bell", 4.4f, 4.2f, 1.05f, 0.5f, new Color(0.72f, 0.45f, 0.95f), 55, 0f, $"{TowerArtFolder}/bell_sniper.png");
        var laser = EnsureTower("LaserPointerTower", "laser_pointer", "Laser", 3.2f, 0.65f, 0.14f, 0.48f, new Color(0.38f, 1f, 0.62f), 60, 0f, $"{TowerArtFolder}/laser_pointer.png");
        var blanket = EnsureTower("BlanketBoomTower", "blanket_boom", "Blanket", 1.9f, 4.8f, 1.15f, 0.82f, new Color(1f, 0.42f, 0.62f), 65, 1.05f, $"{TowerArtFolder}/blanket_boom.png");

        return new[] { dart, yarn, bell, laser, blanket };
    }

    private static TowerConfig EnsureTower(
        string assetName,
        string id,
        string displayName,
        float range,
        float damage,
        float interval,
        float scale,
        Color color,
        int buildCost,
        float splashRadius,
        string spritePath)
    {
        var tower = EnsureAsset<TowerConfig>($"{TowerConfigFolder}/{assetName}.asset");
        tower.Configure(id, displayName, range, damage, interval, scale, color, buildCost, splashRadius, EnsureUnitSprite(spritePath));
        EditorUtility.SetDirty(tower);
        return tower;
    }

    private static EnemyConfig[] EnsureEnemyConfigs()
    {
        var scout = EnsureEnemy("MouseScoutEnemy", "mouse_scout", "Mouse Scout", 2f, 1.25f, 1, 0.34f, new Color(1f, 0.34f, 0.34f), 5, $"{EnemyArtFolder}/mouse_scout.png");
        var bruiser = EnsureEnemy("RatBruiserEnemy", "rat_bruiser", "Rat Bruiser", 6f, 0.55f, 2, 0.58f, new Color(0.86f, 0.42f, 0.18f), 12, $"{EnemyArtFolder}/rat_bruiser.png");
        var guard = EnsureEnemy("BeetleGuardEnemy", "beetle_guard", "Beetle Guard", 4f, 0.85f, 1, 0.46f, new Color(0.42f, 0.8f, 0.35f), 8, $"{EnemyArtFolder}/beetle_guard.png");
        var moth = EnsureEnemy("MothSwarmEnemy", "moth_swarm", "Moth Swarm", 1.5f, 1.55f, 1, 0.3f, new Color(0.95f, 0.74f, 1f), 4, $"{EnemyArtFolder}/moth_swarm.png");
        var snail = EnsureEnemy("SnailTankEnemy", "snail_tank", "Snail Tank", 10f, 0.35f, 3, 0.7f, new Color(0.35f, 0.72f, 0.92f), 18, $"{EnemyArtFolder}/snail_tank.png");

        return new[] { scout, bruiser, guard, moth, snail };
    }

    private static EnemyConfig EnsureEnemy(
        string assetName,
        string id,
        string displayName,
        float health,
        float speed,
        int baseDamage,
        float scale,
        Color color,
        int battleFishReward,
        string spritePath)
    {
        var enemy = EnsureAsset<EnemyConfig>($"{EnemyConfigFolder}/{assetName}.asset");
        enemy.Configure(id, displayName, health, speed, baseDamage, scale, color, battleFishReward, EnsureUnitSprite(spritePath));
        EditorUtility.SetDirty(enemy);
        return enemy;
    }

    private static Sprite EnsureUnitSprite(string assetPath)
    {
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            throw new InvalidDataException($"Unit sprite texture is missing or invalid: {assetPath}");
        }

        var requiresReimport = importer.textureType != TextureImporterType.Sprite
            || importer.spriteImportMode != SpriteImportMode.Single
            || !importer.alphaIsTransparency
            || importer.mipmapEnabled
            || importer.spritePixelsPerUnit != 256f
            || importer.filterMode != FilterMode.Bilinear
            || importer.wrapMode != TextureWrapMode.Clamp
            || importer.textureCompression != TextureImporterCompression.Uncompressed;

        if (requiresReimport)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.spritePixelsPerUnit = 256f;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (sprite == null)
        {
            throw new InvalidDataException($"Unit sprite import did not produce a Sprite: {assetPath}");
        }

        return sprite;
    }

    private static WaveConfig[] EnsureWaveConfigs(IReadOnlyList<EnemyConfig> enemies)
    {
        var waves = new[]
        {
            EnsureWave("FirstCoreWave", new[] { new WaveEnemyGroup(enemies[0], 4, 0.7f), new WaveEnemyGroup(enemies[2], 2, 0.85f, 0.8f), new WaveEnemyGroup(enemies[3], 2, 0.45f, 0.5f) }),
            EnsureWave("GardenPushWave", new[] { new WaveEnemyGroup(enemies[0], 5, 0.58f), new WaveEnemyGroup(enemies[2], 3, 0.76f, 0.6f), new WaveEnemyGroup(enemies[3], 4, 0.42f, 0.5f) }),
            EnsureWave("PorchStandWave", new[] { new WaveEnemyGroup(enemies[0], 4, 0.52f), new WaveEnemyGroup(enemies[1], 3, 0.9f, 0.5f), new WaveEnemyGroup(enemies[2], 3, 0.74f, 0.6f) }),
            EnsureWave("LanternPathWave", new[] { new WaveEnemyGroup(enemies[3], 8, 0.34f), new WaveEnemyGroup(enemies[0], 6, 0.5f, 0.4f), new WaveEnemyGroup(enemies[1], 3, 0.82f, 0.7f) }),
            EnsureWave("FishBarrelWave", new[] { new WaveEnemyGroup(enemies[2], 5, 0.66f), new WaveEnemyGroup(enemies[1], 4, 0.78f, 0.5f), new WaveEnemyGroup(enemies[3], 8, 0.32f, 0.5f) }),
            EnsureWave("MoonlitFenceWave", new[] { new WaveEnemyGroup(enemies[0], 6, 0.46f), new WaveEnemyGroup(enemies[4], 2, 1.1f, 0.8f), new WaveEnemyGroup(enemies[2], 5, 0.62f, 0.7f), new WaveEnemyGroup(enemies[3], 6, 0.3f, 0.4f) }),
            EnsureWave("RoofCornerWave", new[] { new WaveEnemyGroup(enemies[1], 5, 0.7f), new WaveEnemyGroup(enemies[4], 3, 1f, 0.6f), new WaveEnemyGroup(enemies[3], 10, 0.28f, 0.6f) }),
            EnsureWave("OldWellWave", new[] { new WaveEnemyGroup(enemies[2], 9, 0.52f, 0f, 1.5f, 1.2f), new WaveEnemyGroup(enemies[1], 7, 0.62f, 0.4f, 1.5f, 1.2f), new WaveEnemyGroup(enemies[4], 5, 0.82f, 0.6f, 1.5f, 1.2f) }),
            EnsureWave("OrchardWallWave", new[] { new WaveEnemyGroup(enemies[3], 14, 0.24f, 0f, 1.7f, 1.3f), new WaveEnemyGroup(enemies[0], 9, 0.36f, 0.3f, 1.7f, 1.3f), new WaveEnemyGroup(enemies[4], 6, 0.78f, 0.6f, 1.7f, 1.3f), new WaveEnemyGroup(enemies[1], 7, 0.56f, 0.4f, 1.7f, 1.3f) }),
            EnsureWave("QuietAlleyWave", new[] { new WaveEnemyGroup(enemies[2], 10, 0.44f, 0f, 1.9f, 1.4f), new WaveEnemyGroup(enemies[1], 8, 0.56f, 0.4f, 1.9f, 1.4f), new WaveEnemyGroup(enemies[4], 7, 0.74f, 0.6f, 1.9f, 1.4f), new WaveEnemyGroup(enemies[3], 16, 0.22f, 0.4f, 1.9f, 1.4f) })
        };

        return waves;
    }

    private static WaveConfig EnsureWave(string assetName, WaveEnemyGroup[] groups)
    {
        var wave = EnsureAsset<WaveConfig>($"{LevelConfigFolder}/{assetName}.asset");
        wave.Configure(groups);
        EditorUtility.SetDirty(wave);
        return wave;
    }

    private static LevelConfig[] EnsureLevelConfigs(IReadOnlyList<TowerConfig> towers, IReadOnlyList<WaveConfig> waves)
    {
        var firstThreeTowers = new[] { towers[0], towers[1], towers[2] };
        var firstFourTowers = new[] { towers[0], towers[1], towers[2], towers[3] };
        var allTowers = new[] { towers[0], towers[1], towers[2], towers[3], towers[4] };

        return new[]
        {
            EnsureLevel("Level01Config", "level_01", "Garden Gate", 7, 105, firstThreeTowers, waves[0], 35, 8, "level_02", DefaultPath(), "tutorial.level_01"),
            EnsureLevel("Level02Config", "level_02", "Greenhouse", 7, 115, firstThreeTowers, waves[1], 50, 10, "level_03", GreenhousePath()),
            EnsureLevel("Level03Config", "level_03", "Porch Stand", 7, 125, firstFourTowers, waves[2], 65, 12, "level_04", DefaultPath()),
            EnsureLevel("Level04Config", "level_04", "Lantern Path", 8, 135, firstFourTowers, waves[3], 80, 14, "level_05", LanternPath()),
            EnsureLevel("Level05Config", "level_05", "Fish Barrel", 8, 145, allTowers, waves[4], 100, 16, "level_06", BarrelPath()),
            EnsureLevel("Level06Config", "level_06", "Moonlit Fence", 8, 155, allTowers, waves[5], 120, 18, "level_07", FencePath()),
            EnsureLevel("Level07Config", "level_07", "Roof Corner", 9, 165, allTowers, waves[6], 145, 20, "level_08", RoofPath()),
            EnsureLevel("Level08Config", "level_08", "Old Well", 9, 175, allTowers, waves[7], 170, 22, "level_09", WellPath()),
            EnsureLevel("Level09Config", "level_09", "Orchard Wall", 10, 185, allTowers, waves[8], 200, 25, "level_10", OrchardPath()),
            EnsureLevel("Level10Config", "level_10", "Quiet Alley", 10, 195, allTowers, waves[9], 235, 30, string.Empty, AlleyPath())
        };
    }

    private static LevelConfig EnsureLevel(
        string assetName,
        string id,
        string displayName,
        int lives,
        int startingBattleFish,
        TowerConfig[] towers,
        WaveConfig wave,
        int firstReward,
        int replayReward,
        string nextLevelId,
        Vector2[] path,
        string tutorialKey = "")
    {
        var level = EnsureAsset<LevelConfig>($"{LevelConfigFolder}/{assetName}.asset");
        level.Configure(
            id,
            displayName,
            lives,
            4,
            3,
            1.15f,
            new Vector2(-1.75f, -2.8f),
            path,
            towers,
            wave,
            firstReward,
            replayReward,
            string.IsNullOrWhiteSpace(nextLevelId) ? new string[0] : new[] { nextLevelId },
            tutorialKey,
            startingBattleFish);
        EditorUtility.SetDirty(level);
        return level;
    }

    private static LevelCatalogConfig EnsureLevelCatalog(LevelConfig[] levels)
    {
        var catalog = EnsureAsset<LevelCatalogConfig>(LevelCatalogPath);
        catalog.Configure(levels);
        EditorUtility.SetDirty(catalog);
        return catalog;
    }

    private static UpgradeConfig[] EnsureUpgradeConfigs()
    {
        var damage = EnsureAsset<UpgradeConfig>(DamageUpgradePath);
        damage.Configure("claw_training", "Claw Training", UpgradeEffectType.TowerDamageMultiplier, 3, 100, 75, 0.15f);

        var range = EnsureAsset<UpgradeConfig>(RangeUpgradePath);
        range.Configure("whisker_focus", "Whisker Focus", UpgradeEffectType.TowerRangeMultiplier, 3, 100, 75, 0.12f);

        var lives = EnsureAsset<UpgradeConfig>(LivesUpgradePath);
        lives.Configure("cozy_cushions", "Cozy Cushions", UpgradeEffectType.BaseLivesBonus, 3, 125, 100, 1f);

        EditorUtility.SetDirty(damage);
        EditorUtility.SetDirty(range);
        EditorUtility.SetDirty(lives);
        return new[] { damage, range, lives };
    }

    private static UpgradeCatalogConfig EnsureUpgradeCatalog(UpgradeConfig[] upgrades)
    {
        var catalog = EnsureAsset<UpgradeCatalogConfig>(UpgradeCatalogPath);
        catalog.Configure(upgrades);
        EditorUtility.SetDirty(catalog);
        return catalog;
    }

    private static void ConfigureMainMenuScene(LevelCatalogConfig levelCatalog)
    {
        var upgradeCatalog = AssetDatabase.LoadAssetAtPath<UpgradeCatalogConfig>(UpgradeCatalogPath);
        var dailyRewardChain = AssetDatabase.LoadAssetAtPath<DailyRewardChainConfig>(DailyRewardChainPath);
        var dailyMissionCatalog = AssetDatabase.LoadAssetAtPath<DailyMissionCatalogConfig>(DailyMissionCatalogPath);
        var scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
        var controller = Object.FindFirstObjectByType<MainMenuController>();
        if (controller == null)
        {
            var controllerObject = new GameObject("MainMenuController");
            controller = controllerObject.AddComponent<MainMenuController>();
        }

        controller.Configure(levelCatalog, upgradeCatalog, dailyRewardChain, dailyMissionCatalog);
        EditorUtility.SetDirty(controller);
        EditorSceneManager.SaveScene(scene);
    }

    private static void ConfigureLevelScene(LevelConfig defaultLevel)
    {
        var scene = EditorSceneManager.OpenScene(LevelScenePath, OpenSceneMode.Single);
        var levelRoot = GameObject.Find("PrototypeLevel") ?? new GameObject("PrototypeLevel");
        var runtimeRoot = FindOrCreateChild(levelRoot.transform, "RuntimeRoot");
        var gridRoot = FindOrCreateChild(levelRoot.transform, "TowerGrid");

        var controller = GetOrAddComponent<PrototypeLevelController>(levelRoot);
        var spawner = GetOrAddComponent<PrototypeWaveSpawner>(levelRoot);
        var hud = GetOrAddComponent<PrototypeHud>(levelRoot);
        var grid = GetOrAddComponent<TowerGrid>(gridRoot.gameObject);

        controller.Configure(defaultLevel, grid, spawner, hud, runtimeRoot);
        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(spawner);
        EditorUtility.SetDirty(hud);
        EditorUtility.SetDirty(grid);
        EditorSceneManager.SaveScene(scene);
    }

    private static TAsset EnsureAsset<TAsset>(string assetPath)
        where TAsset : ScriptableObject
    {
        var asset = AssetDatabase.LoadAssetAtPath<TAsset>(assetPath);
        if (asset != null)
        {
            return asset;
        }

        asset = ScriptableObject.CreateInstance<TAsset>();
        asset.name = Path.GetFileNameWithoutExtension(assetPath);
        AssetDatabase.CreateAsset(asset, assetPath);
        return asset;
    }

    private static Transform FindOrCreateChild(Transform parent, string childName)
    {
        var child = parent.Find(childName);
        if (child != null)
        {
            return child;
        }

        var childObject = new GameObject(childName);
        childObject.transform.SetParent(parent, false);
        return childObject.transform;
    }

    private static TComponent GetOrAddComponent<TComponent>(GameObject target)
        where TComponent : Component
    {
        var component = target.GetComponent<TComponent>();
        return component != null ? component : target.AddComponent<TComponent>();
    }

    private static Vector2[] DefaultPath()
    {
        return new[]
        {
            new Vector2(-3.7f, 3f),
            new Vector2(-1.4f, 2.2f),
            new Vector2(1.5f, 2.2f),
            new Vector2(2.8f, 0.7f),
            new Vector2(1.2f, -0.9f),
            new Vector2(3.7f, -2.7f)
        };
    }

    private static Vector2[] GreenhousePath() => new[]
    {
        new Vector2(-3.8f, 2.7f),
        new Vector2(-2.2f, 1.5f),
        new Vector2(0.3f, 1.8f),
        new Vector2(2.5f, 0.8f),
        new Vector2(0.9f, -1.5f),
        new Vector2(3.7f, -2.7f)
    };

    private static Vector2[] LanternPath() => new[]
    {
        new Vector2(-3.8f, 3.1f),
        new Vector2(-3f, 1.1f),
        new Vector2(-0.7f, 1f),
        new Vector2(1.2f, 2.2f),
        new Vector2(3f, 0.4f),
        new Vector2(3.7f, -2.8f)
    };

    private static Vector2[] BarrelPath() => new[]
    {
        new Vector2(-3.7f, 2.9f),
        new Vector2(-1.8f, 2.9f),
        new Vector2(-0.9f, 0.4f),
        new Vector2(1.7f, 0.4f),
        new Vector2(2.4f, -1.4f),
        new Vector2(3.7f, -2.6f)
    };

    private static Vector2[] FencePath() => new[]
    {
        new Vector2(-3.8f, 2.8f),
        new Vector2(-2.6f, 0.9f),
        new Vector2(-0.4f, 2.3f),
        new Vector2(1.2f, 0f),
        new Vector2(2.7f, -0.8f),
        new Vector2(3.7f, -2.7f)
    };

    private static Vector2[] RoofPath() => new[]
    {
        new Vector2(-3.7f, 3f),
        new Vector2(-2.8f, -0.2f),
        new Vector2(-0.8f, -0.4f),
        new Vector2(0.8f, 1.5f),
        new Vector2(2.6f, 0.2f),
        new Vector2(3.7f, -2.8f)
    };

    private static Vector2[] WellPath() => new[]
    {
        new Vector2(-3.8f, 2.6f),
        new Vector2(-1.6f, 1.2f),
        new Vector2(-2.4f, -0.8f),
        new Vector2(0.5f, -0.7f),
        new Vector2(1.7f, 1.3f),
        new Vector2(3.7f, -2.7f)
    };

    private static Vector2[] OrchardPath() => new[]
    {
        new Vector2(-3.8f, 3f),
        new Vector2(-0.9f, 2.5f),
        new Vector2(-1.9f, 0.2f),
        new Vector2(1.4f, 0.5f),
        new Vector2(2.2f, -1.7f),
        new Vector2(3.7f, -2.8f)
    };

    private static Vector2[] AlleyPath() => new[]
    {
        new Vector2(-3.7f, 2.9f),
        new Vector2(-2.7f, 1.8f),
        new Vector2(-0.7f, 2.4f),
        new Vector2(0.4f, -0.5f),
        new Vector2(2.5f, -0.2f),
        new Vector2(3.7f, -2.8f)
    };

    private static void ValidateAndExit()
    {
        var errors = new List<string>();
        var levelCatalog = AssetDatabase.LoadAssetAtPath<LevelCatalogConfig>(LevelCatalogPath);
        var upgradeCatalog = AssetDatabase.LoadAssetAtPath<UpgradeCatalogConfig>(UpgradeCatalogPath);

        ValidateCatalog(levelCatalog, errors);
        ValidateUpgradeBalance(levelCatalog, upgradeCatalog, errors);
        ValidateSceneReferences(levelCatalog, errors);
        ValidateLocalization(levelCatalog, errors);

        if (errors.Count > 0)
        {
            foreach (var error in errors)
            {
                Debug.LogError(error);
            }

            EditorApplication.Exit(1);
            return;
        }

        Debug.Log("Phase 9 validation passed: campaign content, tower roles, splash attacks, kill rewards, battle budgets, meta-upgrade balance, tutorial, and difficulty ramp are configured.");
        EditorApplication.Exit(0);
    }

    private static void ValidateCatalog(LevelCatalogConfig levelCatalog, ICollection<string> errors)
    {
        if (levelCatalog == null || !levelCatalog.IsValid())
        {
            errors.Add("LevelCatalog must exist and contain valid levels.");
            return;
        }

        if (levelCatalog.Levels.Length < 10 || levelCatalog.Levels.Length > 20)
        {
            errors.Add("MVP content must contain 10-20 level configs.");
        }

        var towers = new HashSet<TowerConfig>();
        var enemies = new HashSet<EnemyConfig>();
        var previousThreat = 0f;
        var previousBattleFish = 0;
        for (var index = 0; index < levelCatalog.Levels.Length; index++)
        {
            var level = levelCatalog.Levels[index];
            if (level == null || !level.IsValidForCore())
            {
                errors.Add($"Level at index {index} is invalid.");
                continue;
            }

            foreach (var tower in level.AvailableTowers)
            {
                if (tower != null)
                {
                    towers.Add(tower);
                }
            }

            var threat = CalculateThreat(level.WaveConfig, enemies);
            if (index > 0 && threat <= previousThreat)
            {
                errors.Add($"Difficulty threat must increase level by level: {level.LevelId}.");
            }

            if (level.StartingBattleFish < previousBattleFish)
            {
                errors.Add($"Starting battle Fish must not decrease: {level.LevelId}.");
            }

            previousThreat = threat;
            previousBattleFish = level.StartingBattleFish;
            ValidateUnlock(levelCatalog, index, errors);
        }

        if (towers.Count < 3 || towers.Count > 5)
        {
            errors.Add("MVP content must expose 3-5 tower configs.");
        }

        var splashTowerCount = 0;
        var hasLongRangeTower = false;
        foreach (var tower in towers)
        {
            splashTowerCount += tower.SplashRadius > 0f ? 1 : 0;
            hasLongRangeTower |= tower.Range >= 4f;
            if (tower.VisualSprite == null)
            {
                errors.Add($"Tower {tower.TowerId} must reference its unit sprite.");
            }
        }

        if (splashTowerCount < 2)
        {
            errors.Add("MVP tower roster must contain at least two splash-damage roles.");
        }

        if (!hasLongRangeTower)
        {
            errors.Add("MVP tower roster must contain a long-range role.");
        }

        if (enemies.Count < 5 || enemies.Count > 8)
        {
            errors.Add("MVP content must use 5-8 enemy configs.");
        }

        foreach (var enemy in enemies)
        {
            if (enemy.BattleFishReward <= 0)
            {
                errors.Add($"Enemy {enemy.EnemyId} must grant a positive battle Fish reward.");
            }

            if (enemy.VisualSprite == null)
            {
                errors.Add($"Enemy {enemy.EnemyId} must reference its unit sprite.");
            }
        }

        if (!levelCatalog.Levels[0].HasTutorialText)
        {
            errors.Add("First MVP level must include tutorial text.");
        }
    }

    private static void ValidateUpgradeBalance(
        LevelCatalogConfig levelCatalog,
        UpgradeCatalogConfig upgradeCatalog,
        ICollection<string> errors)
    {
        if (levelCatalog == null || !levelCatalog.IsValid())
        {
            return;
        }

        if (upgradeCatalog == null || !upgradeCatalog.IsValid())
        {
            errors.Add("UpgradeCatalog must contain three valid upgrade configs.");
            return;
        }

        var campaignRewards = 0;
        foreach (var level in levelCatalog.Levels)
        {
            campaignRewards += level.FirstClearRewardCoins;
        }

        var fullUpgradeCost = 0;
        foreach (var upgrade in upgradeCatalog.Upgrades)
        {
            for (var level = 1; level <= upgrade.MaxLevel; level++)
            {
                fullUpgradeCost += upgrade.GetCostForLevel(level);
            }
        }

        if (fullUpgradeCost <= campaignRewards)
        {
            errors.Add("Full meta-upgrade cost must exceed total first-clear campaign rewards.");
        }
    }

    private static float CalculateThreat(WaveConfig wave, ISet<EnemyConfig> enemies)
    {
        var threat = 0f;
        if (wave?.Groups == null)
        {
            return threat;
        }

        foreach (var group in wave.Groups)
        {
            if (group?.EnemyConfig == null)
            {
                continue;
            }

            enemies.Add(group.EnemyConfig);
            threat += group.EnemyConfig.Health
                * group.HealthMultiplier
                * group.SpeedMultiplier
                * group.Count;
        }

        return threat;
    }

    private static void ValidateUnlock(LevelCatalogConfig levelCatalog, int index, ICollection<string> errors)
    {
        var level = levelCatalog.Levels[index];
        if (index == levelCatalog.Levels.Length - 1)
        {
            if (level.UnlocksLevelIds.Length > 0)
            {
                errors.Add("Last MVP level must not unlock a missing next level.");
            }

            return;
        }

        var expectedNextId = levelCatalog.Levels[index + 1]?.LevelId;
        if (level.UnlocksLevelIds.Length != 1 || level.UnlocksLevelIds[0] != expectedNextId)
        {
            errors.Add($"{level.LevelId} must unlock {expectedNextId}.");
        }
    }

    private static void ValidateSceneReferences(LevelCatalogConfig levelCatalog, ICollection<string> errors)
    {
        var mainMenuScene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
        var mainMenu = Object.FindFirstObjectByType<MainMenuController>();
        if (!mainMenuScene.isLoaded || mainMenu == null || !mainMenu.IsConfigured || !mainMenu.IsDailyConfigured)
        {
            errors.Add("MainMenu scene must keep configured catalog, upgrades, and daily content.");
        }

        var levelScene = EditorSceneManager.OpenScene(LevelScenePath, OpenSceneMode.Single);
        var controller = Object.FindFirstObjectByType<PrototypeLevelController>();
        if (!levelScene.isLoaded || controller == null || !controller.IsConfigured)
        {
            errors.Add("Level scene must keep a configured PrototypeLevelController.");
            return;
        }

        if (levelCatalog?.FirstLevel != null && controller.Config != levelCatalog.FirstLevel)
        {
            errors.Add("Level scene default config must point to the first MVP level.");
        }
    }

    private static void ValidateLocalization(LevelCatalogConfig levelCatalog, ICollection<string> errors)
    {
        ValidateLanguage(LocalizationService.English, levelCatalog, errors);
        ValidateLanguage(LocalizationService.Russian, levelCatalog, errors);
    }

    private static void ValidateLanguage(string languageCode, LevelCatalogConfig levelCatalog, ICollection<string> errors)
    {
        LocalizationService.SetLanguage(languageCode);
        foreach (var level in levelCatalog.Levels)
        {
            var key = $"level.{level.LevelId}";
            if (LocalizationService.Text(key) == key)
            {
                errors.Add($"{languageCode} localization must include {level.LevelId}.");
            }
        }

        foreach (var tower in levelCatalog.Levels[^1].AvailableTowers)
        {
            var key = $"tower.{tower.TowerId}";
            if (LocalizationService.Text(key) == key)
            {
                errors.Add($"{languageCode} localization must include {tower.TowerId}.");
            }
        }

        var tutorialKey = levelCatalog.Levels[0].TutorialTextKey;
        if (LocalizationService.Text(tutorialKey) == tutorialKey)
        {
            errors.Add($"{languageCode} localization must include the first tutorial text.");
        }
    }
}
