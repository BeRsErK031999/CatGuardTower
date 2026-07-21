using System;
using System.Collections.Generic;
using CatGuard.Gameplay.Battlefield;
using CatGuard.Gameplay.Enemies;
using UnityEngine;

namespace CatGuard.Gameplay.Waves
{
    public enum WaveRouteSelectionRule
    {
        Explicit,
        RoundRobin
    }

    [CreateAssetMenu(fileName = "WaveConfig", menuName = "Cat Guard/Wave Config")]
    public sealed class WaveConfig : ScriptableObject
    {
        [SerializeField] private WaveEnemyGroup[] groups = Array.Empty<WaveEnemyGroup>();
        [SerializeField] private bool concurrentGroups;

        public WaveEnemyGroup[] Groups => groups ?? Array.Empty<WaveEnemyGroup>();
        public bool ConcurrentGroups => concurrentGroups;

        public int TotalEnemyCount
        {
            get
            {
                var count = 0;
                foreach (var group in Groups)
                {
                    if (group?.EnemyConfig != null)
                    {
                        count += group.Count;
                    }
                }

                return count;
            }
        }

        public bool IsValid()
        {
            if (Groups.Length == 0)
            {
                return false;
            }

            foreach (var group in Groups)
            {
                if (group == null || !group.IsValid())
                {
                    return false;
                }
            }

            return TotalEnemyCount > 0;
        }

        public bool IsValid(BattlefieldDefinition battlefield, out string error)
        {
            if (!IsValid() || battlefield == null)
            {
                error = "Wave and battlefield must both be valid.";
                return false;
            }

            foreach (var group in Groups)
            {
                if (!group.ValidateRoutes(battlefield, out error))
                {
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }

        public int GetExpectedEnemyCount(string routeFilter)
        {
            if (string.IsNullOrWhiteSpace(routeFilter))
            {
                return TotalEnemyCount;
            }

            var count = 0;
            foreach (var group in Groups)
            {
                if (group == null)
                {
                    continue;
                }

                for (var spawnIndex = 0; spawnIndex < group.Count; spawnIndex++)
                {
                    if (string.Equals(group.GetConfiguredRouteId(spawnIndex), routeFilter, StringComparison.Ordinal))
                    {
                        count++;
                    }
                }
            }

            return count;
        }

        public IReadOnlyList<string> GetReferencedRouteIds()
        {
            var result = new List<string>();
            var unique = new HashSet<string>(StringComparer.Ordinal);
            foreach (var group in Groups)
            {
                if (group == null)
                {
                    continue;
                }

                foreach (var routeId in group.RouteIds)
                {
                    if (!string.IsNullOrWhiteSpace(routeId) && unique.Add(routeId))
                    {
                        result.Add(routeId);
                    }
                }
            }

            return result;
        }

        public void Configure(WaveEnemyGroup[] waveGroups)
        {
            groups = waveGroups ?? Array.Empty<WaveEnemyGroup>();
            concurrentGroups = false;
        }

        public void ConfigureConcurrentGroups(bool concurrent)
        {
            concurrentGroups = concurrent;
        }
    }

    [Serializable]
    public sealed class WaveEnemyGroup
    {
        [SerializeField] private EnemyConfig enemyConfig;
        [Min(1)]
        [SerializeField] private int count = 1;
        [Min(0.1f)]
        [SerializeField] private float spawnInterval = 0.8f;
        [Min(0f)]
        [SerializeField] private float delayBeforeGroup;
        [Min(0.1f)]
        [SerializeField] private float healthMultiplier = 1f;
        [Min(0.1f)]
        [SerializeField] private float speedMultiplier = 1f;
        [SerializeField] private WaveRouteSelectionRule routeSelectionRule;
        [SerializeField] private string[] routeIds = { "main" };

        public WaveEnemyGroup(
            EnemyConfig config,
            int enemyCount,
            float interval,
            float delay = 0f,
            float healthScale = 1f,
            float speedScale = 1f,
            string routeId = "main")
        {
            enemyConfig = config;
            count = enemyCount;
            spawnInterval = interval;
            delayBeforeGroup = delay;
            healthMultiplier = healthScale;
            speedMultiplier = speedScale;
            routeSelectionRule = WaveRouteSelectionRule.Explicit;
            routeIds = new[] { string.IsNullOrWhiteSpace(routeId) ? "main" : routeId };
        }

        public EnemyConfig EnemyConfig => enemyConfig;
        public int Count => Mathf.Max(1, count);
        public float SpawnInterval => Mathf.Max(0.1f, spawnInterval);
        public float DelayBeforeGroup => Mathf.Max(0f, delayBeforeGroup);
        public float HealthMultiplier => Mathf.Max(0.1f, healthMultiplier);
        public float SpeedMultiplier => Mathf.Max(0.1f, speedMultiplier);
        public WaveRouteSelectionRule RouteSelectionRule => routeSelectionRule;
        public string[] RouteIds => routeIds ?? Array.Empty<string>();

        public bool IsValid()
        {
            if (enemyConfig == null
                || !enemyConfig.IsValid()
                || Count <= 0
                || SpawnInterval <= 0f
                || healthMultiplier <= 0f
                || speedMultiplier <= 0f
                || RouteIds.Length == 0)
            {
                return false;
            }

            if (routeSelectionRule == WaveRouteSelectionRule.Explicit && RouteIds.Length != 1)
            {
                return false;
            }

            foreach (var routeId in RouteIds)
            {
                if (string.IsNullOrWhiteSpace(routeId))
                {
                    return false;
                }
            }

            return true;
        }

        public bool ValidateRoutes(BattlefieldDefinition battlefield, out string error)
        {
            if (!IsValid())
            {
                error = "Wave group routing data is invalid.";
                return false;
            }

            var unique = new HashSet<string>(StringComparer.Ordinal);
            foreach (var routeId in RouteIds)
            {
                if (!unique.Add(routeId))
                {
                    error = $"Wave group contains duplicate route id '{routeId}'.";
                    return false;
                }

                if (!battlefield.TryGetRoute(routeId, out _))
                {
                    error = $"Wave group references missing route '{routeId}' on battlefield '{battlefield.BattlefieldId}'.";
                    return false;
                }
            }

            error = string.Empty;
            return true;
        }

        public string GetConfiguredRouteId(int spawnIndex)
        {
            if (RouteIds.Length == 0)
            {
                return string.Empty;
            }

            return routeSelectionRule == WaveRouteSelectionRule.RoundRobin
                ? RouteIds[Math.Abs(spawnIndex) % RouteIds.Length]
                : RouteIds[0];
        }

        public void ConfigureRoute(string routeId, float? startDelay = null)
        {
            routeSelectionRule = WaveRouteSelectionRule.Explicit;
            routeIds = new[] { string.IsNullOrWhiteSpace(routeId) ? "main" : routeId };
            if (startDelay.HasValue)
            {
                delayBeforeGroup = Mathf.Max(0f, startDelay.Value);
            }
        }

        public void ConfigureRoundRobin(string[] deterministicRouteIds, float? startDelay = null)
        {
            routeSelectionRule = WaveRouteSelectionRule.RoundRobin;
            routeIds = deterministicRouteIds ?? Array.Empty<string>();
            if (startDelay.HasValue)
            {
                delayBeforeGroup = Mathf.Max(0f, startDelay.Value);
            }
        }
    }
}
