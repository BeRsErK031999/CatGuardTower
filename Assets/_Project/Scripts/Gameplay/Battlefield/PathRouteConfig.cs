using System;
using UnityEngine;

namespace CatGuard.Gameplay.Battlefield
{
    [Serializable]
    public sealed class PathRouteConfig
    {
        [SerializeField] private string routeId = "main";
        [SerializeField] private string displayName = "Main Route";
        [SerializeField] private Vector2[] points = Array.Empty<Vector2>();
        [SerializeField] private Vector2 spawnAnchor;
        [SerializeField] private Vector2 goalAnchor;
        [SerializeField] private string visualStyleId = "main";
        [Min(0.01f)]
        [SerializeField] private float visualWidth = 0.5f;
        [Min(0.01f)]
        [SerializeField] private float routeWeight = 1f;
        [SerializeField] private string[] tags = Array.Empty<string>();
        [Min(0f)]
        [SerializeField] private float spawnDelayOffset;
        [SerializeField] private string validationMetadata = string.Empty;

        public PathRouteConfig(
            string id,
            string title,
            Vector2[] routePoints,
            Vector2 routeSpawnAnchor,
            Vector2 routeGoalAnchor,
            string styleId,
            float width,
            float weight,
            string[] routeTags,
            float delayOffset = 0f,
            string metadata = "")
        {
            Configure(
                id,
                title,
                routePoints,
                routeSpawnAnchor,
                routeGoalAnchor,
                styleId,
                width,
                weight,
                routeTags,
                delayOffset,
                metadata);
        }

        public string RouteId => routeId ?? string.Empty;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? RouteId : displayName;
        public Vector2[] Points => points ?? Array.Empty<Vector2>();
        public Vector2 SpawnAnchor => spawnAnchor;
        public Vector2 GoalAnchor => goalAnchor;
        public string VisualStyleId => string.IsNullOrWhiteSpace(visualStyleId) ? "main" : visualStyleId;
        public float VisualWidth => Mathf.Max(0.01f, visualWidth);
        public float RouteWeight => Mathf.Max(0.01f, routeWeight);
        public string[] Tags => tags ?? Array.Empty<string>();
        public float SpawnDelayOffset => Mathf.Max(0f, spawnDelayOffset);
        public string ValidationMetadata => validationMetadata ?? string.Empty;

        public bool HasTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                return false;
            }

            foreach (var candidate in Tags)
            {
                if (string.Equals(candidate, tag, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public PathRouteDefinition CreateDefinition()
        {
            return new PathRouteDefinition(
                RouteId,
                DisplayName,
                Points,
                SpawnAnchor,
                GoalAnchor,
                VisualStyleId,
                VisualWidth,
                RouteWeight,
                Tags,
                SpawnDelayOffset,
                ValidationMetadata);
        }

        public void Configure(
            string id,
            string title,
            Vector2[] routePoints,
            Vector2 routeSpawnAnchor,
            Vector2 routeGoalAnchor,
            string styleId,
            float width,
            float weight,
            string[] routeTags,
            float delayOffset = 0f,
            string metadata = "")
        {
            routeId = id;
            displayName = title;
            points = routePoints ?? Array.Empty<Vector2>();
            spawnAnchor = routeSpawnAnchor;
            goalAnchor = routeGoalAnchor;
            visualStyleId = styleId;
            visualWidth = Mathf.Max(0.01f, width);
            routeWeight = Mathf.Max(0.01f, weight);
            tags = routeTags ?? Array.Empty<string>();
            spawnDelayOffset = Mathf.Max(0f, delayOffset);
            validationMetadata = metadata ?? string.Empty;
        }
    }

    public sealed class PathRouteDefinition
    {
        private readonly float[] cumulativeDistances;

        internal PathRouteDefinition(
            string id,
            string title,
            Vector2[] routePoints,
            Vector2 routeSpawnAnchor,
            Vector2 routeGoalAnchor,
            string styleId,
            float width,
            float weight,
            string[] routeTags,
            float delayOffset,
            string metadata)
        {
            RouteId = id ?? string.Empty;
            DisplayName = string.IsNullOrWhiteSpace(title) ? RouteId : title;
            Points = routePoints == null ? Array.Empty<Vector2>() : (Vector2[])routePoints.Clone();
            SpawnAnchor = routeSpawnAnchor;
            GoalAnchor = routeGoalAnchor;
            VisualStyleId = string.IsNullOrWhiteSpace(styleId) ? "main" : styleId;
            VisualWidth = Mathf.Max(0.01f, width);
            RouteWeight = Mathf.Max(0.01f, weight);
            Tags = routeTags == null ? Array.Empty<string>() : (string[])routeTags.Clone();
            SpawnDelayOffset = Mathf.Max(0f, delayOffset);
            ValidationMetadata = metadata ?? string.Empty;

            cumulativeDistances = new float[Points.Length];
            for (var index = 1; index < Points.Length; index++)
            {
                cumulativeDistances[index] = cumulativeDistances[index - 1]
                    + Vector2.Distance(Points[index - 1], Points[index]);
            }

            TotalLength = cumulativeDistances.Length == 0 ? 0f : cumulativeDistances[^1];
        }

        public string RouteId { get; }
        public string DisplayName { get; }
        public Vector2[] Points { get; }
        public Vector2 SpawnAnchor { get; }
        public Vector2 GoalAnchor { get; }
        public string VisualStyleId { get; }
        public float VisualWidth { get; }
        public float RouteWeight { get; }
        public string[] Tags { get; }
        public float SpawnDelayOffset { get; }
        public string ValidationMetadata { get; }
        public float TotalLength { get; }

        public bool HasTag(string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                return false;
            }

            foreach (var candidate in Tags)
            {
                if (string.Equals(candidate, tag, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public float GetNormalizedProgress(int nextPointIndex, Vector2 currentPosition)
        {
            if (Points.Length < 2 || TotalLength <= 0.0001f)
            {
                return 0f;
            }

            var clampedNext = Mathf.Clamp(nextPointIndex, 1, Points.Length - 1);
            var segmentStart = Points[clampedNext - 1];
            var segmentEnd = Points[clampedNext];
            var segmentLength = Vector2.Distance(segmentStart, segmentEnd);
            var segmentProgress = segmentLength <= 0.0001f
                ? segmentLength
                : Mathf.Clamp(Vector2.Distance(segmentStart, currentPosition), 0f, segmentLength);
            var travelled = cumulativeDistances[clampedNext - 1] + segmentProgress;
            return Mathf.Clamp01(travelled / TotalLength);
        }
    }
}
