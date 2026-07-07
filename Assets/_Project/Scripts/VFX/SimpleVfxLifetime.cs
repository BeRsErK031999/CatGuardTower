using UnityEngine;

namespace CatGuard.VFX
{
    public sealed class SimpleVfxLifetime : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private float duration;
        private float elapsed;
        private float startScale;
        private float endScale;
        private Color startColor;
        private Color endColor;

        public void Configure(
            SpriteRenderer renderer,
            float lifeSeconds,
            float fromScale,
            float toScale,
            Color fromColor,
            Color toColor)
        {
            spriteRenderer = renderer;
            duration = Mathf.Max(0.05f, lifeSeconds);
            startScale = fromScale;
            endScale = toScale;
            startColor = fromColor;
            endColor = toColor;
            elapsed = 0f;
            Apply(0f);
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            Apply(t);

            if (t >= 1f)
            {
                Destroy(gameObject);
            }
        }

        private void Apply(float t)
        {
            transform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, t);
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.Lerp(startColor, endColor, t);
            }
        }
    }
}
