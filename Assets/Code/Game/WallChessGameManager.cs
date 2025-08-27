using UnityEngine;
using System.Collections.Generic;

namespace WallChess
{
    public enum GameState
    {
        PlayerTurn,
        OpponentTurn,
        PlayerMoving,
        OpponentMoving,
        WallPlacement,
        GameOver
    }

    public enum PawnType
    {
        Player,
        Opponent
    }

    public enum ActionType
    {
        Idle,
        MovingPawn,
        PlacingWall
    }

    /// <summary>
    /// WallChessGameManager - Central game state controller
    /// 
    /// ENHANCED STATE MANAGEMENT:
    /// This system now uses a hybrid approach with separate tracking of:
    /// - currentPlayer: Whose turn it is (Player/Opponent)
    /// - currentAction: What action is being performed (Idle/MovingPawn/PlacingWall)
    /// - currentState: Combined state for compatibility (PlayerTurn/OpponentTurn/PlayerMoving/etc.)
    /// 
    /// KEY BENEFITS:
    /// - Turn ownership never changes during actions (fixes wall placement validation)
    /// - Clear separation between "whose turn" vs "what action"
    /// - Proper mutual exclusion between movement and wall placement
    /// - Debug mode bypasses all restrictions
    /// 
    /// STATE FLOW:
    /// 1. PlayerTurn (Idle) → Can move pawn or place wall
    /// 2. PlayerTurn → WallPlacement (PlacingWall) → OpponentTurn (after completion)
    /// 3. PlayerTurn → PlayerMoving (MovingPawn) → OpponentTurn (after completion)
    /// 4. Cancellations return to the current player's turn state
    /// 
    /// WALL PLACEMENT FIX:
    /// - CurrentPlayerHasWalls() now works correctly during WallPlacement state
    /// - Turn ownership is preserved throughout the wall placement process
    /// </summary>
    public class WallChessGameManager : MonoBehaviour
    {
        [Header("Game Settings")]
        public int gridSize = 9;
        public float tileSize = 1f;
        public float tileGap = 0.2f;
        public int wallsPerPlayer = 9;
        
        [Header("Debug Settings")]
        [Tooltip("When enabled, allows any pawn to be moved regardless of turn")]
        public bool debugMode = false;
        
        [Header("Wall Settings")]
        public float wallThickness = 0.15f;
        public float wallHeight = 1f;

        [Header("Prefabs")]
        public GameObject tilePrefab;
        public GameObject playerPrefab;
        public GameObject opponentPrefab;
        public GameObject wallPrefab;
        public GameObject highlightPrefab;
        public GameObject wallPreviewPrefab;

        [Header("Current State")]
        public GameState currentState = GameState.PlayerTurn;
        private GameState previousState = GameState.PlayerTurn; // Track previous state for cancellations
        
        [Header("Enhanced State Management")]
        public PawnType currentPlayer = PawnType.Player;
        public ActionType currentAction = ActionType.Idle;
        
        public int playerWallsRemaining = 9;
        public int opponentWallsRemaining = 9;

        private GridSystem gridSystem;
        private PlayerControllerV2 playerController;
        private WallManager wallManager;
        private GameObject playerAvatar;
        private GameObject opponentAvatar;

        public Vector2Int playerPosition = new Vector2Int(4, 8); // Bottom center
        public Vector2Int opponentPosition = new Vector2Int(4, 0); // Top center

        // Context menu items (no params)
        [ContextMenu("Grid/Apply Current Settings")]
        private void Ctx_ApplyCurrent() => UpdateGridConfiguration(gridSize, tileSize, tileGap);

        [ContextMenu("Grid/Presets/Small 8x8")]
        private void Ctx_Small() => UpdateGridConfiguration(8, tileSize, tileGap);

        [ContextMenu("Grid/Presets/Medium 12x12")]
        private void Ctx_Medium() => UpdateGridConfiguration(12, tileSize, tileGap);

        [ContextMenu("Grid/Presets/Large 16x16")]
        private void Ctx_Large() => UpdateGridConfiguration(16, tileSize, tileGap);

        [ContextMenu("Debug/Toggle Debug Mode")]
        private void Ctx_ToggleDebugMode()
        {
            debugMode = !debugMode;
            Debug.Log($"Debug Mode {(debugMode ? "ENABLED" : "DISABLED")} - Any pawn can now be moved regardless of turn");
            
            // Update UI or visual indicators if needed
            if (debugMode)
            {
                Debug.Log("DEBUG MODE ACTIVE: Both pawns can be moved freely");
            }
        }

        [ContextMenu("Debug/Enable Debug Mode")]
        private void Ctx_EnableDebugMode()
        {
            debugMode = true;
            Debug.Log("DEBUG MODE ENABLED - Any pawn can be moved regardless of turn");
        }

        [ContextMenu("Debug/Disable Debug Mode")]
        private void Ctx_DisableDebugMode()
        {
            debugMode = false;
            Debug.Log("DEBUG MODE DISABLED - Normal turn-based gameplay restored");
        }

        [ContextMenu("Debug/Test Wall Placement State")]
        private void Ctx_TestWallPlacementState()
        {
            ChangeState(GameState.WallPlacement);
            Debug.Log("Switched to Wall Placement state - pawn movement should be blocked");
        }

        [ContextMenu("Debug/Test Player Moving State")]
        private void Ctx_TestPlayerMovingState()
        {
            ChangeState(GameState.PlayerMoving);
            Debug.Log("Switched to Player Moving state - wall placement should be blocked");
        }

        [ContextMenu("Debug/Return to Player Turn")]
        private void Ctx_ReturnToPlayerTurn()
        {
            ChangeState(GameState.PlayerTurn);
            Debug.Log("Returned to Player Turn state");
        }

        [ContextMenu("Debug/Test Wall Placement Cancellation")]
        private void Ctx_TestWallPlacementCancellation()
        {
            ChangeState(GameState.WallPlacement);
            Debug.Log("Started wall placement - test cancellation by calling CompleteWallPlacement(false)");
            CompleteWallPlacement(false);
        }
        
        [ContextMenu("Debug/Test State Management")]
        private void Ctx_TestStateManagement()
        {
            Debug.Log($"Current State: {currentState}, Player: {currentPlayer}, Action: {currentAction}");
            Debug.Log($"Player Walls: {playerWallsRemaining}, Opponent Walls: {opponentWallsRemaining}");
            Debug.Log($"Can Place Walls: {CanPlaceWalls()}, Current Player Has Walls: {CurrentPlayerHasWalls()}");
        }

        [ContextMenu("Debug/Switch to Opponent Turn")]
        private void Ctx_SwitchToOpponentTurn()
        {
            ChangeState(GameState.OpponentTurn);
            Debug.Log("Switched to Opponent Turn");
        }

        [ContextMenu("Debug/Force End Turn")]
        private void Ctx_ForceEndTurn()
        {
            EndTurn();
            Debug.Log("Forced turn end");
        }
        
        [ContextMenu("Debug/Test Wall Placement Validation")]
        private void Ctx_TestWallPlacementValidation()
        {
            Debug.Log($"=== WALL PLACEMENT VALIDATION TEST ===");
            Debug.Log($"Current State: {currentState}");
            Debug.Log($"Current Player: {currentPlayer}");
            Debug.Log($"Current Action: {currentAction}");
            Debug.Log($"Player Walls: {playerWallsRemaining}");
            Debug.Log($"Opponent Walls: {opponentWallsRemaining}");
            Debug.Log($"CanPlaceWalls(): {CanPlaceWalls()}");
            Debug.Log($"CurrentPlayerHasWalls(): {CurrentPlayerHasWalls()}");
            Debug.Log($"CanInitiateWallPlacement(): {CanInitiateWallPlacement()}");
            Debug.Log($"Debug Mode: {debugMode}");
        }
        
        [ContextMenu("Debug/Force Start Wall Placement")]
        private void Ctx_ForceStartWallPlacement()
        {
            bool result = TryStartWallPlacement();
            Debug.Log($"TryStartWallPlacement() result: {result}");
            Debug.Log($"New State: {currentState}, Action: {currentAction}");
        }

        void Start()
        {
            InitializeGame();
        }

        void InitializeGame()
        {
            // Initialize GridSystem
            gridSystem = gameObject.AddComponent<GridSystem>();
            
            // Create grid settings from game manager values
            GridSystem.GridSettings gridSettings = new GridSystem.GridSettings
            {
                gridSize = this.gridSize,
                tileSize = this.tileSize,
                tileGap = this.tileGap,
                wallThickness = this.wallThickness,
                wallHeight = this.wallHeight
            };
            
            // Initialize the grid system
            gridSystem.Initialize(gridSettings);
            
            playerPosition = new Vector2Int(Mathf.FloorToInt((gridSettings.gridSize - 1) / 2), 0); // Bottom center
            opponentPosition = new Vector2Int(Mathf.FloorToInt((gridSettings.gridSize - 1) / 2), gridSettings.gridSize - 1);
            // Initialize other components
            playerController = gameObject.AddComponent<PlayerControllerV2>();
            wallManager = gameObject.AddComponent<WallManager>();

            // Create player avatars
            CreatePlayerAvatars();

            // Initialize controllers
            playerController.Initialize(this);
            wallManager.Initialize(this);

            // Set initial player positions as occupied
            
            gridSystem.SetTileOccupied(playerPosition, true);
            gridSystem.SetTileOccupied(opponentPosition, true);

            // Subscribe to grid events
            gridSystem.OnTileOccupancyChanged += OnTileOccupancyChanged;
            gridSystem.OnWallPlaced += OnWallPlaced;
            gridSystem.OnGridCleared += OnGridCleared;
            
            // Initialize enhanced state management
            currentPlayer = PawnType.Player;
            currentAction = ActionType.Idle;
            UpdatePlayerAndAction(); // Ensure consistency

            if (debugMode)
            {
                Debug.Log("Game started in DEBUG MODE - Any pawn can be moved");
            }
            
            Debug.Log($"Game initialized. Current Player: {currentPlayer}, Action: {currentAction}");
        }

        void CreatePlayerAvatars()
        {
            Vector3 playerWorldPos = gridSystem.GridToWorldPosition(playerPosition);
            Vector3 opponentWorldPos = gridSystem.GridToWorldPosition(opponentPosition);

            if (playerPrefab != null)
            {
                playerAvatar = Instantiate(playerPrefab, playerWorldPos, Quaternion.identity);
                playerAvatar.name = "PlayerAvatar";
            }

            if (opponentPrefab != null)
            {
                opponentAvatar = Instantiate(opponentPrefab, opponentWorldPos, Quaternion.identity);
                opponentAvatar.name = "OpponentAvatar";
            }
        }


        public void ChangeState(GameState newState)
        {
            previousState = currentState;
            currentState = newState;
            
            // Update currentPlayer and currentAction based on the state
            UpdatePlayerAndAction();
            
            Debug.Log($"Game state changed from {previousState} to {newState} | Player: {currentPlayer}, Action: {currentAction}");
        }
        
        /// <summary>
        /// Update currentPlayer and currentAction based on the current game state
        /// </summary>
        private void UpdatePlayerAndAction()
        {
            switch (currentState)
            {
                case GameState.PlayerTurn:
                    currentPlayer = PawnType.Player;
                    currentAction = ActionType.Idle;
                    break;
                case GameState.OpponentTurn:
                    currentPlayer = PawnType.Opponent;
                    currentAction = ActionType.Idle;
                    break;
                case GameState.PlayerMoving:
                    currentPlayer = PawnType.Player;
                    currentAction = ActionType.MovingPawn;
                    break;
                case GameState.OpponentMoving:
                    currentPlayer = PawnType.Opponent;
                    currentAction = ActionType.MovingPawn;
                    break;
                case GameState.WallPlacement:
                    // currentPlayer stays the same (whoever initiated the wall placement)
                    currentAction = ActionType.PlacingWall;
                    break;
                case GameState.GameOver:
                    // Keep current player, set action to idle
                    currentAction = ActionType.Idle;
                    break;
            }
        }

        public bool IsPlayerTurn()
        {
            return debugMode || currentPlayer == PawnType.Player;
        }

        public bool IsOpponentTurn()
        {
            return debugMode || currentPlayer == PawnType.Opponent;
        }

        public bool CanMoveAnyPawn()
        {
            return debugMode;
        }

        /// <summary>
        /// Check if pawn movement is currently allowed
        /// </summary>
        public bool CanMovePawns()
        {
            if (debugMode) return true;
            
            return currentAction == ActionType.Idle;
        }

        /// <summary>
        /// Check if wall placement is currently allowed
        /// </summary>
        public bool CanPlaceWalls()
        {
            if (debugMode) return true;
            
            // Wall placement is allowed when idle OR actively placing walls
            bool result = currentAction == ActionType.Idle || currentAction == ActionType.PlacingWall;
            Debug.Log($"[DEBUG] CanPlaceWalls() check: currentAction={currentAction}, result={result}");
            return result;
        }

        /// <summary>
        /// Check if the game is currently in a movement state
        /// </summary>
        public bool IsInMovementState()
        {
            return currentAction == ActionType.MovingPawn;
        }

        /// <summary>
        /// Check if the current player has walls remaining
        /// </summary>
        public bool CurrentPlayerHasWalls()
        {
            if (debugMode) return true;
            
            return currentPlayer == PawnType.Player 
                ? playerWallsRemaining > 0 
                : opponentWallsRemaining > 0;
        }

        /// <summary>
        /// Check if a specific pawn can be moved in current state
        /// In debug mode, any pawn can always be moved
        /// In normal mode, only the current player's pawn can be moved
        /// </summary>
        public bool CanMovePawn(bool isPlayerPawn)
        {
            if (debugMode)
            {
                return true; // Debug mode allows moving any pawn
            }

            // Check if movement is allowed at all
            if (!CanMovePawns()) return false;
            
            // Normal mode - only current player's pawn can move
            if (isPlayerPawn)
                return currentPlayer == PawnType.Player;
            else
                return currentPlayer == PawnType.Opponent;
        }

        /// <summary>
        /// Attempt to move any pawn to a new position
        /// Handles both player and opponent pawns based on debug mode settings
        /// </summary>
        public bool TryMovePawn(Vector2Int fromPosition, Vector2Int toPosition)
        {
            // Check if movement is allowed at all
            if (!CanMovePawns())
            {
                Debug.LogWarning("Cannot move pawns - game is in wall placement or other non-movement state");
                return false;
            }

            bool isPlayerPawn = (fromPosition == playerPosition);
            bool isOpponentPawn = (fromPosition == opponentPosition);

            if (!isPlayerPawn && !isOpponentPawn)
            {
                Debug.LogWarning($"No pawn found at position {fromPosition}");
                return false;
            }

            // Check if this pawn can be moved
            if (!CanMovePawn(isPlayerPawn))
            {
                Debug.LogWarning($"Cannot move {(isPlayerPawn ? "player" : "opponent")} pawn - not their turn");
                return false;
            }

            // Validate the move through grid system
            List<Vector2Int> validMoves = gridSystem.GetValidMoves(fromPosition);
            if (!validMoves.Contains(toPosition))
            {
                Debug.LogWarning($"Invalid move from {fromPosition} to {toPosition}");
                return false;
            }

            // Transition to movement state (unless in debug mode)
            if (!debugMode)
            {
                GameState movementState = isPlayerPawn ? GameState.PlayerMoving : GameState.OpponentMoving;
                ChangeState(movementState);
            }

            // Execute the move
            if (isPlayerPawn)
            {
                MovePlayer(toPosition);
                Debug.Log($"Player pawn moved from {fromPosition} to {toPosition}");
            }
            else
            {
                MoveOpponent(toPosition);
                Debug.Log($"Opponent pawn moved from {fromPosition} to {toPosition}");
            }

            // Complete the movement
            CompletePawnMovement();
            
            return true;
        }

        /// <summary>
        /// Complete the pawn movement and handle turn transitions
        /// </summary>
        private void CompletePawnMovement()
        {
            if (CheckVictory()) return;

            // In debug mode, return to previous turn state
            if (debugMode)
            {
                Debug.Log("Debug Mode: Movement completed, either pawn can be moved again");
                return;
            }

            // Normal mode - end turn and switch to opponent
            EndTurn();
        }

        public bool CheckVictory()
        {
            // Player wins if reaches top row (y = 0)
            if (playerPosition.y == gridSize - 1)
            {
                Debug.Log("Player Wins!");
                ChangeState(GameState.GameOver);
                return true;
            }

            // Opponent wins if reaches bottom row (y = gridSize - 1)
            if (opponentPosition.y == 0)
            {
                Debug.Log("Opponent Wins!");
                ChangeState(GameState.GameOver);
                return true;
            }

            return false;
        }

        public void EndTurn()
        {
            if (CheckVictory()) return;

            // In debug mode, don't change turns automatically
            if (debugMode)
            {
                Debug.Log("Debug Mode: Turn not changed automatically");
                return;
            }

            // Switch to the other player
            currentPlayer = (currentPlayer == PawnType.Player) ? PawnType.Opponent : PawnType.Player;
            
            // Set the appropriate turn state
            GameState newState = (currentPlayer == PawnType.Player) ? GameState.PlayerTurn : GameState.OpponentTurn;
            ChangeState(newState);
            
            Debug.Log($"Turn ended. Now it's {currentPlayer}'s turn.");
        }

        /// <summary>
        /// Attempt to start wall placement
        /// </summary>
        public bool TryStartWallPlacement()
        {
            if (!CanPlaceWalls())
            {
                Debug.LogWarning("Cannot place walls - game is in movement state");
                return false;
            }

            if (debugMode)
            {
                Debug.Log("Debug Mode: Wall placement initiated");
                return true;
            }

            ChangeState(GameState.WallPlacement);
            Debug.Log("Wall placement mode activated");
            return true;
        }

        /// <summary>
        /// Complete wall placement and return to turn state
        /// </summary>
        public void CompleteWallPlacement(bool wallWasPlaced = true)
        {
            if (debugMode)
            {
                Debug.Log($"Debug Mode: Wall placement completed (success: {wallWasPlaced})");
                return;
            }

            if (wallWasPlaced)
            {
                Debug.Log("Wall placement successful - ending turn");
                EndTurn(); // Switch to opponent after successful wall placement
            }
            else
            {
                Debug.Log("Wall placement cancelled - returning to current player's turn state");
                // Return to the current player's turn state
                GameState turnState = (currentPlayer == PawnType.Player) ? GameState.PlayerTurn : GameState.OpponentTurn;
                ChangeState(turnState);
                Debug.Log($"Returned to {currentPlayer}'s turn after cancelled wall placement");
            }
        }

        public void MovePlayer(Vector2Int newPosition)
        {
            // Clear old position
            gridSystem.SetTileOccupied(playerPosition, false);
            
            // Update position
            playerPosition = newPosition;
            
            // Set new position as occupied
            gridSystem.SetTileOccupied(playerPosition, true);
            
            // Move avatar if it exists
            if (playerAvatar != null)
            {
                Vector3 worldPos = gridSystem.GridToWorldPosition(playerPosition);
                playerAvatar.transform.position = worldPos;
            }

            if (debugMode)
                Debug.Log($"Debug Mode: Player moved to {newPosition}");
        }

        public void MoveOpponent(Vector2Int newPosition)
        {
            // Clear old position
            gridSystem.SetTileOccupied(opponentPosition, false);
            
            // Update position
            opponentPosition = newPosition;
            
            // Set new position as occupied
            gridSystem.SetTileOccupied(opponentPosition, true);
            
            // Move avatar if it exists
            if (opponentAvatar != null)
            {
                Vector3 worldPos = gridSystem.GridToWorldPosition(opponentPosition);
                opponentAvatar.transform.position = worldPos;
            }

            if (debugMode)
                Debug.Log($"Debug Mode: Opponent moved to {newPosition}");
        }

        #region Event Handlers
        private void OnTileOccupancyChanged(Vector2Int gridPos, bool occupied)
        {
            // Handle tile occupancy changes if needed
            Debug.Log($"Tile {gridPos} occupancy changed to: {occupied}");
        }

        private void OnWallPlaced(GridSystem.WallInfo wallInfo)
        {
            Debug.Log($"Wall placed: {wallInfo.orientation} at ({wallInfo.x}, {wallInfo.y})");
            
            // In debug mode, don't decrement walls or change state
            if (debugMode)
            {
                Debug.Log("Debug Mode: Wall count not decremented, state unchanged");
                return;
            }
            
            // Decrement wall count for current player
            if (currentPlayer == PawnType.Player)
                playerWallsRemaining--;
            else
                opponentWallsRemaining--;
                
            Debug.Log($"{currentPlayer} walls remaining: {(currentPlayer == PawnType.Player ? playerWallsRemaining : opponentWallsRemaining)}");

            // Complete wall placement and handle turn transition
            CompleteWallPlacement(true);
        }

        private void OnGridCleared()
        {
            Debug.Log("Grid cleared");
            playerWallsRemaining = wallsPerPlayer;
            opponentWallsRemaining = wallsPerPlayer;
        }
        #endregion

        #region Public API - Grid Access
        public GridSystem GetGridSystem() => gridSystem;
        
        // Legacy compatibility methods - redirect to GridSystem
        public GridSystem GetGridManager() => gridSystem; // For backward compatibility
        public PlayerControllerV2 GetPlayerController() => playerController;
        public WallManager GetWallManager() => wallManager;
        public GameObject GetPlayerAvatar() => playerAvatar;
        public GameObject GetOpponentAvatar() => opponentAvatar;
        #endregion

        #region Public API - Action Validation
        /// <summary>
        /// Check if a move action can be initiated (not the specific move validation)
        /// </summary>
        public bool CanInitiateMove()
        {
            return CanMovePawns() && !IsInMovementState();
        }

        /// <summary>
        /// Check if a wall placement action can be initiated
        /// </summary>
        public bool CanInitiateWallPlacement()
        {
            return CanPlaceWalls() && !IsInMovementState();
        }

        /// <summary>
        /// Get current game state for external systems
        /// </summary>
        public GameState GetCurrentState()
        {
            return currentState;
        }
        
        /// <summary>
        /// Get current player for external systems
        /// </summary>
        public PawnType GetCurrentPlayer()
        {
            return currentPlayer;
        }
        
        /// <summary>
        /// Get current action for external systems
        /// </summary>
        public ActionType GetCurrentAction()
        {
            return currentAction;
        }

        /// <summary>
        /// Check if any action is currently blocked
        /// </summary>
        public bool IsActionBlocked()
        {
            return currentState == GameState.GameOver;
        }
        #endregion

        #region Grid Configuration Updates
        /// <summary>
        /// Update the grid configuration at runtime
        /// </summary>
        public void UpdateGridConfiguration(int newGridSize = -1, float newTileSize = -1, float newTileGap = -1)
        {
            bool changed = false;
            
            if (newGridSize > 0 && newGridSize != gridSize)
            {
                gridSize = newGridSize;
                changed = true;
            }
            
            if (newTileSize > 0 && newTileSize != tileSize)
            {
                tileSize = newTileSize;
                changed = true;
            }
            
            if (newTileGap >= 0 && newTileGap != tileGap)
            {
                tileGap = newTileGap;
                changed = true;
            }

            if (changed && gridSystem != null)
            {
                GridSystem.GridSettings newSettings = new GridSystem.GridSettings
                {
                    gridSize = this.gridSize,
                    tileSize = this.tileSize,
                    tileGap = this.tileGap,
                    wallThickness = this.wallThickness,
                    wallHeight = this.wallHeight
                };
                
                gridSystem.ReconfigureGrid(newSettings);
                Debug.Log($"Grid reconfigured: {gridSize}x{gridSize}, tileSize={tileSize}, gap={tileGap}");
            }
        }
        #endregion

        void OnDestroy()
        {
            // Unsubscribe from events
            if (gridSystem != null)
            {
                gridSystem.OnTileOccupancyChanged -= OnTileOccupancyChanged;
                gridSystem.OnWallPlaced -= OnWallPlaced;
                gridSystem.OnGridCleared -= OnGridCleared;
            }
        }
    }
}
