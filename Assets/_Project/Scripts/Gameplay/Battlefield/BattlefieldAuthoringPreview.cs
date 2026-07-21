using UnityEngine;

namespace CatGuard.Gameplay.Battlefield
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class BattlefieldAuthoringPreview : MonoBehaviour
    {
        [SerializeField] private BattlefieldConfig battlefieldConfig;
        [SerializeField] private bool showPlacementCells = true;
        [SerializeField] private bool showDecorationAnchors = true;

        public BattlefieldConfig BattlefieldConfig => battlefieldConfig;

        public void Configure(BattlefieldConfig config)
        {
            battlefieldConfig = config;
        }

        private void OnDrawGizmos()
        {
            var battlefield = battlefieldConfig?.CreateDefinition();
            if (battlefield == null)
            {
                return;
            }

            DrawRect(battlefield.WorldBounds, new Color(1f, 1f, 1f, 0.8f));
            DrawRect(battlefield.CameraBounds, new Color(0.2f, 0.85f, 1f, 0.9f));

            foreach (var zone in battlefield.PlacementZones)
            {
                DrawRect(zone.Bounds, new Color(0.2f, 0.9f, 0.45f, 0.68f));
            }

            foreach (var zone in battlefield.BlockedZones)
            {
                DrawRect(zone.Bounds, new Color(1f, 0.24f, 0.2f, 0.9f));
            }

            if (showPlacementCells)
            {
                Gizmos.color = new Color(0.3f, 1f, 0.55f, 0.72f);
                var cellSize = battlefield.PlacementCellSize * 0.82f;
                foreach (var center in battlefield.BuildCellCenters)
                {
                    Gizmos.DrawWireCube(center, new Vector3(cellSize, cellSize, 0f));
                }
            }

            for (var routeIndex = 0; routeIndex < battlefield.Routes.Length; routeIndex++)
            {
                var route = battlefield.Routes[routeIndex];
                var colors = BattlefieldRouteVisualStyle.GetColors(route.VisualStyleId, routeIndex);
                Gizmos.color = colors.End;
                for (var pointIndex = 1; pointIndex < route.Points.Length; pointIndex++)
                {
                    Gizmos.DrawLine(route.Points[pointIndex - 1], route.Points[pointIndex]);
                }

                Gizmos.color = colors.Start;
                Gizmos.DrawSphere(route.SpawnAnchor, 0.18f);
                Gizmos.color = colors.End;
                Gizmos.DrawCube(route.GoalAnchor, Vector3.one * 0.3f);
            }

            if (!showDecorationAnchors)
            {
                return;
            }

            Gizmos.color = new Color(1f, 0.88f, 0.25f, 0.9f);
            foreach (var decoration in battlefield.DecorationAnchors)
            {
                Gizmos.DrawWireSphere(decoration.Position, 0.16f * decoration.Scale);
            }
        }

        private static void DrawRect(Rect rect, Color color)
        {
            Gizmos.color = color;
            var bottomLeft = new Vector3(rect.xMin, rect.yMin);
            var bottomRight = new Vector3(rect.xMax, rect.yMin);
            var topRight = new Vector3(rect.xMax, rect.yMax);
            var topLeft = new Vector3(rect.xMin, rect.yMax);
            Gizmos.DrawLine(bottomLeft, bottomRight);
            Gizmos.DrawLine(bottomRight, topRight);
            Gizmos.DrawLine(topRight, topLeft);
            Gizmos.DrawLine(topLeft, bottomLeft);
        }
    }
}
