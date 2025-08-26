using UnityEngine;

namespace WallChess
{
    public class SimpleDragMovement : MonoBehaviour
    {
        private Camera mainCamera;
        private bool isDragging = false;
        private Vector3 originalPosition;
        private float gridSpacing = 1.2f;

        void Start()
        {
            mainCamera = Camera.main;
            originalPosition = transform.position;
        }

        void OnMouseDown()
        {
            if (gameObject.name == "Player")
            {
                isDragging = true;
                originalPosition = transform.position;
                Debug.Log("Started dragging player");
            }
        }

        void OnMouseDrag()
        {
            if (isDragging)
            {
                Vector3 mouseWorldPos = GetMouseWorldPosition();
                transform.position = mouseWorldPos;
            }
        }

        void OnMouseUp()
        {
            if (isDragging)
            {
                Vector3 mouseWorldPos = GetMouseWorldPosition();
                Vector2Int targetGridPos = WorldToGridPosition(mouseWorldPos);
                Vector2Int currentGridPos = WorldToGridPosition(originalPosition);
                
                if (IsValidMove(currentGridPos, targetGridPos))
                {
                    Vector3 snapPosition = GridToWorldPosition(targetGridPos);
                    transform.position = snapPosition;
                    Debug.Log("Moved to: " + targetGridPos);
                }
                else
                {
                    transform.position = originalPosition;
                    Debug.Log("Invalid move - blocked or out of range");
                }
                
                isDragging = false;
            }
        }

        Vector3 GetMouseWorldPosition()
        {
            Vector3 mouseScreenPos = Input.mousePosition;
            mouseScreenPos.z = 10f;
            return mainCamera.ScreenToWorldPoint(mouseScreenPos);
        }

        Vector2Int WorldToGridPosition(Vector3 worldPos)
        {
            int x = Mathf.RoundToInt(worldPos.x / gridSpacing);
            int y = Mathf.RoundToInt(worldPos.y / gridSpacing);
            x = Mathf.Clamp(x, 0, 8);
            y = Mathf.Clamp(y, 0, 8);
            return new Vector2Int(x, y);
        }

        Vector3 GridToWorldPosition(Vector2Int gridPos)
        {
            return new Vector3(gridPos.x * gridSpacing, gridPos.y * gridSpacing, 0);
        }

        bool IsValidMove(Vector2Int from, Vector2Int to)
        {
            int deltaX = Mathf.Abs(to.x - from.x);
            int deltaY = Mathf.Abs(to.y - from.y);
            
            // Only adjacent moves allowed
            if (!((deltaX == 1 && deltaY == 0) || (deltaX == 0 && deltaY == 1)))
            {
                return false;
            }
            
            // Check wall collisions
            Vector3 fromWorld = GridToWorldPosition(from);
            Vector3 toWorld = GridToWorldPosition(to);
            
            return !WallCollisionChecker.IsBlocked(fromWorld, toWorld);
        }
    }
}