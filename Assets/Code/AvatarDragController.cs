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
        private List<GameObject> moveHighlights = new List<GameObject>();
        
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
            
            foreach (Vector2Int move in validMoves)
            {
                Vector3 worldPos = controller.GridToWorldPosition(move);
                
                GameObject highlight = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                highlight.name = "MoveHighlight";
                highlight.transform.position = worldPos + Vector3.back * 0.2f;
                highlight.transform.localScale = Vector3.one * 0.3f;
                
                Renderer renderer = highlight.GetComponent<Renderer>();
                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = new Color(0, 1, 0, 0.7f);
                renderer.material = mat;
                
                // Remove collider to prevent interference
                Collider col = highlight.GetComponent<Collider>();
                if (col != null) DestroyImmediate(col);
                
                moveHighlights.Add(highlight);
            }
            
            Debug.Log($"Showing {validMoves.Count} valid move highlights for {(isPlayerAvatar ? "player" : "opponent")}");
        }

        void ClearHighlights()
        {
            foreach (GameObject highlight in moveHighlights)
            {
                if (highlight != null)
                {
#if UNITY_EDITOR
                    if (Application.isPlaying)
                        Destroy(highlight);
                    else
                        DestroyImmediate(highlight);
#endif
                }
            }
            moveHighlights.Clear();
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