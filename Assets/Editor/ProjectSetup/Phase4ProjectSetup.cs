using System.Collections.Generic;
using System.IO;
using CatGuard.Gameplay.Enemies;
using CatGuard.Gameplay.Grid;
using CatGuard.Gameplay.Levels;
using CatGuard.Gameplay.Towers;
using CatGuard.Gameplay.Waves;
using CatGuard.Meta.Upgrades;
using CatGuard.UI.HUD;
using CatGuard.UI.Screens;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Phase4ProjectSetup
{
    private const string MainMenuScenePath = "Assets/_Project/Scenes/MainMenu.unity";
    private const string LevelScenePath = "Assets/_Project/Scenes/Level.unity";
    private const string TowerConfigFolder = "Assets/_Project/ScriptableObjects/Towers";
    private const string EnemyConfigFolder = "Assets/_Project/ScriptableObjects/Enemies";
    private const string LevelConfigFolder = "Assets/_Project/ScriptableObjects/Levels";
    private const string EconomyConfigFolder = "Assets/_Project/ScriptableObjects/Economy";

    private const string DartTowerPath = TowerConfigFolder + "/CatDartTower.asset";
    private const string YarnTowerPath = TowerConfigFolder + "/YarnCannonTower.asset";
    private const string BellTowerPath = TowerConfigFolder + "/BellSniperTower.asset";

    private const string ScoutEnemyPath = EnemyConfigFolder + "/MouseScoutEnemy.asset";
    private const string BruiserEnemyPath = EnemyConfigFolder + "/RatBruiserEnemy.asset";
    private const string GuardEnemyPath = EnemyConfigFolder + "/BeetleGuardEnemy.asset";

    private const string Wave01Path = LevelConfigFolder + "/FirstCoreWave.asset";
    private const string Wave02Path = LevelConfigFolder + "/GardenPushWave.asset";
    private const string Wave03Path = LevelConfigFolder + "/PorchStandWave.asset";

    private const string Level01Path = LevelConfigFolder + "/Level01Config.asset";
    private const string Level02Path = LevelConfigFolder + "/Level02Config.asset";
    private const string Level03Path = LevelConfigFolder + "/Level03Config.asset";
    private const string LevelCatalogPath = LevelConfigFolder + "/LevelCatalog.asset";

    private const string DamageUpgradePath = EconomyConfigFolder + "/ClawTrainingUpgrade.asset";
    private const string RangeUpgradePath = EconomyConfigFolder + "/WhiskerFocusUpgrade.asset";
    private const string LivesUpgradePath = EconomyConfigFolder + "/CozyCushionsUpgrade.asset";
    private const string UpgradeCatalogPath = EconomyConfigFolder + "/UpgradeCatalog.asset";

    public static void Run()
    {
        EnsureFolders();

        var towers = EnsureTowerConfigs();
        var enemies = EnsureEnemyConfigs();
        var waves = EnsureWaveConfigs(enemies);
        var levels = EnsureLevelConfigs(towers, waves);
        var levelCatalog = EnsureLevelCatalog(levels);
        var upgrades = EnsureUpgradeConfigs();
        var upgradeCatalog = EnsureUpgradeCatalog(upgrades);

        ConfigureMainMenuScene(levelCatalog, upgradeCatalog);
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
    }

    private static TowerConfig[] EnsureTowerConfigs()
    {
        var dart = EnsureAsset<TowerConfig>(DartTowerPath);
        dart.Configure("cat_dart", "Dart", 2.7f, 1f, 0.26f, 0.56f, new Color(0.24f, 0.78f, 0.96f));

        var yarn = EnsureAsset<TowerConfig>(YarnTowerPath);
        yarn.Configure("yarn_cannon", "Yarn", 2.15f, 2.4f, 0.72f, 0.72f, new Color(0.96f, 0.72f, 0.28f));

        var bell = EnsureAsset<TowerConfig>(BellTowerPath);
        bell.Configure("bell_sniper", "Bell", 4f, 1.7f, 0.55f, 0.5f, new Color(0.72f, 0.45f, 0.95f));

        EditorUtility.SetDirty(dart);
        EditorUtility.SetDirty(yarn);
        EditorUtility.SetDirty(bell);

        return new[] { dart, yarn, bell };
    }

    private static EnemyConfig[] EnsureEnemyConfigs()
    {
        var scout = EnsureAsset<EnemyConfig>(ScoutEnemyPath);
        scout.Configure("mouse_scout", "Mouse Scout", 2f, 1.25f, 1, 0.34f, new Color(1f, 0.34f, 0.34f));

        var bruiser = EnsureAsset<EnemyConfig>(BruiserEnemyPath);
        bruiser.Configure("rat_bruiser", "Rat Bruiser", 6f, 0.55f, 2, 0.58f, new Color(0.86f, 0.42f, 0.18f));

        var guard = EnsureAsset<EnemyConfig>(GuardEnemyPath);
        guard.Configure("beetle_guard", "Beetle Guard", 4f, 0.85f, 1, 0.46f, new Color(0.42f, 0.8f, 0.35f));

        EditorUtility.SetDirty(scout);
        EditorUtility.SetDirty(bruiser);
        EditorUtility.SetDirty(guard);

        return new[] { scout, bruiser, guard };
    }

    private static WaveConfig[] EnsureWaveConfigs(IReadOnlyList<EnemyConfig> enemies)
    {
        var wave01 = EnsureAsset<WaveConfig>(Wave01Path);
        wave01.Configure(new[]
        {
            new WaveEnemyGroup(enemies[0], 4, 0.62f),
            new WaveEnemyGroup(enemies[1], 3, 0.95f, 0.7f),
            new WaveEnemyGroup(enemies[2], 3, 0.78f, 0.6f)
        });

        var wave02 = EnsureAsset<WaveConfig>(Wave02Path);
        wave02.Configure(new[]
        {
            new WaveEnemyGroup(enemies[0], 5, 0.52f),
            new WaveEnemyGroup(enemies[2], 4, 0.72f, 0.6f),
            new WaveEnemyGroup(enemies[1], 4, 0.88f, 0.8f)
        });

        var wave03 = EnsureAsset<WaveConfig>(Wave03Path);
        wave03.Configure(new[]
        {
            new WaveEnemyGroup(enemies[0], 4, 0.48f),
            new WaveEnemyGroup(enemies[1], 5, 0.82f, 0.5f),
            new WaveEnemyGroup(enemies[2], 5, 0.66f, 0.6f)
        });

        EditorUtility.SetDirty(wave01);
        EditorUtility.SetDirty(wave02);
        EditorUtility.SetDirty(wave03);

        return new[] { wave01, wave02, wave03 };
    }

    private static LevelConfig[] EnsureLevelConfigs(TowerConfig[] towers, IReadOnlyList<WaveConfig> waves)
    {
        var level01 = EnsureAsset<LevelConfig>(Level01Path);
        level01.Configure(
            "level_01",
            "Garden Gate",
            6,
            4,
            3,
            1.15f,
            new Vector2(-1.75f, -2.8f),
            DefaultPath(),
            towers,
            waves[0],
            35,
            8,
            new[] { "level_02" });

        var level02 = EnsureAsset<LevelConfig>(Level02Path);
        level02.Configure(
            "level_02",
            "Greenhouse",
            6,
            4,
            3,
            1.15f,
            new Vector2(-1.75f, -2.8f),
            DefaultPath(),
            towers,
            waves[1],
            50,
            10,
            new[] { "level_03" });

        var level03 = EnsureAsset<LevelConfig>(Level03Path);
        level03.Configure(
            "level_03",
            "Porch Stand",
            7,
            4,
            3,
            1.15f,
            new Vector2(-1.75f, -2.8f),
            DefaultPath(),
            towers,
            waves[2],
            70,
            12,
            new string[0]);

        EditorUtility.SetDirty(level01);
        EditorUtility.SetDirty(level02);
        EditorUtility.SetDirty(level03);

        return new[] { level01, level02, level03 };
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
        damage.Configure("claw_training", "Claw Training", UpgradeEffectType.TowerDamageMultiplier, 3, 25, 25, 0.15f);

        var range = EnsureAsset<UpgradeConfig>(RangeUpgradePath);
        range.Configure("whisker_focus", "Whisker Focus", UpgradeEffectType.TowerRangeMultiplier, 3, 25, 25, 0.12f);

        var lives = EnsureAsset<UpgradeConfig>(LivesUpgradePath);
        lives.Configure("cozy_cushions", "Cozy Cushions", UpgradeEffectType.BaseLivesBonus, 3, 30, 30, 1f);

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

    private static void ConfigureMainMenuScene(LevelCatalogConfig levelCatalog, UpgradeCatalogConfig upgradeCatalog)
    {
        var scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
        var controller = Object.FindFirstObjectByType<MainMenuController>();
        if (controller == null)
        {
            var controllerObject = new GameObject("MainMenuController");
            controller = controllerObject.AddComponent<MainMenuController>();
        }

        controller.Configure(levelCatalog, upgradeCatalog);
        EditorUtility.SetDirty(controller);
        EditorSceneManager.SaveScene(scene);
    }

    private static void ConfigureLevelScene(LevelConfig defaultLevel)
    {
        var scene = EditorSceneManager.OpenScene(LevelScenePath, OpenSceneMode.Single);
        EnsureCamera();

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

    private static void EnsureCamera()
    {
        var camera = Camera.main;
        if (camera == null)
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            camera = cameraObject.AddComponent<Camera>();
        }

        camera.orthographic = true;
        camera.orthographicSize = 5.4f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.08f, 0.09f, 0.12f);
        EditorUtility.SetDirty(camera);
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

    private static void ValidateAndExit()
    {
        var errors = new List<string>();

        var levelCatalog = AssetDatabase.LoadAssetAtPath<LevelCatalogConfig>(LevelCatalogPath);
        var upgradeCatalog = AssetDatabase.LoadAssetAtPath<UpgradeCatalogConfig>(UpgradeCatalogPath);

        if (levelCatalog == null || !levelCatalog.IsValid() || levelCatalog.Levels.Length < 3)
        {
            errors.Add("LevelCatalog must contain at least three valid level configs.");
        }

        if (upgradeCatalog == null || !upgradeCatalog.IsValid() || upgradeCatalog.Upgrades.Length < 3)
        {
            errors.Add("UpgradeCatalog must contain three valid upgrade configs.");
        }

        ValidateMainMenu(levelCatalog, upgradeCatalog, errors);
        ValidateLevelScene(levelCatalog, errors);

        if (errors.Count > 0)
        {
            foreach (var error in errors)
            {
                Debug.LogError(error);
            }

            EditorApplication.Exit(1);
            return;
        }

        Debug.Log("Phase 4 validation passed: save, currency, upgrades, level selection, and unlock catalogs are configured.");
        EditorApplication.Exit(0);
    }

    private static void ValidateMainMenu(
        LevelCatalogConfig levelCatalog,
        UpgradeCatalogConfig upgradeCatalog,
        ICollection<string> errors)
    {
        var scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
        var controller = Object.FindFirstObjectByType<MainMenuController>();

        if (!scene.isLoaded)
        {
            errors.Add($"{MainMenuScenePath} did not load for validation.");
        }

        if (controller == null || !controller.IsConfigured)
        {
            errors.Add("MainMenuController must be configured with level and upgrade catalogs.");
        }
    }

    private static void ValidateLevelScene(LevelCatalogConfig levelCatalog, ICollection<string> errors)
    {
        var scene = EditorSceneManager.OpenScene(LevelScenePath, OpenSceneMode.Single);
        var controller = Object.FindFirstObjectByType<PrototypeLevelController>();

        if (!scene.isLoaded)
        {
            errors.Add($"{LevelScenePath} did not load for validation.");
        }

        if (controller == null || !controller.IsConfigured)
        {
            errors.Add("PrototypeLevelController must remain configured after Phase 4 setup.");
            return;
        }

        if (levelCatalog?.FirstLevel != null && controller.Config != levelCatalog.FirstLevel)
        {
            errors.Add("PrototypeLevelController default config must point to the first catalog level.");
        }
    }
}
