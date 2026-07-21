using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CatGuard.Gameplay.Battlefield;
using CatGuard.Core.SceneLoading;
using CatGuard.Gameplay.Grid;
using CatGuard.Gameplay.Levels;
using CatGuard.Gameplay.Towers;
using CatGuard.Gameplay.Ultimates;
using CatGuard.Meta.Progression;
using CatGuard.SDK.Analytics;
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
        public int startingBattleFish;
        public string[] upgradeBranchIds = Array.Empty<string>();
        public int upgradeTargetTier;
        public string targetPriority;
        public bool sellAfterUpgrade;
        public string[] ultimateIds = Array.Empty<string>();
        public bool exerciseUltimateTargeting;
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
        public int purchasedBattleUpgrades;
        public int soldTowers;
        public string[] selectedUpgradeBranches = Array.Empty<string>();
        public string[] selectedTargetPriorities = Array.Empty<string>();
        public bool battleUpgradesExcludedFromSave;
        public bool analyticsPayloadValid;
        public int ultimateReadyEvents;
        public int ultimateUses;
        public int ultimateResults;
        public int ultimateTargetCancels;
        public int ultimateInvalidTargets;
        public int ultimateHits;
        public float ultimateDamage;
        public int wardBlocks;
        public int pooledUltimateVfxCreated;
        public bool ultimateBattleStateExcludedFromSave;
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
        private int purchasedBattleUpgrades;
        private int soldTowers;
        private bool sellCompleted;
        private string permanentUpgradeFingerprint;
        private int analyticsEventStartIndex;
        private int nextUltimateIndex;
        private float nextUltimateAt;
        private bool targetingGateExercised;

        public void Initialize(
            PrototypeLevelController levelController,
            TowerGrid grid,
            DevelopmentQaCommand qaCommand)
        {
            controller = levelController;
            towerGrid = grid;
            command = qaCommand;
            startedAt = Time.unscaledTime;
            permanentUpgradeFingerprint = GetPermanentUpgradeFingerprint();
            analyticsEventStartIndex = (AnalyticsService.Current as FakeAnalyticsService)?.Events.Count ?? 0;
            nextUltimateAt = Time.unscaledTime + 1f;

            if (!controller.ConfigureDevelopmentScenario(
                    command.routeIdFilter,
                    command.startingLives,
                    command.startingBattleFish))
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
            TrySellRequestedTower();
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
                TrySellRequestedTower();
                TryUseRequestedUltimate();
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
                    purchasedBattleUpgrades = purchasedBattleUpgrades,
                    soldTowers = soldTowers,
                    selectedUpgradeBranches = GetSelectedUpgradeBranches(),
                    selectedTargetPriorities = GetSelectedTargetPriorities(),
                    battleUpgradesExcludedFromSave = permanentUpgradeFingerprint == GetPermanentUpgradeFingerprint(),
                    ultimateReadyEvents = controller.GuardianUltimates?.ReadyEventCount ?? 0,
                    ultimateUses = controller.GuardianUltimates?.UseEventCount ?? 0,
                    ultimateResults = controller.GuardianUltimates?.ResultEventCount ?? 0,
                    ultimateTargetCancels = controller.GuardianUltimates?.CancelCount ?? 0,
                    ultimateInvalidTargets = controller.GuardianUltimates?.InvalidTargetCount ?? 0,
                    ultimateHits = controller.GuardianUltimates?.UltimateHitCount ?? 0,
                    ultimateDamage = controller.GuardianUltimates?.UltimateDamageDealt ?? 0f,
                    wardBlocks = controller.GuardianUltimates?.WardBlocks ?? 0,
                    pooledUltimateVfxCreated = controller.GuardianUltimates?.PooledVfxCreated ?? 0,
                    ultimateBattleStateExcludedFromSave = !typeof(CatGuard.Core.Save.GameSaveData)
                        .GetFields()
                        .Any(field => field.Name.Contains("ultimateCharge", StringComparison.OrdinalIgnoreCase)),
                    analyticsPayloadValid = HasValidBattleUpgradeAnalytics() && HasValidUltimateAnalytics(),
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

                ConfigurePlacedTower(nextTowerIndex);
                nextTowerIndex++;
            }
        }

        private void ConfigurePlacedTower(int requestIndex)
        {
            if (controller.Towers.Count == 0)
            {
                return;
            }

            var tower = controller.Towers[controller.Towers.Count - 1];
            var branches = command.upgradeBranchIds ?? Array.Empty<string>();
            var hasRequestedBranch = requestIndex >= 0
                && requestIndex < branches.Length
                && !string.IsNullOrWhiteSpace(branches[requestIndex]);
            var hasRequestedPriority = Enum.TryParse(command.targetPriority, true, out TowerTargetPriority priority);
            if (hasRequestedBranch || hasRequestedPriority)
            {
                controller.SelectPlacedTower(tower);
            }

            if (hasRequestedPriority)
            {
                controller.TrySetSelectedTowerPriority(priority);
            }

            if (!hasRequestedBranch)
            {
                return;
            }

            var targetTier = Mathf.Max(0, command.upgradeTargetTier);
            while (tower.CurrentTier < targetTier)
            {
                var beforeTier = tower.CurrentTier;
                var result = controller.TryPurchaseSelectedTowerUpgrade(branches[requestIndex]);
                if (result != CatGuard.Gameplay.Towers.Upgrades.TowerUpgradeAvailability.Available
                    || tower.CurrentTier <= beforeTier)
                {
                    break;
                }

                purchasedBattleUpgrades++;
            }
        }

        private void TrySellRequestedTower()
        {
            if (!command.sellAfterUpgrade || sellCompleted || controller.Towers.Count < 2)
            {
                return;
            }

            var tower = controller.Towers[0];
            if (tower == null || tower.CurrentTier < Mathf.Max(1, command.upgradeTargetTier))
            {
                return;
            }

            controller.SelectPlacedTower(tower);
            if (controller.TrySellSelectedTower())
            {
                soldTowers++;
                sellCompleted = true;
            }
        }

        private void TryUseRequestedUltimate()
        {
            var requested = command.ultimateIds ?? Array.Empty<string>();
            var ultimates = controller.GuardianUltimates;
            if (ultimates == null || nextUltimateIndex >= requested.Length || Time.unscaledTime < nextUltimateAt)
            {
                return;
            }

            var ultimateId = requested[nextUltimateIndex];
            if (ultimateId == GuardianUltimateController.YarnMeteorId && controller.ActiveEnemies.Count == 0)
            {
                nextUltimateAt = Time.unscaledTime + 0.1f;
                return;
            }

            ultimates.GrantReady(ultimateId);
            if (command.exerciseUltimateTargeting
                && !targetingGateExercised
                && ultimateId == GuardianUltimateController.YarnMeteorId)
            {
                targetingGateExercised = true;
                if (controller.TryActivateUltimate(ultimateId))
                {
                    controller.TryConfirmUltimateTarget();
                    controller.CancelUltimateTargeting();
                }
            }

            var used = controller.TryActivateUltimate(ultimateId);
            if (used && ultimateId == GuardianUltimateController.YarnMeteorId)
            {
                var world = controller.ActiveEnemies.Count > 0 && controller.ActiveEnemies[0] != null
                    ? (Vector2)controller.ActiveEnemies[0].transform.position
                    : controller.Battlefield.WorldBounds.center;
                var screen = Camera.main == null ? Vector3.zero : Camera.main.WorldToScreenPoint(world);
                controller.UpdateUltimateTargetFromScreen(screen);
                used = controller.TryConfirmUltimateTarget();
            }

            if (used)
            {
                nextUltimateIndex++;
                nextUltimateAt = Time.unscaledTime + 2.2f;
            }
            else
            {
                nextUltimateAt = Time.unscaledTime + 0.25f;
            }
        }

        private string[] GetSelectedUpgradeBranches()
        {
            var result = new List<string>();
            foreach (var tower in controller.Towers)
            {
                if (tower != null && !string.IsNullOrWhiteSpace(tower.SelectedBranchId))
                {
                    result.Add(tower.SelectedBranchId);
                }
            }

            return result.ToArray();
        }

        private string[] GetSelectedTargetPriorities()
        {
            var result = new List<string>();
            foreach (var tower in controller.Towers)
            {
                if (tower != null)
                {
                    result.Add(tower.TargetPriority.ToString().ToLowerInvariant());
                }
            }

            return result.ToArray();
        }

        private bool HasValidBattleUpgradeAnalytics()
        {
            if (purchasedBattleUpgrades <= 0)
            {
                return command.upgradeTargetTier <= 0;
            }

            if (AnalyticsService.Current is not FakeAnalyticsService fake)
            {
                return true;
            }

            var validUpgrades = 0;
            for (var index = analyticsEventStartIndex; index < fake.Events.Count; index++)
            {
                var eventRecord = fake.Events[index];
                if (eventRecord.Name != AnalyticsEventNames.BattleTowerUpgrade)
                {
                    continue;
                }

                if (eventRecord.Parameters.ContainsKey(AnalyticsParameterNames.TowerId)
                    && eventRecord.Parameters.ContainsKey(AnalyticsParameterNames.BranchId)
                    && eventRecord.Parameters.ContainsKey(AnalyticsParameterNames.Tier)
                    && eventRecord.Parameters.ContainsKey(AnalyticsParameterNames.CostFishCoins))
                {
                    validUpgrades++;
                }
            }

            return validUpgrades == purchasedBattleUpgrades;
        }

        private bool HasValidUltimateAnalytics()
        {
            var requested = command.ultimateIds ?? Array.Empty<string>();
            if (requested.Length == 0)
            {
                return true;
            }

            if (AnalyticsService.Current is not FakeAnalyticsService fake)
            {
                return true;
            }

            var ready = 0;
            var uses = 0;
            var results = 0;
            for (var index = analyticsEventStartIndex; index < fake.Events.Count; index++)
            {
                var record = fake.Events[index];
                if (!record.Parameters.ContainsKey(AnalyticsParameterNames.UltimateId)
                    || !record.Parameters.ContainsKey(AnalyticsParameterNames.UltimateTargetingMode))
                {
                    continue;
                }

                ready += record.Name == AnalyticsEventNames.UltimateReady ? 1 : 0;
                uses += record.Name == AnalyticsEventNames.UltimateUse ? 1 : 0;
                results += record.Name == AnalyticsEventNames.UltimateResult ? 1 : 0;
            }

            return ready >= requested.Length && uses >= requested.Length && results >= requested.Length;
        }

        private static string GetPermanentUpgradeFingerprint()
        {
            var save = ProgressionService.EnsureSave();
            if (save?.upgrades == null || save.upgrades.Count == 0)
            {
                return string.Empty;
            }

            var values = new List<string>();
            foreach (var upgrade in save.upgrades)
            {
                if (upgrade != null)
                {
                    values.Add($"{upgrade.upgradeId}:{upgrade.level}");
                }
            }

            values.Sort(StringComparer.Ordinal);
            return string.Join("|", values);
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
