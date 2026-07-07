using CatGuard.Gameplay.Enemies;
using CatGuard.Gameplay.Levels;
using CatGuard.Meta.Progression;
using CatGuard.Utils;
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

            var target = levelController.FindNearestEnemy(transform.position, range);
            if (target == null)
            {
                return;
            }

            target.ApplyDamage(damage);
            fireTimer = fireInterval;
        }

        private void EnsureVisual()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }

            spriteRenderer.sprite = PrototypeSpriteFactory.SquareSprite;
            spriteRenderer.color = config.VisualColor;
            spriteRenderer.sortingOrder = 15;
            transform.localScale = new Vector3(config.VisualScale, config.VisualScale, 1f);
        }
    }
}
