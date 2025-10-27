// Force Unity recompilation - fix caching issue v5
using UnityEngine;
using System.Collections.Generic;
using WallChess.Core.Session;
using WallChess.Gameplay.Pawns;
using WallChess.Data;
using WallChess.Core;

namespace WallChess
{
    /// <summary>
    /// Comprehensive data structure for wall placement events - accessible to UI
    /// </summary>
    [System.Serializable]
    public class WallPlacementResult
    {
        public GridSystem.WallInfo wallInfo;
        public int playerIndex;
        public int previousWallCount;
        public int remainingWalls;
        public bool turnEnded;
        public int nextPlayerIndex;
        
        public override string ToString()
        {
            return $"Wall {wallInfo.orientation} at ({wallInfo.x},{wallInfo.y}) by Player {playerIndex}. Walls: {previousWallCount}→{remainingWalls}. Turn ended: {turnEnded}";
        }
    }

    /// <summary>
    /// REFACTORED WallChessGameManager - Cleaned up and decoupled
    /// 
    /// RESPONSIBILITIES:
    /// - Core game initialization and coordination
    /// - Wall placement event handling
    /// - Grid system management
    /// - Event coordination between systems
    /// 
    /// MOVED TO OTHER MANAGERS:
    /// - Pawn management → PawnManager
    /// - State management → SessionStateManager/ApplicationStates  
    /// - Turn management → TurnManager
    /// - Active player tracking → PawnManager
    /// 
    /// CLEAN ARCHITECTURE:
    /// - No more monolithic responsibilities
    /// - Event-driven communication
    /// - Clear separation of concerns
    /// </summary>
    public class WallChessGameManager : MonoBehaviour
    {
        /// <summary>
        /// Update state display fields for inspector readability
        /// </summary>
        private void Update()
        {
            UpdateStateDisplayFields();
        }
        
        /// <summary>
        /// Updates the inspector display fields with current state information
        /// </summary>
        private void UpdateStateDisplayFields()
        {
            // Update application state
            var appController = FindObjectOfType<MainApplicationController>();
            currentApplicationState = appController?.GetCurrentApplicationState() ?? "Not Found";
            
            // Update session state
            var sessionManager = FindObjectOfType<SessionManager>();
            currentSessionState = sessionManager?.IsSessionActive == true ? "Active Session" : "No Session";
            
            // Update active pawn info
            var pawnManager = FindObjectOfType<PawnManager>();
            if (pawnManager?.ActivePawn != null)
            {
                var activePawn = pawnManager.ActivePawn;
                currentActivePawn = $"{activePawn.PlayerData.playerName} (Index: {pawnManager.ActivePawnIndex})";
            }
            else
            {
                currentActivePawn = "No active pawn";
            }
        }

        [Header("State Information - Read Only")]
        [SerializeField, Tooltip("Current main application state")] private string currentApplicationState = "Unknown";
        [SerializeField, Tooltip("Current session state")] private string currentSessionState = "Unknown"; 
        [SerializeField, Tooltip("Current active pawn info")] private string currentActivePawn = "No active pawn";
        
        [Header("Configuration")]
        [Tooltip("Core game settings (grid size, walls per player, etc.)")] 
        public GameSettings gameSettings;
        
        [Tooltip("Grid layout and visual settings (tile size, gaps, wall dimensions)")]
        public GridSettings gridSettings;
        
        [Tooltip("References to all prefabs used in the game")]
        public PrefabReferences prefabReferences;
        
        [Tooltip("Debug and development configuration")]
        public DebugSettings debugSettings;

        // PROPERTY ACCESSORS - Backward compatibility and cleaner access
        public int gridSize => gameSettings ? gameSettings.gridSize : 9;
        public float tileSize => gridSettings ? gridSettings.tileSize : 1f;
        public float tileGap => gridSettings ? gridSettings.tileGap : 0.2f;
        public int wallsPerPlayer => gameSettings ? gameSettings.wallsPerPlayer : 9;
        
        public int numberOfPlayers 
        {
            get => gameSettings ? gameSettings.numberOfPlayers : 2;
            set 
            {
                if (gameSettings != null)
                {
                    gameSettings.numberOfPlayers = value;
                }
                else
                {
                    Debug.LogWarning("Cannot set numberOfPlayers: GameSettings not assigned!");
                }
            }
        }
        
        public bool debugMode => debugSettings ? debugSettings.debugMode : false;
        public float wallThickness => gridSettings ? gridSettings.wallThickness : 0.15f;
        public float wallHeight => gridSettings ? gridSettings.wallHeight : 1f;
        
        // Prefab accessors with null checks
        public GameObject tilePrefab => prefabReferences ? prefabReferences.tilePrefab : null;
        public GameObject[] playerPrefabs => prefabReferences ? prefabReferences.playerPrefabs : null;
        public GameObject wallPrefab => prefabReferences ? prefabReferences.wallPrefab : null;
        public GameObject highlightPrefab => prefabReferences ? prefabReferences.highlightPrefab : null;
        public GameObject highlightConfirmPrefab => prefabReferences ? prefabReferences.highlightConfirmPrefab : null;
        public GameObject wallPreviewPrefab => prefabReferences ? prefabReferences.wallPreviewPrefab : null;
        
        private GridSystem gridSystem;
        private WallManager wallManager;
        private HighlightManager highlightManager;
        private WallValidator wallValidator;
        private TileAnimationController tileAnimationController;
        
        // CLEAN EVENT SYSTEM - UI accessible events
        public static System.Action<WallPlacementResult> OnWallPlacedComplete;
        public static System.Action<int> OnPlayerTurnChanged; // playerIndex
        public static System.Action<WallChess.Gameplay.Pawns.Pawn, Vector2Int, bool> OnPawnMoveComplete; // pawn, newPosition, wasSuccessful
        public static System.Action<int> OnPlayerVictory; // winning playerIndex

        // Flag to prevent double processing of wall placement
        private bool isProcessingWallPlacement = false;

        // Missing properties for backward compatibility
        public int activePlayerIndex
        {
            get
            {
                var pawnManager = FindObjectOfType<PawnManager>();
                return pawnManager?.ActivePawnIndex ?? 0;
            }
            set
            {
                var pawnManager = FindObjectOfType<PawnManager>();
                pawnManager?.SetActivePawn(value);
            }
        }
        
        public List<WallChess.Gameplay.Pawns.Pawn> pawns
        {
            get
            {
                var pawnManager = FindObjectOfType<PawnManager>();
                return pawnManager?.Pawns ?? new List<WallChess.Gameplay.Pawns.Pawn>();
            }
        }

        

        
// Legacy pawn type enum for compatibility
        public enum PawnType { Player, Opponent }

        #region Unity Lifecycle
        
        private void Start()
        {
            EnsureRequiredComponents();
            ValidateScriptableObjects();
            
            // Initialize SessionStateManager for proper game flow
            InitializeSessionStateManager();
            
            // Don't auto-initialize here - let SessionStateManager handle it
            LogInfo("WallChessGameManager ready - waiting for SessionStateManager");
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from session events
            SessionStateManager.OnSessionReady -= OnSessionReady;
            SessionStateManager.OnSessionStateChanged -= OnSessionStateChanged;
            SessionStateManager.OnSessionCompleted -= OnSessionCompleted;
            
            // Unsubscribe from events
            if (gridSystem != null)
            {
                gridSystem.OnTileOccupancyChanged -= OnTileOccupancyChanged;
                gridSystem.OnWallPlaced -= OnWallPlaced;
                gridSystem.OnGridCleared -= OnGridCleared;
            }
            
            // Unsubscribe from animation events
            if (tileAnimationController != null)
            {
                tileAnimationController.OnTileAnimationCompleted -= OnTileAnimationCompleted;
            }
            
            // Unsubscribe from pawn events
            OnPawnMoveComplete -= HandlePawnMoveComplete;
        }

        
        // Dummy method to force recompilation
        public void DummyMethod() { }
        
        #endregion

        #region Initialization

        /// <summary>
        /// Ensures all required components are present on this GameObject
        /// </summary>
        private void EnsureRequiredComponents()
        {
            // Add PawnManager if missing
            if (GetComponent<PawnManager>() == null)
            {
                var pawnManager = gameObject.AddComponent<PawnManager>();
                Debug.Log("[WallChessGameManager] Added missing PawnManager component");
            }

            // Add HighlightManager if missing  
            if (GetComponent<HighlightManager>() == null)
            {
                var highlightManager = gameObject.AddComponent<HighlightManager>();
                Debug.Log("[WallChessGameManager] Added missing HighlightManager component - will be initialized later");
            }

            // Add PawnController if missing
            if (GetComponent<PawnController>() == null)
            {
                var pawnController = gameObject.AddComponent<PawnController>();
                Debug.Log("[WallChessGameManager] Added missing PawnController component");
            }
        }

        /// <summary>
        /// Validates that all required ScriptableObjects are assigned
        /// </summary>
        private void ValidateScriptableObjects()
        {
            bool hasErrors = false;
            
            if (gameSettings == null)
            {
                Debug.LogError("GameSettings ScriptableObject not assigned! Using default values.", this);
                hasErrors = true;
            }
            
            if (gridSettings == null)
            {
                Debug.LogError("GridSettings ScriptableObject not assigned! Using default values.", this);
                hasErrors = true;
            }
            
            if (prefabReferences == null)
            {
                Debug.LogError("PrefabReferences ScriptableObject not assigned! Game may not function properly.", this);
                hasErrors = true;
            }
            
            if (debugSettings == null)
            {
                Debug.LogWarning("DebugSettings ScriptableObject not assigned! Using default values.", this);
            }
            
            if (!hasErrors)
            {
                Debug.Log("All ScriptableObjects validated successfully.", this);
            }
        }

        /// <summary>
        /// Initialize SessionStateManager for coordinated game flow
        /// </summary>
        private void InitializeSessionStateManager()
        {
            var sessionStateManager = GetComponent<SessionStateManager>();
            if (sessionStateManager == null)
            {
                sessionStateManager = gameObject.AddComponent<SessionStateManager>();
                LogInfo("Added SessionStateManager component");
            }
                        
            // Subscribe to pawn movement events
            OnPawnMoveComplete += HandlePawnMoveComplete;
            // Subscribe to session events
            SessionStateManager.OnSessionReady += OnSessionReady;
            SessionStateManager.OnSessionStateChanged += OnSessionStateChanged;
            SessionStateManager.OnSessionCompleted += OnSessionCompleted;
            
            LogInfo("SessionStateManager integration initialized");
        }

        /// <summary>
        /// Public initialization method for SessionStateManager coordination
        /// </summary>
        public void InitializeForSession()
        {
            LogInfo("Initializing for session state management");
            
            // Initialize core systems without starting gameplay immediately
            InitializeCoreSystemsOnly();
        }
        
        /// <summary>
        /// Initialize only core systems without starting gameplay
        /// </summary>
        private void InitializeCoreSystemsOnly()
        {
            LogInfo("Initializing core systems for session management");
            
            // Initialize GridSystem
            gridSystem = gameObject.GetComponent<GridSystem>();
            
            GridSystem.GridSettings gridSettings = new GridSystem.GridSettings
            {
                gridSize = this.gridSize,
                tileSize = this.tileSize,
                tileGap = this.tileGap,
                wallThickness = this.wallThickness,
                wallHeight = this.wallHeight
            };
            
            // Initialize grid system without creating tiles (session states will handle that)
            gridSystem.Initialize(gridSettings, default, true);
            
            // FIXED: Subscribe to wall placement events for proper turn management
            gridSystem.OnWallPlaced += OnWallPlaced;
            Debug.Log("Subscribed to GridSystem.OnWallPlaced event");
            
            // Initialize other core managers
            wallManager = gameObject.GetComponent<WallManager>();
            if (wallManager != null)
            {
                wallManager.Initialize(this);
            }
            
            LogInfo("Core systems initialized for session state coordination");
        }

        public void InitializeGame()
        {
            LogInfo("InitializeGame called - delegating to session state management");
            
            // Initialize core systems
            InitializeCoreSystemsOnly();
            
            // Session state manager will handle the rest of the flow
            LogInfo("Core initialization complete - session states will handle tile building and pawn spawning");
        }

        #endregion

        #region Game Logic

        public bool CanMovePawn(int pawnIndex)
        {
            if (debugMode) return true; // Debug mode allows any pawn movement

            // Check if movement is allowed at all
            if (!CanMovePawns()) return false;
            
            // Use PawnManager to check if this is the active pawn
            var pawnManager = FindObjectOfType<PawnManager>();
            return pawnManager?.ActivePawnIndex == pawnIndex;
        }

public bool CanMovePawns()
        {
            if (debugMode)
            {
                Debug.Log("[CanMovePawns] Debug mode - returning true");
                return true;
            }
            
            // Check if session allows movement
            var sessionManager = FindObjectOfType<SessionManager>();
            if (sessionManager != null && !sessionManager.IsSessionActive)
            {
                Debug.LogWarning($"[CanMovePawns] Session is not active");
                return false;
            }
            
            Debug.Log($"[CanMovePawns] All checks passed - session active: {sessionManager?.IsSessionActive ?? false}");
            return true;
        }

public bool CanPlaceWalls()
        {
            if (debugMode) return true;
            
            // Check if session allows wall placement
            var sessionManager = FindObjectOfType<SessionManager>();
            if (sessionManager != null && !sessionManager.IsSessionActive)
            {
                return false;
            }
            
            // Check if current player has walls
            return CurrentPlayerHasWalls();
        }

        public bool CurrentPlayerHasWalls()
        {
            if (debugMode) return true;
            
            // Use PawnManager to check current player's walls
            var pawnManager = FindObjectOfType<WallChess.Gameplay.Pawns.PawnManager>();
            if (pawnManager != null)
            {
                return pawnManager.GetPlayerWallsRemaining(pawnManager.ActivePawnIndex) > 0;
            }
            
            return false;
        }

        /// <summary>
        /// Unified pawn movement method - works with any pawn index
        /// </summary>
        public bool TryMovePawn(Vector2Int fromPosition, Vector2Int toPosition)
        {
            // Use PawnManager to find which pawn is at the from position
            var pawnManager = FindObjectOfType<WallChess.Gameplay.Pawns.PawnManager>();
            if (pawnManager == null)
            {
                Debug.LogError("PawnManager not found! Cannot move pawn.");
                return false;
            }
            
            // Find pawn at fromPosition
            var pawn = pawnManager.GetPawnAtPosition(fromPosition);
            if (pawn == null)
            {
                Debug.LogWarning($"No pawn found at position {fromPosition}");
                return false;
            }
            
            // Get pawn index in the PawnManager system
            int pawnIndex = pawnManager.Pawns.IndexOf(pawn);
            if (pawnIndex == -1)
            {
                Debug.LogError($"Pawn found at {fromPosition} but not in PawnManager list!");
                return false;
            }
            
            // Use PawnManager to move the pawn
            bool success = pawnManager.MovePawn(pawnIndex, toPosition);
            
            if (success)
            {
                // Update grid system
                gridSystem.SetTileOccupied(fromPosition, false);
                gridSystem.SetTileOccupied(toPosition, true);
                
                // Fire events
                OnPawnMoveComplete?.Invoke(pawn, toPosition, true);
                
                if (debugMode)
                    Debug.Log($"Successfully moved pawn from {fromPosition} to {toPosition}");
            }
            
            return success;
        }

        private void NextTurn()
        {
            var pawnManager = FindObjectOfType<WallChess.Gameplay.Pawns.PawnManager>();
            if (pawnManager != null)
            {
                pawnManager.NextTurn();
                
                OnPlayerTurnChanged?.Invoke(pawnManager.ActivePawnIndex);
                
                Debug.Log($"Turn advanced to player {pawnManager.ActivePawnIndex}: {pawnManager.ActivePawn?.PlayerData.playerName}");
            }
        }

        public bool CheckVictory()
        {
            // Use PawnManager instead of legacy pawns list
            var pawnManager = FindObjectOfType<WallChess.Gameplay.Pawns.PawnManager>();
            if (pawnManager == null || pawnManager.PawnCount == 0)
            {
                return false;
            }

            for (int i = 0; i < pawnManager.PawnCount; i++)
            {
                var pawn = pawnManager.GetPawn(i);
                if (pawn == null) continue;
                
                // Check if pawn has won using the PawnManager's victory logic
                if (pawn.HasWon())
                {
                    Debug.Log($"VICTORY: Player {i} Wins!");
                    
                    // Hide highlights when game ends
                    HideValidMovesForActivePawn();
                    
                    // Trigger UI victory event
                    OnPlayerVictory?.Invoke(i);
                    
                    return true;
                }
            }

            return false;
        }

        public void EndTurn()
        {
            if (CheckVictory()) return;

            // Use PawnManager system instead of legacy pawns list
            var pawnManager = FindFirstObjectByType<WallChess.Gameplay.Pawns.PawnManager>();
            if (pawnManager == null || pawnManager.PawnCount == 0)
            {
                Debug.LogError("EndTurn() called but PawnManager is not initialized! Cannot switch turns.");
                return;
            }

            // Track previous player for debugging
            int previousPlayer = pawnManager.ActivePawnIndex;
            
            // Hide highlights for the current player before switching
            HideValidMovesForActivePawn();
            
            // Use PawnManager to advance turns
            pawnManager.NextTurn();
            
            // Trigger turn change event
            OnPlayerTurnChanged?.Invoke(pawnManager.ActivePawnIndex);
            
            // Update PawnController for turn change
            var pawnController = GetComponent<PawnController>();
            if (pawnController != null)
            {
                pawnController.HandleTurnChanged();
            }
            
            Debug.Log($"Turn ended. Previous player: {previousPlayer}, New active player: {pawnManager.ActivePawnIndex}, Total players: {pawnManager.PawnCount}");
        }

        public bool TryStartWallPlacement()
        {
            if (!CanPlaceWalls())
            {
                Debug.LogWarning("Cannot place walls - game session not active or no walls remaining");
                return false;
            }

            if (debugMode)
            {
                Debug.Log("Debug Mode: Wall placement initiated");
                return true;
            }

            Debug.Log("Wall placement mode activated");
            return true;
        }

        public void CompleteWallPlacement(bool wallWasPlaced = true)
        {
            var pawnManager = FindObjectOfType<PawnManager>();
            
            Debug.Log($"CompleteWallPlacement called: wallWasPlaced={wallWasPlaced}, activePlayer={pawnManager?.ActivePawnIndex ?? -1}, debugMode={debugMode}");
            
            if (wallWasPlaced)
            {
                Debug.Log($"CompleteWallPlacement: Wall placement successful - calling EndTurn() for activePlayer {pawnManager?.ActivePawnIndex ?? -1}");
                EndTurn(); // Switch to next player after successful wall placement (even in debug mode)
            }
            else
            {
                Debug.Log("Wall placement cancelled - remaining in current turn");
            }
        }

        #endregion

        #region Event Handlers
        
        private void OnTileOccupancyChanged(Vector2Int gridPos, bool occupied)
        {
            Debug.Log($"Tile {gridPos} occupancy changed to: {occupied}");
        }

        private void OnWallPlaced(GridSystem.WallInfo wallInfo)
        {
            // Prevent double processing of the same wall placement
            if (isProcessingWallPlacement)
            {
                Debug.LogWarning($"OnWallPlaced: Already processing wall placement, ignoring duplicate call for {wallInfo.orientation} at ({wallInfo.x}, {wallInfo.y})");
                return;
            }
            
            isProcessingWallPlacement = true;
            
            try
            {
                var pawnManager = FindObjectOfType<PawnManager>();
                int currentPlayerIndex = pawnManager?.ActivePawnIndex ?? -1;
                
                Debug.Log($"*** WallChessGameManager.OnWallPlaced CALLED *** Wall {wallInfo.orientation} at ({wallInfo.x}, {wallInfo.y}) by Player {currentPlayerIndex}");
                
                // In debug mode, don't modify game state
                if (debugMode)
                {
                    Debug.Log("Debug Mode: Wall placed but no game state changes");
                    OnWallPlacedComplete?.Invoke(new WallPlacementResult
                    {
                        wallInfo = wallInfo,
                        playerIndex = currentPlayerIndex,
                        remainingWalls = 0,
                        turnEnded = false,
                        nextPlayerIndex = currentPlayerIndex
                    });
                    return;
                }
                
                // Use PawnManager system instead of legacy GetActivePawn
                var activePawn = GetActivePawnFromManager();
                if (activePawn == null)
                {
                    Debug.LogError("OnWallPlaced: No active pawn found - cannot process wall placement");
                    return;
                }
                
                int previousWalls = activePawn.PlayerData.wallsRemaining;
                
                // Decrement wall count for active player
                if (activePawn.PlayerData.UseWall())
                {
                    Debug.Log($"OnWallPlaced: Player {activePawn.PlayerData.playerName} walls: {previousWalls} → {activePawn.PlayerData.wallsRemaining}");
                }
                else
                {
                    Debug.LogWarning($"OnWallPlaced: Failed to use wall for player {activePawn.PlayerData.playerName}");
                }
                
                // Calculate next player for UI
                int nextPlayerIndex = (currentPlayerIndex + 1) % (pawnManager?.PawnCount ?? 2);
                
                // Trigger comprehensive UI event
                OnWallPlacedComplete?.Invoke(new WallPlacementResult
                {
                    wallInfo = wallInfo,
                    playerIndex = currentPlayerIndex,
                    previousWallCount = previousWalls,
                    remainingWalls = activePawn.PlayerData.wallsRemaining,
                    turnEnded = true,
                    nextPlayerIndex = nextPlayerIndex
                });
                
                // CRITICAL: Trigger turn advancement through CompleteWallPlacement
                Debug.Log("*** OnWallPlaced: About to call CompleteWallPlacement(true) to advance turn ***");
                CompleteWallPlacement(true);
                Debug.Log("*** OnWallPlaced: CompleteWallPlacement(true) call completed ***");
            }
            finally
            {
                isProcessingWallPlacement = false;
            }
        }

        private void OnTileAnimationCompleted()
        {
            Debug.Log("Tile animation completed");
            // Handle tile animation completion if needed
        }
        
        
        private void OnGridCleared()
        {
            Debug.Log("Grid cleared");
            // Grid cleared - PawnManager will handle pawn state reset
        }

        #endregion

        #region Session State Integration
        
        /// <summary>
        /// Called when session is ready for gameplay
        /// </summary>
private void OnSessionReady()
        {
            LogInfo("Session ready - gameplay can begin");
            
            // Ensure pawn controller is properly initialized and can handle input
            var pawnController = GetComponent<PawnController>();
            if (pawnController != null)
            {
                pawnController.HandleTurnChanged();
                LogInfo("PawnController refreshed for session ready state");
            }
        }
        
        /// <summary>
        /// Called when session state changes
        /// </summary>
        private void OnSessionStateChanged(string newStateName)
        {
            LogInfo($"Session state changed to: {newStateName}");
        }
        
        /// <summary>
        /// Called when session is completed
        /// </summary>
        private void OnSessionCompleted(int winnerIndex)
        {
            LogInfo($"Session completed - winner: {(winnerIndex >= 0 ? $"Player {winnerIndex}" : "None")}");
        }
        
        private void HandlePawnMoveComplete(WallChess.Gameplay.Pawns.Pawn pawn, Vector2Int newPosition, bool wasSuccessful)
        {
            if (!wasSuccessful) return;
            
            // Clear current highlights
            if (highlightManager != null)
            {
                highlightManager.ClearValidMoveHighlights();
            }
            
            // Check for victory
            if (CheckVictoryCondition(pawn))
            {
                return;
            }
            
            // End current turn and move to next player
            EndTurn();
        }

        #endregion

        #region Helper Methods
        
        /// <summary>
        /// Get the active pawn from PawnManager (new system)
        /// </summary>
        public WallChess.Gameplay.Pawns.Pawn GetActivePawnFromManager()
        {
            var pawnManager = FindObjectOfType<WallChess.Gameplay.Pawns.PawnManager>();
            return pawnManager?.ActivePawn;
        }

        /// <summary>
        /// Always show valid move highlights when it's the active pawn's turn
        /// </summary>
        private void ShowValidMovesForActivePawn()
        {
            // Use PawnManager instead of legacy GetActivePawn
            var activePawn = GetActivePawnFromManager();
            if (activePawn != null && highlightManager != null && GetComponent<PawnController>() != null)
            {
                // Use PlayerController's GetValidMoves which includes jump logic
                List<Vector2Int> validMoves = GetComponent<PawnController>().GetValidMoves(activePawn.CurrentPosition);
                highlightManager.ShowValidMoveHighlights(validMoves, gridSystem);
                Debug.Log($"Always showing {validMoves.Count} valid move highlights for active pawn {activePawn.PlayerData.playerIndex} (includes jumps)");
            }
        }

        /// <summary>
        /// Hide valid move highlights (called when turn ends or game state changes)
        /// </summary>
        private void HideValidMovesForActivePawn()
        {
            if (highlightManager != null)
            {
                highlightManager.ClearValidMoveHighlights();
            }
        }

        private bool CheckVictoryCondition(WallChess.Gameplay.Pawns.Pawn pawn)
        {
            if (pawn == null) return false;
            
            if (pawn.HasWon())
            {
                OnPlayerVictory?.Invoke(pawn.PlayerData.playerIndex);
                Debug.Log($"Victory! {pawn.PlayerData.playerName} has won the game!");
                return true;
            }
            
            return false;
        }

        private void LogInfo(string message)
        {
            if (debugMode) Debug.Log($"[WallChessGameManager] {message}");
        }
        
        #endregion

        #region Legacy Compatibility API
        
        // Legacy properties updated to use PawnManager system
        public Vector2Int playerPosition
        {
            get
            {
                var pawnManager = FindObjectOfType<WallChess.Gameplay.Pawns.PawnManager>();
                var pawn = pawnManager?.GetPawn(0);
                return pawn?.CurrentPosition ?? Vector2Int.zero;
            }
        }
        
        public Vector2Int opponentPosition
        {
            get
            {
                var pawnManager = FindObjectOfType<WallChess.Gameplay.Pawns.PawnManager>();
                var pawn = pawnManager?.GetPawn(1);
                return pawn?.CurrentPosition ?? Vector2Int.zero;
            }
        }
        
        public int playerWallsRemaining
        {
            get
            {
                var pawnManager = FindObjectOfType<WallChess.Gameplay.Pawns.PawnManager>();
                return pawnManager?.GetPlayerWallsRemaining(0) ?? 0;
            }
        }
        
        public int opponentWallsRemaining
        {
            get
            {
                var pawnManager = FindObjectOfType<WallChess.Gameplay.Pawns.PawnManager>();
                return pawnManager?.GetPlayerWallsRemaining(1) ?? 0;
            }
        }
        
        // Legacy methods updated to use PawnManager system
        public GameObject GetPlayerAvatar()
        {
            var pawnManager = FindObjectOfType<WallChess.Gameplay.Pawns.PawnManager>();
            var pawn = pawnManager?.GetPawn(0);
            return pawn?.gameObject;
        }
        
        public GameObject GetOpponentAvatar()
        {
            var pawnManager = FindObjectOfType<WallChess.Gameplay.Pawns.PawnManager>();
            var pawn = pawnManager?.GetPawn(1);
            return pawn?.gameObject;
        }
        
        // Legacy state checking methods for backward compatibility 
        public bool IsPlayerTurn() => debugMode || (FindObjectOfType<WallChess.Gameplay.Pawns.PawnManager>()?.ActivePawn?.PlayerData.playerType == PlayerType.Human);
        public bool IsOpponentTurn() => debugMode || (FindObjectOfType<WallChess.Gameplay.Pawns.PawnManager>()?.ActivePawn?.PlayerData.playerType == PlayerType.AI);
        public bool IsInMovementState() => CanMovePawns();
        
        // Legacy current player method (returns old enum values) 
        public int GetCurrentPlayerLegacy()
        {
            var pawnManager = FindObjectOfType<PawnManager>();
            return pawnManager?.ActivePawnIndex ?? 0;
        }
        
        public PawnType GetCurrentPlayer()
        {
            var pawnManager = FindObjectOfType<PawnManager>();
            int activeIndex = pawnManager?.ActivePawnIndex ?? 0;
            return activeIndex == 0 ? PawnType.Player : PawnType.Opponent;
        }
        
        // Core API
        public GridSystem GetGridSystem() => gridSystem;
        public WallManager GetWallManager() => wallManager;
        public HighlightManager GetHighlightManager() => highlightManager;
        public WallValidator GetWallValidator() => wallValidator;
        
        // Missing methods for backward compatibility
public WallChess.Core.GameState GetCurrentState()
        {
            // Delegate to session manager for state
            var sessionManager = FindObjectOfType<SessionManager>();
            if (sessionManager != null && sessionManager.IsSessionActive)
            {
                // Return active gameplay state when session is running
                return WallChess.Core.GameState.PlayerTurn;
            }
            return WallChess.Core.GameState.GameStart;
        }
        
public void ChangeState(WallChess.Core.GameState newState)
        {
            // State management now handled by SessionStateManager and Player Action FSMs
            // This method kept for backward compatibility but does nothing
            Debug.Log($"WallChessGameManager.ChangeState({newState}) - delegated to state managers");
        }
        
        public WallChess.Gameplay.Pawns.Pawn GetActivePawn()
        {
            var pawnManager = FindObjectOfType<PawnManager>();
            return pawnManager?.ActivePawn;
        }
        
        public bool CanInitiateMove()
        {
            return CanMovePawns();
        }
        
        public bool CanInitiateWallPlacement()
        {
            return CanPlaceWalls();
        }
        
public void CleanupGameSession()
        {
            // Clear grid if exists
            if (gridSystem != null)
            {
                gridSystem.ClearGrid();
            }
            
            // Clear highlights
            if (highlightManager != null)
            {
                highlightManager.ClearAllHighlights();
            }
            
            Debug.Log("Game session cleaned up");
        }
        
        public void UpdateGridConfiguration()
        {
            if (gridSystem != null)
            {
                GridSystem.GridSettings gridSettings = new GridSystem.GridSettings
                {
                    gridSize = this.gridSize,
                    tileSize = this.tileSize,
                    tileGap = this.tileGap,
                    wallThickness = this.wallThickness,
                    wallHeight = this.wallHeight
                };
                
                gridSystem.ReconfigureGrid(gridSettings);
                Debug.Log("Grid configuration updated");
            }
        }
        
public WallChess.Core.ActionType GetCurrentAction()
        {
            // Action state now handled by Player Action FSMs
            // Check if any pawn has an active action state
            var pawnController = GetComponent<PawnController>();
            if (pawnController != null)
            {
                // Could check pawn controller's action state machine here
                return WallChess.Core.ActionType.Idle;
            }
            return WallChess.Core.ActionType.Idle;
        }
        
public void SetCurrentAction(WallChess.Core.ActionType action)
        {
            // Action state now handled by Player Action FSMs
            // This method kept for backward compatibility but does nothing
            Debug.Log($"WallChessGameManager.SetCurrentAction({action}) - delegated to Player Action FSMs");
        }
        
        
        // New pawn system API
        public int GetActivePawnIndex()
        {
            var pawnManager = FindObjectOfType<PawnManager>();
            return pawnManager?.ActivePawnIndex ?? 0;
        }

        public Vector2Int GetPawnPosition(int pawnIndex)
        {
            var pawnManager = FindObjectOfType<PawnManager>();
            var pawn = pawnManager?.GetPawn(pawnIndex);
            return pawn?.CurrentPosition ?? Vector2Int.zero;
        }
        
        public GameObject GetPawnAvatar(int pawnIndex)
        {
            var pawnManager = FindObjectOfType<PawnManager>();
            var pawn = pawnManager?.GetPawn(pawnIndex);
            return pawn?.gameObject;
        }

        #endregion

        #region Context Menu Debug Methods

        [ContextMenu("Debug/Test Pawn System")]
        private void Ctx_TestPawnSystem()
        {
            var pawnManager = FindObjectOfType<PawnManager>();
            Debug.Log($"Active Player: {pawnManager?.ActivePawnIndex ?? -1}, Total Pawns: {pawnManager?.PawnCount ?? 0}");
        }

        [ContextMenu("Debug/Toggle Debug Mode")]
        private void Ctx_ToggleDebugMode()
        {
            if (debugSettings != null)
            {
                debugSettings.debugMode = !debugSettings.debugMode;
                Debug.Log($"Debug Mode {(debugMode ? "ENABLED" : "DISABLED")} - Any pawn can be moved");
            }
            else
            {
                Debug.LogWarning("Debug Settings ScriptableObject not assigned!");
            }
        }

        #endregion
    }
}