using CatGuard.Gameplay.Grid;
using CatGuard.Gameplay.Levels;
using CatGuard.UI.Layout;
using UnityEngine;

namespace CatGuard.Gameplay.CameraControl
{
    public sealed class BattlefieldInputController : MonoBehaviour
    {
        private const float DragThresholdLogical = 24f;

        private PrototypeLevelController levelController;
        private TowerGrid towerGrid;
        private BattlefieldCameraController cameraController;
        private bool pointerActive;
        private bool pointerBlocked;
        private bool pointerDragged;
        private Vector2 pointerStart;
        private Vector2 pointerPrevious;

        public bool LastGestureWasDrag { get; private set; }

        public void Initialize(
            PrototypeLevelController owner,
            TowerGrid grid,
            BattlefieldCameraController battlefieldCamera)
        {
            levelController = owner;
            towerGrid = grid;
            cameraController = battlefieldCamera;
            pointerActive = false;
            pointerBlocked = false;
            pointerDragged = false;
            LastGestureWasDrag = false;
        }

        private void Update()
        {
            if (levelController == null || towerGrid == null || cameraController == null)
            {
                return;
            }

            if (Input.touchCount > 0)
            {
                HandleTouch(Input.GetTouch(0));
                return;
            }

            HandleMouse();
        }

        private void HandleTouch(Touch touch)
        {
            switch (touch.phase)
            {
                case TouchPhase.Began:
                    BeginPointer(touch.position);
                    break;
                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    MovePointer(touch.position);
                    break;
                case TouchPhase.Ended:
                    EndPointer(touch.position, false);
                    break;
                case TouchPhase.Canceled:
                    EndPointer(touch.position, true);
                    break;
            }
        }

        private void HandleMouse()
        {
            if (Input.GetMouseButtonDown(0))
            {
                BeginPointer(Input.mousePosition);
            }
            else if (Input.GetMouseButton(0))
            {
                MovePointer(Input.mousePosition);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                EndPointer(Input.mousePosition, false);
            }
        }

        private void BeginPointer(Vector2 screenPosition)
        {
            pointerActive = true;
            pointerBlocked = levelController.IsScreenPointOverHud(screenPosition);
            pointerDragged = false;
            pointerStart = screenPosition;
            pointerPrevious = screenPosition;
            LastGestureWasDrag = false;
        }

        private void MovePointer(Vector2 screenPosition)
        {
            if (!pointerActive || pointerBlocked)
            {
                return;
            }

            var layout = LandscapeLayout.Calculate();
            var startLogical = layout.ScreenToLogical(pointerStart);
            var currentLogical = layout.ScreenToLogical(screenPosition);
            if (!pointerDragged && (currentLogical - startLogical).sqrMagnitude >= DragThresholdLogical * DragThresholdLogical)
            {
                pointerDragged = true;
            }

            if (pointerDragged)
            {
                cameraController.PanByScreenDelta(pointerPrevious, screenPosition);
            }

            pointerPrevious = screenPosition;
        }

        private void EndPointer(Vector2 screenPosition, bool canceled)
        {
            if (!pointerActive)
            {
                return;
            }

            MovePointer(screenPosition);
            LastGestureWasDrag = pointerDragged;
            if (!canceled
                && !pointerBlocked
                && !pointerDragged
                && !levelController.IsScreenPointOverHud(screenPosition))
            {
                towerGrid.TryPlaceFromScreen(screenPosition);
            }

            pointerActive = false;
            pointerBlocked = false;
            pointerDragged = false;
        }
    }
}
