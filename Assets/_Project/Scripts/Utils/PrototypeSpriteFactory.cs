using UnityEngine;

namespace CatGuard.Utils
{
    public static class PrototypeSpriteFactory
    {
        private static Sprite squareSprite;
        private static Sprite circleSprite;
        private static Sprite diamondSprite;

        public static Sprite SquareSprite
        {
            get
            {
                if (squareSprite != null)
                {
                    return squareSprite;
                }

                var texture = new Texture2D(1, 1)
                {
                    filterMode = FilterMode.Point,
                    hideFlags = HideFlags.HideAndDontSave
                };

                texture.SetPixel(0, 0, Color.white);
                texture.Apply();

                squareSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
                squareSprite.hideFlags = HideFlags.HideAndDontSave;

                return squareSprite;
            }
        }

        public static Sprite CircleSprite
        {
            get
            {
                if (circleSprite != null)
                {
                    return circleSprite;
                }

                circleSprite = CreateShapeSprite("CircleSprite", IsInsideCircle);
                return circleSprite;
            }
        }

        public static Sprite DiamondSprite
        {
            get
            {
                if (diamondSprite != null)
                {
                    return diamondSprite;
                }

                diamondSprite = CreateShapeSprite("DiamondSprite", IsInsideDiamond);
                return diamondSprite;
            }
        }

        private static Sprite CreateShapeSprite(string spriteName, System.Func<float, float, bool> shapeTest)
        {
            const int size = 32;
            var texture = new Texture2D(size, size)
            {
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var u = ((x + 0.5f) / size) * 2f - 1f;
                    var v = ((y + 0.5f) / size) * 2f - 1f;
                    texture.SetPixel(x, y, shapeTest(u, v) ? Color.white : Color.clear);
                }
            }

            texture.Apply();

            var sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            sprite.name = spriteName;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        private static bool IsInsideCircle(float x, float y)
        {
            return (x * x) + (y * y) <= 0.92f;
        }

        private static bool IsInsideDiamond(float x, float y)
        {
            return Mathf.Abs(x) + Mathf.Abs(y) <= 1.05f;
        }
    }
}
