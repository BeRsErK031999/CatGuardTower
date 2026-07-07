using CatGuard.Gameplay.Levels;
using CatGuard.Utils;
using UnityEngine;

namespace CatGuard.Gameplay.Enemies
{
    public sealed class BasicEnemy : MonoBehaviour
    {
        private PrototypeLevelController levelController;
        private EnemyConfig config;
        private Vector2[] pathPoints;
        private SpriteRenderer spriteRenderer;
        private float maxHealth;
        private float currentHealth;
        private float speed;
        private int baseDamage;
        private int nextPathIndex;
        private bool completed;

        public bool IsAlive => !completed && currentHealth > 0f;
        public float HealthPercent => maxHealth <= 0f ? 0f : Mathf.Clamp01(currentHealth / maxHealth);

        public void Initialize(
            PrototypeLevelController owner,
            Vector2[] path,
            EnemyConfig enemyConfig)
        {
            levelController = owner;
            config = enemyConfig;
            pathPoints = path;
            maxHealth = config.Health;
            currentHealth = maxHealth;
            speed = config.Speed;
            baseDamage = config.BaseDamage;
            nextPathIndex = 1;
            completed = false;

            transform.position = pathPoints[0];
            EnsureVisual();
            UpdateVisual();
        }

        public void ApplyDamage(float amount)
        {
            if (!IsAlive)
            {
                return;
            }

            currentHealth = Mathf.Max(0f, currentHealth - Mathf.Max(0f, amount));
            UpdateVisual();

            if (currentHealth <= 0f)
            {
                completed = true;
                levelController.HandleEnemyDefeated(this);
            }
        }

        private void Update()
        {
            if (!IsAlive || levelController.State != PrototypeLevelState.Running)
            {
                return;
            }

            if (pathPoints == null || nextPathIndex >= pathPoints.Length)
            {
                ReachBase();
                return;
            }

            var target = (Vector3)pathPoints[nextPathIndex];
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

            if (Vector3.Distance(transform.position, target) <= 0.02f)
            {
                nextPathIndex++;
                if (nextPathIndex >= pathPoints.Length)
                {
                    ReachBase();
                }
            }
        }

        private void EnsureVisual()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

            spriteRenderer.sprite = PrototypeSpriteFactory.CircleSprite;
            spriteRenderer.sortingOrder = 20;
            transform.localScale = new Vector3(config.VisualScale, config.VisualScale, 1f);
        }

        private void UpdateVisual()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            spriteRenderer.color = Color.Lerp(new Color(0.35f, 0.08f, 0.08f), config.VisualColor, HealthPercent);
        }

        private void ReachBase()
        {
            if (completed)
            {
                return;
            }

            completed = true;
            levelController.HandleEnemyReachedBase(this, baseDamage);
        }
    }
}
