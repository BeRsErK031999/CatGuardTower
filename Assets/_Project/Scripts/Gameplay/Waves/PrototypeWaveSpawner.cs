using System.Collections;
using CatGuard.Gameplay.Levels;
using UnityEngine;

namespace CatGuard.Gameplay.Waves
{
    public sealed class PrototypeWaveSpawner : MonoBehaviour
    {
        private PrototypeLevelController levelController;
        private PrototypeLevelConfig config;
        private Coroutine spawnRoutine;

        public int SpawnedCount { get; private set; }

        public void Initialize(PrototypeLevelController owner, PrototypeLevelConfig levelConfig)
        {
            levelController = owner;
            config = levelConfig;
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
            while (SpawnedCount < config.EnemyCount && levelController.State == PrototypeLevelState.Running)
            {
                levelController.SpawnEnemy();
                SpawnedCount++;
                yield return new WaitForSeconds(config.SpawnInterval);
            }

            levelController.HandleWaveCompleted();
        }
    }
}
