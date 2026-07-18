using System.Collections;
using CatGuard.Gameplay.Levels;
using UnityEngine;

namespace CatGuard.Gameplay.Waves
{
    public sealed class PrototypeWaveSpawner : MonoBehaviour
    {
        private PrototypeLevelController levelController;
        private WaveConfig config;
        private Coroutine spawnRoutine;

        public int SpawnedCount { get; private set; }

        public void Initialize(PrototypeLevelController owner, WaveConfig waveConfig)
        {
            levelController = owner;
            config = waveConfig;
            SpawnedCount = 0;
        }

        public void Begin()
        {
            if (spawnRoutine != null)
            {
                StopCoroutine(spawnRoutine);
            }

            spawnRoutine = StartCoroutine(SpawnWave());
        }

        private IEnumerator SpawnWave()
        {
            foreach (var group in config.Groups)
            {
                if (group == null || group.EnemyConfig == null)
                {
                    continue;
                }

                if (group.DelayBeforeGroup > 0f)
                {
                    yield return new WaitForSeconds(group.DelayBeforeGroup);
                }

                for (var index = 0; index < group.Count; index++)
                {
                    while (levelController.State == PrototypeLevelState.Lost)
                    {
                        yield return null;
                    }

                    if (levelController.State != PrototypeLevelState.Running)
                    {
                        yield break;
                    }

                    levelController.SpawnEnemy(
                        group.EnemyConfig,
                        group.HealthMultiplier,
                        group.SpeedMultiplier);
                    SpawnedCount++;
                    yield return new WaitForSeconds(group.SpawnInterval);
                }
            }

            while (levelController.State == PrototypeLevelState.Lost)
            {
                yield return null;
            }

            if (levelController.State == PrototypeLevelState.Running)
            {
                levelController.HandleWaveCompleted();
            }
        }
    }
}
