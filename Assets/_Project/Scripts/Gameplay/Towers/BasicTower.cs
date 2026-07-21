using CatGuard.Core.Audio;
using CatGuard.Gameplay.Enemies;
using CatGuard.Gameplay.Levels;
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
        private float range;
        private float damage;
        private float fireInterval;
        private float fireTimer;
        private SpriteRenderer spriteRenderer;

        public void Initialize(PrototypeLevelController owner, TowerConfig towerConfig)
        {
            levelController = owner;
            config = towerConfig;
            range = config.Range * ProgressionService.GetTowerRangeMultiplier();
            damage = config.Damage * ProgressionService.GetTowerDamageMultiplier();
            fireInterval = config.FireInterval;
            fireTimer = 0f;

            EnsureVisual();
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

            var target = levelController.FindTargetEnemy(transform.position, range, config.TargetPriority);
            if (target == null)
            {
                return;
            }

            var targetPosition = target.transform.position;
            levelController.ApplyTowerAttack(target, damage, config.SplashRadius);
            ProceduralAudioService.Play(ProceduralSoundId.TowerShot);
            SimpleVfxFactory.Spawn(targetPosition, SimpleVfxStyle.TowerShot);
            fireTimer = fireInterval;
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
            var visualScale = config.VisualScale / Mathf.Max(0.01f, spriteSize);
            transform.localScale = new Vector3(visualScale, visualScale, 1f);
        }
    }
}
