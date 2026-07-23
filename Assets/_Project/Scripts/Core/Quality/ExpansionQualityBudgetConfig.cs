using UnityEngine;

namespace CatGuard.Core.Quality
{
    [CreateAssetMenu(
        fileName = "ExpansionQualityBudget",
        menuName = "Cat Guard/Quality/Expansion Quality Budget")]
    public sealed class ExpansionQualityBudgetConfig : ScriptableObject
    {
        private const string ResourcePath = "Quality/E14QualityBudget";

        [Header("Frame Budgets")]
        [Min(1)]
        [SerializeField] private int targetMidRangeFps = 60;
        [Min(1)]
        [SerializeField] private int lowEndFloorFps = 24;
        [Min(1f)]
        [SerializeField] private float lowEndP95FrameTimeMs = 70f;
        [SerializeField] private string worstCaseLevelId = "level_12";

        [Header("Runtime Pools")]
        [Min(1)]
        [SerializeField] private int maximumPooledEnemies = 96;
        [Min(1)]
        [SerializeField] private int maximumPooledVfx = 96;

        [Header("Content Budgets")]
        [Min(256)]
        [SerializeField] private int maximumAnimationTextureSize = 2048;
        [Min(1)]
        [SerializeField] private int maximumMapDecorations = 128;

        [Header("Economy Guardrails")]
        [Min(1)]
        [SerializeField] private int maximumQuestFishReward = 35;
        [Min(1)]
        [SerializeField] private int maximumAchievementFishReward = 120;

        [Header("Accessibility")]
        [Range(80, 100)]
        [SerializeField] private int minimumTextScalePercent = 90;
        [Range(100, 140)]
        [SerializeField] private int maximumTextScalePercent = 120;
        [Min(1)]
        [SerializeField] private int textScaleStepPercent = 10;
        [SerializeField] private int[] supportedBattleSpeeds = { 1, 2 };

        public int TargetMidRangeFps => Mathf.Max(1, targetMidRangeFps);
        public int LowEndFloorFps => Mathf.Max(1, lowEndFloorFps);
        public float LowEndP95FrameTimeMs => Mathf.Max(1f, lowEndP95FrameTimeMs);
        public string WorstCaseLevelId => string.IsNullOrWhiteSpace(worstCaseLevelId) ? "level_12" : worstCaseLevelId;
        public int MaximumPooledEnemies => Mathf.Max(1, maximumPooledEnemies);
        public int MaximumPooledVfx => Mathf.Max(1, maximumPooledVfx);
        public int MaximumAnimationTextureSize => Mathf.Max(256, maximumAnimationTextureSize);
        public int MaximumMapDecorations => Mathf.Max(1, maximumMapDecorations);
        public int MaximumQuestFishReward => Mathf.Max(1, maximumQuestFishReward);
        public int MaximumAchievementFishReward => Mathf.Max(1, maximumAchievementFishReward);
        public int MinimumTextScalePercent => Mathf.Clamp(minimumTextScalePercent, 80, 100);
        public int MaximumTextScalePercent => Mathf.Clamp(maximumTextScalePercent, 100, 140);
        public int TextScaleStepPercent => Mathf.Max(1, textScaleStepPercent);
        public int[] SupportedBattleSpeeds => supportedBattleSpeeds ?? System.Array.Empty<int>();

        public static ExpansionQualityBudgetConfig LoadDefault()
        {
            return Resources.Load<ExpansionQualityBudgetConfig>(ResourcePath);
        }

        public bool IsValid()
        {
            if (TargetMidRangeFps < 30
                || LowEndFloorFps < 20
                || LowEndFloorFps > TargetMidRangeFps
                || LowEndP95FrameTimeMs < 1000f / LowEndFloorFps
                || string.IsNullOrWhiteSpace(WorstCaseLevelId)
                || MaximumPooledEnemies < 64
                || MaximumPooledVfx < 48
                || MaximumAnimationTextureSize > 2048
                || MaximumMapDecorations > 256
                || MaximumQuestFishReward <= 0
                || MaximumAchievementFishReward < MaximumQuestFishReward
                || MinimumTextScalePercent >= MaximumTextScalePercent
                || TextScaleStepPercent <= 0
                || SupportedBattleSpeeds.Length != 2
                || SupportedBattleSpeeds[0] != 1
                || SupportedBattleSpeeds[1] != 2)
            {
                return false;
            }

            return (MaximumTextScalePercent - MinimumTextScalePercent) % TextScaleStepPercent == 0;
        }

        public void Configure(
            int midRangeFps,
            int lowEndFps,
            float p95FrameTimeMs,
            string stressLevelId,
            int enemyPoolCapacity,
            int vfxPoolCapacity,
            int textureSize,
            int decorationLimit,
            int questRewardCap,
            int achievementRewardCap,
            int minimumTextPercent,
            int maximumTextPercent,
            int textStepPercent,
            int[] battleSpeeds)
        {
            targetMidRangeFps = Mathf.Max(1, midRangeFps);
            lowEndFloorFps = Mathf.Max(1, lowEndFps);
            lowEndP95FrameTimeMs = Mathf.Max(1f, p95FrameTimeMs);
            worstCaseLevelId = stressLevelId ?? string.Empty;
            maximumPooledEnemies = Mathf.Max(1, enemyPoolCapacity);
            maximumPooledVfx = Mathf.Max(1, vfxPoolCapacity);
            maximumAnimationTextureSize = Mathf.Max(256, textureSize);
            maximumMapDecorations = Mathf.Max(1, decorationLimit);
            maximumQuestFishReward = Mathf.Max(1, questRewardCap);
            maximumAchievementFishReward = Mathf.Max(1, achievementRewardCap);
            minimumTextScalePercent = Mathf.Clamp(minimumTextPercent, 80, 100);
            maximumTextScalePercent = Mathf.Clamp(maximumTextPercent, 100, 140);
            textScaleStepPercent = Mathf.Max(1, textStepPercent);
            supportedBattleSpeeds = battleSpeeds ?? System.Array.Empty<int>();
        }
    }
}
