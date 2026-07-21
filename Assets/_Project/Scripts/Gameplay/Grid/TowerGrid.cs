using System.Collections.Generic;
using CatGuard.Gameplay.Battlefield;
using CatGuard.Gameplay.Levels;
using CatGuard.Gameplay.Towers;
using CatGuard.Utils;
using UnityEngine;

namespace CatGuard.Gameplay.Grid
{
    public sealed class TowerGrid : MonoBehaviour
    {
        private sealed class CellVisual
        {
            public CellVisual(SpriteRenderer border, SpriteRenderer fill)
            {
                Border = border;
                Fill = fill;
            }

            public SpriteRenderer Border { get; }
            public SpriteRenderer Fill { get; }
        }

        private readonly Dictionary<int, CellVisual> cellRenderers = new();
        private readonly HashSet<int> occupiedCells = new();
        private readonly List<Vector2> cellCenters = new();

        private PrototypeLevelController levelController;
        private BattlefieldDefinition battlefield;
        private Camera mainCamera;

        public int PlacedTowerCount => occupiedCells.Count;
        public IReadOnlyList<Vector2> CellCenters => cellCenters;

        public void Initialize(PrototypeLevelController owner, BattlefieldDefinition definition)
        {
            levelController = owner;
            battlefield = definition;
            mainCamera = Camera.main;

            ClearCells();
            CreateCells();
        }

        public bool TryPlaceAtWorld(Vector2 worldPosition)
        {
            if (levelController == null || battlefield == null || !levelController.CanPlaceTowers)
            {
                return false;
            }

            if (!TryGetCellIndex(worldPosition, out var cellIndex))
            {
                return false;
            }

            if (occupiedCells.Contains(cellIndex))
            {
                return false;
            }

            if (!levelController.TryCreateTower(cellCenters[cellIndex]))
            {
                return false;
            }

            occupiedCells.Add(cellIndex);
            UpdateCellVisual(cellIndex);
            return true;
        }

        public bool TryPlaceAtCellIndex(int cellIndex)
        {
            return cellIndex >= 0
                && cellIndex < cellCenters.Count
                && TryPlaceAtWorld(cellCenters[cellIndex]);
        }

        public bool IsOccupied(int cellIndex)
        {
            return occupiedCells.Contains(cellIndex);
        }

        public bool TryPlaceFromScreen(Vector2 screenPosition)
        {
            if (levelController == null || levelController.IsScreenPointOverHud(screenPosition))
            {
                return false;
            }

            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (mainCamera == null)
            {
                return false;
            }

            var world = mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 0f));
            return TryPlaceAtWorld(world);
        }

        private void CreateCells()
        {
            if (battlefield?.BuildCellCenters == null)
            {
                return;
            }

            for (var index = 0; index < battlefield.BuildCellCenters.Count; index++)
            {
                var center = battlefield.BuildCellCenters[index];
                cellCenters.Add(center);
                var cellObject = new GameObject($"Cell_{index:00}");
                cellObject.transform.SetParent(transform, false);
                cellObject.transform.position = center;
                cellObject.transform.localScale = Vector3.one * (battlefield.PlacementCellSize * 0.82f);

                var borderRenderer = cellObject.AddComponent<SpriteRenderer>();
                borderRenderer.sprite = PrototypeSpriteFactory.SquareSprite;
                borderRenderer.sortingOrder = 2;
                borderRenderer.color = new Color(0.07f, 0.26f, 0.24f, 0.5f);

                var fillObject = new GameObject("Fill");
                fillObject.transform.SetParent(cellObject.transform, false);
                fillObject.transform.localScale = new Vector3(0.84f, 0.84f, 1f);

                var fillRenderer = fillObject.AddComponent<SpriteRenderer>();
                fillRenderer.sprite = PrototypeSpriteFactory.SquareSprite;
                fillRenderer.sortingOrder = 3;
                fillRenderer.color = new Color(0.15f, 0.38f, 0.31f, 0.18f);

                cellRenderers[index] = new CellVisual(borderRenderer, fillRenderer);
            }
        }

        private void ClearCells()
        {
            cellRenderers.Clear();
            occupiedCells.Clear();
            cellCenters.Clear();

            for (var index = transform.childCount - 1; index >= 0; index--)
            {
                Destroy(transform.GetChild(index).gameObject);
            }
        }

        private bool TryGetCellIndex(Vector2 worldPosition, out int cellIndex)
        {
            cellIndex = -1;
            var nearestDistance = float.MaxValue;
            for (var index = 0; index < cellCenters.Count; index++)
            {
                var distance = (worldPosition - cellCenters[index]).sqrMagnitude;
                if (distance >= nearestDistance)
                {
                    continue;
                }

                nearestDistance = distance;
                cellIndex = index;
            }

            var maximumDistance = battlefield.PlacementCellSize * 0.45f;
            return cellIndex >= 0 && nearestDistance <= maximumDistance * maximumDistance;
        }

        private void UpdateCellVisual(int cellIndex)
        {
            if (!cellRenderers.TryGetValue(cellIndex, out var visual))
            {
                return;
            }

            visual.Border.color = new Color(0.1f, 0.42f, 0.37f, 0.72f);
            visual.Fill.color = new Color(0.17f, 0.5f, 0.42f, 0.34f);
        }
    }
}
