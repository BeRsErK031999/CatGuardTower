using System;

namespace CatGuard.Gameplay.Towers
{
    public readonly struct EnemyTargetingMetrics
    {
        public EnemyTargetingMetrics(
            string routeId,
            float normalizedProgress,
            float maxHealth,
            float distanceSquared,
            int spawnOrder)
        {
            RouteId = routeId ?? string.Empty;
            NormalizedProgress = normalizedProgress;
            MaxHealth = maxHealth;
            DistanceSquared = distanceSquared;
            SpawnOrder = spawnOrder;
        }

        public string RouteId { get; }
        public float NormalizedProgress { get; }
        public float MaxHealth { get; }
        public float DistanceSquared { get; }
        public int SpawnOrder { get; }
    }

    public static class EnemyTargeting
    {
        public static bool IsBetter(
            TowerTargetPriority priority,
            EnemyTargetingMetrics candidate,
            EnemyTargetingMetrics current)
        {
            var primaryComparison = priority switch
            {
                TowerTargetPriority.Last => current.NormalizedProgress.CompareTo(candidate.NormalizedProgress),
                TowerTargetPriority.Strong => candidate.MaxHealth.CompareTo(current.MaxHealth),
                _ => candidate.NormalizedProgress.CompareTo(current.NormalizedProgress)
            };
            if (primaryComparison != 0)
            {
                return primaryComparison > 0;
            }

            var progressComparison = candidate.NormalizedProgress.CompareTo(current.NormalizedProgress);
            if (progressComparison != 0)
            {
                return progressComparison > 0;
            }

            var distanceComparison = current.DistanceSquared.CompareTo(candidate.DistanceSquared);
            if (distanceComparison != 0)
            {
                return distanceComparison > 0;
            }

            var routeComparison = string.Compare(candidate.RouteId, current.RouteId, StringComparison.Ordinal);
            return routeComparison < 0
                || (routeComparison == 0 && candidate.SpawnOrder < current.SpawnOrder);
        }
    }
}
