using UnityEngine;

namespace WallChess
{
    public class ImprovedDragMovement : MonoBehaviour
    {
        private Camera mainCamera;
        private bool isDragging = false;
        private Vector3 originalPosition;
        private float gridSpacing = 1.2f;
        private GameObject[] moveHighlights;

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
                ShowValidMoveHighlights();
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
                ClearHighlights();
                
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

        void ShowValidMoveHighlights()
        {
            Vector2Int currentGrid = WorldToGridPosition(originalPosition);
            Vector2Int[] directions = { 
                Vector2Int.up, Vector2Int.down, 
                Vector2Int.left, Vector2Int.right 
            };

            moveHighlights = new GameObject[4];
            int validMoves = 0;

            for (int i = 0; i < directions.Length; i++)
            {
                Vector2Int targetPos = currentGrid + directions[i];
                
                if (IsValidMove(currentGrid, targetPos))
                {
                    Vector3 worldPos = GridToWorldPosition(targetPos);
                    
                    GameObject highlight = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    highlight.name = "MoveHighlight";
                    highlight.transform.position = worldPos + Vector3.forward * -0.2f;
                    highlight.transform.localScale = Vector3.one * 0.3f;
                    
                    Renderer renderer = highlight.GetComponent<Renderer>();
                    renderer.material = new Material(Shader.Find("Sprites/Default"));
                    renderer.material.color = new Color(0, 1, 0, 0.7f);
                    
                    DestroyImmediate(highlight.GetComponent<Collider>());
                    moveHighlights[validMoves] = highlight;
                    validMoves++;
                }
            }
        }

        void ClearHighlights()
        {
            if (moveHighlights != null)
            {
                foreach (GameObject highlight in moveHighlights)
                {
                    if (highlight != null)
                        DestroyImmediate(highlight);
                }
                moveHighlights = null;
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
            // Check bounds
            if (to.x < 0 || to.x > 8 || to.y < 0 || to.y > 8)
                return false;

            // Check if it's an adjacent move
            int deltaX = Mathf.Abs(to.x - from.x);
            int deltaY = Mathf.Abs(to.y - from.y);
            
            if (!((deltaX == 1 && deltaY == 0) || (deltaX == 0 && deltaY == 1)))
                return false;
            
            // Check wall collisions using improved system
            Vector3 fromWorld = GridToWorldPosition(from);
            Vector3 toWorld = GridToWorldPosition(to);
            
            return !ImprovedCollisionChecker.IsBlocked(fromWorld, toWorld);
        }
    }
}