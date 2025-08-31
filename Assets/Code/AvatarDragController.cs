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
        private Vector2Int lastHighlightedPosition = Vector2Int.one * -1; // Track last highlighted position
        private bool wasLastPositionValid = false; // Track if last position was valid
        
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
            
            // Reset highlight tracking when starting drag
            lastHighlightedPosition = Vector2Int.one * -1;
            wasLastPositionValid = false;
        }

        void OnMouseDrag()
        {
            if (isDragging && controller.CanMoveAvatar(isPlayerAvatar))
            {
                Vector3 mouseWorldPos = GetMouseWorldPosition();
                transform.position = mouseWorldPos;
                
                Vector2Int targetGridPos = controller.WorldToGridPosition(mouseWorldPos);
                bool isValidMove = controller.IsValidMove(originalGridPosition, targetGridPos);
                
                // Only update highlights when position or validity changes
                if (targetGridPos != lastHighlightedPosition || isValidMove != wasLastPositionValid)
                {
                    HighlightManager highlightManager = controller.GetHighlightManager();
                    if (highlightManager != null)
                    {
                        if (isValidMove)
                        {
                            // Show confirm highlight for valid move
                            highlightManager.ShowConfirmHighlight(targetGridPos, controller.GetGridSystem());
                        }
                        else
                        {
                            // Clear confirm highlight for invalid move
                            highlightManager.ClearConfirmHighlights();
                        }
                    }
                    
                    // Update tracking variables
                    lastHighlightedPosition = targetGridPos;
                    wasLastPositionValid = isValidMove;
                }
            }
        }

        void OnMouseUp()
        {
            if (isDragging)
            {
                // Clear only confirm highlights (valid move highlights stay visible)
                ClearConfirmHighlights();
                
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
                // Reset highlight tracking when ending drag
                lastHighlightedPosition = Vector2Int.one * -1;
                wasLastPositionValid = false;
            }
        }

        void ClearConfirmHighlights()
        {
            // Clear only the confirm highlights, leaving valid move highlights visible
            HighlightManager highlightManager = controller.GetHighlightManager();
            if (highlightManager != null)
            {
                highlightManager.ClearConfirmHighlights();
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