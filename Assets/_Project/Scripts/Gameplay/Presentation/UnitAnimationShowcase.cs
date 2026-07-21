using System;
using CatGuard.Gameplay.Enemies;
using CatGuard.Utils;
using UnityEngine;

namespace CatGuard.Gameplay.Presentation
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public sealed class UnitAnimationShowcase : MonoBehaviour
    {
        private static readonly UnitAnimationState[] PreviewStates =
        {
            UnitAnimationState.Spawn,
            UnitAnimationState.Idle,
            UnitAnimationState.Walk,
            UnitAnimationState.Hit,
            UnitAnimationState.GoalAttack,
            UnitAnimationState.Ability,
            UnitAnimationState.Death
        };

        [SerializeField] private EnemyConfig[] roster = Array.Empty<EnemyConfig>();
        [SerializeField] private UnitAnimationState previewState = UnitAnimationState.Walk;
        [SerializeField] private UnitFacingDirection previewDirection = UnitFacingDirection.East;
        [SerializeField] private UnitStatusModifier previewModifier;
        [Range(0.4f, 1.8f)]
        [SerializeField] private float previewSpeedMultiplier = 1f;
        [Range(0f, 1f)]
        [SerializeField] private float previewPhase = 0.45f;
        [SerializeField] private bool autoCycle = true;

        public EnemyConfig[] Roster => roster ?? Array.Empty<EnemyConfig>();
        public UnitAnimationState PreviewState => previewState;
        public UnitFacingDirection PreviewDirection => previewDirection;
        public UnitStatusModifier PreviewModifier => previewModifier;
        public int ActorCount => GetComponentsInChildren<UnitAnimationShowcaseActor>(true).Length;

        private void OnEnable()
        {
            EnsureRosterActors();
        }

        public void ConfigureRoster(EnemyConfig[] enemyRoster)
        {
            roster = enemyRoster ?? Array.Empty<EnemyConfig>();
            for (var childIndex = transform.childCount - 1; childIndex >= 0; childIndex--)
            {
                var child = transform.GetChild(childIndex).gameObject;
                if (Application.isPlaying)
                {
                    Destroy(child);
                    continue;
                }

                DestroyImmediate(child);
            }

            const float spacing = 1.55f;
            var startX = -spacing * (Roster.Length - 1) * 0.5f;
            for (var index = 0; index < Roster.Length; index++)
            {
                CreateActor(Roster[index], index, new Vector3(startX + (spacing * index), 0f, 0f));
            }

            EvaluatePreview();
        }

        public void SetPreview(
            UnitAnimationState state,
            UnitFacingDirection direction,
            UnitStatusModifier modifier,
            float speedMultiplier,
            float normalizedPhase,
            bool cycleAutomatically = false)
        {
            previewState = state;
            previewDirection = direction;
            previewModifier = modifier;
            previewSpeedMultiplier = Mathf.Clamp(speedMultiplier, 0.4f, 1.8f);
            previewPhase = Mathf.Clamp01(normalizedPhase);
            autoCycle = cycleAutomatically;
            EvaluatePreview();
        }

        public void EvaluatePreview()
        {
            var state = previewState;
            var phase = previewPhase;
            if (autoCycle)
            {
                var cycleTime = (float)(Time.realtimeSinceStartupAsDouble * 0.72d);
                state = PreviewStates[Mathf.FloorToInt(cycleTime) % PreviewStates.Length];
                phase = cycleTime - Mathf.Floor(cycleTime);
            }

            foreach (var actor in GetComponentsInChildren<UnitAnimationShowcaseActor>(true))
            {
                actor.Presenter?.SetControlledPreview(
                    state,
                    previewDirection,
                    previewModifier,
                    previewSpeedMultiplier,
                    phase);
            }
        }

        private void Update()
        {
            EvaluatePreview();
        }

        private void OnValidate()
        {
            EnsureRosterActors();
        }

        private void EnsureRosterActors()
        {
            if (Roster.Length > 0 && ActorCount != Roster.Length)
            {
                ConfigureRoster(Roster);
                return;
            }

            EvaluatePreview();
        }

        private void CreateActor(EnemyConfig enemy, int index, Vector3 localPosition)
        {
            if (enemy == null)
            {
                return;
            }

            var actorObject = new GameObject($"Showcase_{enemy.EnemyId}");
            actorObject.transform.SetParent(transform, false);
            actorObject.transform.localPosition = localPosition;
            var sprite = enemy.VisualSprite != null ? enemy.VisualSprite : PrototypeSpriteFactory.CircleSprite;
            var spriteSize = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
            var visualScale = enemy.VisualScale / Mathf.Max(0.01f, spriteSize);
            actorObject.transform.localScale = new Vector3(visualScale, visualScale, 1f);

            var actor = actorObject.AddComponent<UnitAnimationShowcaseActor>();
            var presenter = actorObject.AddComponent<UnitAnimationPresenter>();
            presenter.Initialize(
                enemy.AnimationProfile,
                sprite,
                enemy.VisualSprite != null ? Color.white : enemy.VisualColor,
                enemy.Speed,
                index + 1);
            actor.Configure(enemy, presenter);

            var labelObject = new GameObject($"Label_{enemy.EnemyId}");
            labelObject.transform.SetParent(transform, false);
            labelObject.transform.localPosition = localPosition + new Vector3(0f, -0.72f, 0f);
            labelObject.transform.localScale = Vector3.one;
            var label = labelObject.AddComponent<TextMesh>();
            label.text = enemy.DisplayName;
            label.anchor = TextAnchor.UpperCenter;
            label.alignment = TextAlignment.Center;
            label.characterSize = 0.075f;
            label.fontSize = 32;
            label.color = new Color(0.9f, 0.95f, 0.96f, 1f);
            var renderer = labelObject.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sortingOrder = 40;
            }
        }
    }
}
