using CatGuard.Gameplay.Enemies;
using CatGuard.Gameplay.Levels;
using CatGuard.Utils;
using UnityEngine;

namespace CatGuard.Gameplay.Towers
{
    public sealed class BasicTower : MonoBehaviour
    {
        private PrototypeLevelController levelController;
        private float range;
        private float damage;
        private float fireInterval;
        private float fireTimer;
        private SpriteRenderer spriteRenderer;

        public void Initialize(PrototypeLevelController owner, PrototypeLevelConfig config)
        {
            levelController = owner;
            range = config.TowerRange;
            damage = config.TowerDamage;
            fireInterval = config.TowerFireInterval;
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
            spriteRenderer.color = new Color(0.24f, 0.78f, 0.96f);
            spriteRenderer.sortingOrder = 15;
            transform.localScale = new Vector3(0.62f, 0.62f, 1f);
        }
    }
}
