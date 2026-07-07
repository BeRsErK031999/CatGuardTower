using System;
using CatGuard.Gameplay.Towers;
using CatGuard.Gameplay.Waves;
using UnityEngine;

namespace CatGuard.Gameplay.Levels
{
    [CreateAssetMenu(fileName = "LevelConfig", menuName = "Cat Guard/Level Config")]
    public sealed class LevelConfig : ScriptableObject
    {
        [Header("Base")]
        [Min(1)]
        [SerializeField] private int baseLives = 6;

        [Header("Grid")]
        [Min(1)]
        [SerializeField] private int gridColumns = 4;
        [Min(1)]
        [SerializeField] private int gridRows = 3;
        [Min(0.5f)]
        [SerializeField] private float cellSize = 1.15f;
        [SerializeField] private Vector2 gridOrigin = new(-1.75f, -2.8f);

        [Header("Path")]
        [SerializeField]
        private Vector2[] pathPoints =
        {
            new(-3.7f, 3f),
            new(-1.4f, 2.2f),
            new(1.5f, 2.2f),
            new(2.8f, 0.7f),
            new(1.2f, -0.9f),
            new(3.7f, -2.7f)
        };

        [Header("Content")]
        [SerializeField] private TowerConfig[] availableTowers = Array.Empty<TowerConfig>();
        [SerializeField] private WaveConfig waveConfig;

        public int BaseLives => Mathf.Max(1, baseLives);
        public int GridColumns => Mathf.Max(1, gridColumns);
        public int GridRows => Mathf.Max(1, gridRows);
        public float CellSize => Mathf.Max(0.5f, cellSize);
        public Vector2 GridOrigin => gridOrigin;
        public Vector2[] PathPoints => pathPoints;
        public TowerConfig[] AvailableTowers => availableTowers;
        public WaveConfig WaveConfig => waveConfig;

        public bool IsValidForCore()
        {
            if (BaseLives <= 0 || GridColumns <= 0 || GridRows <= 0 || CellSize <= 0f)
            {
                return false;
            }

            if (pathPoints == null || pathPoints.Length < 2)
            {
                return false;
            }

            if (availableTowers == null || availableTowers.Length < 3)
            {
                return false;
            }

            foreach (var tower in availableTowers)
            {
                if (tower == null || !tower.IsValid())
                {
                    return false;
                }
            }

            return waveConfig != null && waveConfig.IsValid();
        }

        public void Configure(
            int lives,
            int columns,
            int rows,
            float size,
            Vector2 origin,
            Vector2[] path,
            TowerConfig[] towers,
            WaveConfig wave)
        {
            baseLives = lives;
            gridColumns = columns;
            gridRows = rows;
            cellSize = size;
            gridOrigin = origin;
            pathPoints = path ?? Array.Empty<Vector2>();
            availableTowers = towers ?? Array.Empty<TowerConfig>();
            waveConfig = wave;
        }
    }
}
