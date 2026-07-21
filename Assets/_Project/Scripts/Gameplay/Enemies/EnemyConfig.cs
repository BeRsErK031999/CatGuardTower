using CatGuard.Gameplay.Presentation;
using UnityEngine;

namespace CatGuard.Gameplay.Enemies
{
    [CreateAssetMenu(fileName = "EnemyConfig", menuName = "Cat Guard/Enemy Config")]
    public sealed class EnemyConfig : ScriptableObject
    {
        [SerializeField] private string enemyId = "enemy";
        [SerializeField] private string displayName = "Enemy";
        [Min(1f)]
        [SerializeField] private float health = 3f;
        [Min(0.1f)]
        [SerializeField] private float speed = 0.9f;
        [Min(1)]
        [SerializeField] private int baseDamage = 1;
        [Min(0)]
        [SerializeField] private int battleFishReward = 3;
        [Min(0.1f)]
        [SerializeField] private float visualScale = 0.42f;
        [SerializeField] private Color visualColor = new(1f, 0.3f, 0.22f);
        [SerializeField] private Sprite visualSprite;
        [SerializeField] private UnitAnimationConfig animationProfile;

        [Header("Guardian Ultimate Resistance")]
        [Range(0f, 2f)]
        [SerializeField] private float ultimateDamageMultiplier = 1f;
        [Range(0f, 1f)]
        [SerializeField] private float ultimateSlowDurationMultiplier = 1f;
        [Range(0f, 1f)]
        [SerializeField] private float ultimateStunDurationMultiplier = 1f;
        [SerializeField] private bool ultimateControlImmune;

        public string EnemyId => string.IsNullOrWhiteSpace(enemyId) ? name : enemyId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? EnemyId : displayName;
        public float Health => Mathf.Max(1f, health);
        public float Speed => Mathf.Max(0.1f, speed);
        public int BaseDamage => Mathf.Max(1, baseDamage);
        public int BattleFishReward => Mathf.Max(0, battleFishReward);
        public float VisualScale => Mathf.Max(0.1f, visualScale);
        public Color VisualColor => visualColor;
        public Sprite VisualSprite => visualSprite;
        public UnitAnimationConfig AnimationProfile => animationProfile;
        public float UltimateDamageMultiplier => Mathf.Clamp(ultimateDamageMultiplier, 0f, 2f);
        public float UltimateSlowDurationMultiplier => ultimateControlImmune ? 0f : Mathf.Clamp01(ultimateSlowDurationMultiplier);
        public float UltimateStunDurationMultiplier => ultimateControlImmune ? 0f : Mathf.Clamp01(ultimateStunDurationMultiplier);
        public bool UltimateControlImmune => ultimateControlImmune;

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(EnemyId)
                && Health > 0f
                && Speed > 0f
                && BaseDamage > 0
                && battleFishReward >= 0
                && VisualScale > 0f;
        }

        public void Configure(
            string id,
            string title,
            float maxHealth,
            float moveSpeed,
            int damageToBase,
            float scale,
            Color color,
            int fishReward = 3,
            Sprite sprite = null)
        {
            enemyId = id;
            displayName = title;
            health = maxHealth;
            speed = moveSpeed;
            baseDamage = damageToBase;
            battleFishReward = Mathf.Max(0, fishReward);
            visualScale = scale;
            visualColor = color;
            visualSprite = sprite;
        }

        public void ConfigureAnimationProfile(UnitAnimationConfig profile)
        {
            animationProfile = profile;
        }

        public void ConfigureUltimateResistance(
            float damageMultiplier,
            float slowDurationMultiplier,
            float stunDurationMultiplier,
            bool controlImmune)
        {
            ultimateDamageMultiplier = Mathf.Clamp(damageMultiplier, 0f, 2f);
            ultimateSlowDurationMultiplier = Mathf.Clamp01(slowDurationMultiplier);
            ultimateStunDurationMultiplier = Mathf.Clamp01(stunDurationMultiplier);
            ultimateControlImmune = controlImmune;
        }
    }
}
