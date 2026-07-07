using UnityEngine;

namespace CatGuard.Gameplay.Levels
{
    [CreateAssetMenu(fileName = "PrototypeLevelConfig", menuName = "Cat Guard/Prototype Level Config")]
    public sealed class PrototypeLevelConfig : ScriptableObject
    {
        [Header("Base")]
        [Min(1)]
        [SerializeField] private int baseLives = 5;

        [Header("Enemy")]
        [Min(1)]
        [SerializeField] private int enemyCount = 8;
        [Min(0.1f)]
        [SerializeField] private float spawnInterval = 0.9f;
        [Min(1f)]
        [SerializeField] private float enemyHealth = 3f;
        [Min(0.1f)]
        [SerializeField] private float enemySpeed = 0.9f;
        [Min(1)]
        [SerializeField] private int enemyBaseDamage = 1;

        [Header("Tower")]
        [Min(0.1f)]
        [SerializeField] private float towerRange = 2.5f;
        [Min(0.1f)]
        [SerializeField] private float towerDamage = 1f;
        [Min(0.05f)]
        [SerializeField] private float towerFireInterval = 0.35f;

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

        public int BaseLives => Mathf.Max(1, baseLives);
        public int EnemyCount => Mathf.Max(1, enemyCount);
        public float SpawnInterval => Mathf.Max(0.1f, spawnInterval);
        public float EnemyHealth => Mathf.Max(1f, enemyHealth);
        public float EnemySpeed => Mathf.Max(0.1f, enemySpeed);
        public int EnemyBaseDamage => Mathf.Max(1, enemyBaseDamage);
        public float TowerRange => Mathf.Max(0.1f, towerRange);
        public float TowerDamage => Mathf.Max(0.1f, towerDamage);
        public float TowerFireInterval => Mathf.Max(0.05f, towerFireInterval);
        public int GridColumns => Mathf.Max(1, gridColumns);
        public int GridRows => Mathf.Max(1, gridRows);
        public float CellSize => Mathf.Max(0.5f, cellSize);
        public Vector2 GridOrigin => gridOrigin;
        public Vector2[] PathPoints => pathPoints;

        public bool IsValidForPrototype()
        {
            return BaseLives > 0
                && EnemyCount > 0
                && EnemyHealth > 0f
                && EnemySpeed > 0f
                && TowerRange > 0f
                && TowerDamage > 0f
                && TowerFireInterval > 0f
                && GridColumns > 0
                && GridRows > 0
                && pathPoints is { Length: >= 2 };
        }
    }
}
