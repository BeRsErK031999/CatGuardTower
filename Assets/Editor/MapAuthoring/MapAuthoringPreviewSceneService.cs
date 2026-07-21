using System;
using System.IO;
using CatGuard.Gameplay.Battlefield;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CatGuard.EditorTools.MapAuthoring
{
    public static class MapAuthoringPreviewSceneService
    {
        public const string PreviewScenePath = "Assets/_Project/Scenes/Editor/MapAuthoringPreview.unity";

        public static void EnsurePreviewScene(BattlefieldConfig initialBattlefield)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(PreviewScenePath) ?? "Assets/_Project/Scenes");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var hostObject = new GameObject("Battlefield Authoring Preview");
            var host = hostObject.AddComponent<BattlefieldAuthoringPreview>();
            host.Configure(initialBattlefield);

            var cameraObject = new GameObject("Authoring Camera");
            cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.025f, 0.045f, 0.055f, 1f);
            ConfigureCamera(camera, initialBattlefield);

            EditorSceneManager.SaveScene(scene, PreviewScenePath);
        }

        public static void OpenPreview(BattlefieldConfig battlefield)
        {
            if (battlefield == null)
            {
                throw new ArgumentNullException(nameof(battlefield));
            }

            if (!File.Exists(PreviewScenePath))
            {
                throw new FileNotFoundException("Run E4ProjectSetup.Run to create the authoring preview scene.", PreviewScenePath);
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            var scene = EditorSceneManager.OpenScene(PreviewScenePath, OpenSceneMode.Single);
            var host = UnityEngine.Object.FindFirstObjectByType<BattlefieldAuthoringPreview>();
            if (host == null)
            {
                throw new InvalidDataException($"Preview scene is missing {nameof(BattlefieldAuthoringPreview)}.");
            }

            Undo.RecordObject(host, "Preview Battlefield");
            host.Configure(battlefield);
            EditorUtility.SetDirty(host);

            var camera = Camera.main;
            if (camera != null)
            {
                Undo.RecordObject(camera.transform, "Frame Battlefield Preview");
                Undo.RecordObject(camera, "Frame Battlefield Preview");
                ConfigureCamera(camera, battlefield);
            }

            Selection.activeGameObject = host.gameObject;
            if (SceneView.lastActiveSceneView != null)
            {
                var bounds = battlefield.WorldBounds;
                SceneView.lastActiveSceneView.LookAt(
                    new Vector3(bounds.center.x, bounds.center.y, 0f),
                    Quaternion.identity,
                    Mathf.Max(bounds.width, bounds.height) * 0.62f,
                    true,
                    false);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            SceneView.RepaintAll();
        }

        private static void ConfigureCamera(Camera camera, BattlefieldConfig battlefield)
        {
            if (camera == null)
            {
                return;
            }

            var bounds = battlefield == null ? new Rect(-7f, -4f, 14f, 8f) : battlefield.WorldBounds;
            camera.transform.position = new Vector3(bounds.center.x, bounds.center.y, -10f);
            camera.transform.rotation = Quaternion.identity;
            camera.orthographicSize = Mathf.Max(3f, bounds.height * 0.58f);
        }
    }
}
