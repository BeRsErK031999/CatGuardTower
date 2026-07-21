using System;
using System.Collections.Generic;
using CatGuard.Gameplay.Battlefield;
using CatGuard.Gameplay.Levels;
using UnityEditor;
using UnityEngine;

namespace CatGuard.EditorTools.MapAuthoring
{
    public sealed class MapAuthoringWindow : EditorWindow
    {
        private BattlefieldConfig selectedBattlefield;
        private LevelConfig selectedLevel;
        private LevelConfig contentTemplate;
        private string outputFolder = "Assets/_Project/ScriptableObjects/Authoring";
        private string assetPrefix = "NewMap";
        private string battlefieldId = "new_map";
        private string levelId = "level_new_map";
        private string displayName = "New Map";
        private Vector2 scroll;
        private readonly List<string> validationMessages = new();

        [MenuItem("Cat Guard/Map Authoring/Open Pipeline")]
        public static void Open()
        {
            GetWindow<MapAuthoringWindow>("Map Authoring");
        }

        private void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            EditorGUILayout.LabelField("Inspect And Preview", EditorStyles.boldLabel);
            selectedLevel = (LevelConfig)EditorGUILayout.ObjectField("Level", selectedLevel, typeof(LevelConfig), false);
            selectedBattlefield = (BattlefieldConfig)EditorGUILayout.ObjectField("Battlefield", selectedBattlefield, typeof(BattlefieldConfig), false);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(selectedLevel == null && selectedBattlefield == null))
                {
                    if (GUILayout.Button("Validate"))
                    {
                        ValidateSelection();
                    }
                }

                using (new EditorGUI.DisabledScope(ResolveBattlefield() == null))
                {
                    if (GUILayout.Button("Preview"))
                    {
                        MapAuthoringPreviewSceneService.OpenPreview(ResolveBattlefield());
                    }
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Create Starter Level", EditorStyles.boldLabel);
            contentTemplate = (LevelConfig)EditorGUILayout.ObjectField("Content Template", contentTemplate, typeof(LevelConfig), false);
            outputFolder = EditorGUILayout.TextField("Output Folder", outputFolder);
            assetPrefix = EditorGUILayout.TextField("Asset Prefix", assetPrefix);
            battlefieldId = EditorGUILayout.TextField("Battlefield Id", battlefieldId);
            levelId = EditorGUILayout.TextField("Level Id", levelId);
            displayName = EditorGUILayout.TextField("Display Name", displayName);
            using (new EditorGUI.DisabledScope(contentTemplate == null))
            {
                if (GUILayout.Button("Create Or Update Starter Assets"))
                {
                    try
                    {
                        var created = MapAuthoringAssetFactory.CreateOrUpdateStarterLevel(
                            outputFolder,
                            assetPrefix,
                            battlefieldId,
                            levelId,
                            displayName,
                            contentTemplate);
                        selectedBattlefield = created.Battlefield;
                        selectedLevel = created.Level;
                        Selection.activeObject = created.Level;
                        ValidateSelection();
                    }
                    catch (Exception exception)
                    {
                        Debug.LogException(exception);
                    }
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Legacy Migration", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(selectedLevel == null || !selectedLevel.UsesLegacyBattlefield))
            {
                if (GUILayout.Button("Migrate Selected Legacy Level"))
                {
                    var path = EditorUtility.SaveFilePanelInProject(
                        "Create Battlefield From Legacy Level",
                        $"{selectedLevel.name}Battlefield",
                        "asset",
                        "Choose a project path for the migrated battlefield.");
                    if (!string.IsNullOrWhiteSpace(path))
                    {
                        selectedBattlefield = MapAuthoringMigrationService.MigrateLegacyLevelToAsset(selectedLevel, path);
                        ValidateSelection();
                    }
                }
            }

            if (validationMessages.Count > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);
                foreach (var message in validationMessages)
                {
                    EditorGUILayout.HelpBox(message, message.StartsWith("PASS", StringComparison.Ordinal) ? MessageType.Info : MessageType.Error);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private BattlefieldConfig ResolveBattlefield()
        {
            return selectedBattlefield != null ? selectedBattlefield : selectedLevel?.BattlefieldConfig;
        }

        private void ValidateSelection()
        {
            var result = selectedLevel != null
                ? MapAuthoringValidator.ValidateLevel(selectedLevel)
                : MapAuthoringValidator.ValidateBattlefield(selectedBattlefield);
            validationMessages.Clear();
            if (result.IsValid)
            {
                validationMessages.Add("PASS: selected authoring data is valid.");
            }
            else
            {
                foreach (var issue in result.Issues)
                {
                    validationMessages.Add(issue.ToString());
                }
            }

            Repaint();
        }
    }
}
