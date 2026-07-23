using System.Collections.Generic;
using CatGuard.Core.Quality;
using UnityEngine;

namespace CatGuard.Gameplay.Enemies
{
    public sealed class EnemyRuntimePool : MonoBehaviour
    {
        private readonly Stack<BasicEnemy> available = new();
        private readonly HashSet<BasicEnemy> leased = new();
        private readonly List<BasicEnemy> releaseBuffer = new();
        private Transform poolRoot;
        private int capacity = 96;

        public int CreatedCount { get; private set; }
        public int ReusedCount { get; private set; }
        public int PeakActiveCount { get; private set; }
        public int ActiveCount => leased.Count;
        public int AvailableCount => available.Count;
        public int Capacity => capacity;

        public void Initialize(Transform runtimeRoot)
        {
            ReleaseAll();
            var budget = ExpansionQualityBudgetConfig.LoadDefault();
            capacity = budget == null ? 96 : budget.MaximumPooledEnemies;

            if (poolRoot == null)
            {
                var rootObject = new GameObject("EnemyPool");
                poolRoot = rootObject.transform;
            }

            poolRoot.SetParent(runtimeRoot, false);
            CreatedCount = 0;
            ReusedCount = 0;
            PeakActiveCount = 0;
            available.Clear();
            leased.Clear();
        }

        public BasicEnemy Acquire(Transform parent, string displayName)
        {
            BasicEnemy enemy = null;
            while (available.Count > 0 && enemy == null)
            {
                enemy = available.Pop();
            }

            if (enemy != null)
            {
                ReusedCount++;
            }
            else if (CreatedCount < capacity)
            {
                var enemyObject = new GameObject("PooledEnemy");
                enemy = enemyObject.AddComponent<BasicEnemy>();
                CreatedCount++;
            }

            if (enemy == null)
            {
                Debug.LogError($"Enemy pool capacity {capacity} was exhausted.");
                return null;
            }

            enemy.gameObject.name = displayName;
            enemy.transform.SetParent(parent, false);
            enemy.gameObject.SetActive(true);
            leased.Add(enemy);
            PeakActiveCount = Mathf.Max(PeakActiveCount, leased.Count);
            return enemy;
        }

        public void Release(BasicEnemy enemy)
        {
            if (enemy == null || !leased.Remove(enemy))
            {
                return;
            }

            enemy.PrepareForPool();
            enemy.gameObject.name = "PooledEnemy";
            enemy.transform.SetParent(poolRoot, false);
            enemy.gameObject.SetActive(false);
            available.Push(enemy);
        }

        public void ReleaseAll()
        {
            releaseBuffer.Clear();
            foreach (var enemy in leased)
            {
                if (enemy != null)
                {
                    releaseBuffer.Add(enemy);
                }
            }

            foreach (var enemy in releaseBuffer)
            {
                Release(enemy);
            }

            releaseBuffer.Clear();
        }
    }
}
