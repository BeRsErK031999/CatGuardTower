using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class Phase0ProjectSetup
{
    private static readonly string[] ScenePaths =
    {
        "Assets/_Project/Scenes/Boot.unity",
        "Assets/_Project/Scenes/MainMenu.unity",
        "Assets/_Project/Scenes/Level.unity"
    };

    public static void Run()
    {
        EnsureProjectFolders();
        ConfigureProjectSettings();
        CreateInitialScenes();
        ConfigureBuildScenes();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var androidSelected = EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
        EditorApplication.Exit(androidSelected ? 0 : 1);
    }

    private static void EnsureProjectFolders()
    {
        foreach (var path in ScenePaths)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "Assets/_Project/Scenes");
        }

        Directory.CreateDirectory("Assets/_Project/Scripts/Core/Bootstrap");
        Directory.CreateDirectory("Assets/_Project/Scripts/Core/SceneLoading");
    }

    private static void ConfigureProjectSettings()
    {
        EditorSettings.defaultBehaviorMode = EditorBehaviorMode.Mode2D;
        PlayerSettings.productName = "Cat Guard: Tower Defense";
        PlayerSettings.companyName = "CatGuard";
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;
    }

    private static void CreateInitialScenes()
    {
        foreach (var path in ScenePaths)
        {
            if (File.Exists(path))
            {
                continue;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.09f, 0.12f);

            EditorSceneManager.SaveScene(scene, path);
        }
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
}
