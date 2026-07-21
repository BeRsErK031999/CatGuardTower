using System;
using UnityEngine;

namespace CatGuard.Gameplay.Battlefield
{
    public enum BattlefieldCameraMode
    {
        FixedOverview,
        ScrollableLarge
    }

    [Serializable]
    public struct BattlefieldZone
    {
        [SerializeField] private string zoneId;
        [SerializeField] private Rect bounds;

        public BattlefieldZone(string id, Rect zoneBounds)
        {
            zoneId = id;
            bounds = zoneBounds;
        }

        public string ZoneId => zoneId ?? string.Empty;
        public Rect Bounds => bounds;
        public bool Contains(Vector2 point) => bounds.Contains(point);
    }

    [Serializable]
    public struct BattlefieldDecorationAnchor
    {
        [SerializeField] private string decorationId;
        [SerializeField] private Vector2 position;
        [Min(0.1f)]
        [SerializeField] private float scale;
        [SerializeField] private bool foreground;

        public BattlefieldDecorationAnchor(string id, Vector2 anchorPosition, float anchorScale, bool renderInForeground)
        {
            decorationId = id;
            position = anchorPosition;
            scale = anchorScale;
            foreground = renderInForeground;
        }

        public string DecorationId => decorationId ?? string.Empty;
        public Vector2 Position => position;
        public float Scale => Mathf.Max(0.1f, scale);
        public bool Foreground => foreground;
    }

    [CreateAssetMenu(fileName = "BattlefieldConfig", menuName = "Cat Guard/Battlefield Config")]
    public sealed class BattlefieldConfig : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string battlefieldId = "garden_gate_wide";
        [SerializeField] private string displayName = "Garden Gate Wide";
        [SerializeField] private string backgroundId = "garden_day";
        [SerializeField] private string biomeId = "garden";

        [Header("World And Camera")]
        [SerializeField] private BattlefieldCameraMode cameraMode = BattlefieldCameraMode.FixedOverview;
        [SerializeField] private Rect worldBounds = new(-7f, -4.5f, 14f, 9f);
        [SerializeField] private Rect cameraBounds = new(-7f, -4.5f, 14f, 9f);
        [SerializeField] private Vector2 initialCameraFocus;
        [Min(3f)]
        [SerializeField] private float scrollableViewHeight = 7.5f;

        [Header("Route")]
        [SerializeField] private Vector2[] pathPoints = Array.Empty<Vector2>();
        [Min(0.1f)]
        [SerializeField] private float pathVisualWidth = 0.48f;
        [SerializeField] private Vector2 spawnPresentationAnchor;
        [SerializeField] private Vector2 goalPresentationAnchor;

        [Header("Placement")]
        [Min(0.5f)]
        [SerializeField] private float placementCellSize = 1.1f;
        [SerializeField] private BattlefieldZone[] placementZones = Array.Empty<BattlefieldZone>();
        [SerializeField] private BattlefieldZone[] blockedZones = Array.Empty<BattlefieldZone>();

        [Header("Presentation")]
        [SerializeField] private BattlefieldDecorationAnchor[] decorationAnchors = Array.Empty<BattlefieldDecorationAnchor>();

        public string BattlefieldId => string.IsNullOrWhiteSpace(battlefieldId) ? name : battlefieldId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? BattlefieldId : displayName;
        public string BackgroundId => backgroundId ?? string.Empty;
        public string BiomeId => biomeId ?? string.Empty;
        public BattlefieldCameraMode CameraMode => cameraMode;
        public Rect WorldBounds => worldBounds;
        public Rect CameraBounds => cameraBounds;
        public Vector2 InitialCameraFocus => initialCameraFocus;
        public float ScrollableViewHeight => Mathf.Max(3f, scrollableViewHeight);
        public Vector2[] PathPoints => pathPoints ?? Array.Empty<Vector2>();
        public float PathVisualWidth => Mathf.Max(0.1f, pathVisualWidth);
        public Vector2 SpawnPresentationAnchor => spawnPresentationAnchor;
        public Vector2 GoalPresentationAnchor => goalPresentationAnchor;
        public float PlacementCellSize => Mathf.Max(0.5f, placementCellSize);
        public BattlefieldZone[] PlacementZones => placementZones ?? Array.Empty<BattlefieldZone>();
        public BattlefieldZone[] BlockedZones => blockedZones ?? Array.Empty<BattlefieldZone>();
        public BattlefieldDecorationAnchor[] DecorationAnchors => decorationAnchors ?? Array.Empty<BattlefieldDecorationAnchor>();

        public BattlefieldDefinition CreateDefinition()
        {
            return BattlefieldDefinition.FromConfig(this);
        }

        public bool IsValid()
        {
            return CreateDefinition().IsValid(out _);
        }

        public void Configure(
            string id,
            string title,
            string mapBackgroundId,
            string mapBiomeId,
            BattlefieldCameraMode mode,
            Rect mapWorldBounds,
            Rect mapCameraBounds,
            Vector2 cameraFocus,
            float viewHeight,
            Vector2[] route,
            float routeVisualWidth,
            Vector2 spawnAnchor,
            Vector2 goalAnchor,
            float cellSize,
            BattlefieldZone[] buildZones,
            BattlefieldZone[] noBuildZones,
            BattlefieldDecorationAnchor[] decorations)
        {
            battlefieldId = id;
            displayName = title;
            backgroundId = mapBackgroundId;
            biomeId = mapBiomeId;
            cameraMode = mode;
            worldBounds = mapWorldBounds;
            cameraBounds = mapCameraBounds;
            initialCameraFocus = cameraFocus;
            scrollableViewHeight = Mathf.Max(3f, viewHeight);
            pathPoints = route ?? Array.Empty<Vector2>();
            pathVisualWidth = Mathf.Max(0.1f, routeVisualWidth);
            spawnPresentationAnchor = spawnAnchor;
            goalPresentationAnchor = goalAnchor;
            placementCellSize = Mathf.Max(0.5f, cellSize);
            placementZones = buildZones ?? Array.Empty<BattlefieldZone>();
            blockedZones = noBuildZones ?? Array.Empty<BattlefieldZone>();
            decorationAnchors = decorations ?? Array.Empty<BattlefieldDecorationAnchor>();
        }
    }
}
