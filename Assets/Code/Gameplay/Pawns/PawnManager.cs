using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using WallChess.Core.States;

namespace WallChess.Gameplay.Pawns
{
    /// <summary>
    /// Manages all pawn-related functionality extracted from WallChessGameManager.
    /// Part of Phase 3: God Object Extraction (MVP Implementation Guide)
    /// </summary>
    public class PawnManager : MonoBehaviour
    {
        #region Types
        [System.Serializable]
        public class PawnData
        {
            public int playerId;
            public Vector2Int currentPosition;
            public Vector2Int startPosition;
            public Vector2Int goalPosition;
            public GameObject pawnObject;
            public bool isAI;
            public bool isActive;
            public Material pawnMaterial;
            
            public PawnData(int id, Vector2Int start, Vector2Int goal)
            {
                playerId = id;
                startPosition = start;
                currentPosition = start;
                goalPosition = goal;
                isActive = true;
                isAI = false;
            }
            
            public bool HasReachedGoal()
            {
                // Check if pawn reached its goal row/column
                if (playerId == 0 || playerId == 1)
                {
                    // Players 0 and 1 move vertically
                    return currentPosition.y == goalPosition.y;
                }
                else
                {
                    // Players 2 and 3 move horizontally
                    return currentPosition.x == goalPosition.x;
                }
            }
        }
        
        public class MoveResult
        {
            public bool success;
            public Vector2Int fromPosition;
            public Vector2Int toPosition;
            public int playerId;
            public bool isJump;
            public bool winConditionMet;
            
            public MoveResult(bool success = false)
            {
                this.success = success;
            }
        }
        #endregion
        
        #region Configuration
        [Header("Pawn Settings")]
        [SerializeField] private GameObject pawnPrefab;
        [SerializeField] private Material[] playerMaterials;
        [SerializeField] private float pawnHeight = 0.5f;
        [SerializeField] private float moveAnimationDuration = 0.3f;
        
        [Header("Board Configuration")]
        [SerializeField] private int boardSize = 9;
        [SerializeField] private float tileSize = 1f;
        #endregion
        
        #region State
        private List<PawnData> pawns = new List<PawnData>();
        private PawnData selectedPawn = null;
        private List<Vector2Int> validMoves = new List<Vector2Int>();
        private GameObject moveHighlight;
        
        // References
        private GridSystem gridSystem;
        private UnifiedStateManager stateManager;
        #endregion
        
        #region Events
        public delegate void PawnEventHandler(int playerId, Vector2Int position);
        public static event PawnEventHandler OnPawnMoved;
        public static event PawnEventHandler OnPawnSelected;
        
        public delegate void WinEventHandler(int playerId);
        public static event WinEventHandler OnPlayerReachedGoal;
        #endregion
        
        #region Unity Lifecycle
        void Awake()
        {
            gridSystem = FindObjectOfType<GridSystem>();
            stateManager = UnifiedStateManager.Instance;
        }
        
        void Start()
        {
            InitializePawns();
        }
        
        void Update()
        {
            if (stateManager != null && stateManager.IsInState(UnifiedStateManager.StateType.PawnSelection))
            {
                HandlePawnSelection();
            }
            else if (stateManager != null && stateManager.IsInState(UnifiedStateManager.StateType.PawnMoving))
            {
                HandlePawnMovement();
            }
        }
        #endregion
        
        #region Initialization
        /// <summary>
        /// Initialize pawns for the specified number of players
        /// </summary>
        public void InitializePawns(int playerCount = 2)
        {
            ClearPawns();
            
            // Standard Quoridor starting positions
            Vector2Int[] startPositions = new Vector2Int[]
            {
                new Vector2Int(4, 0),     // Player 0 - Bottom
                new Vector2Int(4, 8),     // Player 1 - Top
                new Vector2Int(0, 4),     // Player 2 - Left
                new Vector2Int(8, 4)      // Player 3 - Right
            };
            
            Vector2Int[] goalPositions = new Vector2Int[]
            {
                new Vector2Int(4, 8),     // Player 0 goal - Top
                new Vector2Int(4, 0),     // Player 1 goal - Bottom
                new Vector2Int(8, 4),     // Player 2 goal - Right
                new Vector2Int(0, 4)      // Player 3 goal - Left
            };
            
            playerCount = Mathf.Clamp(playerCount, 2, 4);
            
            for (int i = 0; i < playerCount; i++)
            {
                CreatePawn(i, startPositions[i], goalPositions[i]);
            }
            
            Debug.Log($"[PawnManager] Initialized {playerCount} pawns");
        }
        
        private PawnData CreatePawn(int playerId, Vector2Int startPos, Vector2Int goalPos)
        {
            var pawnData = new PawnData(playerId, startPos, goalPos);
            
            // Create pawn GameObject
            if (pawnPrefab != null)
            {
                Vector3 worldPos = GridToWorld(startPos);
                pawnData.pawnObject = Instantiate(pawnPrefab, worldPos, Quaternion.identity);
                pawnData.pawnObject.name = $"Pawn_Player{playerId}";
                
                // Set material
                if (playerMaterials != null && playerMaterials.Length > playerId)
                {
                    var renderer = pawnData.pawnObject.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        renderer.material = playerMaterials[playerId];
                        pawnData.pawnMaterial = playerMaterials[playerId];
                    }
                }
            }
            
            pawns.Add(pawnData);
            return pawnData;
        }
        
        private void ClearPawns()
        {
            foreach (var pawn in pawns)
            {
                if (pawn.pawnObject != null)
                {
                    Destroy(pawn.pawnObject);
                }
            }
            pawns.Clear();
        }
        #endregion
        
        #region Public API
        /// <summary>
        /// Get pawn data for a specific player
        /// </summary>
        public PawnData GetPawn(int playerId)
        {
            return pawns.FirstOrDefault(p => p.playerId == playerId);
        }
        
        /// <summary>
        /// Get the currently active pawn
        /// </summary>
        public PawnData GetActivePawn()
        {
            int currentPlayer = stateManager?.GetCurrentPlayerId() ?? 0;
            return GetPawn(currentPlayer);
        }
        
        /// <summary>
        /// Move a pawn to a new position
        /// </summary>
        public MoveResult MovePawn(int playerId, Vector2Int targetPosition)
        {
            var pawn = GetPawn(playerId);
            if (pawn == null)
            {
                Debug.LogWarning($"[PawnManager] Pawn for player {playerId} not found");
                return new MoveResult(false);
            }
            
            // Validate move
            if (!IsValidMove(pawn, targetPosition))
            {
                Debug.LogWarning($"[PawnManager] Invalid move from {pawn.currentPosition} to {targetPosition}");
                return new MoveResult(false);
            }
            
            // Check if it's a jump
            int distance = GetManhattanDistance(pawn.currentPosition, targetPosition);
            bool isJump = distance > 1;
            
            // Store move data
            var result = new MoveResult(true)
            {
                fromPosition = pawn.currentPosition,
                toPosition = targetPosition,
                playerId = playerId,
                isJump = isJump
            };
            
            // Update position
            pawn.currentPosition = targetPosition;
            
            // Animate pawn
            if (pawn.pawnObject != null)
            {
                Vector3 newWorldPos = GridToWorld(targetPosition);
                StartCoroutine(AnimatePawnMove(pawn.pawnObject, newWorldPos, moveAnimationDuration));
            }
            
            // Check win condition
            if (pawn.HasReachedGoal())
            {
                result.winConditionMet = true;
                OnPlayerReachedGoal?.Invoke(playerId);
            }
            
            // Fire event
            OnPawnMoved?.Invoke(playerId, targetPosition);
            
            return result;
        }
        
        /// <summary>
        /// Get valid moves for a pawn
        /// </summary>
        public List<Vector2Int> GetValidMoves(int playerId)
        {
            var pawn = GetPawn(playerId);
            if (pawn == null) return new List<Vector2Int>();
            
            return GetValidMovesForPawn(pawn);
        }
        
        /// <summary>
        /// Check if a position is occupied by any pawn
        /// </summary>
        public bool IsPositionOccupied(Vector2Int position)
        {
            return pawns.Any(p => p.currentPosition == position && p.isActive);
        }
        
        /// <summary>
        /// Get pawn at a specific position
        /// </summary>
        public PawnData GetPawnAtPosition(Vector2Int position)
        {
            return pawns.FirstOrDefault(p => p.currentPosition == position && p.isActive);
        }
        
        /// <summary>
        /// Select a pawn for movement
        /// </summary>
        public bool SelectPawn(int playerId)
        {
            var pawn = GetPawn(playerId);
            if (pawn == null || !pawn.isActive) return false;
            
            selectedPawn = pawn;
            validMoves = GetValidMovesForPawn(pawn);
            
            // Highlight valid moves
            HighlightValidMoves(validMoves);
            
            OnPawnSelected?.Invoke(playerId, pawn.currentPosition);
            return true;
        }
        
        /// <summary>
        /// Deselect current pawn
        /// </summary>
        public void DeselectPawn()
        {
            selectedPawn = null;
            validMoves.Clear();
            ClearHighlights();
        }
        
        /// <summary>
        /// Reset all pawns to starting positions
        /// </summary>
        public void ResetPawns()
        {
            foreach (var pawn in pawns)
            {
                pawn.currentPosition = pawn.startPosition;
                if (pawn.pawnObject != null)
                {
                    pawn.pawnObject.transform.position = GridToWorld(pawn.startPosition);
                }
            }
        }
        
        /// <summary>
        /// Set AI control for a player
        /// </summary>
        public void SetPlayerAI(int playerId, bool isAI)
        {
            var pawn = GetPawn(playerId);
            if (pawn != null)
            {
                pawn.isAI = isAI;
            }
        }
        #endregion
        
        #region Private Methods - Movement Validation
        private List<Vector2Int> GetValidMovesForPawn(PawnData pawn)
        {
            List<Vector2Int> moves = new List<Vector2Int>();
            
            // Check all four directions
            Vector2Int[] directions = {
                Vector2Int.up,
                Vector2Int.down,
                Vector2Int.left,
                Vector2Int.right
            };
            
            foreach (var dir in directions)
            {
                Vector2Int targetPos = pawn.currentPosition + dir;
                
                // Check board bounds
                if (!IsInBounds(targetPos)) continue;
                
                // Check for walls blocking the path
                if (IsWallBlocking(pawn.currentPosition, targetPos)) continue;
                
                // Check for other pawns
                var otherPawn = GetPawnAtPosition(targetPos);
                if (otherPawn != null)
                {
                    // Check for jump over opponent
                    Vector2Int jumpPos = targetPos + dir;
                    if (IsInBounds(jumpPos) && 
                        !IsPositionOccupied(jumpPos) &&
                        !IsWallBlocking(targetPos, jumpPos))
                    {
                        moves.Add(jumpPos);
                    }
                    
                    // Check for diagonal moves if straight jump is blocked
                    if (IsWallBlocking(targetPos, jumpPos) || !IsInBounds(jumpPos))
                    {
                        // Check diagonal moves
                        Vector2Int[] perpendiculars = GetPerpendicularDirections(dir);
                        foreach (var perp in perpendiculars)
                        {
                            Vector2Int diagPos = targetPos + perp;
                            if (IsInBounds(diagPos) && 
                                !IsPositionOccupied(diagPos) &&
                                !IsWallBlocking(targetPos, diagPos))
                            {
                                moves.Add(diagPos);
                            }
                        }
                    }
                }
                else
                {
                    // Normal move to empty square
                    moves.Add(targetPos);
                }
            }
            
            return moves;
        }
        
        private bool IsValidMove(PawnData pawn, Vector2Int targetPosition)
        {
            var validMoves = GetValidMovesForPawn(pawn);
            return validMoves.Contains(targetPosition);
        }
        
        private bool IsInBounds(Vector2Int position)
        {
            return position.x >= 0 && position.x < boardSize &&
                   position.y >= 0 && position.y < boardSize;
        }
        
        private bool IsWallBlocking(Vector2Int from, Vector2Int to)
        {
            // Check with grid system for wall blocking
            if (gridSystem != null)
            {
                return gridSystem.IsWallBetween(from, to);
            }
            
            // Fallback - no walls
            return false;
        }
        
        private Vector2Int[] GetPerpendicularDirections(Vector2Int direction)
        {
            if (direction == Vector2Int.up || direction == Vector2Int.down)
            {
                return new Vector2Int[] { Vector2Int.left, Vector2Int.right };
            }
            else
            {
                return new Vector2Int[] { Vector2Int.up, Vector2Int.down };
            }
        }
        
        private int GetManhattanDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }
        #endregion
        
        #region Private Methods - Input Handling
        private void HandlePawnSelection()
        {
            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(UnityEngine.Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    // Check if hit a pawn
                    var pawnObj = hit.collider.GetComponent<PawnIdentifier>();
                    if (pawnObj != null)
                    {
                        int playerId = pawnObj.PlayerId;
                        if (playerId == stateManager.GetCurrentPlayerId())
                        {
                            SelectPawn(playerId);
                            stateManager.RequestStateChange(UnifiedStateManager.StateType.PawnMoving);
                        }
                    }
                }
            }
        }
        
        private void HandlePawnMovement()
        {
            if (selectedPawn == null) return;
            
            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(UnityEngine.Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    Vector2Int gridPos = WorldToGrid(hit.point);
                    
                    if (validMoves.Contains(gridPos))
                    {
                        var result = MovePawn(selectedPawn.playerId, gridPos);
                        if (result.success)
                        {
                            DeselectPawn();
                            
                            if (result.winConditionMet)
                            {
                                stateManager.RequestStateChange(UnifiedStateManager.StateType.GameOver);
                            }
                            else
                            {
                                stateManager.RequestStateChange(UnifiedStateManager.StateType.TurnValidation);
                            }
                        }
                    }
                }
            }
            
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
            {
                DeselectPawn();
                stateManager.RequestStateChange(UnifiedStateManager.StateType.PlayerTurn);
            }
        }
        #endregion
        
        #region Private Methods - Visual
        private void HighlightValidMoves(List<Vector2Int> moves)
        {
            ClearHighlights();
            
            // Create highlights for each valid move
            foreach (var move in moves)
            {
                // This would create visual highlights
                // Implementation depends on your highlighting system
            }
        }
        
        private void ClearHighlights()
        {
            // Clear all move highlights
        }
        
        private System.Collections.IEnumerator AnimatePawnMove(GameObject pawn, Vector3 targetPos, float duration)
        {
            Vector3 startPos = pawn.transform.position;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                // Add a small jump arc
                Vector3 pos = Vector3.Lerp(startPos, targetPos, t);
                pos.y += Mathf.Sin(t * Mathf.PI) * 0.2f;
                
                pawn.transform.position = pos;
                yield return null;
            }
            
            pawn.transform.position = targetPos;
        }
        #endregion
        
        #region Utility
        private Vector3 GridToWorld(Vector2Int gridPos)
        {
            return new Vector3(gridPos.x * tileSize, pawnHeight, gridPos.y * tileSize);
        }
        
        private Vector2Int WorldToGrid(Vector3 worldPos)
        {
            return new Vector2Int(
                Mathf.RoundToInt(worldPos.x / tileSize),
                Mathf.RoundToInt(worldPos.z / tileSize)
            );
        }
        #endregion
    }
    
    /// <summary>
    /// Component to identify pawns in the scene
    /// </summary>
    public class PawnIdentifier : MonoBehaviour
    {
        public int PlayerId { get; set; }
    }
}