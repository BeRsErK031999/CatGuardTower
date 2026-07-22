using System.Collections;
using System.Collections.Generic;
using CatGuard.Gameplay.Battlefield;
using CatGuard.Gameplay.Levels;
using UnityEngine;

namespace CatGuard.Gameplay.Waves
{
    public sealed class PrototypeWaveSpawner : MonoBehaviour
    {
        private const float RouteWarningLeadSeconds = 0.65f;

        private PrototypeLevelController levelController;
        private WaveConfig config;
        private Coroutine spawnRoutine;
        private int pendingConcurrentGroups;

        public int SpawnedCount { get; private set; }

        public void Initialize(PrototypeLevelController owner, WaveConfig waveConfig)
        {
            levelController = owner;
            config = waveConfig;
            SpawnedCount = 0;
            pendingConcurrentGroups = 0;
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
            if (config.ConcurrentGroups)
            {
                pendingConcurrentGroups = config.Groups.Length;
                foreach (var group in config.Groups)
                {
                    StartCoroutine(SpawnConcurrentGroup(group));
                }

                while (pendingConcurrentGroups > 0)
                {
                    yield return null;
                }
            }
            else
            {
                foreach (var group in config.Groups)
                {
                    yield return SpawnGroup(group);
                    if (levelController.State != PrototypeLevelState.Running)
                    {
                        yield break;
                    }
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

        private IEnumerator SpawnConcurrentGroup(WaveEnemyGroup group)
        {
            yield return SpawnGroup(group);
            pendingConcurrentGroups = Mathf.Max(0, pendingConcurrentGroups - 1);
        }

        private IEnumerator SpawnGroup(WaveEnemyGroup group)
        {
            if (group == null || group.EnemyConfig == null)
            {
                yield break;
            }

            if (group.DelayBeforeGroup > 0f)
            {
                yield return new WaitForSeconds(group.DelayBeforeGroup);
            }

            var warnedRoutes = new HashSet<string>();
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

                var routeId = group.GetConfiguredRouteId(index);
                if (!levelController.Battlefield.TryGetRoute(routeId, out var route))
                {
                    yield break;
                }

                while (levelController.State == PrototypeLevelState.Running
                    && !levelController.IsRouteAvailable(route.RouteId))
                {
                    yield return null;
                }

                if (levelController.State != PrototypeLevelState.Running)
                {
                    yield break;
                }

                if (!string.IsNullOrWhiteSpace(levelController.DevelopmentRouteFilter)
                    && route.RouteId != levelController.DevelopmentRouteFilter)
                {
                    continue;
                }

                if (warnedRoutes.Add(route.RouteId))
                {
                    if (route.SpawnDelayOffset > 0f)
                    {
                        yield return new WaitForSeconds(route.SpawnDelayOffset);
                    }

                    levelController.RegisterIncomingRoute(route.RouteId, RouteWarningLeadSeconds + 2.5f);
                    yield return new WaitForSeconds(RouteWarningLeadSeconds);
                }

                levelController.SpawnEnemy(
                    group.EnemyConfig,
                    route,
                    group.HealthMultiplier,
                    group.SpeedMultiplier);
                SpawnedCount++;
                yield return new WaitForSeconds(group.SpawnInterval);
            }
        }
    }
}
