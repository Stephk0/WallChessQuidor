using UnityEngine;
using System.Collections.Generic;

namespace WallChess
{
    public enum GameState
    {
        PlayerTurn,
        OpponentTurn,
        PlayerMoving,
        WallPlacement,
        GameOver
    }

    public class WallChessGameManager : MonoBehaviour
    {
        [Header("Game Settings")]
        public int gridSize = 9;
        public float tileSize = 1f;
        public float tileGap = 0.2f;
        public int wallsPerPlayer = 9;
        
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
        public int playerWallsRemaining = 9;
        public int opponentWallsRemaining = 9;

        private GridSystem gridSystem;
        private PlayerController playerController;
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

            // Initialize other components
            playerController = gameObject.AddComponent<PlayerController>();
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

        void SetupCamera()
        {
            if (Camera.main != null)
            {
                Camera.main.transform.position = new Vector3(4f, 4f, -10f);
                Camera.main.orthographicSize = 6f;
            }
        }

        public void ChangeState(GameState newState)
        {
            currentState = newState;
            Debug.Log($"Game state changed to: {newState}");
        }

        public bool IsPlayerTurn()
        {
            return currentState == GameState.PlayerTurn;
        }

        public bool CheckVictory()
        {
            // Player wins if reaches top row (y = 0)
            if (playerPosition.y == 0)
            {
                Debug.Log("Player Wins!");
                ChangeState(GameState.GameOver);
                return true;
            }

            // Opponent wins if reaches bottom row (y = gridSize - 1)
            if (opponentPosition.y == gridSize - 1)
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

            // Switch turns
            if (currentState == GameState.PlayerTurn)
                ChangeState(GameState.OpponentTurn);
            else
                ChangeState(GameState.PlayerTurn);
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
            
            // Decrement wall count for current player
            if (currentState == GameState.PlayerTurn)
                playerWallsRemaining--;
            else
                opponentWallsRemaining--;
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
        public PlayerController GetPlayerController() => playerController;
        public WallManager GetWallManager() => wallManager;
        public GameObject GetPlayerAvatar() => playerAvatar;
        public GameObject GetOpponentAvatar() => opponentAvatar;
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
