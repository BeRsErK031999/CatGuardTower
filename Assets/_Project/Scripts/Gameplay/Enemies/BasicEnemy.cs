using CatGuard.Gameplay.Battlefield;
using CatGuard.Gameplay.Levels;
using CatGuard.Gameplay.Presentation;
using CatGuard.Utils;
using UnityEngine;

namespace CatGuard.Gameplay.Enemies
{
    public sealed class BasicEnemy : MonoBehaviour
    {
        private PrototypeLevelController levelController;
        private EnemyConfig config;
        private PathRouteDefinition route;
        private UnitAnimationPresenter animationPresenter;
        private float maxHealth;
        private float currentHealth;
        private float baseSpeed;
        private float speed;
        private int baseDamage;
        private int nextPathIndex;
        private bool completed;
        private UnitStatusModifier statusModifier;

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
        public UnitAnimationPresenter AnimationPresenter => animationPresenter;
        public UnitAnimationState PresentationState => animationPresenter == null ? UnitAnimationState.Idle : animationPresenter.CurrentState;
        public UnitStatusModifier PresentationStatus => statusModifier;
        public float ActualMoveSpeed => speed;

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
            baseSpeed = config.Speed;
            ConfigureSpeedMultiplier(speedMultiplier);
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
                return;
            }

            animationPresenter?.PlayHit();
        }

        public void ConfigureSpeedMultiplier(float speedMultiplier)
        {
            var multiplier = Mathf.Max(0.1f, speedMultiplier);
            speed = Mathf.Max(0.1f, baseSpeed) * multiplier;
            statusModifier &= ~(UnitStatusModifier.Slowed | UnitStatusModifier.Hastened);
            if (multiplier < 0.95f)
            {
                statusModifier |= UnitStatusModifier.Slowed;
            }
            else if (multiplier > 1.05f)
            {
                statusModifier |= UnitStatusModifier.Hastened;
            }

            animationPresenter?.SetStatusModifier(statusModifier);
        }

        public void SetControlStatus(UnitStatusModifier modifier)
        {
            statusModifier = modifier;
            animationPresenter?.SetStatusModifier(statusModifier);
        }

        public float BeginDeathPresentation()
        {
            return animationPresenter == null ? 0f : animationPresenter.PlayDeath();
        }

        public float BeginGoalAttackPresentation()
        {
            return animationPresenter == null ? 0f : animationPresenter.PlayGoalAttack();
        }

        private void Update()
        {
            if (!IsAlive)
            {
                return;
            }

            if (levelController.State != PrototypeLevelState.Running
                || (statusModifier & (UnitStatusModifier.Stunned | UnitStatusModifier.Frozen)) != 0)
            {
                animationPresenter?.SetMovement(Vector2.zero, 0f, false);
                return;
            }

            if (route == null || route.Points == null || nextPathIndex >= route.Points.Length)
            {
                ReachBase();
                return;
            }

            var target = (Vector3)route.Points[nextPathIndex];
            var previousPosition = transform.position;
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
            NormalizedProgress = route.GetNormalizedProgress(nextPathIndex, transform.position);
            var movement = transform.position - previousPosition;
            animationPresenter?.SetMovement(movement, speed, movement.sqrMagnitude > 0.0000001f);

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
            var sprite = config.VisualSprite != null
                ? config.VisualSprite
                : PrototypeSpriteFactory.CircleSprite;
            var spriteSize = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
            var visualScale = config.VisualScale / Mathf.Max(0.01f, spriteSize);
            transform.localScale = new Vector3(visualScale, visualScale, 1f);

            animationPresenter = GetComponent<UnitAnimationPresenter>();
            if (animationPresenter == null)
            {
                animationPresenter = gameObject.AddComponent<UnitAnimationPresenter>();
            }

            var fallbackColor = config.VisualSprite != null ? Color.white : config.VisualColor;
            animationPresenter.Initialize(config.AnimationProfile, sprite, fallbackColor, speed, SpawnOrder);
            animationPresenter.SetStatusModifier(statusModifier);
        }

        private void UpdateVisual()
        {
            animationPresenter?.SetHealthPercent(HealthPercent);
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
