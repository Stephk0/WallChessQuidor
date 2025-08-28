using UnityEngine;
using System.Collections.Generic;

namespace WallChess
{
    public enum GameState
    {
        GameStart,
        BuildTiles,
        PlayerTurn,
        PawnMoving,
        WallPlacement,
        GameOver
    }

    public enum ActionType
    {
        Idle,
        MovingPawn,
        PlacingWall
    }

    /// <summary>
    /// REFACTORED WallChessGameManager - Using Player Pawn System
    /// 
    /// KEY IMPROVEMENTS:
    /// - List-based pawn management (up to 4 players)
    /// - Unified turn handling through activePlayerIndex
    /// - Generalized win/start positions
    /// - Eliminated player/opponent hardcoding
    /// - Proper mutual exclusion between movement and wall placement
    /// 
    /// PAWN SYSTEM:
    /// - pawns[0] = Player 1, pawns[1] = Player 2, etc.
    /// - activePlayerIndex tracks current turn
    /// - All movement/wall logic works with "current active pawn"
    /// - Supports 2-4 players with AI or human control
    /// </summary>
    public class WallChessGameManager : MonoBehaviour
    {
        [System.Serializable]
        public class PawnData
        {
            public Vector2Int position;
            public Vector2Int startPosition;
            public Vector2Int winPosition;
            public int wallsRemaining;
            public GameObject avatar;
            public bool isActive;
            public bool isAI;
            
            public PawnData(Vector2Int start, Vector2Int win, int walls)
            {
                startPosition = start;
                position = start;
                winPosition = win;
                wallsRemaining = walls;
                isActive = false;
                isAI = false;
                avatar = null;
            }
        }

        [Header("Game Settings")]
        public int gridSize = 9;
        public float tileSize = 1f;
        public float tileGap = 0.2f;
        public int wallsPerPlayer = 9;
        public int numberOfPlayers = 2;
        
        [Header("Debug Settings")]
        [Tooltip("When enabled, allows any pawn to be moved regardless of turn")]
        public bool debugMode = false;
        
        [Header("Wall Settings")]
        public float wallThickness = 0.15f;
        public float wallHeight = 1f;

        [Header("Prefabs")]
        public GameObject tilePrefab;
        public GameObject[] playerPrefabs; // Array for different player colors
        public GameObject wallPrefab;
        public GameObject highlightPrefab;
        public GameObject wallPreviewPrefab;

        [Header("Current State")]
        public GameState currentState = GameState.PlayerTurn;
        public ActionType currentAction = ActionType.Idle;
        
        // PLAYER PAWN SYSTEM
        [Header("Player Pawn System")]
        public List<PawnData> pawns = new List<PawnData>();
        public int activePlayerIndex = 0;

        private GridSystem gridSystem;
        private PlayerControllerV2 playerController;
        private WallManager wallManager;

        // Context menu items
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
            Debug.Log($"Debug Mode {(debugMode ? "ENABLED" : "DISABLED")} - Any pawn can be moved");
        }

        [ContextMenu("Debug/Test Pawn System")]
        private void Ctx_TestPawnSystem()
        {
            Debug.Log($"Active Player: {activePlayerIndex}, Total Pawns: {pawns.Count}");
            for (int i = 0; i < pawns.Count; i++)
            {
                var pawn = pawns[i];
                Debug.Log($"Pawn {i}: Position={pawn.position}, Walls={pawn.wallsRemaining}, Active={pawn.isActive}");
            }
        }

        void Start()
        {
            InitializeGame();
        }

        void InitializeGame()
        {
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
            
            gridSystem.Initialize(gridSettings);

            // Initialize Player Pawn System
            InitializePlayerPawnSystem();

            // Initialize controllers
            playerController = gameObject.GetComponent<PlayerControllerV2>();
            wallManager = gameObject.GetComponent<WallManager>();

            playerController.Initialize(this);
            //wallManager.Initialize(this);

            // Set up initial tile occupancy
            foreach (var pawn in pawns)
            {
                gridSystem.SetTileOccupied(pawn.position, true);
            }

            //init game state to player turn for now
            ChangeState(GameState.PlayerTurn);

            // Subscribe to events
            gridSystem.OnTileOccupancyChanged += OnTileOccupancyChanged;
            gridSystem.OnWallPlaced += OnWallPlaced;
            gridSystem.OnGridCleared += OnGridCleared;
            
            // Set first player as active
            SetActivePlayer(0);

            if (debugMode)
            {
                Debug.Log("Game started in DEBUG MODE - Any pawn can be moved");
            }
            
            Debug.Log($"Game initialized with {pawns.Count} players. Active player: {activePlayerIndex}");
        }

        void InitializePlayerPawnSystem()
        {
            // Ensure even number of players (2 or 4)
            numberOfPlayers = Mathf.Clamp(numberOfPlayers, 2, 4);
            if (numberOfPlayers == 3) numberOfPlayers = 4; // Round up to 4 as per requirements

            pawns.Clear();

            // Calculate positions based on grid size
            int center = Mathf.FloorToInt((gridSize - 1) / 2);

            // Create pawns with generalized start/win positions
            for (int i = 0; i < numberOfPlayers; i++)
            {
                Vector2Int startPos, winPos;
                
                if (numberOfPlayers == 2)
                {
                    // 2 players: opposite sides (bottom vs top)
                    if (i == 0)
                    {
                        startPos = new Vector2Int(center, 0);           // Bottom center
                        winPos = new Vector2Int(-1, gridSize - 1);     // Any position on top row
                    }
                    else
                    {
                        startPos = new Vector2Int(center, gridSize - 1); // Top center  
                        winPos = new Vector2Int(-1, 0);                 // Any position on bottom row
                    }
                }
                else // 4 players
                {
                    // 4 players: all sides (bottom, left, top, right)
                    switch (i)
                    {
                        case 0: // Bottom
                            startPos = new Vector2Int(center, 0);
                            winPos = new Vector2Int(-1, gridSize - 1);
                            break;
                        case 1: // Left
                            startPos = new Vector2Int(0, center);
                            winPos = new Vector2Int(gridSize - 1, -1);
                            break;
                        case 2: // Top
                            startPos = new Vector2Int(center, gridSize - 1);
                            winPos = new Vector2Int(-1, 0);
                            break;
                        case 3: // Right
                            startPos = new Vector2Int(gridSize - 1, center);
                            winPos = new Vector2Int(0, -1);
                            break;
                        default:
                            startPos = winPos = Vector2Int.zero;
                            break;
                    }
                }

                PawnData pawn = new PawnData(startPos, winPos, wallsPerPlayer);
                pawns.Add(pawn);
            }

            // Create avatars for all pawns
            CreatePlayerAvatars();
        }

        void CreatePlayerAvatars()
        {
            for (int i = 0; i < pawns.Count; i++)
            {
                var pawn = pawns[i];
                Vector3 worldPos = gridSystem.GridToWorldPosition(pawn.position);

                GameObject prefab = null;
                if (playerPrefabs != null && i < playerPrefabs.Length && playerPrefabs[i] != null)
                {
                    prefab = playerPrefabs[i];
                }
                else if (playerPrefabs != null && playerPrefabs.Length > 0 && playerPrefabs[0] != null)
                {
                    prefab = playerPrefabs[0]; // Fallback to first prefab
                }

                if (prefab != null)
                {
                    pawn.avatar = Instantiate(prefab, worldPos, Quaternion.identity);
                    pawn.avatar.name = $"Player{i}_Avatar";
                }
            }
        }

        public void ChangeState(GameState newState)
        {
            GameState previousState = currentState;
            currentState = newState;
            
            // Update action type based on state
            UpdateCurrentAction();
            
            Debug.Log($"Game state changed from {previousState} to {newState} | Action: {currentAction}");
        }
        
        private void UpdateCurrentAction()
        {
            switch (currentState)
            {
                case GameState.PlayerTurn:
                    currentAction = ActionType.Idle;
                    break;
                case GameState.PawnMoving:
                    currentAction = ActionType.MovingPawn;
                    break;
                case GameState.WallPlacement:
                    currentAction = ActionType.PlacingWall;
                    break;
                case GameState.GameOver:
                    currentAction = ActionType.Idle;
                    break;
            }
        }

        public void SetActivePlayer(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= pawns.Count) return;

            // Deactivate all pawns
            foreach (var pawn in pawns)
                pawn.isActive = false;

            // Activate selected player
            activePlayerIndex = playerIndex;
            pawns[activePlayerIndex].isActive = true;
            
            Debug.Log($"Active player set to: {activePlayerIndex}");
        }

        public PawnData GetActivePawn()
        {
            if (activePlayerIndex >= 0 && activePlayerIndex < pawns.Count)
                return pawns[activePlayerIndex];
            return null;
        }

        public bool CanMovePawn(int pawnIndex)
        {
            if (debugMode) return true; // Debug mode allows any pawn movement

            // Check if movement is allowed at all
            if (!CanMovePawns()) return false;
            
            // Normal mode - only active player's pawn can move
            return pawnIndex == activePlayerIndex;
        }

        public bool CanMovePawns()
        {
            if (debugMode) return true;
            return currentAction == ActionType.Idle;
        }

        public bool CanPlaceWalls()
        {
            if (debugMode) return true;
            return currentAction == ActionType.Idle || currentAction == ActionType.PlacingWall;
        }

        public bool CurrentPlayerHasWalls()
        {
            if (debugMode) return true;
            
            var activePawn = GetActivePawn();
            return activePawn != null && activePawn.wallsRemaining > 0;
        }

        /// <summary>
        /// Unified pawn movement method - works with any pawn index
        /// </summary>
        public bool TryMovePawn(int pawnIndex, Vector2Int toPosition)
        {
            if (pawnIndex < 0 || pawnIndex >= pawns.Count)
            {
                Debug.LogWarning($"Invalid pawn index: {pawnIndex}");
                return false;
            }

            // Check if this pawn can be moved
            if (!CanMovePawn(pawnIndex))
            {
                Debug.LogWarning($"Cannot move pawn {pawnIndex} - not their turn");
                return false;
            }

            var pawn = pawns[pawnIndex];
            Vector2Int fromPosition = pawn.position;

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
                ChangeState(GameState.PawnMoving);
            }

            // Execute the move
            MovePawn(pawnIndex, toPosition);
            Debug.Log($"Pawn {pawnIndex} moved from {fromPosition} to {toPosition}");

            // Complete the movement
            CompletePawnMovement();
            
            return true;
        }

        /// <summary>
        /// Legacy compatibility method for PlayerControllerV2
        /// </summary>
        public bool TryMovePawn(Vector2Int fromPosition, Vector2Int toPosition)
        {
            // Find which pawn is at the from position
            for (int i = 0; i < pawns.Count; i++)
            {
                if (pawns[i].position == fromPosition)
                {
                    return TryMovePawn(i, toPosition);
                }
            }

            Debug.LogWarning($"No pawn found at position {fromPosition}");
            return false;
        }

        void MovePawn(int pawnIndex, Vector2Int newPosition)
        {
            if (pawnIndex < 0 || pawnIndex >= pawns.Count) return;

            var pawn = pawns[pawnIndex];
            
            // Clear old position
            gridSystem.SetTileOccupied(pawn.position, false);
            
            // Update position
            pawn.position = newPosition;
            
            // Set new position as occupied
            gridSystem.SetTileOccupied(pawn.position, true);
            
            // Move avatar if it exists
            if (pawn.avatar != null)
            {
                Vector3 worldPos = gridSystem.GridToWorldPosition(pawn.position);
                pawn.avatar.transform.position = worldPos;
            }

            if (debugMode)
                Debug.Log($"Debug Mode: Pawn {pawnIndex} moved to {newPosition}");
        }

        void CompletePawnMovement()
        {
            if (CheckVictory()) return;

            // In debug mode, don't change turns automatically
            if (debugMode)
            {
                Debug.Log("Debug Mode: Turn not changed automatically");
                return;
            }

            // Normal mode - end turn and switch to next player
            EndTurn();
        }

        public bool CheckVictory()
        {
            for (int i = 0; i < pawns.Count; i++)
            {
                var pawn = pawns[i];
                
                // Check if pawn reached win position
                bool hasWon = false;
                if (pawn.winPosition.x == -1) // Any position on specified row
                {
                    hasWon = (pawn.position.y == pawn.winPosition.y);
                }
                else if (pawn.winPosition.y == -1) // Any position on specified column
                {
                    hasWon = (pawn.position.x == pawn.winPosition.x);
                }
                else // Specific position
                {
                    hasWon = (pawn.position == pawn.winPosition);
                }

                if (hasWon)
                {
                    Debug.Log($"Player {i} Wins!");
                    ChangeState(GameState.GameOver);
                    return true;
                }
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

            // Switch to next player
            int nextPlayer = (activePlayerIndex + 1) % pawns.Count;
            SetActivePlayer(nextPlayer);
            
            // Return to turn state
            ChangeState(GameState.PlayerTurn);
            
            Debug.Log($"Turn ended. Now it's Player {activePlayerIndex}'s turn.");
        }

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
                EndTurn(); // Switch to next player after successful wall placement
            }
            else
            {
                Debug.Log("Wall placement cancelled - returning to player turn");
                ChangeState(GameState.PlayerTurn);
            }
        }

        #region Event Handlers
        private void OnTileOccupancyChanged(Vector2Int gridPos, bool occupied)
        {
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
            
            // Decrement wall count for active player
            var activePawn = GetActivePawn();
            if (activePawn != null)
            {
                activePawn.wallsRemaining--;
                Debug.Log($"Player {activePlayerIndex} walls remaining: {activePawn.wallsRemaining}");
            }

            // Complete wall placement and handle turn transition
            CompleteWallPlacement(true);
        }

        private void OnGridCleared()
        {
            Debug.Log("Grid cleared");
            foreach (var pawn in pawns)
            {
                pawn.wallsRemaining = wallsPerPlayer;
            }
        }
        #endregion

        #region Public API - Legacy Compatibility
        // Legacy properties for PlayerControllerV2 compatibility
        public Vector2Int playerPosition => pawns.Count > 0 ? pawns[0].position : Vector2Int.zero;
        public Vector2Int opponentPosition => pawns.Count > 1 ? pawns[1].position : Vector2Int.zero;
        public int playerWallsRemaining => pawns.Count > 0 ? pawns[0].wallsRemaining : 0;
        public int opponentWallsRemaining => pawns.Count > 1 ? pawns[1].wallsRemaining : 0;
        
        // Legacy methods
        public GameObject GetPlayerAvatar() => pawns.Count > 0 ? pawns[0].avatar : null;
        public GameObject GetOpponentAvatar() => pawns.Count > 1 ? pawns[1].avatar : null;
        
        // Legacy state checking methods for backward compatibility
        public bool IsPlayerTurn() => debugMode || activePlayerIndex == 0;
        public bool IsOpponentTurn() => debugMode || activePlayerIndex == 1;
        public bool IsInMovementState() => currentAction == ActionType.MovingPawn;
        
        // Legacy current player method (returns old enum values) 
        public int GetCurrentPlayerLegacy() => activePlayerIndex;
        
        // Legacy pawn type enum for compatibility
        public enum PawnType { Player, Opponent }
        public PawnType GetCurrentPlayer() => activePlayerIndex == 0 ? PawnType.Player : PawnType.Opponent;
        
        // Core API
        public GridSystem GetGridSystem() => gridSystem;
        public PlayerControllerV2 GetPlayerController() => playerController;
        public WallManager GetWallManager() => wallManager;
        
        // New pawn system API
        public int GetActivePawnIndex() => activePlayerIndex;
        public Vector2Int GetPawnPosition(int pawnIndex)
        {
            if (pawnIndex >= 0 && pawnIndex < pawns.Count)
                return pawns[pawnIndex].position;
            return Vector2Int.zero;
        }
        
        public GameObject GetPawnAvatar(int pawnIndex)
        {
            if (pawnIndex >= 0 && pawnIndex < pawns.Count)
                return pawns[pawnIndex].avatar;
            return null;
        }
        #endregion

        #region Action Validation
        public bool CanInitiateMove()
        {
            return CanMovePawns() && currentAction != ActionType.MovingPawn;
        }

        public bool CanInitiateWallPlacement()
        {
            return CanPlaceWalls() && currentAction != ActionType.MovingPawn;
        }

        public GameState GetCurrentState() => currentState;
        public ActionType GetCurrentAction() => currentAction;
        
        public bool IsActionBlocked()
        {
            return currentState == GameState.GameOver;
        }
        #endregion

        #region Grid Configuration Updates
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
                
                // Reinitialize pawn system with new grid size
                InitializePlayerPawnSystem();
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