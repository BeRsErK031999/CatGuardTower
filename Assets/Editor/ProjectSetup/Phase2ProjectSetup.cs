using System.Collections.Generic;
using CatGuard.Gameplay.Grid;
using CatGuard.Gameplay.Levels;
using CatGuard.Gameplay.Waves;
using CatGuard.UI.HUD;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Phase2ProjectSetup
{
    private const string LevelScenePath = "Assets/_Project/Scenes/Level.unity";
    private const string ConfigAssetPath = "Assets/_Project/ScriptableObjects/Levels/PrototypeLevelConfig.asset";

    public static void Run()
    {
        var config = EnsureLevelConfig();
        ConfigureLevelScene(config);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        ValidateAndExit();
    }

    public static void Validate()
    {
        ValidateAndExit();
    }

    private static PrototypeLevelConfig EnsureLevelConfig()
    {
        var config = AssetDatabase.LoadAssetAtPath<PrototypeLevelConfig>(ConfigAssetPath);
        if (config != null)
        {
            return config;
        }

        config = ScriptableObject.CreateInstance<PrototypeLevelConfig>();
        AssetDatabase.CreateAsset(config, ConfigAssetPath);
        return config;
    }

    private static void ConfigureLevelScene(PrototypeLevelConfig config)
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

        var config = AssetDatabase.LoadAssetAtPath<PrototypeLevelConfig>(ConfigAssetPath);
        if (config == null)
        {
            errors.Add($"Missing prototype config asset: {ConfigAssetPath}.");
        }
        else if (!config.IsValidForPrototype())
        {
            errors.Add("Prototype level config is invalid.");
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
        else if (!controller.IsConfigured)
        {
            errors.Add("PrototypeLevelController exists but is not fully configured.");
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

        Debug.Log("Phase 2 validation passed: first playable prototype scene is configured.");
        EditorApplication.Exit(0);
    }
}
