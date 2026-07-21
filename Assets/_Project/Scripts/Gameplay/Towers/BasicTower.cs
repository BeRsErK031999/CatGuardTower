using CatGuard.Core.Audio;
using CatGuard.Gameplay.Enemies;
using CatGuard.Gameplay.Levels;
using CatGuard.Gameplay.Towers.Upgrades;
using CatGuard.Meta.Progression;
using CatGuard.Utils;
using CatGuard.VFX;
using UnityEngine;

namespace CatGuard.Gameplay.Towers
{
    public sealed class BasicTower : MonoBehaviour
    {
        private PrototypeLevelController levelController;
        private TowerConfig config;
        private TowerRuntimeStats runtimeStats;
        private TowerTargetPriority targetPriority;
        private string selectedBranchId = string.Empty;
        private int currentTier;
        private int investedUpgradeCost;
        private float fireTimer;
        private float permanentDamageMultiplier = 1f;
        private float permanentRangeMultiplier = 1f;
        private float ultimateAttackSpeedMultiplier = 1f;
        private SpriteRenderer spriteRenderer;
        private Transform rangePreview;
        private Transform upgradeMarker;
        private float baseVisualWorldScale;

        public TowerConfig Config => config;
        public TowerRuntimeStats RuntimeStats => runtimeStats;
        public TowerTargetPriority TargetPriority => targetPriority;
        public string SelectedBranchId => selectedBranchId;
        public int CurrentTier => currentTier;
        public int InvestedUpgradeCost => investedUpgradeCost;
        public bool HasBattleUpgrade => currentTier > 0;
        public float SelectionRadius => config == null ? 0.5f : Mathf.Max(0.5f, config.VisualScale * 0.8f);
        public int SellValue => config == null
            ? 0
            : Mathf.Max(
                1,
                Mathf.RoundToInt(config.BuildCost * (config.BattleUpgradeTree?.BaseSellRate ?? 0.7f))
                + GetUpgradeSellContribution());

        public void Initialize(PrototypeLevelController owner, TowerConfig towerConfig)
        {
            levelController = owner;
            config = towerConfig;
            targetPriority = config.TargetPriority;
            selectedBranchId = string.Empty;
            currentTier = 0;
            investedUpgradeCost = 0;
            permanentRangeMultiplier = ProgressionService.GetTowerRangeMultiplier();
            permanentDamageMultiplier = ProgressionService.GetTowerDamageMultiplier();
            fireTimer = 0f;

            RecalculateRuntimeStats();
            EnsureVisual();
            RefreshUpgradePresentation();
        }

        public TowerUpgradeQuote GetUpgradeQuote(string branchId, int availableBattleFish)
        {
            var tree = config?.BattleUpgradeTree;
            if (tree == null)
            {
                return new TowerUpgradeQuote(TowerUpgradeAvailability.MissingTree, null, null);
            }

            var branch = tree.FindBranch(branchId);
            if (branch == null)
            {
                return new TowerUpgradeQuote(TowerUpgradeAvailability.UnknownBranch, null, null);
            }

            if (!string.IsNullOrWhiteSpace(selectedBranchId)
                && !string.Equals(selectedBranchId, branchId, System.StringComparison.Ordinal))
            {
                return new TowerUpgradeQuote(TowerUpgradeAvailability.BranchLocked, branch, branch.GetTier(1));
            }

            foreach (var prerequisite in branch.PrerequisiteBranchIds)
            {
                if (!string.Equals(prerequisite, selectedBranchId, System.StringComparison.Ordinal))
                {
                    return new TowerUpgradeQuote(TowerUpgradeAvailability.PrerequisiteMissing, branch, branch.GetTier(1));
                }
            }

            if (!string.IsNullOrWhiteSpace(selectedBranchId))
            {
                foreach (var excluded in branch.MutuallyExclusiveBranchIds)
                {
                    if (!string.IsNullOrWhiteSpace(excluded)
                        && !string.Equals(excluded, branchId, System.StringComparison.Ordinal))
                    {
                        var excludedBranch = tree.FindBranch(excluded);
                        if (excludedBranch != null && string.Equals(selectedBranchId, excluded, System.StringComparison.Ordinal))
                        {
                            return new TowerUpgradeQuote(TowerUpgradeAvailability.BranchLocked, branch, branch.GetTier(1));
                        }
                    }
                }
            }

            var nextTier = branch.GetTier(currentTier + 1);
            if (nextTier == null)
            {
                return new TowerUpgradeQuote(TowerUpgradeAvailability.MaximumTier, branch, null);
            }

            return new TowerUpgradeQuote(
                availableBattleFish >= nextTier.Price
                    ? TowerUpgradeAvailability.Available
                    : TowerUpgradeAvailability.InsufficientFunds,
                branch,
                nextTier);
        }

        public bool CommitUpgrade(TowerUpgradeQuote quote)
        {
            if (!quote.CanPurchase
                || quote.Branch == null
                || quote.NextTier == null
                || quote.NextTier.Tier != currentTier + 1)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(selectedBranchId))
            {
                selectedBranchId = quote.Branch.BranchId;
            }

            if (!string.Equals(selectedBranchId, quote.Branch.BranchId, System.StringComparison.Ordinal))
            {
                return false;
            }

            currentTier = quote.NextTier.Tier;
            investedUpgradeCost += quote.NextTier.Price;
            RecalculateRuntimeStats();
            RefreshUpgradePresentation();
            return true;
        }

        public bool ApplyUpgradeForPreview(string branchId, int targetTier)
        {
            while (currentTier < targetTier)
            {
                var quote = GetUpgradeQuote(branchId, int.MaxValue);
                if (!CommitUpgrade(quote))
                {
                    return false;
                }
            }

            return currentTier == targetTier;
        }

        public void SetTargetPriority(TowerTargetPriority priority)
        {
            targetPriority = priority;
        }

        public void SetSelected(bool selected)
        {
            if (rangePreview != null)
            {
                rangePreview.gameObject.SetActive(selected);
            }
        }

        public void SetUltimateAttackSpeedMultiplier(float multiplier)
        {
            ultimateAttackSpeedMultiplier = Mathf.Max(0.1f, multiplier);
        }

        private void Update()
        {
            if (levelController == null || levelController.State != PrototypeLevelState.Running)
            {
                return;
            }

            fireTimer -= Time.deltaTime;
            if (fireTimer > 0f)
            {
                return;
            }

            var target = levelController.FindTargetEnemy(transform.position, runtimeStats.Range, targetPriority);
            if (target == null)
            {
                return;
            }

            var targetPosition = target.transform.position;
            levelController.ApplyTowerAttack(this, target, runtimeStats);
            ProceduralAudioService.Play(ProceduralSoundId.TowerShot);
            SimpleVfxFactory.Spawn(targetPosition, SimpleVfxStyle.TowerShot);
            fireTimer = runtimeStats.FireInterval / ultimateAttackSpeedMultiplier;
        }

        private void RecalculateRuntimeStats()
        {
            var damageMultiplier = permanentDamageMultiplier;
            var rangeMultiplier = permanentRangeMultiplier;
            var attackSpeedMultiplier = 1f;
            var splashBonus = 0f;
            var additionalTargets = 0;
            var pierceTargets = 0;
            var bossMultiplier = 1f;
            var slowPercent = 0f;
            var slowDuration = 0f;
            var burnDps = 0f;
            var burnDuration = 0f;
            var behavior = TowerUpgradeBehavior.None;
            var projectileColor = Color.white;

            var branch = config?.BattleUpgradeTree?.FindBranch(selectedBranchId);
            if (branch != null)
            {
                for (var tier = 1; tier <= currentTier; tier++)
                {
                    var node = branch.GetTier(tier);
                    if (node == null)
                    {
                        continue;
                    }

                    damageMultiplier *= node.DamageMultiplier;
                    rangeMultiplier *= node.RangeMultiplier;
                    attackSpeedMultiplier *= node.AttackSpeedMultiplier;
                    splashBonus += node.SplashRadiusBonus;
                    additionalTargets += node.AdditionalTargets;
                    pierceTargets += node.PierceTargets;
                    bossMultiplier *= node.BossDamageMultiplier;
                    slowPercent = Mathf.Max(slowPercent, node.SlowPercent);
                    slowDuration = Mathf.Max(slowDuration, node.SlowDuration);
                    burnDps += node.BurnDamagePerSecond;
                    burnDuration = Mathf.Max(burnDuration, node.BurnDuration);
                    behavior = node.Behavior;
                    projectileColor = node.ProjectileColor;
                }
            }

            runtimeStats = new TowerRuntimeStats(
                config.Range * rangeMultiplier,
                config.Damage * damageMultiplier,
                config.FireInterval / attackSpeedMultiplier,
                config.SplashRadius + splashBonus,
                additionalTargets,
                pierceTargets,
                bossMultiplier,
                slowPercent,
                slowDuration,
                burnDps,
                burnDuration,
                behavior,
                projectileColor);
            RefreshRangePreviewScale();
        }

        private int GetUpgradeSellContribution()
        {
            var branch = config?.BattleUpgradeTree?.FindBranch(selectedBranchId);
            var result = 0;
            if (branch == null)
            {
                return result;
            }

            for (var tier = 1; tier <= currentTier; tier++)
            {
                result += branch.GetTier(tier)?.SellValueContribution ?? 0;
            }

            return result;
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
                : PrototypeSpriteFactory.DiamondSprite;
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = config.VisualSprite != null ? Color.white : config.VisualColor;
            spriteRenderer.sortingOrder = 15;

            var spriteSize = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
            baseVisualWorldScale = config.VisualScale / Mathf.Max(0.01f, spriteSize);
            transform.localScale = Vector3.one * baseVisualWorldScale;

            var previewObject = new GameObject("SelectedRangePreview");
            previewObject.transform.SetParent(transform, false);
            previewObject.transform.localPosition = Vector3.zero;
            rangePreview = previewObject.transform;
            var previewRenderer = previewObject.AddComponent<SpriteRenderer>();
            previewRenderer.sprite = PrototypeSpriteFactory.CircleSprite;
            previewRenderer.color = new Color(0.2f, 0.86f, 0.72f, 0.1f);
            previewRenderer.sortingOrder = 14;
            previewObject.SetActive(false);
            RefreshRangePreviewScale();
        }

        private void RefreshRangePreviewScale()
        {
            if (rangePreview == null || baseVisualWorldScale <= 0f)
            {
                return;
            }

            var parentScale = Mathf.Max(0.01f, transform.localScale.x);
            var circleDiameter = Mathf.Max(0.01f, PrototypeSpriteFactory.CircleSprite.bounds.size.x);
            var localScale = (runtimeStats.Range * 2f) / (parentScale * circleDiameter);
            rangePreview.localScale = new Vector3(localScale, localScale, 1f);
        }

        private void RefreshUpgradePresentation()
        {
            if (config == null || spriteRenderer == null)
            {
                return;
            }

            if (upgradeMarker != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(upgradeMarker.gameObject);
                }
                else
                {
                    DestroyImmediate(upgradeMarker.gameObject);
                }
                upgradeMarker = null;
            }

            var branch = config.BattleUpgradeTree?.FindBranch(selectedBranchId);
            if (branch == null || currentTier <= 0)
            {
                transform.localScale = Vector3.one * baseVisualWorldScale;
                spriteRenderer.color = config.VisualSprite != null ? Color.white : config.VisualColor;
                RefreshRangePreviewScale();
                return;
            }

            var node = branch.GetTier(currentTier);
            transform.localScale = Vector3.one * (baseVisualWorldScale * node.PresentationScaleMultiplier);
            spriteRenderer.color = Color.Lerp(Color.white, node.PresentationColor, config.VisualSprite != null ? 0.24f : 0.5f);

            var markerObject = new GameObject($"{branch.BranchId}_Tier_{currentTier}");
            markerObject.transform.SetParent(transform, false);
            markerObject.transform.localPosition = new Vector3(0.52f, 0.5f, 0f);
            markerObject.transform.localScale = Vector3.one * 0.28f;
            upgradeMarker = markerObject.transform;
            var markerRenderer = markerObject.AddComponent<SpriteRenderer>();
            markerRenderer.sprite = branch.MarkerShape switch
            {
                TowerUpgradeMarkerShape.Circle => PrototypeSpriteFactory.CircleSprite,
                TowerUpgradeMarkerShape.Square => PrototypeSpriteFactory.SquareSprite,
                _ => PrototypeSpriteFactory.DiamondSprite
            };
            markerRenderer.color = branch.BranchColor;
            markerRenderer.sortingOrder = 18;

            for (var index = 0; index < currentTier; index++)
            {
                var pip = new GameObject($"TierPip_{index + 1}");
                pip.transform.SetParent(markerObject.transform, false);
                pip.transform.localPosition = new Vector3((index - 1) * 0.42f, -0.78f, 0f);
                pip.transform.localScale = Vector3.one * 0.22f;
                var pipRenderer = pip.AddComponent<SpriteRenderer>();
                pipRenderer.sprite = PrototypeSpriteFactory.CircleSprite;
                pipRenderer.color = Color.white;
                pipRenderer.sortingOrder = 19;
            }

            RefreshRangePreviewScale();
        }
    }
}
