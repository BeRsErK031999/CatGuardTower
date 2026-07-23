using System.Collections.Generic;
using CatGuard.Core.Quality;
using CatGuard.Meta.Progression;
using CatGuard.Utils;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CatGuard.VFX
{
    public static class SimpleVfxFactory
    {
        private const int FallbackCapacity = 96;
        private static readonly Stack<SimpleVfxLifetime> Available = new();
        private static readonly HashSet<SimpleVfxLifetime> Active = new();
        private static readonly List<SimpleVfxLifetime> SceneChangeBuffer = new();
        private static Transform poolRoot;
        private static int capacity = FallbackCapacity;
        private static bool sceneHookRegistered;

        public static int CreatedCount { get; private set; }
        public static int ReusedCount { get; private set; }
        public static int DroppedCount { get; private set; }
        public static int PeakActiveCount { get; private set; }
        public static int ActiveCount => Active.Count;
        public static int Capacity => capacity;

        public static void Spawn(Vector3 position, SimpleVfxStyle style, Transform parent = null)
        {
            EnsurePool();
            var lifetime = Acquire();
            if (lifetime == null)
            {
                DroppedCount++;
                return;
            }

            var vfxObject = lifetime.gameObject;
            vfxObject.name = $"VFX_{style}";
            vfxObject.transform.SetParent(parent != null ? parent : poolRoot, false);
            vfxObject.transform.position = position;
            vfxObject.SetActive(true);
            var renderer = vfxObject.GetComponent<SpriteRenderer>();
            renderer.sprite = style is SimpleVfxStyle.TowerShot or SimpleVfxStyle.Victory
                ? PrototypeSpriteFactory.DiamondSprite
                : PrototypeSpriteFactory.CircleSprite;
            renderer.sortingOrder = 35;

            var color = GetColor(style);
            var duration = GetDuration(style);
            var endScale = GetEndScale(style);
            if (ProgressionService.ReducedFlash)
            {
                color.a = Mathf.Min(color.a, 0.46f);
                duration = Mathf.Max(0.45f, duration);
                endScale = Mathf.Min(endScale, style is SimpleVfxStyle.Victory or SimpleVfxStyle.Defeat ? 2.5f : 1.05f);
            }

            var endColor = new Color(color.r, color.g, color.b, 0f);
            lifetime.Configure(renderer, duration, GetStartScale(style), endScale, color, endColor);
        }

        internal static void Release(SimpleVfxLifetime lifetime)
        {
            if (lifetime == null || !Active.Remove(lifetime))
            {
                return;
            }

            EnsurePool();
            lifetime.gameObject.name = "PooledSimpleVfx";
            lifetime.transform.SetParent(poolRoot, false);
            lifetime.gameObject.SetActive(false);
            Available.Push(lifetime);
        }

        public static void StopAll()
        {
            SceneChangeBuffer.Clear();
            foreach (var lifetime in Active)
            {
                if (lifetime != null)
                {
                    SceneChangeBuffer.Add(lifetime);
                }
            }

            foreach (var lifetime in SceneChangeBuffer)
            {
                Release(lifetime);
            }

            SceneChangeBuffer.Clear();
        }

        private static SimpleVfxLifetime Acquire()
        {
            SimpleVfxLifetime lifetime = null;
            while (Available.Count > 0 && lifetime == null)
            {
                lifetime = Available.Pop();
            }

            if (lifetime != null)
            {
                ReusedCount++;
            }
            else if (CreatedCount < capacity)
            {
                var vfxObject = new GameObject("PooledSimpleVfx");
                vfxObject.transform.SetParent(poolRoot, false);
                vfxObject.AddComponent<SpriteRenderer>();
                lifetime = vfxObject.AddComponent<SimpleVfxLifetime>();
                vfxObject.SetActive(false);
                CreatedCount++;
            }

            if (lifetime != null)
            {
                Active.Add(lifetime);
                PeakActiveCount = Mathf.Max(PeakActiveCount, Active.Count);
            }

            return lifetime;
        }

        private static void EnsurePool()
        {
            if (poolRoot == null)
            {
                var rootObject = new GameObject("SimpleVfxPool");
                if (Application.isPlaying)
                {
                    Object.DontDestroyOnLoad(rootObject);
                }
                poolRoot = rootObject.transform;
                var budget = ExpansionQualityBudgetConfig.LoadDefault();
                capacity = budget == null ? FallbackCapacity : budget.MaximumPooledVfx;
            }

            if (Application.isPlaying && !sceneHookRegistered)
            {
                SceneManager.activeSceneChanged += HandleActiveSceneChanged;
                sceneHookRegistered = true;
            }
        }

        private static void HandleActiveSceneChanged(Scene previous, Scene next)
        {
            StopAll();
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
