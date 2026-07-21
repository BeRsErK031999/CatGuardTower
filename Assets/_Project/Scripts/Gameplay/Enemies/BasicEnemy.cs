using CatGuard.Gameplay.Battlefield;
using CatGuard.Gameplay.Levels;
using CatGuard.Utils;
using UnityEngine;

namespace CatGuard.Gameplay.Enemies
{
    public sealed class BasicEnemy : MonoBehaviour
    {
        private PrototypeLevelController levelController;
        private EnemyConfig config;
        private PathRouteDefinition route;
        private SpriteRenderer spriteRenderer;
        private float maxHealth;
        private float currentHealth;
        private float speed;
        private int baseDamage;
        private int nextPathIndex;
        private bool completed;

        public bool IsAlive => !completed && currentHealth > 0f;
        public float HealthPercent => maxHealth <= 0f ? 0f : Mathf.Clamp01(currentHealth / maxHealth);
        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public float NormalizedProgress { get; private set; }
        public int BattleFishReward => config == null ? 0 : config.BattleFishReward;
        public PathRouteDefinition Route => route;
        public string RouteId => route?.RouteId ?? string.Empty;
        public int SpawnOrder { get; private set; }
        public string EnemyId => config?.EnemyId ?? string.Empty;

        public void Initialize(
            PrototypeLevelController owner,
            PathRouteDefinition assignedRoute,
            EnemyConfig enemyConfig,
            int spawnOrder,
            float healthMultiplier = 1f,
            float speedMultiplier = 1f)
        {
            levelController = owner;
            config = enemyConfig;
            route = assignedRoute;
            SpawnOrder = spawnOrder;
            maxHealth = config.Health * Mathf.Max(0.1f, healthMultiplier);
            currentHealth = maxHealth;
            speed = config.Speed * Mathf.Max(0.1f, speedMultiplier);
            baseDamage = config.BaseDamage;
            nextPathIndex = 1;
            completed = false;
            NormalizedProgress = 0f;

            transform.position = route.Points[0];
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

            if (route == null || route.Points == null || nextPathIndex >= route.Points.Length)
            {
                ReachBase();
                return;
            }

            var target = (Vector3)route.Points[nextPathIndex];
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
            NormalizedProgress = route.GetNormalizedProgress(nextPathIndex, transform.position);

            if (Vector3.Distance(transform.position, target) <= 0.02f)
            {
                nextPathIndex++;
                if (nextPathIndex >= route.Points.Length)
                {
                    NormalizedProgress = 1f;
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

            var sprite = config.VisualSprite != null
                ? config.VisualSprite
                : PrototypeSpriteFactory.CircleSprite;
            spriteRenderer.sprite = sprite;
            spriteRenderer.sortingOrder = 20;

            var spriteSize = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
            var visualScale = config.VisualScale / Mathf.Max(0.01f, spriteSize);
            transform.localScale = new Vector3(visualScale, visualScale, 1f);
        }

        private void UpdateVisual()
        {
            if (spriteRenderer == null)
            {
                return;
            }

            var healthyColor = config.VisualSprite != null ? Color.white : config.VisualColor;
            var damagedColor = config.VisualSprite != null
                ? new Color(0.58f, 0.22f, 0.22f)
                : new Color(0.35f, 0.08f, 0.08f);
            spriteRenderer.color = Color.Lerp(damagedColor, healthyColor, HealthPercent);
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
