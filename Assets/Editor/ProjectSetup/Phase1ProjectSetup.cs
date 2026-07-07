using System.Collections.Generic;
using CatGuard.Core.Bootstrap;
using CatGuard.UI.Screens;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Phase1ProjectSetup
{
    private const string BootScenePath = "Assets/_Project/Scenes/Boot.unity";
    private const string MainMenuScenePath = "Assets/_Project/Scenes/MainMenu.unity";
    private const string LevelScenePath = "Assets/_Project/Scenes/Level.unity";

    private static readonly string[] ScenePaths =
    {
        BootScenePath,
        MainMenuScenePath,
        LevelScenePath
    };

    public static void Run()
    {
        ConfigureBuildScenes();
        ConfigureBootScene();
        ConfigureMainMenuScene();
        ConfigureLevelScene();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        ValidateAndExit();
    }

    public static void Validate()
    {
        ValidateAndExit();
    }

    private static void ConfigureBuildScenes()
    {
        var buildScenes = new EditorBuildSettingsScene[ScenePaths.Length];

        for (var index = 0; index < ScenePaths.Length; index++)
        {
            buildScenes[index] = new EditorBuildSettingsScene(ScenePaths[index], true);
        }

        EditorBuildSettings.scenes = buildScenes;
    }

    private static void ConfigureBootScene()
    {
        var scene = EditorSceneManager.OpenScene(BootScenePath, OpenSceneMode.Single);
        EnsureCamera();

        var bootstrapObject = GameObject.Find("GameBootstrap") ?? new GameObject("GameBootstrap");
        if (bootstrapObject.GetComponent<GameBootstrap>() == null)
        {
            bootstrapObject.AddComponent<GameBootstrap>();
        }

        EditorSceneManager.SaveScene(scene);
    }

    private static void ConfigureMainMenuScene()
    {
        var scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
        EnsureCamera();

        var controllerObject = GameObject.Find("MainMenuController") ?? new GameObject("MainMenuController");
        if (controllerObject.GetComponent<MainMenuController>() == null)
        {
            controllerObject.AddComponent<MainMenuController>();
        }

        EditorSceneManager.SaveScene(scene);
    }

    private static void ConfigureLevelScene()
    {
        var scene = EditorSceneManager.OpenScene(LevelScenePath, OpenSceneMode.Single);
        EnsureCamera();
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
        camera.orthographicSize = 5f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.08f, 0.09f, 0.12f);
    }

    private static void ValidateAndExit()
    {
        var errors = new List<string>();

        ValidateBuildSceneOrder(errors);
        ValidateSceneComponent<GameBootstrap>(BootScenePath, "GameBootstrap", errors);
        ValidateSceneComponent<MainMenuController>(MainMenuScenePath, "MainMenuController", errors);

        if (errors.Count > 0)
        {
            foreach (var error in errors)
            {
                Debug.LogError(error);
            }

            EditorApplication.Exit(1);
            return;
        }

        Debug.Log("Phase 1 validation passed: Boot -> MainMenu -> Level bootstrap is configured.");
        EditorApplication.Exit(0);
    }

    private static void ValidateBuildSceneOrder(ICollection<string> errors)
    {
        if (EditorBuildSettings.scenes.Length != ScenePaths.Length)
        {
            errors.Add($"Expected {ScenePaths.Length} build scenes, found {EditorBuildSettings.scenes.Length}.");
            return;
        }

        for (var index = 0; index < ScenePaths.Length; index++)
        {
            var buildScene = EditorBuildSettings.scenes[index];
            if (!buildScene.enabled || buildScene.path != ScenePaths[index])
            {
                errors.Add($"Build scene {index} must be enabled and point to {ScenePaths[index]}.");
            }
        }
    }

    private static void ValidateSceneComponent<TComponent>(string scenePath, string componentName, ICollection<string> errors)
        where TComponent : Component
    {
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        foreach (var rootObject in scene.GetRootGameObjects())
        {
            if (rootObject.GetComponentInChildren<TComponent>(true) != null)
            {
                return;
            }
        }

        errors.Add($"{componentName} is missing in {scenePath}.");
    }
}
