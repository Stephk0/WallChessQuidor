using UnityEngine;
using System.Collections.Generic;

namespace WallChess
{
    public class PlayerControllerV2 : MonoBehaviour
    {
        private WallChessGameManager gameManager;
        private GridSystem gridSystem;
        private WallManager wallManager;
        private Camera mainCamera;
        
        [Header("Debug")]
        public bool enableDebugLogs = true;

        public void Initialize(WallChessGameManager gm)
        {
            gameManager = gm;
            gridSystem = gm.GetGridSystem();
            wallManager = gm.GetComponent<WallManager>();
            mainCamera = Camera.main;
            
            // Add drag controllers to both avatars
            SetupAvatarDragControllers();
            
            if (enableDebugLogs) Debug.Log("PlayerControllerV2 initialized with drag system");
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
                
                // Check if path is blocked by walls using corrected wall blocking logic
                if (IsMovementBlockedByWalls(currentPos, newPos)) continue;
                
                validMoves.Add(newPos);
            }

            if (enableDebugLogs)
            {
                Debug.Log($"Valid moves from {currentPos}: [{string.Join(", ", validMoves)}]");
            }

            return validMoves;
        }

        /// <summary>
        /// FIXED: Corrected wall blocking check that maps tile coordinates to gap coordinates properly
        /// </summary>
        private bool IsMovementBlockedByWalls(Vector2Int from, Vector2Int to)
        {
            if (wallManager == null) return false;
            
            Vector2Int diff = to - from;
            
            if (enableDebugLogs)
            {
                Debug.Log($"[MOVEMENT DEBUG] Checking movement from {from} to {to}, diff={diff}");
            }
            
            // Determine which gap to check based on movement direction
            if (diff.y == 1) // Moving up
            {
                // Check horizontal wall between rows from.y and to.y
                // Horizontal gap is located at the lower row position
                int gapX = from.x;
                int gapY = from.y; // Gap is at the source row for upward movement
                
                return IsHorizontalWallBlocking(gapX, gapY);
            }
            else if (diff.y == -1) // Moving down
            {
                // Check horizontal wall between rows to.y and from.y  
                // Horizontal gap is located at the lower row position
                int gapX = from.x;
                int gapY = to.y; // Gap is at the target row for downward movement
                
                return IsHorizontalWallBlocking(gapX, gapY);
            }
            else if (diff.x == 1) // Moving right
            {
                // Check vertical wall between columns from.x and to.x
                // Vertical gap is located at the left column position
                int gapX = from.x; // Gap is at the source column for rightward movement  
                int gapY = from.y;
                
                return IsVerticalWallBlocking(gapX, gapY);
            }
            else if (diff.x == -1) // Moving left
            {
                // Check vertical wall between columns to.x and from.x
                // Vertical gap is located at the left column position  
                int gapX = to.x; // Gap is at the target column for leftward movement
                int gapY = from.y;
                
                return IsVerticalWallBlocking(gapX, gapY);
            }
            
            return false; // Invalid movement direction
        }

        /// <summary>
        /// Check if a horizontal wall is blocking movement
        /// A horizontal wall blocks if BOTH of its gap positions are occupied
        /// FIXED: Check both possible wall positions that could block this gap
        /// </summary>
        private bool IsHorizontalWallBlocking(int gapX, int gapY)
        {
            // Check bounds first  
            if (gapX < 0 || gapY < 0) return false;
            
            // A horizontal wall can block movement at gapX in two ways:
            // 1. Wall starts at gapX: occupies (gapX, gapY) and (gapX+1, gapY)
            // 2. Wall starts at gapX-1: occupies (gapX-1, gapY) and (gapX, gapY)
            
            WallState wallState = wallManager.GetWallState();
            if (wallState == null) return false;
            
            // Check possibility 1: Wall starts at gapX
            bool wall1Left = wallState.IsOccupied(WallState.Orientation.Horizontal, gapX, gapY);
            bool wall1Right = wallState.IsOccupied(WallState.Orientation.Horizontal, gapX + 1, gapY);
            if (wall1Left && wall1Right)
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"[HORIZONTAL DEBUG] Wall found starting at ({gapX},{gapY}): left={wall1Left}, right={wall1Right}");
                }
                return true;
            }
            
            // Check possibility 2: Wall starts at gapX-1  
            if (gapX - 1 >= 0)
            {
                bool wall2Left = wallState.IsOccupied(WallState.Orientation.Horizontal, gapX - 1, gapY);
                bool wall2Right = wallState.IsOccupied(WallState.Orientation.Horizontal, gapX, gapY);
                if (wall2Left && wall2Right)
                {
                    if (enableDebugLogs)
                    {
                        Debug.Log($"[HORIZONTAL DEBUG] Wall found starting at ({gapX - 1},{gapY}): left={wall2Left}, right={wall2Right}");
                    }
                    return true;
                }
            }
            
            return false; // No wall blocks this position
        }

        /// <summary>
        /// Check if a vertical wall is blocking movement
        /// A vertical wall blocks if BOTH of its gap positions are occupied
        /// FIXED: Check both possible wall positions that could block this gap
        /// </summary>
        private bool IsVerticalWallBlocking(int gapX, int gapY)
        {
            // Check bounds first
            if (gapX < 0 || gapY < 0) return false;
            
            WallState wallState = wallManager.GetWallState();
            if (wallState == null) return false;
            
            // A vertical wall can block movement at gapY in two ways:
            // 1. Wall starts at gapY: occupies (gapX, gapY) and (gapX, gapY+1)
            // 2. Wall starts at gapY-1: occupies (gapX, gapY-1) and (gapX, gapY)
            
            // Check possibility 1: Wall starts at gapY
            bool wall1Bottom = wallState.IsOccupied(WallState.Orientation.Vertical, gapX, gapY);
            bool wall1Top = wallState.IsOccupied(WallState.Orientation.Vertical, gapX, gapY + 1);
            if (wall1Bottom && wall1Top)
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"[VERTICAL DEBUG] Wall found starting at ({gapX},{gapY}): bottom={wall1Bottom}, top={wall1Top}");
                }
                return true;
            }
            
            // Check possibility 2: Wall starts at gapY-1
            if (gapY - 1 >= 0)
            {
                bool wall2Bottom = wallState.IsOccupied(WallState.Orientation.Vertical, gapX, gapY - 1);
                bool wall2Top = wallState.IsOccupied(WallState.Orientation.Vertical, gapX, gapY);
                if (wall2Bottom && wall2Top)
                {
                    if (enableDebugLogs)
                    {
                        Debug.Log($"[VERTICAL DEBUG] Wall found starting at ({gapX},{gapY - 1}): bottom={wall2Bottom}, top={wall2Top}");
                    }
                    return true;
                }
            }
            
            return false; // No wall blocks this position
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
        public WallManager GetWallManager() => wallManager;
        public Camera GetMainCamera() => mainCamera;
        #endregion
    }
}
