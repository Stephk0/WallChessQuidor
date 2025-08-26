using UnityEngine;
using System.Collections.Generic;

namespace WallChess
{
    public class PlayerController : MonoBehaviour
    {
        private WallChessGameManager gameManager;
        private GridSystem gridSystem;
        private WallManager wallManager;
        private Camera mainCamera;
        private bool isDragging = false;
        private GameObject draggedAvatar;
        private Vector2Int originalPosition;
        private List<GameObject> highlightObjects = new List<GameObject>();

        public void Initialize(WallChessGameManager gm)
        {
            gameManager = gm;
            gridSystem = gm.GetGridSystem();
            wallManager = gm.GetWallManager();
            mainCamera = Camera.main;
        }

        void Update()
        {
            if (gameManager.currentState != GameState.PlayerTurn) return;
            HandleInput();
        }

        void HandleInput()
        {
            if (Input.GetMouseButtonDown(0))
            {
                StartDrag();
            }
            else if (Input.GetMouseButton(0) && isDragging)
            {
                UpdateDrag();
            }
            else if (Input.GetMouseButtonUp(0) && isDragging)
            {
                EndDrag();
            }
        }

        void StartDrag()
        {
            Vector3 mouseWorldPos = GetMouseWorldPosition();
            GameObject clickedObject = GetObjectAtPosition(mouseWorldPos);

            if (clickedObject == gameManager.GetPlayerAvatar())
            {
                isDragging = true;
                draggedAvatar = clickedObject;
                originalPosition = gameManager.playerPosition;
                ShowValidMoveHighlights();
                gameManager.ChangeState(GameState.PlayerMoving);
                Debug.Log("Started dragging player avatar");
            }
        }

        void UpdateDrag()
        {
            if (draggedAvatar != null)
            {
                Vector3 mouseWorldPos = GetMouseWorldPosition();
                draggedAvatar.transform.position = mouseWorldPos;
            }
        }

        void EndDrag()
        {
            if (draggedAvatar != null)
            {
                Vector3 mouseWorldPos = GetMouseWorldPosition();
                Vector2Int targetGridPos = gridSystem.WorldToGridPosition(mouseWorldPos);
                List<Vector2Int> validMoves = GetValidMovesWithWallBlocking(originalPosition);
                
                if (validMoves.Contains(targetGridPos))
                {
                    ExecuteMove(targetGridPos);
                }
                else
                {
                    CancelMove();
                }
            }

            isDragging = false;
            draggedAvatar = null;
            ClearHighlights();
            gameManager.ChangeState(GameState.PlayerTurn);
        }

        void ExecuteMove(Vector2Int newPosition)
        {
            // Use the GameManager's MovePlayer method which handles GridSystem updates
            gameManager.MovePlayer(newPosition);
            
            // Update avatar position
            Vector3 worldPos = gridSystem.GridToWorldPosition(newPosition);
            draggedAvatar.transform.position = worldPos;
            
            Debug.Log("Player moved to " + newPosition);
            gameManager.EndTurn();
        }

        void CancelMove()
        {
            Vector3 originalWorldPos = gridSystem.GridToWorldPosition(originalPosition);
            draggedAvatar.transform.position = originalWorldPos;
            Debug.Log("Move cancelled");
        }

        void ShowValidMoveHighlights()
        {
            List<Vector2Int> validMoves = GetValidMovesWithWallBlocking(gameManager.playerPosition);
            
            foreach (Vector2Int move in validMoves)
            {
                Vector3 worldPos = gridSystem.GridToWorldPosition(move);
                GameObject highlight = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                highlight.name = "MoveHighlight";
                highlight.transform.position = worldPos + Vector3.forward * -0.1f;
                highlight.transform.localScale = Vector3.one * 0.3f;
                
                Renderer renderer = highlight.GetComponent<Renderer>();
                renderer.material = new Material(Shader.Find("Sprites/Default"));
                renderer.material.color = new Color(0, 1, 0, 0.7f);
                
                // Remove collider to avoid interference
                var collider = highlight.GetComponent<Collider>();
                if (collider != null)
                    DestroyImmediate(collider);
                    
                highlightObjects.Add(highlight);
            }
        }

        void ClearHighlights()
        {
            foreach (GameObject highlight in highlightObjects)
            {
                if (highlight != null)
                {
#if UNITY_EDITOR
                    if (Application.isPlaying)
                        Destroy(highlight);
                    else
                        DestroyImmediate(highlight);
#else
                    Destroy(highlight);
#endif
                }
            }
            highlightObjects.Clear();
        }

        /// <summary>
        /// Get valid moves considering wall blocking from WallManager
        /// </summary>
        List<Vector2Int> GetValidMovesWithWallBlocking(Vector2Int currentPos)
        {
            List<Vector2Int> validMoves = new List<Vector2Int>();
            
            Vector2Int[] directions = {
                Vector2Int.up, Vector2Int.down,
                Vector2Int.left, Vector2Int.right
            };

            foreach (Vector2Int direction in directions)
            {
                Vector2Int newPos = currentPos + direction;
                
                // Check grid bounds
                if (!gridSystem.IsValidGridPosition(newPos)) continue;
                
                // Check tile occupancy
                if (gridSystem.IsTileOccupied(newPos)) continue;
                
                // Check wall blocking using WallManager
                if (wallManager != null && wallManager.IsMovementBlocked(currentPos, newPos)) continue;
                
                validMoves.Add(newPos);
            }

            return validMoves;
        }

        Vector3 GetMouseWorldPosition()
        {
            Vector3 mouseScreenPos = Input.mousePosition;
            mouseScreenPos.z = 10f;
            return mainCamera.ScreenToWorldPoint(mouseScreenPos);
        }

        GameObject GetObjectAtPosition(Vector3 worldPos)
        {
            float playerDistance = Vector3.Distance(worldPos, gameManager.GetPlayerAvatar().transform.position);
            float opponentDistance = Vector3.Distance(worldPos, gameManager.GetOpponentAvatar().transform.position);
            
            if (playerDistance < 0.5f)
                return gameManager.GetPlayerAvatar();
            if (opponentDistance < 0.5f)
                return gameManager.GetOpponentAvatar();
                
            return null;
        }

        #region Public API
        public bool IsDragging() => isDragging;
        public Vector2Int GetOriginalPosition() => originalPosition;
        
        /// <summary>
        /// Programmatically move the player to a specific position
        /// </summary>
        public bool TryMovePlayerTo(Vector2Int targetPosition)
        {
            if (gameManager.currentState != GameState.PlayerTurn) return false;
            
            List<Vector2Int> validMoves = GetValidMovesWithWallBlocking(gameManager.playerPosition);
            
            if (validMoves.Contains(targetPosition))
            {
                gameManager.MovePlayer(targetPosition);
                Debug.Log($"Player moved programmatically to {targetPosition}");
                gameManager.EndTurn();
                return true;
            }
            
            Debug.Log($"Invalid move: {targetPosition} is not a valid move from {gameManager.playerPosition}");
            return false;
        }
        
        /// <summary>
        /// Get all valid moves for the current player (accounting for walls)
        /// </summary>
        public List<Vector2Int> GetValidMoves()
        {
            return GetValidMovesWithWallBlocking(gameManager.playerPosition);
        }
        
        /// <summary>
        /// Check if a specific move is valid
        /// </summary>
        public bool IsValidMove(Vector2Int from, Vector2Int to)
        {
            // Check if it's a single step move
            Vector2Int diff = to - from;
            if (Mathf.Abs(diff.x) + Mathf.Abs(diff.y) != 1) return false;
            
            // Check grid bounds
            if (!gridSystem.IsValidGridPosition(to)) return false;
            
            // Check tile occupancy
            if (gridSystem.IsTileOccupied(to)) return false;
            
            // Check wall blocking
            if (wallManager != null && wallManager.IsMovementBlocked(from, to)) return false;
            
            return true;
        }
        #endregion
    }
}