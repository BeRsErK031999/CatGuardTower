using System.Collections.Generic;
using CatGuard.Meta.Progression;
using CatGuard.Utils;
using UnityEngine;

namespace CatGuard.Gameplay.Ultimates
{
    public sealed class UltimateVfxPool : MonoBehaviour
    {
        private const int InitialPoolSize = 10;
        private const int MaximumPoolSize = 18;

        private readonly List<UltimateVfxInstance> instances = new();

        public int CreatedCount => instances.Count;
        public int ActiveCount
        {
            get
            {
                var count = 0;
                foreach (var instance in instances)
                {
                    if (instance != null && instance.IsActive)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        public void Initialize(Transform parent)
        {
            if (parent != null && parent != transform)
            {
                transform.SetParent(parent, false);
            }
            while (instances.Count < InitialPoolSize)
            {
                instances.Add(CreateInstance(instances.Count));
            }
        }

        public void Spawn(Vector3 position, Color color, float startScale, float endScale, float duration)
        {
            var instance = FindAvailable();
            if (instance == null)
            {
                return;
            }

            var reducedFlash = ProgressionService.ReducedFlash;
            var safeColor = reducedFlash
                ? new Color(color.r, color.g, color.b, Mathf.Min(color.a, 0.42f))
                : color;
            instance.Play(
                position,
                safeColor,
                Mathf.Max(0.05f, startScale),
                Mathf.Max(startScale, endScale),
                reducedFlash ? Mathf.Max(0.45f, duration) : duration);
        }

        public void StopAll()
        {
            foreach (var instance in instances)
            {
                instance?.Stop();
            }
        }

        private UltimateVfxInstance FindAvailable()
        {
            foreach (var instance in instances)
            {
                if (instance != null && !instance.IsActive)
                {
                    return instance;
                }
            }

            if (instances.Count >= MaximumPoolSize)
            {
                return null;
            }

            var created = CreateInstance(instances.Count);
            instances.Add(created);
            return created;
        }

        private UltimateVfxInstance CreateInstance(int index)
        {
            var item = new GameObject($"UltimateVfx_{index:00}");
            item.transform.SetParent(transform, false);
            var renderer = item.AddComponent<SpriteRenderer>();
            renderer.sprite = PrototypeSpriteFactory.CircleSprite;
            renderer.sortingOrder = 42;
            var instance = item.AddComponent<UltimateVfxInstance>();
            instance.Initialize(renderer);
            return instance;
        }
    }

    public sealed class UltimateVfxInstance : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private Color startColor;
        private float startScale;
        private float endScale;
        private float duration;
        private float elapsed;

        public bool IsActive => gameObject.activeSelf;

        public void Initialize(SpriteRenderer renderer)
        {
            spriteRenderer = renderer;
            gameObject.SetActive(false);
        }

        public void Play(Vector3 position, Color color, float fromScale, float toScale, float seconds)
        {
            transform.position = position;
            startColor = color;
            startScale = fromScale;
            endScale = toScale;
            duration = Mathf.Max(0.08f, seconds);
            elapsed = 0f;
            gameObject.SetActive(true);
            Apply(0f);
        }

        public void Stop()
        {
            gameObject.SetActive(false);
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            var progress = Mathf.Clamp01(elapsed / duration);
            Apply(progress);
            if (progress >= 1f)
            {
                Stop();
            }
        }

        private void Apply(float progress)
        {
            transform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, progress);
            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(
                    startColor.r,
                    startColor.g,
                    startColor.b,
                    Mathf.Lerp(startColor.a, 0f, progress));
            }
        }
    }
}
