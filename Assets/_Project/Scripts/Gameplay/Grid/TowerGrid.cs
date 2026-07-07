using System.Collections.Generic;
using CatGuard.Gameplay.Levels;
using CatGuard.Gameplay.Towers;
using CatGuard.Utils;
using UnityEngine;

namespace CatGuard.Gameplay.Grid
{
    public sealed class TowerGrid : MonoBehaviour
    {
        private readonly Dictionary<Vector2Int, SpriteRenderer> cellRenderers = new();
        private readonly HashSet<Vector2Int> occupiedCells = new();

        private PrototypeLevelController levelController;
        private PrototypeLevelConfig config;
        private Camera mainCamera;

        public int PlacedTowerCount => occupiedCells.Count;

        public void Initialize(PrototypeLevelController owner, PrototypeLevelConfig levelConfig)
        {
            levelController = owner;
            config = levelConfig;
            mainCamera = Camera.main;

            ClearCells();
            CreateCells();
        }

        public bool TryPlaceAtWorld(Vector2 worldPosition)
        {
            if (levelController == null || config == null || levelController.State != PrototypeLevelState.Running)
            {
                return false;
            }

            if (!TryGetCell(worldPosition, out var cell))
            {
                return false;
            }

            if (occupiedCells.Contains(cell))
            {
                return false;
            }

            occupiedCells.Add(cell);
            UpdateCellVisual(cell);
            levelController.CreateTower(GetCellCenter(cell));

            return true;
        }

        private void Update()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (mainCamera == null)
            {
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                TryPlaceFromScreen(Input.mousePosition);
            }

            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                TryPlaceFromScreen(Input.GetTouch(0).position);
            }
        }

        private void TryPlaceFromScreen(Vector2 screenPosition)
        {
            var world = mainCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, 0f));
            TryPlaceAtWorld(world);
        }

        private void CreateCells()
        {
            for (var row = 0; row < config.GridRows; row++)
            {
                for (var column = 0; column < config.GridColumns; column++)
                {
                    var cell = new Vector2Int(column, row);
                    var cellObject = new GameObject($"Cell_{column}_{row}");
                    cellObject.transform.SetParent(transform, false);
                    cellObject.transform.position = GetCellCenter(cell);
                    cellObject.transform.localScale = Vector3.one * (config.CellSize * 0.86f);

                    var renderer = cellObject.AddComponent<SpriteRenderer>();
                    renderer.sprite = PrototypeSpriteFactory.SquareSprite;
                    renderer.sortingOrder = 2;
                    renderer.color = new Color(0.18f, 0.28f, 0.32f, 0.45f);

                    cellRenderers[cell] = renderer;
                }
            }
        }

        private void ClearCells()
        {
            cellRenderers.Clear();
            occupiedCells.Clear();

            for (var index = transform.childCount - 1; index >= 0; index--)
            {
                Destroy(transform.GetChild(index).gameObject);
            }
        }

        private bool TryGetCell(Vector2 worldPosition, out Vector2Int cell)
        {
            var local = worldPosition - config.GridOrigin;
            var column = Mathf.RoundToInt(local.x / config.CellSize);
            var row = Mathf.RoundToInt(local.y / config.CellSize);
            cell = new Vector2Int(column, row);

            if (column < 0 || column >= config.GridColumns || row < 0 || row >= config.GridRows)
            {
                return false;
            }

            var center = GetCellCenter(cell);
            return Mathf.Abs(worldPosition.x - center.x) <= config.CellSize * 0.45f
                && Mathf.Abs(worldPosition.y - center.y) <= config.CellSize * 0.45f;
        }

        private Vector2 GetCellCenter(Vector2Int cell)
        {
            return config.GridOrigin + new Vector2(cell.x * config.CellSize, cell.y * config.CellSize);
        }

        private void UpdateCellVisual(Vector2Int cell)
        {
            if (!cellRenderers.TryGetValue(cell, out var renderer))
            {
                return;
            }

            renderer.color = new Color(0.12f, 0.44f, 0.52f, 0.65f);
        }
    }
}
