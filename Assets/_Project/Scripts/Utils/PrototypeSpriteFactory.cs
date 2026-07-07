using UnityEngine;

namespace CatGuard.Utils
{
    public static class PrototypeSpriteFactory
    {
        private static Sprite squareSprite;

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
    }
}
