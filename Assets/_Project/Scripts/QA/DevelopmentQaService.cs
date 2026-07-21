using System;
using System.Collections.Generic;
using System.IO;
using CatGuard.Gameplay.Battlefield;
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
        private const string SnapshotFileName = "catguard-qa-snapshot.json";

        private static DevelopmentQaCommand activeCommand;
        private static string activeResultPath;
        private static string activeSnapshotPath;

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
                var commandDirectory = Path.GetDirectoryName(commandPath) ?? string.Empty;
                activeResultPath = Path.Combine(commandDirectory, ResultFileName);
                activeSnapshotPath = Path.Combine(commandDirectory, SnapshotFileName);
                File.Delete(commandPath);
                File.Delete(activeSnapshotPath);
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
            activeSnapshotPath = null;
        }

        internal static void WriteSnapshot(DevelopmentQaSnapshot snapshot)
        {
            if (string.IsNullOrWhiteSpace(activeSnapshotPath) || snapshot == null)
            {
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(activeSnapshotPath) ?? Application.persistentDataPath);
            File.WriteAllText(activeSnapshotPath, JsonUtility.ToJson(snapshot, true));
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
        public bool manualInput;
        public string routeIdFilter;
        public int startingLives;
    }

    [Serializable]
    public sealed class DevelopmentQaCellSnapshot
    {
        public int cellIndex;
        public float worldX;
        public float worldY;
        public int screenX;
        public int screenY;
        public bool visible;
        public bool occupied;
    }

    [Serializable]
    public sealed class DevelopmentQaSnapshot
    {
        public string scenarioId;
        public string levelId;
        public string battlefieldId;
        public string cameraMode;
        public bool legacyBattlefield;
        public string state;
        public int towerCount;
        public bool lastGestureWasDrag;
        public float cameraFocusX;
        public float cameraFocusY;
        public float cameraMinFocusX;
        public float cameraMaxFocusX;
        public float cameraMinFocusY;
        public float cameraMaxFocusY;
        public DevelopmentQaCellSnapshot[] cells = Array.Empty<DevelopmentQaCellSnapshot>();
        public string[] configuredRouteIds = Array.Empty<string>();
        public string[] incomingRouteIds = Array.Empty<string>();
    }

    [Serializable]
    public sealed class DevelopmentQaRouteResult
    {
        public string routeId;
        public int spawned;
        public int defeated;
        public int escaped;
        public float firstSpawnTime;
        public float lastSpawnTime;
    }

    [Serializable]
    public sealed class DevelopmentQaResult
    {
        public string scenarioId;
        public string levelId;
        public string battlefieldId;
        public string cameraMode;
        public bool legacyBattlefield;
        public string state;
        public int lives;
        public int defeatedEnemies;
        public int escapedEnemies;
        public int towerCount;
        public int battleFish;
        public float durationSeconds;
        public string[] towerIds = Array.Empty<string>();
        public string routeIdFilter;
        public string[] configuredRouteIds = Array.Empty<string>();
        public DevelopmentQaRouteResult[] routes = Array.Empty<DevelopmentQaRouteResult>();
        public string error;
    }

    public sealed class DevelopmentQaScenarioRunner : MonoBehaviour
    {
        private static readonly float[] PlacementFractions =
        {
            0.5f,
            0.25f,
            0.75f,
            0.125f,
            0.875f,
            0.375f,
            0.625f,
            0f,
            1f,
            0.18f,
            0.82f,
            0.68f
        };

        private readonly HashSet<int> attemptedCells = new();
        private PrototypeLevelController controller;
        private TowerGrid towerGrid;
        private DevelopmentQaCommand command;
        private int nextTowerIndex;
        private int nextCellIndex;
        private float startedAt;
        private float nextSnapshotAt;
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

            if (!controller.ConfigureDevelopmentScenario(command.routeIdFilter, command.startingLives))
            {
                completed = true;
                DevelopmentQaService.Complete(new DevelopmentQaResult
                {
                    scenarioId = command.scenarioId ?? string.Empty,
                    levelId = controller.Config.LevelId,
                    battlefieldId = controller.Battlefield.BattlefieldId,
                    state = "error",
                    routeIdFilter = command.routeIdFilter ?? string.Empty,
                    error = "Development route filter or expected enemy count is invalid."
                });
                return;
            }

            if (command.manualInput)
            {
                WriteManualSnapshot();
                return;
            }

            PlaceAffordableTowers();
            controller.TryStartWave();
        }

        private void Update()
        {
            if (completed || controller == null)
            {
                return;
            }

            if (command.manualInput)
            {
                if (Time.unscaledTime >= nextSnapshotAt)
                {
                    WriteManualSnapshot();
                }

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
                    battlefieldId = controller.Battlefield.BattlefieldId,
                    cameraMode = controller.Battlefield.CameraMode.ToString(),
                    legacyBattlefield = controller.Battlefield.IsLegacy,
                    state = controller.State.ToString().ToLowerInvariant(),
                    lives = controller.Lives,
                    defeatedEnemies = controller.DefeatedEnemies,
                    escapedEnemies = controller.EscapedEnemies,
                    towerCount = controller.TowerCount,
                    battleFish = controller.BattleFish,
                    durationSeconds = Time.unscaledTime - startedAt,
                    towerIds = command.towerIds ?? Array.Empty<string>(),
                    routeIdFilter = command.routeIdFilter ?? string.Empty,
                    configuredRouteIds = GetConfiguredRouteIds(),
                    routes = GetRouteResults(),
                    error = string.Empty
                });
            }
        }

        private void PlaceAffordableTowers()
        {
            var requestedTowers = command.towerIds ?? Array.Empty<string>();
            while (nextTowerIndex < requestedTowers.Length && nextCellIndex < PlacementFractions.Length)
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
                var cellIndex = GetPlacementCellIndex(nextCellIndex);
                nextCellIndex++;
                if (cellIndex < 0 || !towerGrid.TryPlaceAtCellIndex(cellIndex))
                {
                    continue;
                }

                nextTowerIndex++;
            }
        }

        private int GetPlacementCellIndex(int orderIndex)
        {
            var count = towerGrid.CellCenters.Count;
            if (count == 0 || orderIndex < 0 || orderIndex >= PlacementFractions.Length)
            {
                return -1;
            }

            var requested = Mathf.RoundToInt((count - 1) * PlacementFractions[orderIndex]);
            if (attemptedCells.Add(requested))
            {
                return requested;
            }

            for (var offset = 1; offset < count; offset++)
            {
                var candidate = (requested + offset) % count;
                if (attemptedCells.Add(candidate))
                {
                    return candidate;
                }
            }

            return -1;
        }

        private void WriteManualSnapshot()
        {
            nextSnapshotAt = Time.unscaledTime + 0.2f;
            var camera = Camera.main;
            var battlefield = controller.Battlefield;
            var cameraController = controller.BattlefieldCamera;
            if (camera == null || battlefield == null || cameraController == null)
            {
                return;
            }

            var cells = new DevelopmentQaCellSnapshot[towerGrid.CellCenters.Count];
            for (var index = 0; index < towerGrid.CellCenters.Count; index++)
            {
                var world = towerGrid.CellCenters[index];
                var screen = camera.WorldToScreenPoint(world);
                var inputPoint = new Vector2(screen.x, screen.y);
                cells[index] = new DevelopmentQaCellSnapshot
                {
                    cellIndex = index,
                    worldX = world.x,
                    worldY = world.y,
                    screenX = Mathf.RoundToInt(screen.x),
                    screenY = Mathf.RoundToInt(Screen.height - screen.y),
                    visible = screen.z >= 0f
                        && screen.x >= 0f
                        && screen.x <= Screen.width
                        && screen.y >= 0f
                        && screen.y <= Screen.height
                        && !controller.IsScreenPointOverHud(inputPoint),
                    occupied = towerGrid.IsOccupied(index)
                };
            }

            var limits = cameraController.FocusLimits;
            var incomingRoutes = new List<string>();
            foreach (var route in battlefield.Routes)
            {
                if (controller.IsRouteWarningActive(route.RouteId))
                {
                    incomingRoutes.Add(route.RouteId);
                }
            }
            DevelopmentQaService.WriteSnapshot(new DevelopmentQaSnapshot
            {
                scenarioId = command.scenarioId ?? string.Empty,
                levelId = controller.Config.LevelId,
                battlefieldId = battlefield.BattlefieldId,
                cameraMode = battlefield.CameraMode.ToString(),
                legacyBattlefield = battlefield.IsLegacy,
                state = controller.State.ToString().ToLowerInvariant(),
                towerCount = controller.TowerCount,
                lastGestureWasDrag = controller.BattlefieldInput?.LastGestureWasDrag == true,
                cameraFocusX = cameraController.FocusPoint.x,
                cameraFocusY = cameraController.FocusPoint.y,
                cameraMinFocusX = limits.xMin,
                cameraMaxFocusX = limits.xMax,
                cameraMinFocusY = limits.yMin,
                cameraMaxFocusY = limits.yMax,
                cells = cells,
                configuredRouteIds = GetConfiguredRouteIds(),
                incomingRouteIds = incomingRoutes.ToArray()
            });
        }

        private string[] GetConfiguredRouteIds()
        {
            var routes = controller.Battlefield.Routes;
            var result = new string[routes.Length];
            for (var index = 0; index < routes.Length; index++)
            {
                result[index] = routes[index]?.RouteId ?? string.Empty;
            }

            return result;
        }

        private DevelopmentQaRouteResult[] GetRouteResults()
        {
            var result = new List<DevelopmentQaRouteResult>();
            foreach (var route in controller.Battlefield.Routes)
            {
                if (route == null || !controller.RouteStats.TryGetValue(route.RouteId, out var stats))
                {
                    continue;
                }

                result.Add(new DevelopmentQaRouteResult
                {
                    routeId = stats.RouteId,
                    spawned = stats.Spawned,
                    defeated = stats.Defeated,
                    escaped = stats.Escaped,
                    firstSpawnTime = stats.FirstSpawnTime,
                    lastSpawnTime = stats.LastSpawnTime
                });
            }

            return result.ToArray();
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
