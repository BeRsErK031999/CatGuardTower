using UnityEngine;

namespace CatGuard.Gameplay.Towers
{
    [CreateAssetMenu(fileName = "TowerConfig", menuName = "Cat Guard/Tower Config")]
    public sealed class TowerConfig : ScriptableObject
    {
        [SerializeField] private string towerId = "tower";
        [SerializeField] private string displayName = "Tower";
        [Min(0.1f)]
        [SerializeField] private float range = 2.5f;
        [Min(0.1f)]
        [SerializeField] private float damage = 1f;
        [Min(0.05f)]
        [SerializeField] private float fireInterval = 0.35f;
        [Min(0.1f)]
        [SerializeField] private float visualScale = 0.62f;
        [SerializeField] private Color visualColor = new(0.24f, 0.78f, 0.96f);

        [Header("Economy")]
        [Min(1)]
        [SerializeField] private int buildCost = 45;

        public string TowerId => string.IsNullOrWhiteSpace(towerId) ? name : towerId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? TowerId : displayName;
        public float Range => Mathf.Max(0.1f, range);
        public float Damage => Mathf.Max(0.1f, damage);
        public float FireInterval => Mathf.Max(0.05f, fireInterval);
        public float VisualScale => Mathf.Max(0.1f, visualScale);
        public Color VisualColor => visualColor;
        public int BuildCost => Mathf.Max(1, buildCost);

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(TowerId)
                && Range > 0f
                && Damage > 0f
                && FireInterval > 0f
                && VisualScale > 0f
                && BuildCost > 0;
        }

        public void Configure(
            string id,
            string title,
            float attackRange,
            float attackDamage,
            float attackInterval,
            float scale,
            Color color,
            int cost = 45)
        {
            towerId = id;
            displayName = title;
            range = attackRange;
            damage = attackDamage;
            fireInterval = attackInterval;
            visualScale = scale;
            visualColor = color;
            buildCost = Mathf.Max(1, cost);
        }
    }
}
