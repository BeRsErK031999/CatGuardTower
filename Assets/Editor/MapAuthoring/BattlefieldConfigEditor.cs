using CatGuard.Gameplay.Battlefield;
using UnityEditor;
using UnityEngine;

namespace CatGuard.EditorTools.MapAuthoring
{
    [CustomEditor(typeof(BattlefieldConfig))]
    public sealed class BattlefieldConfigEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            var battlefield = (BattlefieldConfig)target;

            if (GUILayout.Button("Validate Authoring Data"))
            {
                var validation = MapAuthoringValidator.ValidateBattlefield(battlefield);
                if (validation.IsValid)
                {
                    Debug.Log($"Map authoring validation passed: {AssetDatabase.GetAssetPath(battlefield)}");
                }
                else
                {
                    foreach (var issue in validation.Issues)
                    {
                        Debug.LogError(issue.ToString(), battlefield);
                    }
                }
            }

            if (GUILayout.Button("Open Scene/Game Preview"))
            {
                MapAuthoringPreviewSceneService.OpenPreview(battlefield);
            }

            using (new EditorGUI.DisabledScope(battlefield.RouteConfigs.Length > 0))
            {
                if (GUILayout.Button("Migrate Legacy Path To 'main' Route"))
                {
                    Undo.RecordObject(battlefield, "Migrate Legacy Route");
                    if (MapAuthoringMigrationService.MigrateLegacyRouteInPlace(battlefield))
                    {
                        AssetDatabase.SaveAssets();
                    }
                }
            }
        }
    }
}
