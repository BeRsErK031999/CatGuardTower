using System;
using System.IO;
using CatGuard.Core.SceneLoading;
using CatGuard.Gameplay.Grid;
using CatGuard.Gameplay.Levels;
using CatGuard.Meta.Progression;
using UnityEngine;

namespace CatGuard.QA
{
    public static class DevelopmentQaService
    {
        private const string CommandFileName = "catguard-qa-command.json";
        private const string ResultFileName = "catguard-qa-result.json";

        private static DevelopmentQaCommand activeCommand;
        private static string activeResultPath;

        public static bool TryBeginLevel(LevelCatalogConfig levelCatalog)
        {
            if (!Debug.isDebugBuild)
            {
                return false;
            }

            var commandPath = FindCommandPath();
            if (string.IsNullOrWhiteSpace(commandPath))
            {
                return false;
            }

            try
            {
                var command = JsonUtility.FromJson<DevelopmentQaCommand>(File.ReadAllText(commandPath));
                activeResultPath = Path.Combine(Path.GetDirectoryName(commandPath) ?? string.Empty, ResultFileName);
                File.Delete(commandPath);
                if (command == null || string.IsNullOrWhiteSpace(command.levelId))
                {
                    WriteFailure(command?.scenarioId, command?.levelId, "QA command must contain levelId.");
                    return false;
                }

                var level = levelCatalog?.FindById(command.levelId);
                if (level == null)
                {
                    WriteFailure(command.scenarioId, command.levelId, "Requested QA level is missing.");
                    return false;
                }

                if (!TrySelectRequestedLevel(level, command))
                {
                    return false;
                }

                activeCommand = command;
                Debug.Log($"Development QA starts scenario '{command.scenarioId}' on level '{command.levelId}'.");
                SceneLoader.LoadLevel();
                return true;
            }
            catch (Exception exception)
            {
                WriteFailure(string.Empty, string.Empty, exception.Message);
                return false;
            }
        }

        public static void TryAttach(PrototypeLevelController controller, TowerGrid towerGrid)
        {
            if (!Debug.isDebugBuild
                || activeCommand == null
                || controller?.Config == null
                || controller.Config.LevelId != activeCommand.levelId)
            {
                return;
            }

            var runner = controller.gameObject.AddComponent<DevelopmentQaScenarioRunner>();
            runner.Initialize(controller, towerGrid, activeCommand);
        }

        internal static void Complete(DevelopmentQaResult result)
        {
            var resultPath = string.IsNullOrWhiteSpace(activeResultPath)
                ? Path.Combine(Application.persistentDataPath, ResultFileName)
                : activeResultPath;
            Directory.CreateDirectory(Path.GetDirectoryName(resultPath) ?? Application.persistentDataPath);
            File.WriteAllText(resultPath, JsonUtility.ToJson(result, true));
            activeCommand = null;
            activeResultPath = null;
        }

        private static string FindCommandPath()
        {
            var persistentPath = Path.Combine(Application.persistentDataPath, CommandFileName);
            if (File.Exists(persistentPath))
            {
                return persistentPath;
            }

            var internalFilesPath = GetInternalFilesPath();
            if (string.IsNullOrWhiteSpace(internalFilesPath))
            {
                return string.Empty;
            }

            var internalPath = Path.Combine(internalFilesPath, CommandFileName);
            return File.Exists(internalPath) ? internalPath : string.Empty;
        }

        private static bool TrySelectRequestedLevel(LevelConfig level, DevelopmentQaCommand command)
        {
            var save = ProgressionService.EnsureSave();
            if (!save.unlockedLevelIds.Contains(level.LevelId))
            {
                save.unlockedLevelIds.Add(level.LevelId);
            }

            ProgressionService.SelectLevel(level);
            var selectedLevel = ProgressionService.GetSelectedLevelOrDefault(null);
            if (selectedLevel != null && selectedLevel.LevelId == level.LevelId)
            {
                return true;
            }

            WriteFailure(command.scenarioId, command.levelId, "Requested QA level could not be selected.");
            return false;
        }

        private static string GetInternalFilesPath()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var filesDirectory = activity.Call<AndroidJavaObject>("getFilesDir"))
                {
                    return filesDirectory.Call<string>("getAbsolutePath");
                }
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Development QA cannot resolve Android files directory: {exception.Message}");
            }
#endif
            return string.Empty;
        }

        private static void WriteFailure(string scenarioId, string levelId, string message)
        {
            Complete(new DevelopmentQaResult
            {
                scenarioId = scenarioId ?? string.Empty,
                levelId = levelId ?? string.Empty,
                state = "error",
                error = message ?? "Unknown QA error."
            });
        }
    }

    [Serializable]
    public sealed class DevelopmentQaCommand
    {
        public string scenarioId;
        public string levelId;
        public string[] towerIds = Array.Empty<string>();
    }

    [Serializable]
    public sealed class DevelopmentQaResult
    {
        public string scenarioId;
        public string levelId;
        public string state;
        public int lives;
        public int defeatedEnemies;
        public int escapedEnemies;
        public int towerCount;
        public int battleFish;
        public float durationSeconds;
        public string[] towerIds = Array.Empty<string>();
        public string error;
    }

    public sealed class DevelopmentQaScenarioRunner : MonoBehaviour
    {
        private static readonly Vector2Int[] PlacementOrder =
        {
            new(1, 1),
            new(2, 1),
            new(1, 0),
            new(2, 0),
            new(0, 1),
            new(3, 1),
            new(0, 0),
            new(3, 0),
            new(1, 2),
            new(2, 2),
            new(0, 2),
            new(3, 2)
        };

        private PrototypeLevelController controller;
        private TowerGrid towerGrid;
        private DevelopmentQaCommand command;
        private int nextTowerIndex;
        private int nextCellIndex;
        private float startedAt;
        private bool completed;

        public void Initialize(
            PrototypeLevelController levelController,
            TowerGrid grid,
            DevelopmentQaCommand qaCommand)
        {
            controller = levelController;
            towerGrid = grid;
            command = qaCommand;
            startedAt = Time.unscaledTime;

            PlaceAffordableTowers();
            controller.TryStartWave();
        }

        private void Update()
        {
            if (completed || controller == null)
            {
                return;
            }

            if (controller.State == PrototypeLevelState.Running)
            {
                PlaceAffordableTowers();
            }

            if (controller.State is PrototypeLevelState.Won or PrototypeLevelState.Lost)
            {
                completed = true;
                DevelopmentQaService.Complete(new DevelopmentQaResult
                {
                    scenarioId = command.scenarioId ?? string.Empty,
                    levelId = controller.Config.LevelId,
                    state = controller.State.ToString().ToLowerInvariant(),
                    lives = controller.Lives,
                    defeatedEnemies = controller.DefeatedEnemies,
                    escapedEnemies = controller.EscapedEnemies,
                    towerCount = controller.TowerCount,
                    battleFish = controller.BattleFish,
                    durationSeconds = Time.unscaledTime - startedAt,
                    towerIds = command.towerIds ?? Array.Empty<string>(),
                    error = string.Empty
                });
            }
        }

        private void PlaceAffordableTowers()
        {
            var requestedTowers = command.towerIds ?? Array.Empty<string>();
            while (nextTowerIndex < requestedTowers.Length && nextCellIndex < PlacementOrder.Length)
            {
                var towerIndex = FindTowerIndex(requestedTowers[nextTowerIndex]);
                if (towerIndex < 0)
                {
                    nextTowerIndex++;
                    continue;
                }

                if (!controller.CanAffordTower(towerIndex))
                {
                    return;
                }

                controller.SelectTower(towerIndex);
                var cell = PlacementOrder[nextCellIndex];
                var worldPosition = controller.Config.GridOrigin + new Vector2(
                    cell.x * controller.Config.CellSize,
                    cell.y * controller.Config.CellSize);
                if (!towerGrid.TryPlaceAtWorld(worldPosition))
                {
                    nextCellIndex++;
                    continue;
                }

                nextTowerIndex++;
                nextCellIndex++;
            }
        }

        private int FindTowerIndex(string towerId)
        {
            var towers = controller.Config.AvailableTowers;
            for (var index = 0; index < towers.Length; index++)
            {
                if (towers[index] != null && towers[index].TowerId == towerId)
                {
                    return index;
                }
            }

            return -1;
        }
    }
}
