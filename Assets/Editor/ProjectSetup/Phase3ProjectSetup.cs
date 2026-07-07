using System.Collections.Generic;
using System.IO;
using CatGuard.Gameplay.Enemies;
using CatGuard.Gameplay.Grid;
using CatGuard.Gameplay.Levels;
using CatGuard.Gameplay.Towers;
using CatGuard.Gameplay.Waves;
using CatGuard.UI.HUD;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Phase3ProjectSetup
{
    private const string LevelScenePath = "Assets/_Project/Scenes/Level.unity";
    private const string TowerConfigFolder = "Assets/_Project/ScriptableObjects/Towers";
    private const string EnemyConfigFolder = "Assets/_Project/ScriptableObjects/Enemies";
    private const string WaveConfigFolder = "Assets/_Project/ScriptableObjects/Levels";
    private const string LevelConfigFolder = "Assets/_Project/ScriptableObjects/Levels";

    private const string DartTowerPath = TowerConfigFolder + "/CatDartTower.asset";
    private const string YarnTowerPath = TowerConfigFolder + "/YarnCannonTower.asset";
    private const string BellTowerPath = TowerConfigFolder + "/BellSniperTower.asset";

    private const string ScoutEnemyPath = EnemyConfigFolder + "/MouseScoutEnemy.asset";
    private const string BruiserEnemyPath = EnemyConfigFolder + "/RatBruiserEnemy.asset";
    private const string GuardEnemyPath = EnemyConfigFolder + "/BeetleGuardEnemy.asset";

    private const string WaveConfigPath = WaveConfigFolder + "/FirstCoreWave.asset";
    private const string LevelConfigPath = LevelConfigFolder + "/Level01Config.asset";

    public static void Run()
    {
        EnsureFolders();

        var towers = EnsureTowerConfigs();
        var enemies = EnsureEnemyConfigs();
        var wave = EnsureWaveConfig(enemies);
        var level = EnsureLevelConfig(towers, wave);

        ConfigureLevelScene(level);

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
        Directory.CreateDirectory(WaveConfigFolder);
        Directory.CreateDirectory(LevelConfigFolder);
    }

    private static TowerConfig[] EnsureTowerConfigs()
    {
        var dart = EnsureAsset<TowerConfig>(DartTowerPath);
        dart.Configure(
            "cat_dart",
            "Dart",
            2.7f,
            1f,
            0.26f,
            0.56f,
            new Color(0.24f, 0.78f, 0.96f));

        var yarn = EnsureAsset<TowerConfig>(YarnTowerPath);
        yarn.Configure(
            "yarn_cannon",
            "Yarn",
            2.15f,
            2.4f,
            0.72f,
            0.72f,
            new Color(0.96f, 0.72f, 0.28f));

        var bell = EnsureAsset<TowerConfig>(BellTowerPath);
        bell.Configure(
            "bell_sniper",
            "Bell",
            4f,
            1.7f,
            0.55f,
            0.5f,
            new Color(0.72f, 0.45f, 0.95f));

        EditorUtility.SetDirty(dart);
        EditorUtility.SetDirty(yarn);
        EditorUtility.SetDirty(bell);

        return new[] { dart, yarn, bell };
    }

    private static EnemyConfig[] EnsureEnemyConfigs()
    {
        var scout = EnsureAsset<EnemyConfig>(ScoutEnemyPath);
        scout.Configure(
            "mouse_scout",
            "Mouse Scout",
            2f,
            1.25f,
            1,
            0.34f,
            new Color(1f, 0.34f, 0.34f));

        var bruiser = EnsureAsset<EnemyConfig>(BruiserEnemyPath);
        bruiser.Configure(
            "rat_bruiser",
            "Rat Bruiser",
            6f,
            0.55f,
            2,
            0.58f,
            new Color(0.86f, 0.42f, 0.18f));

        var guard = EnsureAsset<EnemyConfig>(GuardEnemyPath);
        guard.Configure(
            "beetle_guard",
            "Beetle Guard",
            4f,
            0.85f,
            1,
            0.46f,
            new Color(0.42f, 0.8f, 0.35f));

        EditorUtility.SetDirty(scout);
        EditorUtility.SetDirty(bruiser);
        EditorUtility.SetDirty(guard);

        return new[] { scout, bruiser, guard };
    }

    private static WaveConfig EnsureWaveConfig(IReadOnlyList<EnemyConfig> enemies)
    {
        var wave = EnsureAsset<WaveConfig>(WaveConfigPath);
        wave.Configure(new[]
        {
            new WaveEnemyGroup(enemies[0], 4, 0.62f),
            new WaveEnemyGroup(enemies[1], 3, 0.95f, 0.7f),
            new WaveEnemyGroup(enemies[2], 3, 0.78f, 0.6f)
        });

        EditorUtility.SetDirty(wave);
        return wave;
    }

    private static LevelConfig EnsureLevelConfig(TowerConfig[] towers, WaveConfig wave)
    {
        var level = EnsureAsset<LevelConfig>(LevelConfigPath);
        level.Configure(
            6,
            4,
            3,
            1.15f,
            new Vector2(-1.75f, -2.8f),
            new[]
            {
                new Vector2(-3.7f, 3f),
                new Vector2(-1.4f, 2.2f),
                new Vector2(1.5f, 2.2f),
                new Vector2(2.8f, 0.7f),
                new Vector2(1.2f, -0.9f),
                new Vector2(3.7f, -2.7f)
            },
            towers,
            wave);

        EditorUtility.SetDirty(level);
        return level;
    }

    private static void ConfigureLevelScene(LevelConfig config)
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

        controller.Configure(config, grid, spawner, hud, runtimeRoot);

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

    private static void ValidateAndExit()
    {
        var errors = new List<string>();
        var levelConfig = AssetDatabase.LoadAssetAtPath<LevelConfig>(LevelConfigPath);

        if (levelConfig == null)
        {
            errors.Add($"Missing level config asset: {LevelConfigPath}.");
        }
        else
        {
            ValidateConfigAssets(levelConfig, errors);
        }

        var scene = EditorSceneManager.OpenScene(LevelScenePath, OpenSceneMode.Single);
        var controller = Object.FindFirstObjectByType<PrototypeLevelController>();
        var grid = Object.FindFirstObjectByType<TowerGrid>();
        var spawner = Object.FindFirstObjectByType<PrototypeWaveSpawner>();
        var hud = Object.FindFirstObjectByType<PrototypeHud>();

        if (controller == null)
        {
            errors.Add("Level scene is missing PrototypeLevelController.");
        }
        else
        {
            if (!controller.IsConfigured)
            {
                errors.Add("PrototypeLevelController exists but is not fully configured.");
            }

            if (levelConfig != null && controller.Config != levelConfig)
            {
                errors.Add("PrototypeLevelController is not bound to Level01Config.");
            }
        }

        if (grid == null)
        {
            errors.Add("Level scene is missing TowerGrid.");
        }

        if (spawner == null)
        {
            errors.Add("Level scene is missing PrototypeWaveSpawner.");
        }

        if (hud == null)
        {
            errors.Add("Level scene is missing PrototypeHud.");
        }

        if (!scene.isLoaded)
        {
            errors.Add($"{LevelScenePath} did not load for validation.");
        }

        if (errors.Count > 0)
        {
            foreach (var error in errors)
            {
                Debug.LogError(error);
            }

            EditorApplication.Exit(1);
            return;
        }

        Debug.Log("Phase 3 validation passed: tower, enemy, wave, and level configs drive the playable core.");
        EditorApplication.Exit(0);
    }

    private static void ValidateConfigAssets(LevelConfig levelConfig, ICollection<string> errors)
    {
        if (!levelConfig.IsValidForCore())
        {
            errors.Add("Level01Config is invalid for Phase 3 core.");
            return;
        }

        if (levelConfig.AvailableTowers.Length < 3)
        {
            errors.Add("Level01Config must expose at least three tower configs.");
        }

        var waveEnemyConfigs = new HashSet<EnemyConfig>();
        foreach (var group in levelConfig.WaveConfig.Groups)
        {
            if (group?.EnemyConfig != null)
            {
                waveEnemyConfigs.Add(group.EnemyConfig);
            }
        }

        if (waveEnemyConfigs.Count < 3)
        {
            errors.Add("Phase 3 wave must include at least three enemy configs.");
        }
    }
}
