using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;
using WallChess.Core.PlayerActionStates;

using WallChess.Gameplay.Pawns;

namespace WallChess
{
    /// <summary>
    /// Refactored Pawn Controller - Uses proper PawnManager system
    /// Handles input and movement coordination for pawns
    /// Clean separation from direct game manager dependencies
    /// </summary>
    public class PawnController : MonoBehaviour
    {
        [Header("Pie Dial Control")]
        [SerializeField] private PieDial pieDial;
        [SerializeField] private bool enablePieDialControl = true;
        [SerializeField] private float directionThreshold = 0.7f;
        
        [Header("Player Action State Machine")]
        [SerializeField] private PlayerActionStateMachine actionStateMachine = null;
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;
        
        // Core references
        private PawnManager pawnManager;
        private GridSystem gridSystem;
        private HighlightManager highlightManager;
        private Camera mainCamera;
        
        // Pie dial state
        private bool isPieDialConfirmHighlightActive = false;
        private Vector2Int currentConfirmHighlightPosition = Vector2Int.zero;
        
        // Events for coordination
        public static System.Action<WallChess.Gameplay.Pawns.Pawn, Vector2Int> OnPawnMoveRequested;
        public static System.Action OnTurnChanged;

        #region Initialization
        
public void Initialize(PawnManager manager, GridSystem grid, HighlightManager highlights)
        {
            pawnManager = manager;
            gridSystem = grid;
            highlightManager = highlights;
            mainCamera = Camera.main;
            
            if (pawnManager == null)
            {
                LogError("PawnManager is required but not provided!");
                return;
            }
            
            // Initialize Player Action State Machine
            if (actionStateMachine == null)
            {
                actionStateMachine = gameObject.AddComponent<PlayerActionStateMachine>();
            }
            
            // Subscribe to action state machine events
            actionStateMachine.OnActionConfirmed += OnPlayerActionConfirmed;
            actionStateMachine.OnActionCancelled += OnPlayerActionCancelled;
            
            SetupPieDialControl();
            SetupAvatarDragControllers();
            
            LogInfo("PawnController initialized with PawnManager system and Player Action FSM");
        }
        
        void SetupPieDialControl()
        {
            if (!enablePieDialControl || pieDial == null) return;
            
            pieDial.OnDirectionConfirmed.AddListener(OnPieDialDirectionConfirmed);
            pieDial.OnDirectionChanged.AddListener(OnPieDialDirectionChanged);
            pieDial.OnDirectionCancelled.AddListener(OnPieDialCancelled);
            
            LogInfo("Pie dial control initialized");
        }
        
        void SetupAvatarDragControllers()
        {
            if (pawnManager == null) return;
            
            // Setup drag controllers for all pawns
            foreach (var pawn in pawnManager.Pawns)
            {
                SetupDragControllerForPawn(pawn);
            }
            
            // Listen for new pawns being added
            PawnManager.OnPawnInitialized += HandlePawnInitialized;
        }
        
void SetupDragControllerForPawn(WallChess.Gameplay.Pawns.Pawn pawn)
        {
            if (pawn?.gameObject == null) return;
            
            AvatarDragController dragController = pawn.GetComponent<AvatarDragController>();
            if (dragController == null)
            {
                dragController = pawn.gameObject.AddComponent<AvatarDragController>();
            }
            
            // Initialize with pawn index and human status
            int pawnIndex = pawnManager.Pawns.IndexOf(pawn);
            bool isHuman = pawn.IsHuman;
            
            dragController.Initialize(this, isHuman);
            LogInfo($"Setup drag controller for {pawn.PlayerData.playerName}");
        }
        
        #endregion
        
        #region Turn Management
        
        /// <summary>
        /// Called when turn changes - updates pie dial state and controllers
        /// </summary>
        public void HandleTurnChanged()
        {
            if (enablePieDialControl && pieDial != null)
            {
                WallChess.Gameplay.Pawns.Pawn activePawn = pawnManager.ActivePawn;
                bool isPieDialActive = activePawn != null && activePawn.IsHuman;
                
                pieDial.gameObject.SetActive(isPieDialActive);
                
                if (isPieDialActive)
                {
                    // Position pie dial at active pawn's location
                    Vector3 worldPos = gridSystem.GridToWorldPosition(activePawn.CurrentPosition);
                    pieDial.transform.position = worldPos;
                }
            }
            
            // Clear any existing highlights
            ClearHighlights();
            
            // Show valid move highlights for the new active pawn
            ShowValidMovesForActivePawn();
            
            // Update all drag controllers' active state
            RefreshDragControllers();
            
            OnTurnChanged?.Invoke();
            LogInfo($"Turn changed to: {pawnManager.ActivePawn?.PlayerData.playerName}");
        }
        
        void RefreshDragControllers()
        {
            if (pawnManager == null) return;
            
            foreach (var pawn in pawnManager.Pawns)
            {
                AvatarDragController dragController = pawn.GetComponent<AvatarDragController>();
                if (dragController != null)
                {
                    dragController.RefreshActiveState();
                }
            }
        }
        
        #endregion
        
        #region Pie Dial Control
        
        void OnPieDialDirectionChanged(Vector2 direction)
        {
            if (!enablePieDialControl || !CanMoveActivePawn()) return;
            
            ShowPieDialMovementPreview(direction);
        }
        
        void ShowPieDialMovementPreview(Vector2 direction)
        {
            WallChess.Gameplay.Pawns.Pawn activePawn = pawnManager.ActivePawn;
            if (activePawn == null) return;
            
            Vector2Int currentPos = activePawn.CurrentPosition;
            Vector3 currentWorldPos = gridSystem.GridToWorldPosition(currentPos);
            
            // If direction is too small, snap back to center
            if (direction.magnitude < directionThreshold)
            {
                activePawn.transform.position = currentWorldPos;
                ClearHighlights();
                return;
            }
            
            // Smooth circular movement
            float tileSpacing = gridSystem.GetTileSpacing();
            Vector3 smoothOffset = new Vector3(direction.x, direction.y, 0f) * tileSpacing;
            Vector3 smoothPreviewPos = currentWorldPos + smoothOffset;
            
            activePawn.transform.position = smoothPreviewPos;
            
            // Show highlight when near confirm threshold
            float confirmThreshold = pieDial?.GetConfirmMagnitudeNormalized() ?? 1.0f;
            if (direction.magnitude >= (confirmThreshold * 0.8f))
            {
                Vector2Int gridDirection = ConvertDirectionToGridMovement(direction);
                if (gridDirection != Vector2Int.zero)
                {
                    Vector2Int targetPos = currentPos + gridDirection;
                    if (IsValidMove(currentPos, targetPos))
                    {
                        ShowConfirmHighlight(targetPos);
                    }
                }
            }
        }
        
        void OnPieDialDirectionConfirmed(Vector2 direction)
        {
            if (!enablePieDialControl || !CanMoveActivePawn()) return;
            
            WallChess.Gameplay.Pawns.Pawn activePawn = pawnManager.ActivePawn;
            Vector2Int currentPos = activePawn.CurrentPosition;
            
            ClearHighlights();
            
            Vector2Int gridDirection = ConvertDirectionToGridMovement(direction);
            if (gridDirection == Vector2Int.zero)
            {
                // Snap back to current position
                activePawn.SnapToPosition(currentPos);
                return;
            }
            
            Vector2Int targetPos = currentPos + gridDirection;
            
            // Try direct move first
            if (IsValidMove(currentPos, targetPos))
            {
                MovePawn(activePawn, targetPos);
                LogInfo($"Pie dial move: {currentPos} -> {targetPos}");
                return;
            }
            
            // Try jump move if direct move fails
            List<Vector2Int> validMoves = GetValidMoves(currentPos);
            Vector2Int bestMove = FindBestMoveInDirection(validMoves, currentPos, gridDirection);
            
            if (bestMove != Vector2Int.zero)
            {
                MovePawn(activePawn, bestMove);
                LogInfo($"Pie dial jump move: {currentPos} -> {bestMove}");
            }
            else
            {
                // No valid move - snap back
                activePawn.SnapToPosition(currentPos);
                LogInfo("No valid move found - pawn snapped back");
            }
        }
        
        void OnPieDialCancelled()
        {
            if (!enablePieDialControl) return;
            
            WallChess.Gameplay.Pawns.Pawn activePawn = pawnManager.ActivePawn;
            if (activePawn != null)
            {
                activePawn.SnapToPosition(activePawn.CurrentPosition);
            }
            
            ClearHighlights();
        }
        
        #endregion
        
        #region Movement Logic
        
        /// <summary>
        /// Check if the currently active pawn can move
        /// </summary>
        public bool CanMoveActivePawn()
        {
            WallChess.Gameplay.Pawns.Pawn activePawn = pawnManager.ActivePawn;
            return activePawn != null && activePawn.IsHuman && !activePawn.IsMoving;
        }
        
        /// <summary>
        /// Execute movement for a pawn
        /// </summary>
public void MovePawn(WallChess.Gameplay.Pawns.Pawn pawn, Vector2Int targetPosition)
        {
            if (pawn == null) return;
            
            // Validate move
            if (!IsValidMove(pawn.CurrentPosition, targetPosition))
            {
                LogWarning($"Invalid move attempted: {pawn.CurrentPosition} -> {targetPosition}");
                return;
            }
            
            // Start move using Player Action FSM
            if (actionStateMachine != null && actionStateMachine.IsIdle())
            {
                actionStateMachine.Initialize(pawn);
                actionStateMachine.StartPawnMove(targetPosition);
            }
            
            // Execute the move
            Vector2Int oldPosition = pawn.CurrentPosition;
            pawn.MoveTo(targetPosition);
            
            // Confirm action through FSM (will trigger turn advancement)
            if (actionStateMachine != null)
            {
                actionStateMachine.ConfirmAction();
            }
            
            LogInfo($"Pawn moved from {oldPosition} to {targetPosition} via Player Action FSM");
        }

private void OnPlayerActionConfirmed()
        {
            LogInfo("Player action confirmed - executing action");
            
            // Get pending action from state machine
            var moveTarget = actionStateMachine?.GetPendingMoveTarget();
            var wallPosition = actionStateMachine?.GetPendingWallPosition();
            
            if (moveTarget.HasValue)
            {
                // Pawn movement was confirmed
                var activePawn = pawnManager?.ActivePawn;
                if (activePawn != null)
                {
                    // Fire move completion event - this triggers turn advancement
                    WallChessGameManager.OnPawnMoveComplete?.Invoke(activePawn, moveTarget.Value, true);
                    LogInfo($"Pawn move complete event fired: {activePawn.CurrentPosition} -> {moveTarget.Value}");
                }
            }
            else if (wallPosition.HasValue)
            {
                // Wall placement was confirmed - already handled by WallManager events
                LogInfo("Wall placement confirmed via Player Action FSM");
            }
        }
        
        private void OnPlayerActionCancelled()
        {
            LogInfo("Player action cancelled - reverting to previous state");
            
            // Return pawn to original position if moving
            var activePawn = pawnManager?.ActivePawn;
            if (activePawn != null)
            {
                activePawn.SnapToPosition(activePawn.CurrentPosition);
            }
            
            // Clear highlights
            ClearHighlights();
        }

        
        /// <summary>
        /// Get valid moves for a position
        /// </summary>
public List<Vector2Int> GetValidMoves(Vector2Int position)
        {
            if (gridSystem == null) return new List<Vector2Int>();
            
            // GridSystem already handles jump moves and wall blocking correctly
            // No need to add redundant jump logic here
            return gridSystem.GetValidMoves(position);
        }
        

        

        

        
public bool IsValidMove(Vector2Int from, Vector2Int to)
        {
            // Use GridSystem to check if move is valid (includes wall checking and jump logic)
            List<Vector2Int> validMoves = GetValidMoves(from);
            return validMoves.Contains(to);
        }
        
        bool IsValidGridPosition(Vector2Int pos)
        {
            int gridSize = gridSystem?.GetGridSize() ?? 9;
            return pos.x >= 0 && pos.x < gridSize && pos.y >= 0 && pos.y < gridSize;
        }
        
        #endregion
        
        #region Utility Methods
        
        Vector2Int ConvertDirectionToGridMovement(Vector2 direction)
        {
            float magnitude = direction.magnitude;
            if (magnitude < directionThreshold) return Vector2Int.zero;
            
            Vector2 normalized = direction / magnitude;
            
            if (Mathf.Abs(normalized.x) > Mathf.Abs(normalized.y))
                return normalized.x > 0 ? Vector2Int.right : Vector2Int.left;
            else
                return normalized.y > 0 ? Vector2Int.up : Vector2Int.down;
        }
        
        Vector2Int FindBestMoveInDirection(List<Vector2Int> validMoves, Vector2Int fromPos, Vector2Int preferredDirection)
        {
            Vector2Int bestMove = Vector2Int.zero;
            float bestScore = -1f;
            
            foreach (Vector2Int move in validMoves)
            {
                Vector2Int moveDirection = move - fromPos;
                Vector2 moveDir = new Vector2(moveDirection.x, moveDirection.y).normalized;
                Vector2 preferredDir = new Vector2(preferredDirection.x, preferredDirection.y).normalized;
                float dotProduct = Vector2.Dot(moveDir, preferredDir);
                
                if (dotProduct > bestScore)
                {
                    bestScore = dotProduct;
                    bestMove = move;
                }
            }
            
            return bestScore > 0.5f ? bestMove : Vector2Int.zero;
        }
        
        void ShowConfirmHighlight(Vector2Int position)
        {
            if (highlightManager == null) return;
            
            if (!isPieDialConfirmHighlightActive || currentConfirmHighlightPosition != position)
            {
                if (isPieDialConfirmHighlightActive)
                    highlightManager.UpdateConfirmHighlightPosition(position, gridSystem);
                else
                    highlightManager.ShowConfirmHighlight(position, gridSystem);
                
                isPieDialConfirmHighlightActive = true;
                currentConfirmHighlightPosition = position;
            }
        }
        
        
        
        /// <summary>
        /// Show valid move highlights for the active pawn
        /// </summary>
void ShowValidMovesForActivePawn()
        {
            if (pawnManager == null || highlightManager == null || gridSystem == null) return;
            
            var activePawn = pawnManager.ActivePawn;
            if (activePawn == null) return;
            
            // Get valid moves for the active pawn
            var validMoves = GetValidMoves(activePawn.CurrentPosition);
            
            // Show highlights with different styling based on player type
            bool isPlayerPawn = activePawn.IsHuman;
            highlightManager.ShowValidMoveHighlights(validMoves, gridSystem, isPlayerPawn);
            
            string pawnType = activePawn.IsHuman ? "Human" : "AI";
            LogInfo($"Showing {validMoves.Count} valid move highlights for {pawnType} pawn: {activePawn.PlayerData.playerName}");
        }
void ClearHighlights()
        {
            if (highlightManager != null)
            {
                highlightManager.ClearAllHighlights(); // Clear both valid move and confirm highlights
            }
            isPieDialConfirmHighlightActive = false;
            currentConfirmHighlightPosition = Vector2Int.zero;
        }
        
        #endregion
        
        #region Event Handlers
        
void HandlePawnInitialized(WallChess.Gameplay.Pawns.Pawn pawn)
        {
            SetupDragControllerForPawn(pawn);
        }
        
        #endregion
        
        #region Public API for Legacy Compatibility
        
        // These methods maintain compatibility with existing AvatarDragController
public Vector2Int GetAvatarPosition(bool isPlayer)
        {
            // For legacy compatibility - assume player is index 0, opponent is index 1
            int pawnIndex = isPlayer ? 0 : 1;
            WallChess.Gameplay.Pawns.Pawn pawn = pawnManager.GetPawn(pawnIndex);
            return pawn?.CurrentPosition ?? Vector2Int.zero;
        }
        
public bool CanMoveAvatar(bool isPlayer)
        {
            int pawnIndex = isPlayer ? 0 : 1;
            WallChess.Gameplay.Pawns.Pawn pawn = pawnManager.GetPawn(pawnIndex);
            return pawn != null && pawn.IsActive && pawn.IsHuman && !pawn.IsMoving;
        }
        
public void MoveAvatar(bool isPlayer, Vector2Int newPosition)
        {
            int pawnIndex = isPlayer ? 0 : 1;
            WallChess.Gameplay.Pawns.Pawn pawn = pawnManager.GetPawn(pawnIndex);
            if (pawn != null)
            {
                MovePawn(pawn, newPosition);
            }
        }
        
        public Vector3 GetWorldPosition(Vector2Int gridPos) => gridSystem?.GridToWorldPosition(gridPos) ?? Vector3.zero;
        public Vector2Int GetGridPosition(Vector3 worldPos) => gridSystem?.WorldToGridPosition(worldPos) ?? Vector2Int.zero;
        public Vector2Int WorldToGridPosition(Vector3 worldPos) => GetGridPosition(worldPos);
        public Vector3 GridToWorldPosition(Vector2Int gridPos) => GetWorldPosition(gridPos);
        
        // Component accessors
        public PawnManager GetPawnManager() => pawnManager;
        public GridSystem GetGridSystem() => gridSystem;
        public HighlightManager GetHighlightManager() => highlightManager;
        public Camera GetMainCamera() => mainCamera;
        
        #endregion
        
        #region Cleanup
        
void OnDestroy()
        {
            if (pieDial != null)
            {
                pieDial.OnDirectionConfirmed.RemoveListener(OnPieDialDirectionConfirmed);
                pieDial.OnDirectionChanged.RemoveListener(OnPieDialDirectionChanged);
                pieDial.OnDirectionCancelled.RemoveListener(OnPieDialCancelled);
            }
            
            if (actionStateMachine != null)
            {
                actionStateMachine.OnActionConfirmed -= OnPlayerActionConfirmed;
                actionStateMachine.OnActionCancelled -= OnPlayerActionCancelled;
            }
            
            PawnManager.OnPawnInitialized -= HandlePawnInitialized;
        }
        
        #endregion
        
        #region Debug Utilities
        
        void LogInfo(string message)
        {
            if (enableDebugLogs) Debug.Log($"[PawnController] {message}");
        }
        
        void LogWarning(string message)
        {
            if (enableDebugLogs) Debug.LogWarning($"[PawnController] {message}");
        }
        
        void LogError(string message)
        {
            Debug.LogError($"[PawnController] {message}");
        }
        
        [ContextMenu("Debug/Print Active Pawn Info")]
void DebugPrintActivePawnInfo()
        {
            WallChess.Gameplay.Pawns.Pawn activePawn = pawnManager?.ActivePawn;
            if (activePawn != null)
            {
                Debug.Log($"Active Pawn: {activePawn}");
                Debug.Log($"Valid moves: [{string.Join(", ", GetValidMoves(activePawn.CurrentPosition))}]");
            }
            else
            {
                Debug.Log("No active pawn");
            }
        }
        
        #endregion
    }
}
