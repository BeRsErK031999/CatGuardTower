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
        [Min(0.1f)]
        [SerializeField] private float visualScale = 0.42f;
        [SerializeField] private Color visualColor = new(1f, 0.3f, 0.22f);

        public string EnemyId => string.IsNullOrWhiteSpace(enemyId) ? name : enemyId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? EnemyId : displayName;
        public float Health => Mathf.Max(1f, health);
        public float Speed => Mathf.Max(0.1f, speed);
        public int BaseDamage => Mathf.Max(1, baseDamage);
        public float VisualScale => Mathf.Max(0.1f, visualScale);
        public Color VisualColor => visualColor;

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(EnemyId)
                && Health > 0f
                && Speed > 0f
                && BaseDamage > 0
                && VisualScale > 0f;
        }

        public void Configure(
            string id,
            string title,
            float maxHealth,
            float moveSpeed,
            int damageToBase,
            float scale,
            Color color)
        {
            enemyId = id;
            displayName = title;
            health = maxHealth;
            speed = moveSpeed;
            baseDamage = damageToBase;
            visualScale = scale;
            visualColor = color;
        }
    }
}
