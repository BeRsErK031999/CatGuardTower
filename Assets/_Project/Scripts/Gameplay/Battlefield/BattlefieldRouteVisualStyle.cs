using UnityEngine;

namespace CatGuard.Gameplay.Battlefield
{
    public static class BattlefieldRouteVisualStyle
    {
        public static (Color Start, Color End) GetColors(string styleId, int index)
        {
            var normalized = (styleId ?? string.Empty).ToLowerInvariant();
            if (normalized.Contains("north") || normalized.Contains("moon"))
            {
                return (new Color(0.42f, 0.78f, 0.86f, 0.9f), new Color(0.25f, 0.62f, 0.78f, 0.94f));
            }

            if (normalized.Contains("south") || normalized.Contains("west"))
            {
                return (new Color(0.86f, 0.68f, 0.36f, 0.92f), new Color(0.82f, 0.46f, 0.22f, 0.95f));
            }

            if (normalized.Contains("boss") || normalized.Contains("chimney"))
            {
                return (new Color(0.72f, 0.4f, 0.72f, 0.92f), new Color(0.52f, 0.24f, 0.58f, 0.96f));
            }

            var tint = Mathf.Repeat(index * 0.13f, 0.32f);
            return (
                new Color(0.78f - tint, 0.64f + (tint * 0.25f), 0.38f + tint, 0.92f),
                new Color(0.74f - tint, 0.48f + (tint * 0.2f), 0.22f + tint, 0.94f));
        }
    }
}
