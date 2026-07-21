using CatGuard.Gameplay.Battlefield;
using CatGuard.UI.HUD;
using CatGuard.UI.Layout;
using CatGuard.Meta.Progression;
using UnityEngine;

namespace CatGuard.Gameplay.CameraControl
{
    public sealed class BattlefieldCameraController : MonoBehaviour
    {
        private BattlefieldDefinition battlefield;
        private UnityEngine.Camera mainCamera;
        private int lastScreenWidth;
        private int lastScreenHeight;
        private Rect lastSafeArea;
        private Vector3 baseCameraPosition;
        private float shakeEndsAt;
        private float shakeAmplitude;

        public Vector2 FocusPoint { get; private set; }
        public Rect FocusLimits { get; private set; }
        public bool CanPan => battlefield?.CanPan == true;

        public void Initialize(BattlefieldDefinition definition)
        {
            battlefield = definition;
            mainCamera = UnityEngine.Camera.main;
            RefreshViewport(true);
        }

        public bool PanByScreenDelta(Vector2 previousScreenPosition, Vector2 currentScreenPosition)
        {
            if (!CanPan || mainCamera == null)
            {
                return false;
            }

            var previousWorld = mainCamera.ScreenToWorldPoint(previousScreenPosition);
            var currentWorld = mainCamera.ScreenToWorldPoint(currentScreenPosition);
            var delta = (Vector2)(previousWorld - currentWorld);
            if (delta.sqrMagnitude <= 0.000001f)
            {
                return false;
            }

            return SetFocusClamped(FocusPoint + delta);
        }

        public bool SetFocusClamped(Vector2 requestedFocus)
        {
            if (battlefield == null || mainCamera == null)
            {
                return false;
            }

            var clamped = new Vector2(
                Mathf.Clamp(requestedFocus.x, FocusLimits.xMin, FocusLimits.xMax),
                Mathf.Clamp(requestedFocus.y, FocusLimits.yMin, FocusLimits.yMax));
            var moved = (clamped - FocusPoint).sqrMagnitude > 0.000001f;
            FocusPoint = clamped;
            ApplyCameraTransform();
            return moved;
        }

        public void RequestShake(float amplitude, float durationSeconds)
        {
            var adjustedAmplitude = Mathf.Max(0f, amplitude) * ProgressionService.CameraShakeIntensity;
            if (adjustedAmplitude <= 0f || durationSeconds <= 0f)
            {
                return;
            }

            shakeAmplitude = Mathf.Max(shakeAmplitude, adjustedAmplitude);
            shakeEndsAt = Mathf.Max(shakeEndsAt, Time.unscaledTime + durationSeconds);
        }

        private void LateUpdate()
        {
            if (battlefield == null)
            {
                return;
            }

            if (lastScreenWidth != Screen.width
                || lastScreenHeight != Screen.height
                || lastSafeArea != Screen.safeArea)
            {
                RefreshViewport(false);
            }

            ApplyShakeOffset();
        }

        private void RefreshViewport(bool resetFocus)
        {
            if (battlefield == null)
            {
                return;
            }

            if (mainCamera == null)
            {
                mainCamera = UnityEngine.Camera.main;
            }

            if (mainCamera == null)
            {
                return;
            }

            var layout = LandscapeLayout.Calculate();
            var battlefieldRect = PrototypeHud.GetBattlefieldRect(layout);
            var widthFraction = Mathf.Clamp(
                battlefieldRect.width / Mathf.Max(1f, layout.SurfaceRect.width),
                0.1f,
                1f);
            var heightFraction = Mathf.Clamp(
                battlefieldRect.height / Mathf.Max(1f, layout.SurfaceRect.height),
                0.1f,
                1f);
            var aspect = Mathf.Max(0.1f, mainCamera.aspect);

            float fullVisibleHeight;
            if (battlefield.CameraMode == BattlefieldCameraMode.FixedOverview)
            {
                fullVisibleHeight = Mathf.Max(
                    battlefield.WorldBounds.height / heightFraction,
                    battlefield.WorldBounds.width / (aspect * widthFraction));
            }
            else
            {
                fullVisibleHeight = battlefield.ScrollableViewHeight / heightFraction;
                var maximumHeightInsideBounds = Mathf.Min(
                    battlefield.CameraBounds.height / heightFraction,
                    battlefield.CameraBounds.width / (aspect * widthFraction));
                fullVisibleHeight = Mathf.Min(fullVisibleHeight, maximumHeightInsideBounds);
            }

            mainCamera.orthographic = true;
            mainCamera.orthographicSize = Mathf.Max(1.5f, fullVisibleHeight * 0.5f);
            mainCamera.backgroundColor = new Color(0.025f, 0.07f, 0.08f);

            var visibleHeight = mainCamera.orthographicSize * 2f;
            var visibleWidth = visibleHeight * aspect;
            var battlefieldVisibleWidth = visibleWidth * widthFraction;
            var battlefieldVisibleHeight = visibleHeight * heightFraction;
            FocusLimits = CreateFocusLimits(
                battlefield.CameraBounds,
                battlefieldVisibleWidth * 0.5f,
                battlefieldVisibleHeight * 0.5f);

            if (resetFocus)
            {
                FocusPoint = battlefield.CameraMode == BattlefieldCameraMode.FixedOverview
                    ? battlefield.WorldBounds.center
                    : battlefield.InitialCameraFocus;
            }

            FocusPoint = new Vector2(
                Mathf.Clamp(FocusPoint.x, FocusLimits.xMin, FocusLimits.xMax),
                Mathf.Clamp(FocusPoint.y, FocusLimits.yMin, FocusLimits.yMax));
            ApplyCameraTransform();
            CacheViewportState();
        }

        private void ApplyCameraTransform()
        {
            var layout = LandscapeLayout.Calculate();
            var battlefieldRect = PrototypeHud.GetBattlefieldRect(layout);
            var visibleHeight = mainCamera.orthographicSize * 2f;
            var visibleWidth = visibleHeight * mainCamera.aspect;
            var logicalOffset = battlefieldRect.center - layout.SurfaceRect.center;
            var cameraCenter = new Vector2(
                FocusPoint.x - ((logicalOffset.x / layout.SurfaceRect.width) * visibleWidth),
                FocusPoint.y + ((logicalOffset.y / layout.SurfaceRect.height) * visibleHeight));
            baseCameraPosition = new Vector3(cameraCenter.x, cameraCenter.y, -10f);
            ApplyShakeOffset();
        }

        private void ApplyShakeOffset()
        {
            if (mainCamera == null)
            {
                return;
            }

            if (Time.unscaledTime >= shakeEndsAt || shakeAmplitude <= 0f)
            {
                shakeAmplitude = 0f;
                mainCamera.transform.position = baseCameraPosition;
                return;
            }

            var remaining = Mathf.Clamp01((shakeEndsAt - Time.unscaledTime) / 0.35f);
            var phase = Time.unscaledTime * 52f;
            var offset = new Vector3(Mathf.Sin(phase), Mathf.Cos(phase * 1.31f), 0f)
                * shakeAmplitude
                * remaining;
            mainCamera.transform.position = baseCameraPosition + offset;
        }

        private void CacheViewportState()
        {
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            lastSafeArea = Screen.safeArea;
        }

        private static Rect CreateFocusLimits(Rect bounds, float halfWidth, float halfHeight)
        {
            var minX = bounds.xMin + halfWidth;
            var maxX = bounds.xMax - halfWidth;
            var minY = bounds.yMin + halfHeight;
            var maxY = bounds.yMax - halfHeight;

            if (minX > maxX)
            {
                minX = maxX = bounds.center.x;
            }

            if (minY > maxY)
            {
                minY = maxY = bounds.center.y;
            }

            return Rect.MinMaxRect(minX, minY, maxX, maxY);
        }
    }
}
