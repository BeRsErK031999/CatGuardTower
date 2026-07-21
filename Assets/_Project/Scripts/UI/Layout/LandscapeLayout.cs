using UnityEngine;

namespace CatGuard.UI.Layout
{
    public static class LandscapeLayout
    {
        public const float ReferenceWidth = 1920f;
        public const float ReferenceHeight = 1080f;
        public const float MinimumViewportWidth = 1280f;
        public const float MinimumViewportHeight = 720f;

        public static Context Calculate()
        {
            var screenWidth = Mathf.Max(1f, Screen.width);
            var screenHeight = Mathf.Max(1f, Screen.height);
            var scale = Mathf.Min(
                screenHeight / ReferenceHeight,
                screenWidth / MinimumViewportWidth);
            scale = Mathf.Max(0.01f, scale);

            var surface = new Rect(0f, 0f, screenWidth / scale, screenHeight / scale);
            var physicalSafeArea = Screen.safeArea;
            var safeArea = new Rect(
                physicalSafeArea.xMin / scale,
                (screenHeight - physicalSafeArea.yMax) / scale,
                physicalSafeArea.width / scale,
                physicalSafeArea.height / scale);

            safeArea.xMin = Mathf.Clamp(safeArea.xMin, surface.xMin, surface.xMax);
            safeArea.xMax = Mathf.Clamp(safeArea.xMax, surface.xMin, surface.xMax);
            safeArea.yMin = Mathf.Clamp(safeArea.yMin, surface.yMin, surface.yMax);
            safeArea.yMax = Mathf.Clamp(safeArea.yMax, surface.yMin, surface.yMax);

            return new Context(scale, surface, safeArea, screenHeight);
        }

        public static Context Begin(out Matrix4x4 previousMatrix)
        {
            var context = Calculate();
            previousMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(context.Scale, context.Scale, 1f));
            return context;
        }

        public static void End(Matrix4x4 previousMatrix)
        {
            GUI.matrix = previousMatrix;
        }

        public static Rect Inset(Rect rect, float horizontal, float vertical)
        {
            var insetX = Mathf.Min(horizontal, rect.width * 0.5f);
            var insetY = Mathf.Min(vertical, rect.height * 0.5f);
            return new Rect(
                rect.x + insetX,
                rect.y + insetY,
                Mathf.Max(0f, rect.width - (insetX * 2f)),
                Mathf.Max(0f, rect.height - (insetY * 2f)));
        }

        public readonly struct Context
        {
            private readonly float screenHeight;

            public Context(float scale, Rect surfaceRect, Rect safeRect, float physicalScreenHeight)
            {
                Scale = scale;
                SurfaceRect = surfaceRect;
                SafeRect = safeRect;
                screenHeight = physicalScreenHeight;
            }

            public float Scale { get; }
            public Rect SurfaceRect { get; }
            public Rect SafeRect { get; }

            public Vector2 ScreenToLogical(Vector2 screenPosition)
            {
                return new Vector2(
                    screenPosition.x / Scale,
                    (screenHeight - screenPosition.y) / Scale);
            }
        }
    }
}
