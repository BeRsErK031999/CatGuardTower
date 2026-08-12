using System;
using System.IO;
using CatGuard.Gameplay.Levels;
using UnityEngine;

namespace CatGuard.QA
{
    public static class ReleasePerformanceCheckpoint
    {
        private const string RequestFileName = "catguard-performance-checkpoint.request";
        private const string ResponseFileName = "catguard-performance-checkpoint.json";
        private const float ProbeIntervalSeconds = 0.25f;

        private static float nextProbeAt;

        public static void TryWrite(PrototypeLevelController controller)
        {
            if (controller?.Config == null || Time.unscaledTime < nextProbeAt)
            {
                return;
            }

            nextProbeAt = Time.unscaledTime + ProbeIntervalSeconds;
            var requestPath = Path.Combine(Application.persistentDataPath, RequestFileName);
            if (!File.Exists(requestPath))
            {
                return;
            }

            var responsePath = Path.Combine(Application.persistentDataPath, ResponseFileName);
            try
            {
                var checkpoint = new ReleasePerformanceCheckpointData
                {
                    capturedAtUtc = DateTime.UtcNow.ToString("O"),
                    applicationIdentifier = Application.identifier,
                    applicationVersion = Application.version,
                    levelId = controller.Config.LevelId,
                    state = controller.State.ToString().ToLowerInvariant(),
                    activeEnemyCount = controller.ActiveEnemyCount,
                    spawnedEnemies = controller.SpawnedEnemies,
                    defeatedEnemies = controller.DefeatedEnemies,
                    escapedEnemies = controller.EscapedEnemies,
                    totalEnemies = controller.TotalEnemies,
                    towerCount = controller.TowerCount,
                    bossActive = controller.ActiveBoss != null,
                    lives = controller.Lives,
                    battleFish = controller.BattleFish,
                    paused = controller.IsPaused,
                    battleSpeed = controller.BattleSpeed,
                    graphicsDeviceName = SystemInfo.graphicsDeviceName,
                    graphicsDeviceVendor = SystemInfo.graphicsDeviceVendor,
                    graphicsDeviceType = SystemInfo.graphicsDeviceType.ToString()
                };
                checkpoint.heavyWaveEligible = checkpoint.levelId == "level_12"
                    && checkpoint.state == "running"
                    && !checkpoint.paused
                    && checkpoint.towerCount >= 4
                    && (checkpoint.activeEnemyCount >= 8 || checkpoint.bossActive);

                File.WriteAllText(responsePath, JsonUtility.ToJson(checkpoint, true));
                File.Delete(requestPath);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Release performance checkpoint failed: {exception.Message}");
            }
        }
    }

    [Serializable]
    public sealed class ReleasePerformanceCheckpointData
    {
        public string capturedAtUtc = string.Empty;
        public string applicationIdentifier = string.Empty;
        public string applicationVersion = string.Empty;
        public string levelId = string.Empty;
        public string state = string.Empty;
        public int activeEnemyCount;
        public int spawnedEnemies;
        public int defeatedEnemies;
        public int escapedEnemies;
        public int totalEnemies;
        public int towerCount;
        public bool bossActive;
        public int lives;
        public int battleFish;
        public bool paused;
        public int battleSpeed;
        public string graphicsDeviceName = string.Empty;
        public string graphicsDeviceVendor = string.Empty;
        public string graphicsDeviceType = string.Empty;
        public bool heavyWaveEligible;
    }
}
