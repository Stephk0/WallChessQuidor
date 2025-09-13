using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

namespace WallChess
{
    public class PlayerControllerV2 : MonoBehaviour
    {
        private WallChessGameManager gameManager;
        private GridSystem gridSystem;
        private HighlightManager highlightManager;
        private Camera mainCamera;
        
        [Header("Pie Dial Control")]
        [SerializeField] private PieDial pieDial;
        [SerializeField] private bool enablePieDialControl = true;
        [SerializeField] private float directionThreshold = 0.7f; // Minimum magnitude to register direction
        
        [Header("Debug")]
        public bool enableDebugLogs = true; // Fixed compilation issues

        public void Initialize(WallChessGameManager gm)
        {
            gameManager = gm;
            gridSystem = gm.GetGridSystem();
            highlightManager = gm.GetHighlightManager();
            mainCamera = Camera.main;
            
            // Add drag controllers to both avatars
            SetupAvatarDragControllers();
            
            // Setup pie dial if available
            SetupPieDialControl();
            
            if (enableDebugLogs) Debug.Log("PlayerControllerV2 initialized with unified grid system");
        }
        
        #region Pie Dial Control
        
void SetupPieDialControl()
        {
            if (!enablePieDialControl || pieDial == null) return;
            
            // Subscribe to direction confirmed event
            pieDial.OnDirectionConfirmed.AddListener(OnPieDialDirectionConfirmed);
            pieDial.OnDirectionChanged.AddListener(OnPieDialDirectionChanged);
            
            if (enableDebugLogs) Debug.Log("Pie dial control initialized");
        }

void OnPieDialDirectionChanged(Vector2 direction)
        {
            if (!enablePieDialControl) return;
            
            // Only process if it's the current player's turn
            if (!CanMoveAvatar(true)) return;
            
            // Show preview of movement during pie dial interaction
            ShowPieDialMovementPreview(direction);
        }
        
void ShowPieDialMovementPreview(Vector2 direction)
        {
            // Get current player position
            Vector2Int currentPos = GetAvatarPosition(true);
            
            // Get player avatar once for the entire method
            GameObject playerAvatar = gameManager.GetPlayerAvatar();
            if (playerAvatar == null) return;
            
            // Convert direction to grid movement
            Vector2Int gridDirection = ConvertDirectionToGridMovement(direction);
            if (gridDirection == Vector2Int.zero) 
            {
                // If no clear direction, move avatar back to current position
                playerAvatar.transform.position = GetWorldPosition(currentPos);
                return;
            }
            
            Vector2Int targetPos = currentPos + gridDirection;
            
            // Move player avatar to show preview based on direction magnitude
            Vector3 currentWorldPos = GetWorldPosition(currentPos);
            Vector3 targetWorldPos = GetWorldPosition(targetPos);
            
            // Use direction magnitude to interpolate between current and target position
            float lerpFactor = Mathf.Clamp01(direction.magnitude);
            Vector3 previewPos = Vector3.Lerp(currentWorldPos, targetWorldPos, lerpFactor);
            
            playerAvatar.transform.position = previewPos;
        }
        


/// <summary>
        /// Call this when the turn changes to update PieDial position to active player
        /// </summary>
/// <summary>
        /// Call this when the turn changes to enable/disable PieDial based on active player
        /// </summary>
        public void OnTurnChanged()
        {
            if (enablePieDialControl && pieDial != null)
            {
                // Enable/disable PieDial based on if it's the current player's turn
                bool isPlayerTurn = CanMoveAvatar(true);
                pieDial.gameObject.SetActive(isPlayerTurn);
                
                if (enableDebugLogs)
                {
                    Debug.Log($"PieDial updated for turn change. Active: {isPlayerTurn}");
                }
            }
        }


        
void OnPieDialDirectionConfirmed(Vector2 direction)
        {
            if (!enablePieDialControl) return;
            
            // Only process if it's the current player's turn
            if (!CanMoveAvatar(true)) return;
            
            // Reset player avatar to actual position before confirming move
            GameObject playerAvatar = gameManager.GetPlayerAvatar();
            Vector2Int playerCurrentPos = GetAvatarPosition(true);
            if (playerAvatar != null)
            {
                playerAvatar.transform.position = GetWorldPosition(playerCurrentPos);
            }
            
            // Convert direction to grid movement
            Vector2Int gridDirection = ConvertDirectionToGridMovement(direction);
            if (gridDirection == Vector2Int.zero) return;
            
            // Get current player position
            Vector2Int currentPos = GetAvatarPosition(true);
            Vector2Int targetPos = currentPos + gridDirection;
            
            // Validate and execute move
            if (IsValidMove(currentPos, targetPos))
            {
                MoveAvatar(true, targetPos);
                
                if (enableDebugLogs)
                {
                    Debug.Log($"Pie dial move executed: {currentPos} -> {targetPos} (direction: {direction})");
                }
            }
            else
            {
                // Try jump move if direct move is invalid
                List<Vector2Int> validMoves = GetValidMoves(currentPos);
                Vector2Int bestMove = FindBestMoveInDirection(validMoves, currentPos, gridDirection);
                
                if (bestMove != Vector2Int.zero)
                {
                    MoveAvatar(true, bestMove);
                    
                    if (enableDebugLogs)
                    {
                        Debug.Log($"Pie dial jump move executed: {currentPos} -> {bestMove} (direction: {direction})");
                    }
                }
                else if (enableDebugLogs)
                {
                    Debug.Log($"No valid move found for pie dial direction: {direction}");
                }
            }
        }
        
        Vector2Int ConvertDirectionToGridMovement(Vector2 direction)
        {
            // Normalize and check magnitude
            float magnitude = direction.magnitude;
            if (magnitude < directionThreshold) return Vector2Int.zero;
            
            Vector2 normalized = direction / magnitude;
            
            // Convert to 4-directional movement (snap to cardinal directions)
            Vector2Int gridDirection = Vector2Int.zero;
            
            // Determine primary direction
            if (Mathf.Abs(normalized.x) > Mathf.Abs(normalized.y))
            {
                // Horizontal movement
                gridDirection = normalized.x > 0 ? Vector2Int.right : Vector2Int.left;
            }
            else
            {
                // Vertical movement
                gridDirection = normalized.y > 0 ? Vector2Int.up : Vector2Int.down;
            }
            
            return gridDirection;
        }
        
        Vector2Int FindBestMoveInDirection(List<Vector2Int> validMoves, Vector2Int fromPos, Vector2Int preferredDirection)
        {
            Vector2Int bestMove = Vector2Int.zero;
            float bestScore = -1f;
            
            foreach (Vector2Int move in validMoves)
            {
                Vector2Int moveDirection = move - fromPos;
                
                // Calculate how well this move aligns with preferred direction
                Vector2 moveDir = new Vector2(moveDirection.x, moveDirection.y).normalized;
                Vector2 preferredDir = new Vector2(preferredDirection.x, preferredDirection.y).normalized;
                float dotProduct = Vector2.Dot(moveDir, preferredDir);
                
                if (dotProduct > bestScore)
                {
                    bestScore = dotProduct;
                    bestMove = move;
                }
            }
            
            // Only return move if it's reasonably aligned (> 0.5 means < 60 degrees off)
            return bestScore > 0.5f ? bestMove : Vector2Int.zero;
        }
        
        /// <summary>
        /// Public method to manually trigger pie dial movement (for external systems)
        /// </summary>
        public void MovePawnWithPieDial(Vector2 direction)
        {
            OnPieDialDirectionConfirmed(direction);
        }
        
void OnDestroy()
        {
            // Cleanup pie dial event subscriptions
            if (pieDial != null)
            {
                pieDial.OnDirectionConfirmed.RemoveListener(OnPieDialDirectionConfirmed);
                pieDial.OnDirectionChanged.RemoveListener(OnPieDialDirectionChanged);
            }
        }
        
        #endregion
        
                /// <summary>
        /// Reset all avatar drag controllers to their initial state
        /// Called when external events (like wall placement) should cancel any active drags
        /// </summary>
        public void ResetAllAvatarDragControllers()
        {
            // Reset player avatar drag controller
            GameObject playerAvatar = gameManager.GetPlayerAvatar();
            if (playerAvatar != null)
            {
                AvatarDragController playerDrag = playerAvatar.GetComponent<AvatarDragController>();
                if (playerDrag != null)
                {
                    playerDrag.ForceReset();
                }
            }

            // Reset opponent avatar drag controller  
            GameObject opponentAvatar = gameManager.GetOpponentAvatar();
            if (opponentAvatar != null)
            {
                AvatarDragController opponentDrag = opponentAvatar.GetComponent<AvatarDragController>();
                if (opponentDrag != null)
                {
                    opponentDrag.ForceReset();
                }
            }
        }

public void SetupAvatarDragControllers()
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
                
                // Add jump moves over adjacent pawns
                validMoves.AddRange(GetValidJumpMoves(currentPos));
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
                
                // Add jump moves
                validMoves.AddRange(GetValidJumpMoves(currentPos));
            }

            if (enableDebugLogs)
            {
                Debug.Log($"Valid moves from {currentPos}: [{string.Join(", ", validMoves)}]");
            }

            return validMoves;
        }

        /// <summary>
        /// Get valid jump moves when there's an adjacent pawn to jump over
        /// </summary>
        public List<Vector2Int> GetValidJumpMoves(Vector2Int currentPos)
        {
            List<Vector2Int> jumpMoves = new List<Vector2Int>();
            
            Vector2Int playerPos = gameManager.playerPosition;
            Vector2Int opponentPos = gameManager.opponentPosition;
            
            // Define the four cardinal directions
            Vector2Int[] directions = {
                Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
            };
            
            foreach (Vector2Int direction in directions)
            {
                Vector2Int adjacentPos = currentPos + direction;
                
                // Check if there's a pawn in the adjacent position
                if (adjacentPos == playerPos || adjacentPos == opponentPos)
                {
                    // Try to jump over the pawn
                    Vector2Int jumpPos = adjacentPos + direction; // Position behind the pawn
                    
                    // Check if we can jump straight over
                    if (CanJumpTo(currentPos, adjacentPos, jumpPos))
                    {
                        jumpMoves.Add(jumpPos);
                        if (enableDebugLogs)
                        {
                            Debug.Log($"Valid straight jump from {currentPos} over {adjacentPos} to {jumpPos}");
                        }
                    }
                    else
                    {
                        // If we can't jump straight, try diagonal jumps
                        List<Vector2Int> diagonalJumps = GetValidDiagonalJumps(currentPos, adjacentPos, direction);
                        jumpMoves.AddRange(diagonalJumps);
                    }
                }
            }
            
            return jumpMoves;
        }

        /// <summary>
        /// Check if a jump move is valid (straight jump over a pawn)
        /// </summary>
        private bool CanJumpTo(Vector2Int from, Vector2Int over, Vector2Int to)
        {
            // Check if destination is within grid bounds
            if (!IsValidGridPosition(to)) return false;
            
            // Check if destination is not occupied by another pawn
            Vector2Int playerPos = gameManager.playerPosition;
            Vector2Int opponentPos = gameManager.opponentPosition;
            if (to == playerPos || to == opponentPos) return false;
            
            // Check if movement from 'from' to 'over' is blocked by walls
            if (IsMovementBlockedByWalls(from, over)) return false;
            
            // Check if movement from 'over' to 'to' is blocked by walls
            if (IsMovementBlockedByWalls(over, to)) return false;
            
            return true;
        }

        /// <summary>
        /// Get valid diagonal jump moves when straight jump is blocked
        /// </summary>
        private List<Vector2Int> GetValidDiagonalJumps(Vector2Int from, Vector2Int over, Vector2Int jumpDirection)
        {
            List<Vector2Int> diagonalJumps = new List<Vector2Int>();
            
            // Calculate perpendicular directions for diagonal jumps
            Vector2Int[] perpendiculars;
            
            if (jumpDirection == Vector2Int.up || jumpDirection == Vector2Int.down)
            {
                // Jumping vertically, so perpendiculars are left and right
                perpendiculars = new Vector2Int[] { Vector2Int.left, Vector2Int.right };
            }
            else
            {
                // Jumping horizontally, so perpendiculars are up and down
                perpendiculars = new Vector2Int[] { Vector2Int.up, Vector2Int.down };
            }
            
            foreach (Vector2Int perpendicular in perpendiculars)
            {
                Vector2Int diagonalPos = over + perpendicular;
                
                // Check if diagonal jump is valid
                if (CanJumpTo(from, over, diagonalPos))
                {
                    diagonalJumps.Add(diagonalPos);
                    if (enableDebugLogs)
                    {
                        Debug.Log($"Valid diagonal jump from {from} over {over} to {diagonalPos}");
                    }
                }
            }
            
            return diagonalJumps;
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
            // Check if it's a single step move or a jump move
            Vector2Int diff = to - from;
            int distance = Mathf.Abs(diff.x) + Mathf.Abs(diff.y);
            
            if (distance == 1)
            {
                // Single step move
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
            else if (distance == 2)
            {
                // Potential jump move - check if it's in our valid jump moves
                List<Vector2Int> validJumps = GetValidJumpMoves(from);
                return validJumps.Contains(to);
            }
            
            // Invalid distance or move type
            return false;
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

        public Vector2Int WorldToGridPosition(Vector3 worldPos) => GetGridPosition(worldPos);
        public Vector3 GridToWorldPosition(Vector2Int gridPos) => GetWorldPosition(gridPos);
        #region Public API
        public WallChessGameManager GetGameManager() => gameManager;
        public GridSystem GetGridSystem() => gridSystem;
        public HighlightManager GetHighlightManager() => highlightManager;
        public Camera GetMainCamera() => mainCamera;
        #endregion
    }
}