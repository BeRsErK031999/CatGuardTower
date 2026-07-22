using System;
using System.Collections.Generic;
using CatGuard.Gameplay.Levels;
using CatGuard.Utils;
using UnityEngine;

namespace CatGuard.Gameplay.Battlefield
{
    public sealed class AdvancedMapRuleRuntimeState
    {
        internal AdvancedMapRuleRuntimeState(AdvancedMapRuleConfig config)
        {
            Config = config;
        }

        public AdvancedMapRuleConfig Config { get; }
        public bool Active { get; internal set; }
        public float ForcedUntil { get; internal set; }
        public bool ForcePersistent { get; internal set; }
        internal GameObject PresentationObject { get; set; }
        internal SpriteRenderer PresentationRenderer { get; set; }
    }

    public sealed class AdvancedMapRuleController : MonoBehaviour
    {
        private readonly List<AdvancedMapRuleRuntimeState> states = new();
        private PrototypeLevelController owner;
        private BattlefieldDefinition battlefield;
        private Transform presentationRoot;
        private float timelineStartedAt;
        private bool timelineRunning;
        private bool cleanedUp;

        public IReadOnlyList<AdvancedMapRuleRuntimeState> States => states;
        public int ActivationCount { get; private set; }
        public int DeactivationCount { get; private set; }
        public int Revision { get; private set; }
        public bool TimelineRunning => timelineRunning;
        public bool IsClean => !timelineRunning && states.TrueForAll(state => !state.Active);

        public float TowerRangeMultiplier
        {
            get
            {
                var result = 1f;
                foreach (var state in states)
                {
                    if (state.Active && state.Config.RuleType == AdvancedMapRuleType.Fog)
                    {
                        result = Mathf.Min(result, state.Config.TowerRangeMultiplier);
                    }
                }

                return result;
            }
        }

        public void Initialize(
            PrototypeLevelController levelOwner,
            BattlefieldDefinition definition,
            AdvancedMapRuleConfig[] configs,
            Transform root)
        {
            owner = levelOwner;
            battlefield = definition;
            presentationRoot = root != null ? root : transform;
            states.Clear();
            ActivationCount = 0;
            DeactivationCount = 0;
            Revision = 0;
            timelineRunning = false;
            cleanedUp = false;

            foreach (var config in configs ?? Array.Empty<AdvancedMapRuleConfig>())
            {
                if (config == null)
                {
                    continue;
                }

                var state = new AdvancedMapRuleRuntimeState(config);
                CreatePresentation(state);
                states.Add(state);
            }
        }

        public void BeginTimeline()
        {
            timelineStartedAt = Time.unscaledTime;
            timelineRunning = true;
            cleanedUp = false;
            foreach (var state in states)
            {
                if (state.PresentationObject != null)
                {
                    state.PresentationObject.SetActive(state.Config.RuleType == AdvancedMapRuleType.SecondaryEntrance);
                }
            }
            EvaluateStates();
        }

        public bool IsRouteAvailable(string routeId)
        {
            foreach (var state in states)
            {
                if (state.Config.RuleType == AdvancedMapRuleType.SecondaryEntrance
                    && string.Equals(state.Config.TargetRouteId, routeId, StringComparison.Ordinal))
                {
                    return state.Active;
                }
            }

            return true;
        }

        public bool IsPlacementBlocked(Vector2 worldPosition)
        {
            foreach (var state in states)
            {
                if (state.Active
                    && state.Config.RuleType == AdvancedMapRuleType.FloodedPlacementZone
                    && state.Config.TargetZone.Contains(worldPosition))
                {
                    return true;
                }
            }

            return false;
        }

        public bool TriggerRule(string ruleId, float durationSeconds)
        {
            foreach (var state in states)
            {
                if (!string.Equals(state.Config.RuleId, ruleId, StringComparison.Ordinal))
                {
                    continue;
                }

                if (state.Config.RuleType == AdvancedMapRuleType.SecondaryEntrance)
                {
                    state.ForcePersistent = true;
                }
                else
                {
                    state.ForcedUntil = Mathf.Max(
                        state.ForcedUntil,
                        Time.unscaledTime + Mathf.Max(0.1f, durationSeconds));
                }

                SetActive(state, true);
                return true;
            }

            return false;
        }

        public float GetActivationCountdown(AdvancedMapRuleRuntimeState state)
        {
            if (state == null || !timelineRunning || state.Active)
            {
                return 0f;
            }

            return Mathf.Max(0f, state.Config.ActivationDelaySeconds - (Time.unscaledTime - timelineStartedAt));
        }

        public void EndBattle(string reason)
        {
            if (cleanedUp)
            {
                return;
            }

            cleanedUp = true;
            timelineRunning = false;
            foreach (var state in states)
            {
                state.ForcedUntil = 0f;
                state.ForcePersistent = false;
                SetActive(state, false);
                if (state.PresentationObject != null)
                {
                    state.PresentationObject.SetActive(false);
                }
            }
        }

        private void Update()
        {
            if (timelineRunning && owner?.State == PrototypeLevelState.Running)
            {
                EvaluateStates();
            }
        }

        private void OnDisable()
        {
            EndBattle("disabled");
        }

        private void EvaluateStates()
        {
            var elapsed = Time.unscaledTime - timelineStartedAt;
            foreach (var state in states)
            {
                var config = state.Config;
                var forced = state.ForcePersistent || state.ForcedUntil > Time.unscaledTime;
                var scheduled = config.RuleType == AdvancedMapRuleType.SecondaryEntrance
                    ? elapsed >= config.ActivationDelaySeconds
                    : elapsed >= config.ActivationDelaySeconds
                        && elapsed < config.ActivationDelaySeconds + config.DurationSeconds;
                SetActive(state, forced || scheduled);
            }
        }

        private void SetActive(AdvancedMapRuleRuntimeState state, bool active)
        {
            if (state.Active == active)
            {
                return;
            }

            state.Active = active;
            Revision++;
            if (active)
            {
                ActivationCount++;
            }
            else
            {
                DeactivationCount++;
            }

            RefreshPresentation(state);
            owner?.NotifyMapRuleStateChanged(state.Config, active);
        }

        private void CreatePresentation(AdvancedMapRuleRuntimeState state)
        {
            var config = state.Config;
            var presentation = new GameObject($"AdvancedRule_{config.RuleId}");
            presentation.transform.SetParent(presentationRoot, false);
            var renderer = presentation.AddComponent<SpriteRenderer>();
            renderer.sprite = config.RuleType == AdvancedMapRuleType.SecondaryEntrance
                ? PrototypeSpriteFactory.DiamondSprite
                : PrototypeSpriteFactory.SquareSprite;
            renderer.sortingOrder = config.RuleType == AdvancedMapRuleType.Fog ? 34 : 36;

            switch (config.RuleType)
            {
                case AdvancedMapRuleType.SecondaryEntrance:
                    if (battlefield.TryGetRoute(config.TargetRouteId, out var route))
                    {
                        presentation.transform.position = route.SpawnAnchor;
                    }
                    presentation.transform.localScale = Vector3.one * 0.9f;
                    presentation.SetActive(true);
                    break;
                case AdvancedMapRuleType.FloodedPlacementZone:
                    presentation.transform.position = config.TargetZone.Bounds.center;
                    presentation.transform.localScale = new Vector3(
                        config.TargetZone.Bounds.width,
                        config.TargetZone.Bounds.height,
                        1f);
                    presentation.SetActive(false);
                    break;
                case AdvancedMapRuleType.Fog:
                    presentation.transform.position = battlefield.WorldBounds.center;
                    presentation.transform.localScale = new Vector3(
                        battlefield.WorldBounds.width,
                        battlefield.WorldBounds.height,
                        1f);
                    presentation.SetActive(false);
                    break;
            }

            state.PresentationObject = presentation;
            state.PresentationRenderer = renderer;
            RefreshPresentation(state);
        }

        private static void RefreshPresentation(AdvancedMapRuleRuntimeState state)
        {
            if (state.PresentationObject == null || state.PresentationRenderer == null)
            {
                return;
            }

            var config = state.Config;
            if (config.RuleType == AdvancedMapRuleType.SecondaryEntrance)
            {
                state.PresentationObject.SetActive(true);
                state.PresentationRenderer.color = state.Active
                    ? new Color(0.2f, 0.9f, 0.52f, 0.92f)
                    : new Color(0.94f, 0.3f, 0.18f, 0.82f);
                return;
            }

            state.PresentationObject.SetActive(state.Active);
            state.PresentationRenderer.color = config.PresentationColor;
        }
    }
}
