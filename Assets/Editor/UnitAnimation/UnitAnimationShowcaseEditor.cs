using CatGuard.Gameplay.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CatGuard.EditorTools.UnitAnimation
{
    [CustomEditor(typeof(UnitAnimationShowcase))]
    public sealed class UnitAnimationShowcaseEditor : UnityEditor.Editor
    {
        [MenuItem("Cat Guard/Animation/Open Controlled Showcase")]
        public static void OpenShowcase()
        {
            const string scenePath = "Assets/_Project/Scenes/Editor/UnitAnimationShowcase.unity";
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var showcase = Object.FindAnyObjectByType<UnitAnimationShowcase>();
            if (showcase != null)
            {
                Selection.activeObject = showcase;
                SceneView.lastActiveSceneView?.FrameSelected();
            }
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var showcase = (UnitAnimationShowcase)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Controlled State", EditorStyles.boldLabel);
            foreach (UnitAnimationState state in System.Enum.GetValues(typeof(UnitAnimationState)))
            {
                if (GUILayout.Button(state.ToString()))
                {
                    Undo.RecordObject(showcase, $"Preview {state}");
                    showcase.SetPreview(state, showcase.PreviewDirection, showcase.PreviewModifier, 1f, 0.55f);
                    EditorUtility.SetDirty(showcase);
                    SceneView.RepaintAll();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Slow"))
                {
                    showcase.SetPreview(UnitAnimationState.Walk, UnitFacingDirection.East, UnitStatusModifier.Slowed, 0.6f, 0.55f);
                }

                if (GUILayout.Button("Fast"))
                {
                    showcase.SetPreview(UnitAnimationState.Walk, UnitFacingDirection.East, UnitStatusModifier.Hastened, 1.45f, 0.55f);
                }

                if (GUILayout.Button("Frozen"))
                {
                    showcase.SetPreview(UnitAnimationState.Walk, UnitFacingDirection.East, UnitStatusModifier.Frozen, 0f, 0.55f);
                }
            }

            EditorGUILayout.HelpBox(
                "The scene contains all five current enemies. State buttons, cardinal direction, speed modifiers, hit flash, terminal poses, shadows, health/status cues, and y-based sorting use the same runtime presenters as battle.",
                MessageType.Info);
        }
    }
}
