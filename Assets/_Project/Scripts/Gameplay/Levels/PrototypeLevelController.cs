using System.Collections.Generic;
using CatGuard.Gameplay.Enemies;
using CatGuard.Gameplay.Grid;
using CatGuard.Gameplay.Towers;
using CatGuard.Gameplay.Waves;
using CatGuard.UI.HUD;
using CatGuard.Utils;
using UnityEngine;

namespace CatGuard.Gameplay.Levels
{
    public sealed class PrototypeLevelController : MonoBehaviour
    {
        [SerializeField] private PrototypeLevelConfig config;
        [SerializeField] private TowerGrid towerGrid;
        [SerializeField] private PrototypeWaveSpawner waveSpawner;
        [SerializeField] private PrototypeHud hud;
        [SerializeField] private Transform runtimeRoot;

        private readonly List<BasicEnemy> activeEnemies = new();
        private readonly List<BasicTower> towers = new();
        private bool waveCompleted;

        public PrototypeLevelState State { get; private set; } = PrototypeLevelState.NotStarted;
        public int Lives { get; private set; }
        public int DefeatedEnemies { get; private set; }
        public int EscapedEnemies { get; private set; }
        public int SpawnedEnemies { get; private set; }
        public int TotalEnemies => config == null ? 0 : config.EnemyCount;
        public int ActiveEnemyCount => activeEnemies.Count;
        public int TowerCount => towers.Count;

        public bool IsConfigured => config != null
            && towerGrid != null
            && waveSpawner != null
            && hud != null
            && runtimeRoot != null
            && config.IsValidForPrototype();

        public void Configure(
            PrototypeLevelConfig levelConfig,
            TowerGrid grid,
            PrototypeWaveSpawner spawner,
            PrototypeHud levelHud,
            Transform root)
        {
            config = levelConfig;
            towerGrid = grid;
            waveSpawner = spawner;
            hud = levelHud;
            runtimeRoot = root;
        }

        public BasicEnemy FindNearestEnemy(Vector3 origin, float range)
        {
            BasicEnemy nearest = null;
            var nearestDistance = range * range;

            foreach (var enemy in activeEnemies)
            {
                if (enemy == null || !enemy.IsAlive)
                {
                    continue;
                }

                var distance = (enemy.transform.position - origin).sqrMagnitude;
                if (distance > nearestDistance)
                {
                    continue;
                }

                nearest = enemy;
                nearestDistance = distance;
            }

            return nearest;
        }

        public void CreateTower(Vector2 worldPosition)
        {
            if (State != PrototypeLevelState.Running)
            {
                return;
            }

            var towerObject = new GameObject($"CatTower_{towers.Count + 1:00}");
            towerObject.transform.SetParent(runtimeRoot, false);
            towerObject.transform.position = worldPosition;

            var tower = towerObject.AddComponent<BasicTower>();
            tower.Initialize(this, config);
            towers.Add(tower);
        }

        public void SpawnEnemy()
        {
            if (State != PrototypeLevelState.Running)
            {
                return;
            }

            var enemyObject = new GameObject($"BasicEnemy_{SpawnedEnemies + 1:00}");
            enemyObject.transform.SetParent(runtimeRoot, false);

            var enemy = enemyObject.AddComponent<BasicEnemy>();
            enemy.Initialize(this, config.PathPoints, config.EnemyHealth, config.EnemySpeed, config.EnemyBaseDamage);

            activeEnemies.Add(enemy);
            SpawnedEnemies++;
        }

        public void HandleEnemyDefeated(BasicEnemy enemy)
        {
            if (activeEnemies.Remove(enemy))
            {
                DefeatedEnemies++;
            }

            Destroy(enemy.gameObject);
            EvaluateResult();
        }

        public void HandleEnemyReachedBase(BasicEnemy enemy, int damage)
        {
            if (activeEnemies.Remove(enemy))
            {
                EscapedEnemies++;
            }

            Lives = Mathf.Max(0, Lives - Mathf.Max(1, damage));
            Destroy(enemy.gameObject);
            EvaluateResult();
        }

        public void HandleWaveCompleted()
        {
            waveCompleted = true;
            EvaluateResult();
        }

        private void Start()
        {
            if (!IsConfigured)
            {
                Debug.LogError("PrototypeLevelController is not configured.");
                enabled = false;
                return;
            }

            StartLevel();
        }

        private void StartLevel()
        {
            ClearRuntimeObjects();
            BuildMapView();

            Lives = config.BaseLives;
            DefeatedEnemies = 0;
            EscapedEnemies = 0;
            SpawnedEnemies = 0;
            waveCompleted = false;
            State = PrototypeLevelState.Running;

            towerGrid.Initialize(this, config);
            waveSpawner.Initialize(this, config);
            hud.Initialize(this);
            waveSpawner.Begin();
        }

        private void EvaluateResult()
        {
            if (State != PrototypeLevelState.Running)
            {
                return;
            }

            if (Lives <= 0)
            {
                State = PrototypeLevelState.Lost;
                return;
            }

            if (waveCompleted && activeEnemies.Count == 0 && SpawnedEnemies >= config.EnemyCount)
            {
                State = PrototypeLevelState.Won;
            }
        }

        private void BuildMapView()
        {
            var mapRoot = new GameObject("MapView");
            mapRoot.transform.SetParent(runtimeRoot, false);

            CreatePathLine(mapRoot.transform);
            CreateMarker("Spawn", config.PathPoints[0], new Color(0.3f, 0.85f, 0.45f), mapRoot.transform);
            CreateMarker("Base", config.PathPoints[^1], new Color(0.95f, 0.65f, 0.2f), mapRoot.transform);
        }

        private void CreatePathLine(Transform parent)
        {
            var pathObject = new GameObject("EnemyPath");
            pathObject.transform.SetParent(parent, false);

            var line = pathObject.AddComponent<LineRenderer>();
            line.positionCount = config.PathPoints.Length;
            line.useWorldSpace = true;
            line.startWidth = 0.18f;
            line.endWidth = 0.18f;
            line.sortingOrder = 5;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.startColor = new Color(0.78f, 0.74f, 0.52f);
            line.endColor = new Color(0.78f, 0.74f, 0.52f);

            for (var index = 0; index < config.PathPoints.Length; index++)
            {
                line.SetPosition(index, config.PathPoints[index]);
            }
        }

        private static void CreateMarker(string markerName, Vector2 position, Color color, Transform parent)
        {
            var markerObject = new GameObject(markerName);
            markerObject.transform.SetParent(parent, false);
            markerObject.transform.position = position;
            markerObject.transform.localScale = new Vector3(0.58f, 0.58f, 1f);

            var renderer = markerObject.AddComponent<SpriteRenderer>();
            renderer.sprite = PrototypeSpriteFactory.SquareSprite;
            renderer.color = color;
            renderer.sortingOrder = 8;
        }

        private void ClearRuntimeObjects()
        {
            activeEnemies.Clear();
            towers.Clear();

            for (var index = runtimeRoot.childCount - 1; index >= 0; index--)
            {
                Destroy(runtimeRoot.GetChild(index).gameObject);
            }
        }
    }
}
