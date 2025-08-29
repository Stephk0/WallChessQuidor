using UnityEngine;
using System.Collections.Generic;

namespace WallChess
{
    public class PlayerControllerV2 : MonoBehaviour
    {
        private WallChessGameManager gameManager;
        private GridSystem gridSystem;
        private Camera mainCamera;
        
        [Header("Debug")]
        public bool enableDebugLogs = true;

        public void Initialize(WallChessGameManager gm)
        {
            gameManager = gm;
            gridSystem = gm.GetGridSystem();
            mainCamera = Camera.main;
            
            // Add drag controllers to both avatars
            SetupAvatarDragControllers();
            
            if (enableDebugLogs) Debug.Log("PlayerControllerV2 initialized with unified grid system");
        }
        
        void SetupAvatarDragControllers()
        {
            // Add drag controller to player avatar
            GameObject playerAvatar = gameManager.GetPlayerAvatar();
            if (playerAvatar != null)
            {
                AvatarDragController playerDrag = playerAvatar.GetComponent<AvatarDragController>();
                if (playerDrag == null)
                {
                    playerDrag = playerAvatar.AddComponent<AvatarDragController>();
                }
                playerDrag.Initialize(this, true); // true = is player
                Debug.Log("Player avatar drag controller setup complete");
            }

            // Add drag controller to opponent avatar  
            GameObject opponentAvatar = gameManager.GetOpponentAvatar();
            if (opponentAvatar != null)
            {
                AvatarDragController opponentDrag = opponentAvatar.GetComponent<AvatarDragController>();
                if (opponentDrag == null)
                {
                    opponentDrag = opponentAvatar.AddComponent<AvatarDragController>();
                }
                opponentDrag.Initialize(this, false); // false = is opponent
                Debug.Log("Opponent avatar drag controller setup complete");
            }
        }

        public bool CanMoveAvatar(bool isPlayer)
        {
            // Convert bool isPlayer to pawn index for new system
            int pawnIndex = isPlayer ? 0 : 1;
            return gameManager.CanMovePawn(pawnIndex) && gameManager.CanInitiateMove();
        }

        public Vector2Int GetAvatarPosition(bool isPlayer)
        {
            return isPlayer ? gameManager.playerPosition : gameManager.opponentPosition;
        }

        public void MoveAvatar(bool isPlayer, Vector2Int newPosition)
        {
            // Use GameManager's new unified movement method
            Vector2Int fromPosition = GetAvatarPosition(isPlayer);
            gameManager.TryMovePawn(fromPosition, newPosition);
        }

        public List<Vector2Int> GetValidMoves(Vector2Int currentPos)
        {
            List<Vector2Int> validMoves = new List<Vector2Int>();
            
            // Use GridSystem's GetValidMoves which already handles the unified grid logic
            if (gridSystem != null)
            {
                validMoves = gridSystem.GetValidMoves(currentPos);
                
                // Additional check for pawn occupancy since GridSystem may not know about pawn positions
                Vector2Int playerPos = gameManager.playerPosition;
                Vector2Int opponentPos = gameManager.opponentPosition;
                validMoves.RemoveAll(pos => pos == playerPos || pos == opponentPos);
            }
            else
            {
                // Fallback to manual calculation if GridSystem is unavailable
                Vector2Int[] directions = {
                    Vector2Int.up, Vector2Int.down,
                    Vector2Int.left, Vector2Int.right
                };

                foreach (Vector2Int direction in directions)
                {
                    Vector2Int newPos = currentPos + direction;
                    
                    // Check grid bounds
                    if (!IsValidGridPosition(newPos)) continue;
                    
                    // Check tile occupancy - neither player can occupy the same tile
                    Vector2Int playerPos = gameManager.playerPosition;
                    Vector2Int opponentPos = gameManager.opponentPosition;
                    if (newPos == playerPos || newPos == opponentPos) continue;
                    
                    // Check if path is blocked by walls
                    if (IsMovementBlockedByWalls(currentPos, newPos)) continue;
                    
                    validMoves.Add(newPos);
                }
            }

            if (enableDebugLogs)
            {
                Debug.Log($"Valid moves from {currentPos}: [{string.Join(", ", validMoves)}]");
            }

            return validMoves;
        }

        /// <summary>
        /// Check if movement is blocked by walls using unified grid system
        /// In the unified grid, any occupied gap blocks movement regardless of orientation
        /// </summary>
        private bool IsMovementBlockedByWalls(Vector2Int from, Vector2Int to)
        {
            if (gridSystem == null) return false;
            
            Vector2Int diff = to - from;
            
            if (enableDebugLogs)
            {
                Debug.Log($"[MOVEMENT DEBUG] Checking movement from {from} to {to}, diff={diff}");
            }
            
            // Convert tile positions to unified grid positions
            Vector2Int fromUnified = gridSystem.TileToUnifiedPosition(from);
            Vector2Int toUnified = gridSystem.TileToUnifiedPosition(to);
            
            // Calculate the gap position between the two tiles in unified grid
            Vector2Int gapUnified = fromUnified + (toUnified - fromUnified) / 2;
            
            // Check if the gap is occupied (blocked by any wall)
            var gapCell = gridSystem.GetCell(gapUnified);
            if (gapCell != null && gapCell.isOccupied)
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"[MOVEMENT DEBUG] Gap at unified position {gapUnified} is occupied, blocking movement");
                }
                return true;
            }
            
            return false;
        }



        public bool IsValidMove(Vector2Int from, Vector2Int to)
        {
            // Check if it's a single step move
            Vector2Int diff = to - from;
            if (Mathf.Abs(diff.x) + Mathf.Abs(diff.y) != 1) return false;
            
            // Check grid bounds
            if (!IsValidGridPosition(to)) return false;
            
            // Check tile occupancy
            Vector2Int playerPos = gameManager.playerPosition;
            Vector2Int opponentPos = gameManager.opponentPosition;
            if (to == playerPos || to == opponentPos) return false;
            
            // Check wall blocking using corrected logic
            if (IsMovementBlockedByWalls(from, to)) return false;
            
            return true;
        }

        bool IsValidGridPosition(Vector2Int pos)
        {
            return pos.x >= 0 && pos.x < gameManager.gridSize && 
                   pos.y >= 0 && pos.y < gameManager.gridSize;
        }

        public Vector3 GetWorldPosition(Vector2Int gridPos)
        {
            if (gridSystem != null)
            {
                return gridSystem.GridToWorldPosition(gridPos);
            }
            else
            {
                // Fallback calculation
                float spacing = gameManager.tileSize + gameManager.tileGap;
                return new Vector3(gridPos.x * spacing, gridPos.y * spacing, 0);
            }
        }

        public Vector2Int GetGridPosition(Vector3 worldPos)
        {
            if (gridSystem != null)
            {
                return gridSystem.WorldToGridPosition(worldPos);
            }
            else
            {
                // Fallback calculation
                float spacing = gameManager.tileSize + gameManager.tileGap;
                int x = Mathf.RoundToInt(worldPos.x / spacing);
                int y = Mathf.RoundToInt(worldPos.y / spacing);
                return new Vector2Int(Mathf.Clamp(x, 0, gameManager.gridSize - 1), Mathf.Clamp(y, 0, gameManager.gridSize - 1));
            }
        }

        // Alias methods for AvatarDragController compatibility
        public Vector3 GridToWorldPosition(Vector2Int gridPos) => GetWorldPosition(gridPos);
        public Vector2Int WorldToGridPosition(Vector3 worldPos) => GetGridPosition(worldPos);

        #region Public API
        public WallChessGameManager GetGameManager() => gameManager;
        public GridSystem GetGridSystem() => gridSystem;
        public Camera GetMainCamera() => mainCamera;
        #endregion
    }
}
