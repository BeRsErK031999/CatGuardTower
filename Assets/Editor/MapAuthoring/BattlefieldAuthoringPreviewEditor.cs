using CatGuard.Gameplay.Battlefield;
using UnityEditor;
using UnityEngine;

namespace CatGuard.EditorTools.MapAuthoring
{
    [CustomEditor(typeof(BattlefieldAuthoringPreview))]
    public sealed class BattlefieldAuthoringPreviewEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var preview = (BattlefieldAuthoringPreview)target;
            using (new EditorGUI.DisabledScope(preview.BattlefieldConfig == null))
            {
                if (GUILayout.Button("Validate Battlefield"))
                {
                    LogValidation(MapAuthoringValidator.ValidateBattlefield(preview.BattlefieldConfig));
                }

                if (GUILayout.Button("Select Battlefield Asset"))
                {
                    Selection.activeObject = preview.BattlefieldConfig;
                }
            }

            EditorGUILayout.HelpBox(
                "Select this preview host to edit route points with Scene view handles. Enable Gizmos in Game view to see the same bounds, zones, cells, and route geometry.",
                MessageType.Info);
        }

        private void OnSceneGUI()
        {
            var preview = (BattlefieldAuthoringPreview)target;
            var battlefield = preview.BattlefieldConfig;
            if (battlefield == null)
            {
                return;
            }

            DrawReferenceGeometry(battlefield);
            var serializedBattlefield = new SerializedObject(battlefield);
            var routes = serializedBattlefield.FindProperty("routes");
            if (routes == null)
            {
                return;
            }

            for (var routeIndex = 0; routeIndex < routes.arraySize; routeIndex++)
            {
                var route = routes.GetArrayElementAtIndex(routeIndex);
                var routeId = route.FindPropertyRelative("routeId")?.stringValue ?? $"route_{routeIndex}";
                var styleId = route.FindPropertyRelative("visualStyleId")?.stringValue ?? string.Empty;
                var width = route.FindPropertyRelative("visualWidth")?.floatValue ?? 0.5f;
                var points = route.FindPropertyRelative("points");
                if (points == null || points.arraySize == 0)
                {
                    continue;
                }

                var colors = BattlefieldRouteVisualStyle.GetColors(styleId, routeIndex);
                Handles.color = colors.End;
                var worldPoints = new Vector3[points.arraySize];
                for (var pointIndex = 0; pointIndex < points.arraySize; pointIndex++)
                {
                    worldPoints[pointIndex] = preview.transform.TransformPoint(points.GetArrayElementAtIndex(pointIndex).vector2Value);
                }

                Handles.DrawAAPolyLine(Mathf.Max(2f, width * 9f), worldPoints);
                Handles.Label(worldPoints[0] + (Vector3.up * 0.28f), $"{routeId} / spawn");
                Handles.Label(worldPoints[^1] + (Vector3.up * 0.28f), $"{routeId} / goal");

                for (var pointIndex = 0; pointIndex < points.arraySize; pointIndex++)
                {
                    EditorGUI.BeginChangeCheck();
                    var moved = Handles.PositionHandle(worldPoints[pointIndex], Quaternion.identity);
                    if (!EditorGUI.EndChangeCheck())
                    {
                        continue;
                    }

                    Undo.RecordObject(battlefield, $"Move {routeId} Point {pointIndex}");
                    var local = preview.transform.InverseTransformPoint(moved);
                    points.GetArrayElementAtIndex(pointIndex).vector2Value = new Vector2(local.x, local.y);
                    serializedBattlefield.ApplyModifiedProperties();
                    EditorUtility.SetDirty(battlefield);
                    SceneView.RepaintAll();
                }
            }
        }

        private static void DrawReferenceGeometry(BattlefieldConfig battlefield)
        {
            DrawRect(battlefield.WorldBounds, new Color(1f, 1f, 1f, 0.55f));
            DrawRect(battlefield.CameraBounds, new Color(0.2f, 0.85f, 1f, 0.8f));
            foreach (var zone in battlefield.PlacementZones)
            {
                DrawRect(zone.Bounds, new Color(0.2f, 0.9f, 0.45f, 0.55f));
            }

            foreach (var zone in battlefield.BlockedZones)
            {
                DrawRect(zone.Bounds, new Color(1f, 0.24f, 0.2f, 0.75f));
            }
        }

        private static void DrawRect(Rect rect, Color color)
        {
            Handles.color = color;
            Handles.DrawWireCube(rect.center, new Vector3(rect.width, rect.height, 0f));
        }

        private static void LogValidation(MapAuthoringValidationResult result)
        {
            if (result.IsValid)
            {
                Debug.Log("Map authoring validation passed.");
                return;
            }

            foreach (var issue in result.Issues)
            {
                Debug.LogError(issue.ToString());
            }
        }
    }
}
