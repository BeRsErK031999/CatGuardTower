using CatGuard.Utils;
using UnityEngine;

namespace CatGuard.VFX
{
    public static class SimpleVfxFactory
    {
        public static void Spawn(Vector3 position, SimpleVfxStyle style, Transform parent = null)
        {
            var vfxObject = new GameObject($"VFX_{style}");
            if (parent != null)
            {
                vfxObject.transform.SetParent(parent, false);
            }

            vfxObject.transform.position = position;
            var renderer = vfxObject.AddComponent<SpriteRenderer>();
            renderer.sprite = style is SimpleVfxStyle.TowerShot or SimpleVfxStyle.Victory
                ? PrototypeSpriteFactory.DiamondSprite
                : PrototypeSpriteFactory.CircleSprite;
            renderer.sortingOrder = 35;

            var color = GetColor(style);
            var endColor = new Color(color.r, color.g, color.b, 0f);
            var lifetime = vfxObject.AddComponent<SimpleVfxLifetime>();
            lifetime.Configure(renderer, GetDuration(style), GetStartScale(style), GetEndScale(style), color, endColor);
        }

        private static Color GetColor(SimpleVfxStyle style)
        {
            return style switch
            {
                SimpleVfxStyle.TowerPlaced => new Color(0.22f, 0.84f, 1f, 0.72f),
                SimpleVfxStyle.TowerShot => new Color(1f, 0.96f, 0.32f, 0.86f),
                SimpleVfxStyle.EnemyDefeated => new Color(1f, 0.28f, 0.24f, 0.82f),
                SimpleVfxStyle.BaseHit => new Color(1f, 0.5f, 0.18f, 0.88f),
                SimpleVfxStyle.Victory => new Color(0.34f, 1f, 0.58f, 0.72f),
                SimpleVfxStyle.Defeat => new Color(0.9f, 0.16f, 0.22f, 0.75f),
                _ => Color.white
            };
        }

        private static float GetDuration(SimpleVfxStyle style)
        {
            return style is SimpleVfxStyle.Victory or SimpleVfxStyle.Defeat ? 0.95f : 0.34f;
        }

        private static float GetStartScale(SimpleVfxStyle style)
        {
            return style == SimpleVfxStyle.TowerShot ? 0.18f : 0.34f;
        }

        private static float GetEndScale(SimpleVfxStyle style)
        {
            return style is SimpleVfxStyle.Victory or SimpleVfxStyle.Defeat ? 3.4f : 1.4f;
        }
    }
}
