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
        private float waveSpeedMultiplier = 1f;
        private float combatSlowMultiplier = 1f;
        private float combatSlowUntil;
        private float burnDamagePerSecond;
        private float burnUntil;
        private UnitStatusModifier controlStatus;
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
        public bool IsHeavyTarget => maxHealth >= 8f || baseDamage >= 3;

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
            waveSpeedMultiplier = Mathf.Max(0.1f, speedMultiplier);
            RefreshMovementStatus();
        }

        public void SetControlStatus(UnitStatusModifier modifier)
        {
            controlStatus = modifier & ~(UnitStatusModifier.Slowed | UnitStatusModifier.Hastened);
            RefreshMovementStatus();
        }

        public void ApplyTemporarySlow(float slowPercent, float durationSeconds)
        {
            if (!IsAlive || slowPercent <= 0f || durationSeconds <= 0f)
            {
                return;
            }

            combatSlowMultiplier = Mathf.Min(combatSlowMultiplier, 1f - Mathf.Clamp(slowPercent, 0f, 0.85f));
            combatSlowUntil = Mathf.Max(combatSlowUntil, Time.time + durationSeconds);
            RefreshMovementStatus();
        }

        public void ApplyBurn(float damagePerSecond, float durationSeconds)
        {
            if (!IsAlive || damagePerSecond <= 0f || durationSeconds <= 0f)
            {
                return;
            }

            burnDamagePerSecond = Mathf.Max(burnDamagePerSecond, damagePerSecond);
            burnUntil = Mathf.Max(burnUntil, Time.time + durationSeconds);
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

            UpdateCombatEffects();
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

        private void UpdateCombatEffects()
        {
            if (combatSlowUntil > 0f && Time.time >= combatSlowUntil)
            {
                combatSlowUntil = 0f;
                combatSlowMultiplier = 1f;
                RefreshMovementStatus();
            }

            if (burnUntil <= 0f)
            {
                return;
            }

            if (Time.time >= burnUntil)
            {
                burnUntil = 0f;
                burnDamagePerSecond = 0f;
                return;
            }

            ApplyDamage(burnDamagePerSecond * Time.deltaTime);
        }

        private void RefreshMovementStatus()
        {
            var effectiveSlow = combatSlowUntil > Time.time ? combatSlowMultiplier : 1f;
            speed = Mathf.Max(0.1f, baseSpeed) * waveSpeedMultiplier * effectiveSlow;
            statusModifier = controlStatus;
            var effectiveMultiplier = waveSpeedMultiplier * effectiveSlow;
            if (effectiveMultiplier < 0.95f)
            {
                statusModifier |= UnitStatusModifier.Slowed;
            }
            else if (effectiveMultiplier > 1.05f)
            {
                statusModifier |= UnitStatusModifier.Hastened;
            }

            animationPresenter?.SetStatusModifier(statusModifier);
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
