using System;
using UnityEngine;

namespace CatGuard.Gameplay.Battlefield
{
    public enum AdvancedMapRuleType
    {
        SecondaryEntrance,
        FloodedPlacementZone,
        Fog
    }

    [Serializable]
    public sealed class AdvancedMapRuleConfig
    {
        [SerializeField] private string ruleId = "map_rule";
        [SerializeField] private AdvancedMapRuleType ruleType;
        [SerializeField] private string nameLocalizationKey = string.Empty;
        [SerializeField] private string activeCueLocalizationKey = string.Empty;
        [Min(0f)]
        [SerializeField] private float activationDelaySeconds;
        [Min(0f)]
        [SerializeField] private float durationSeconds = 8f;
        [SerializeField] private string targetRouteId = string.Empty;
        [SerializeField] private BattlefieldZone targetZone;
        [Range(0.35f, 1f)]
        [SerializeField] private float towerRangeMultiplier = 1f;
        [SerializeField] private Color presentationColor = new(0.2f, 0.5f, 0.8f, 0.34f);

        public string RuleId => ruleId ?? string.Empty;
        public AdvancedMapRuleType RuleType => ruleType;
        public string NameLocalizationKey => nameLocalizationKey ?? string.Empty;
        public string ActiveCueLocalizationKey => activeCueLocalizationKey ?? string.Empty;
        public float ActivationDelaySeconds => Mathf.Max(0f, activationDelaySeconds);
        public float DurationSeconds => Mathf.Max(0f, durationSeconds);
        public string TargetRouteId => targetRouteId ?? string.Empty;
        public BattlefieldZone TargetZone => targetZone;
        public float TowerRangeMultiplier => Mathf.Clamp(towerRangeMultiplier, 0.35f, 1f);
        public Color PresentationColor => presentationColor;

        public void Configure(
            string id,
            AdvancedMapRuleType type,
            string nameKey,
            string cueKey,
            float activationDelay,
            float duration,
            string routeId,
            BattlefieldZone zone,
            float rangeMultiplier,
            Color color)
        {
            ruleId = id;
            ruleType = type;
            nameLocalizationKey = nameKey;
            activeCueLocalizationKey = cueKey;
            activationDelaySeconds = activationDelay;
            durationSeconds = duration;
            targetRouteId = routeId;
            targetZone = zone;
            towerRangeMultiplier = rangeMultiplier;
            presentationColor = color;
        }

        public bool IsValid(BattlefieldDefinition battlefield, out string error)
        {
            if (battlefield == null
                || string.IsNullOrWhiteSpace(RuleId)
                || string.IsNullOrWhiteSpace(NameLocalizationKey)
                || string.IsNullOrWhiteSpace(ActiveCueLocalizationKey))
            {
                error = "Advanced map rules require a battlefield, stable id, and localized name/cue.";
                return false;
            }

            switch (RuleType)
            {
                case AdvancedMapRuleType.SecondaryEntrance:
                    if (!battlefield.TryGetRoute(TargetRouteId, out _))
                    {
                        error = $"Secondary entrance rule '{RuleId}' references missing route '{TargetRouteId}'.";
                        return false;
                    }
                    break;
                case AdvancedMapRuleType.FloodedPlacementZone:
                    if (string.IsNullOrWhiteSpace(TargetZone.ZoneId)
                        || !Contains(battlefield.WorldBounds, TargetZone.Bounds)
                        || DurationSeconds <= 0f)
                    {
                        error = $"Flood rule '{RuleId}' requires a finite zone inside the battlefield.";
                        return false;
                    }
                    break;
                case AdvancedMapRuleType.Fog:
                    if (DurationSeconds <= 0f || TowerRangeMultiplier >= 0.99f)
                    {
                        error = $"Fog rule '{RuleId}' requires a finite readable range penalty.";
                        return false;
                    }
                    break;
            }

            error = string.Empty;
            return true;
        }

        private static bool Contains(Rect outer, Rect inner)
        {
            const float tolerance = 0.001f;
            return inner.xMin >= outer.xMin - tolerance
                && inner.xMax <= outer.xMax + tolerance
                && inner.yMin >= outer.yMin - tolerance
                && inner.yMax <= outer.yMax + tolerance;
        }
    }
}
