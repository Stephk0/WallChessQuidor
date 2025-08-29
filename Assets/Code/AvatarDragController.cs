using UnityEngine;
using System.Collections.Generic;

namespace WallChess
{
    /// <summary>
    /// Individual drag controller for each avatar
    /// </summary>
    public class AvatarDragController : MonoBehaviour
    {
        private PlayerControllerV2 controller;
        private bool isPlayerAvatar;
        private bool isDragging = false;
        private Vector3 originalPosition;
        private Vector2Int originalGridPosition;
        
        public void Initialize(PlayerControllerV2 ctrl, bool isPlayer)
        {
            controller = ctrl;
            isPlayerAvatar = isPlayer;
            originalPosition = transform.position;
            
            // Ensure the object has a collider for mouse detection
            if (GetComponent<Collider>() == null)
            {
                gameObject.AddComponent<BoxCollider>();
            }
        }

        void OnMouseDown()
        {
            if (controller == null) return;
            
            if (!controller.CanMoveAvatar(isPlayerAvatar))
            {
                Debug.Log($"Cannot move {(isPlayerAvatar ? "player" : "opponent")}: not their turn");
                return;
            }
            
            isDragging = true;
            originalPosition = transform.position;
            originalGridPosition = controller.GetAvatarPosition(isPlayerAvatar);
            ShowValidMoveHighlights();
            
            string avatarType = isPlayerAvatar ? "player" : "opponent";
            Debug.Log($"Started dragging {avatarType} avatar");
        }

        void OnMouseDrag()
        {
            if (isDragging && controller.CanMoveAvatar(isPlayerAvatar))
            {
                Vector3 mouseWorldPos = GetMouseWorldPosition();
                transform.position = mouseWorldPos;
            }
        }

        void OnMouseUp()
        {
            if (isDragging)
            {
                ClearHighlights();
                
                Vector3 mouseWorldPos = GetMouseWorldPosition();
                Vector2Int targetGridPos = controller.WorldToGridPosition(mouseWorldPos);
                
                if (controller.IsValidMove(originalGridPosition, targetGridPos))
                {
                    // Snap to grid position
                    Vector3 snapPosition = controller.GridToWorldPosition(targetGridPos);
                    transform.position = snapPosition;
                    
                    // Execute the move through the controller
                    controller.MoveAvatar(isPlayerAvatar, targetGridPos);
                    
                    string avatarType = isPlayerAvatar ? "player" : "opponent";
                    Debug.Log($"{avatarType} moved to: {targetGridPos}");
                }
                else
                {
                    // Invalid move - return to original position
                    transform.position = originalPosition;
                    string avatarType = isPlayerAvatar ? "player" : "opponent";
                    Debug.Log($"Invalid move for {avatarType} from {originalGridPosition} to {targetGridPos}");
                }
                
                isDragging = false;
            }
        }

        void ShowValidMoveHighlights()
        {
            Vector2Int currentPos = controller.GetAvatarPosition(isPlayerAvatar);
            List<Vector2Int> validMoves = controller.GetValidMoves(currentPos);
            
            // Use the pooled highlight system from the controller
            HighlightManager highlightManager = controller.GetHighlightManager();
            if (highlightManager != null)
            {
                highlightManager.ShowHighlights(validMoves, controller.GetGridSystem());
                Debug.Log($"Showing {validMoves.Count} valid move highlights for {(isPlayerAvatar ? "player" : "opponent")}");
            }
            else
            {
                Debug.LogWarning("HighlightManager not found! Cannot show move highlights.");
            }
        }

        void ClearHighlights()
        {
            // Use the pooled highlight system from the controller
            HighlightManager highlightManager = controller.GetHighlightManager();
            if (highlightManager != null)
            {
                highlightManager.ClearHighlights();
            }
        }

        Vector3 GetMouseWorldPosition()
        {
            Vector3 mouseScreenPos = Input.mousePosition;
            Ray cameraRay = controller.GetMainCamera().ScreenPointToRay(mouseScreenPos);
            
            // Create a plane at Z=0 position, facing towards the camera
            Plane gamePlane = new Plane(Vector3.back, new Vector3(0, 0, 0));
            
            float intersectionDistance;
            if (gamePlane.Raycast(cameraRay, out intersectionDistance))
            {
                return cameraRay.GetPoint(intersectionDistance);
            }
            
            // Fallback: Project ray to Z=0 plane mathematically
            if (Mathf.Abs(cameraRay.direction.z) > 0.001f)
            {
                float t = (0f - cameraRay.origin.z) / cameraRay.direction.z;
                return cameraRay.origin + cameraRay.direction * t;
            }
            
            return new Vector3(cameraRay.origin.x, cameraRay.origin.y, 0f);
        }
    }
}