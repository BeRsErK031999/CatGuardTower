using System;
using CatGuard.Gameplay.Enemies;
using UnityEngine;

namespace CatGuard.Gameplay.Waves
{
    [CreateAssetMenu(fileName = "WaveConfig", menuName = "Cat Guard/Wave Config")]
    public sealed class WaveConfig : ScriptableObject
    {
        [SerializeField] private WaveEnemyGroup[] groups = Array.Empty<WaveEnemyGroup>();

        public WaveEnemyGroup[] Groups => groups;

        public int TotalEnemyCount
        {
            get
            {
                var count = 0;
                foreach (var group in groups)
                {
                    if (group?.EnemyConfig != null)
                    {
                        count += group.Count;
                    }
                }

                return count;
            }
        }

        public bool IsValid()
        {
            if (groups == null || groups.Length == 0)
            {
                return false;
            }

            foreach (var group in groups)
            {
                if (group == null || !group.IsValid())
                {
                    return false;
                }
            }

            return TotalEnemyCount > 0;
        }

        public void Configure(WaveEnemyGroup[] waveGroups)
        {
            groups = waveGroups ?? Array.Empty<WaveEnemyGroup>();
        }
    }

    [Serializable]
    public sealed class WaveEnemyGroup
    {
        [SerializeField] private EnemyConfig enemyConfig;
        [Min(1)]
        [SerializeField] private int count = 1;
        [Min(0.1f)]
        [SerializeField] private float spawnInterval = 0.8f;
        [Min(0f)]
        [SerializeField] private float delayBeforeGroup;
        [Min(0.1f)]
        [SerializeField] private float healthMultiplier = 1f;
        [Min(0.1f)]
        [SerializeField] private float speedMultiplier = 1f;

        public WaveEnemyGroup(
            EnemyConfig config,
            int enemyCount,
            float interval,
            float delay = 0f,
            float healthScale = 1f,
            float speedScale = 1f)
        {
            enemyConfig = config;
            count = enemyCount;
            spawnInterval = interval;
            delayBeforeGroup = delay;
            healthMultiplier = healthScale;
            speedMultiplier = speedScale;
        }

        public EnemyConfig EnemyConfig => enemyConfig;
        public int Count => Mathf.Max(1, count);
        public float SpawnInterval => Mathf.Max(0.1f, spawnInterval);
        public float DelayBeforeGroup => Mathf.Max(0f, delayBeforeGroup);
        public float HealthMultiplier => Mathf.Max(0.1f, healthMultiplier);
        public float SpeedMultiplier => Mathf.Max(0.1f, speedMultiplier);

        public bool IsValid()
        {
            return enemyConfig != null
                && enemyConfig.IsValid()
                && Count > 0
                && SpawnInterval > 0f
                && healthMultiplier > 0f
                && speedMultiplier > 0f;
        }
    }
}
