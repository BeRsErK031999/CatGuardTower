using UnityEngine;

namespace CatGuard.Gameplay.Presentation
{
    public static class UnitDirectionResolver
    {
        public static UnitFacingDirection Resolve(Vector2 movement, UnitFacingDirection previous)
        {
            if (movement.sqrMagnitude <= 0.000001f)
            {
                return previous;
            }

            var horizontal = Mathf.Abs(movement.x);
            var vertical = Mathf.Abs(movement.y);
            if (Mathf.Abs(horizontal - vertical) <= 0.0001f)
            {
                if (previous is UnitFacingDirection.East or UnitFacingDirection.West)
                {
                    return movement.x >= 0f ? UnitFacingDirection.East : UnitFacingDirection.West;
                }

                return movement.y >= 0f ? UnitFacingDirection.North : UnitFacingDirection.South;
            }

            if (horizontal > vertical)
            {
                return movement.x >= 0f ? UnitFacingDirection.East : UnitFacingDirection.West;
            }

            return movement.y >= 0f ? UnitFacingDirection.North : UnitFacingDirection.South;
        }
    }

    public static class UnitSortingPolicy
    {
        public const int MinimumBodyOrder = 19;
        public const int MaximumBodyOrder = 24;
        public const int ForegroundDecorationOrder = 26;

        public static int CalculateBodyOrder(float worldY, int spawnOrder)
        {
            var yBand = Mathf.Clamp(Mathf.RoundToInt((4f - worldY) * 0.55f), 0, 4);
            var stableTieBreak = Mathf.Abs(spawnOrder) % 2;
            return Mathf.Clamp(MinimumBodyOrder + yBand + stableTieBreak, MinimumBodyOrder, MaximumBodyOrder);
        }
    }
}
