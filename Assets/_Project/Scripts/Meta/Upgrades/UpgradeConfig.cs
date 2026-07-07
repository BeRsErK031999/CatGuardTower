using UnityEngine;

namespace CatGuard.Meta.Upgrades
{
    [CreateAssetMenu(fileName = "UpgradeConfig", menuName = "Cat Guard/Upgrade Config")]
    public sealed class UpgradeConfig : ScriptableObject
    {
        [SerializeField] private string upgradeId = "upgrade";
        [SerializeField] private string displayName = "Upgrade";
        [SerializeField] private UpgradeEffectType effectType;
        [Min(1)]
        [SerializeField] private int maxLevel = 3;
        [Min(0)]
        [SerializeField] private int baseCost = 25;
        [Min(0)]
        [SerializeField] private int costIncrease = 20;
        [SerializeField] private float effectPerLevel = 0.1f;

        public string UpgradeId => string.IsNullOrWhiteSpace(upgradeId) ? name : upgradeId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? UpgradeId : displayName;
        public UpgradeEffectType EffectType => effectType;
        public int MaxLevel => Mathf.Max(1, maxLevel);
        public float EffectPerLevel => effectPerLevel;

        public int GetCostForLevel(int nextLevel)
        {
            if (nextLevel <= 0 || nextLevel > MaxLevel)
            {
                return 0;
            }

            return Mathf.Max(0, baseCost + (costIncrease * (nextLevel - 1)));
        }

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(UpgradeId)
                && MaxLevel > 0
                && baseCost >= 0
                && costIncrease >= 0;
        }

        public void Configure(
            string id,
            string title,
            UpgradeEffectType effect,
            int levels,
            int firstCost,
            int costStep,
            float valuePerLevel)
        {
            upgradeId = id;
            displayName = title;
            effectType = effect;
            maxLevel = levels;
            baseCost = firstCost;
            costIncrease = costStep;
            effectPerLevel = valuePerLevel;
        }
    }
}
