using UnityEngine;

namespace WallChess
{
    /// <summary>
    /// Individual drag controller for each avatar with simple dangle animation support
    /// </summary>
    public class AvatarDragController : MonoBehaviour
    {
        #region Private Fields
        private PlayerControllerV2 controller;
        private bool isPlayerAvatar;
        private bool isDragging = false;
        private Vector3 originalPosition;
        private Vector2Int originalGridPosition;
        private const int INVALID_POSITION = -1;
        private Vector2Int lastHighlightedPosition = Vector2Int.one * INVALID_POSITION;
        private bool wasLastPositionValid = false;
        
        // Cached objects to avoid allocations
        private Camera cachedCamera;
        private Plane gamePlane = new Plane(Vector3.back, Vector3.zero);
        private const float PLANE_Z_POSITION = 0f;
        private const float MIN_RAY_DIRECTION_Z = 0.001f;
        #endregion
        
        #region Animation Settings
        [Header("Animation")]
        [SerializeField] private SimplePawnDangleAnimation dangleAnimation;
        [SerializeField] private bool enableDangleAnimation = true;
        #endregion
        
        #region Public Methods
        public void Initialize(PlayerControllerV2 ctrl, bool isPlayer)
        {
            controller = ctrl;
            isPlayerAvatar = isPlayer;
            originalPosition = transform.position;
            
            // Cache camera reference
            cachedCamera = controller.GetMainCamera();
            
            // Ensure the object has a collider for mouse detection
            if (GetComponent<Collider>() == null)
                gameObject.AddComponent<BoxCollider>();
            
            // Auto-find dangle animation if not assigned
            if (dangleAnimation == null && enableDangleAnimation)
            {
                dangleAnimation = GetComponent<SimplePawnDangleAnimation>();
                enableDangleAnimation = dangleAnimation != null;
            }
        }

        /// <summary>
        /// Force reset the drag controller to its initial state
        /// Called when external events (like wall placement) should cancel any active drag
        /// </summary>
        public void ForceReset()
        {
            if (isDragging)
            {
                transform.position = originalPosition;
                EndDragging();
                ClearConfirmHighlights();
            }
            else
            {
                SyncToGridPosition();
            }
        }
        #endregion
        
        #region Unity Mouse Events
        void OnMouseDown()
        {
            if (controller == null || !controller.CanMoveAvatar(isPlayerAvatar)) return;
            
            isDragging = true;
            
            if (enableDangleAnimation && dangleAnimation != null)
                dangleAnimation.StartDangling();
            
            originalGridPosition = controller.GetAvatarPosition(isPlayerAvatar);
            originalPosition = controller.GridToWorldPosition(originalGridPosition);
            transform.position = originalPosition;
            
            ResetHighlightTracking();
        }

        void OnMouseDrag()
        {
            if (!isDragging || !controller.CanMoveAvatar(isPlayerAvatar)) return;
            
            Vector3 mouseWorldPos = GetMouseWorldPosition();
            transform.position = mouseWorldPos;
            
            Vector2Int targetGridPos = controller.WorldToGridPosition(mouseWorldPos);
            bool isValidMove = controller.IsValidMove(originalGridPosition, targetGridPos);
            
            if (ShouldUpdateHighlights(targetGridPos, isValidMove))
            {
                UpdateMoveHighlights(targetGridPos, isValidMove);
                lastHighlightedPosition = targetGridPos;
                wasLastPositionValid = isValidMove;
            }
        }

        void OnMouseUp()
        {
            if (!isDragging) return;
            
            ClearConfirmHighlights();
            
            Vector3 mouseWorldPos = GetMouseWorldPosition();
            Vector2Int targetGridPos = controller.WorldToGridPosition(mouseWorldPos);
            
            if (controller.IsValidMove(originalGridPosition, targetGridPos))
            {
                ExecuteValidMove(targetGridPos);
            }
            else
            {
                ExecuteInvalidMove();
            }
            
            EndDragging();
        }
        #endregion
        
        #region Private Helper Methods
        private bool ShouldUpdateHighlights(Vector2Int targetPos, bool isValid)
        {
            return targetPos != lastHighlightedPosition || isValid != wasLastPositionValid;
        }
        
        private void UpdateMoveHighlights(Vector2Int targetPos, bool isValid)
        {
            HighlightManager highlightManager = controller.GetHighlightManager();
            if (highlightManager == null) return;
            
            if (isValid)
                highlightManager.ShowConfirmHighlight(targetPos, controller.GetGridSystem());
            else
                highlightManager.ClearConfirmHighlights();
        }
        
        private void ResetHighlightTracking()
        {
            lastHighlightedPosition = Vector2Int.one * INVALID_POSITION;
            wasLastPositionValid = false;
        }

        private void ExecuteValidMove(Vector2Int targetGridPos)
        {
            Vector3 snapPosition = controller.GridToWorldPosition(targetGridPos);
            transform.position = snapPosition;
            controller.MoveAvatar(isPlayerAvatar, targetGridPos);
        }
        
        private void ExecuteInvalidMove()
        {
            transform.position = originalPosition;
        }
        
        private void EndDragging()
        {
            isDragging = false;
            ResetHighlightTracking();
            
            if (enableDangleAnimation && dangleAnimation != null)
                dangleAnimation.StopDangling();
        }
        
        private void SyncToGridPosition()
        {
            if (controller == null) return;
            
            Vector2Int currentGridPos = controller.GetAvatarPosition(isPlayerAvatar);
            originalPosition = controller.GridToWorldPosition(currentGridPos);
            transform.position = originalPosition;
        }

        private void ClearConfirmHighlights()
        {
            HighlightManager highlightManager = controller.GetHighlightManager();
            if (highlightManager != null)
            {
                highlightManager.ClearConfirmHighlights();
            }
        }

        private Vector3 GetMouseWorldPosition()
        {
            Vector3 mouseScreenPos = Input.mousePosition;
            Ray cameraRay = cachedCamera.ScreenPointToRay(mouseScreenPos);
            
            float intersectionDistance;
            if (gamePlane.Raycast(cameraRay, out intersectionDistance))
            {
                return cameraRay.GetPoint(intersectionDistance);
            }
            
            // Fallback: Project ray to Z=0 plane mathematically
            if (Mathf.Abs(cameraRay.direction.z) > MIN_RAY_DIRECTION_Z)
            {
                float t = (PLANE_Z_POSITION - cameraRay.origin.z) / cameraRay.direction.z;
                return cameraRay.origin + cameraRay.direction * t;
            }
            
            return new Vector3(cameraRay.origin.x, cameraRay.origin.y, PLANE_Z_POSITION);
        }
        #endregion
    }
}
