// Force recompilation v3 - fix gameManager scope
using UnityEngine;
using System.Collections.Generic;
using WallChess.Core.Session;
using WallChess.Core;
using WallChess.Data;

namespace WallChess.Gameplay.Pawns
{
    /// <summary>
    /// Manages the list-based pawn system for up to 4 players
    /// Integrates with SessionManager for proper turn-based gameplay
    /// Replaces hardcoded player/opponent system with generalized pawn management
    /// </summary>
    public class PawnManager : MonoBehaviour
    {
        [Header("Pawn Configuration")]
        [SerializeField] private PrefabReferences prefabReferences;
        [SerializeField] private Transform pawnContainer;
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;
        
        [Header("Pawn States - Read Only")]
        [SerializeField, Tooltip("Current active pawn information")] private string activePawnInfo = "No active pawn";
        [SerializeField, Tooltip("All pawns status")] private string[] allPawnsStatus = new string[0];
        
        // Core pawn management
        private List<Pawn> pawns = new List<Pawn>(4);
        private int activePawnIndex = -1;
        
        // Component references
        private SessionManager sessionManager;
        private GridSystem gridSystem;
        
        // Events for system integration
        public static System.Action<int> OnActivePawnChanged;
        public static System.Action<Pawn> OnPawnMoved;
        public static System.Action<Pawn> OnPawnInitialized;
        
                // Early initialization flags
        private bool earlyInitialized = false;
        private bool pawnsHidden = true;
        
// Properties
        public List<Pawn> Pawns => pawns;
        public Pawn ActivePawn => activePawnIndex >= 0 && activePawnIndex < pawns.Count ? pawns[activePawnIndex] : null;
        public int ActivePawnIndex => activePawnIndex;
        public int PawnCount => pawns.Count;
        
        #region Unity Lifecycle
        
void Awake()
        {
            InitializeComponents();
        }
        
        
        void Start()
        {
            SubscribeToEvents();
            
            // Check if early initialization is needed
            CheckForEarlyInitialization();
        }
        
        /// <summary>
        /// Check if we need to initialize pawns early (before tile building)
        /// </summary>
        private void CheckForEarlyInitialization()
        {
            // Look for SessionStateManager to determine if we should initialize early
            var sessionStateManager = Object.FindFirstObjectByType<SessionStateManager>();
            if (sessionStateManager != null && !earlyInitialized)
            {
                LogInfo("SessionStateManager found - performing early pawn initialization");
                EarlyInitializePawns();
            }
        }
        
        /// <summary>
        /// Initialize pawns early and hide them until they're needed
        /// Called before tile building starts
        /// </summary>
/// <summary>
        /// Initialize pawns early and hide them until they're needed
        /// Called before tile building starts
        /// </summary>
        public void EarlyInitializePawns()
        {
            if (earlyInitialized)
            {
                LogWarning("Pawns already early initialized");
                return;
            }
            
            LogInfo("Starting early pawn initialization");
            
            // Get game manager for settings
            var gameManager = Object.FindFirstObjectByType<WallChessGameManager>();
            if (gameManager == null)
            {
                LogError("Cannot early initialize: WallChessGameManager not found");
                return;
            }
            
            // Ensure GridSystem is available - add more diagnostics
            if (gridSystem == null)
            {
                LogWarning("GridSystem is null, trying to get it from GameManager");
                gridSystem = gameManager.GetGridSystem();
                if (gridSystem == null)
                {
                    LogError("CRITICAL: Cannot early initialize - GridSystem not found in GameManager");
                    LogError("Trying to force initialization...");
                    gameManager.InitializeForSession();
                    gridSystem = gameManager.GetGridSystem();
                    if (gridSystem == null)
                    {
                        LogError("FAILED: GridSystem still null after forced initialization");
                        return;
                    }
                    else
                    {
                        LogInfo("SUCCESS: GridSystem initialized after forced initialization");
                    }
                }
            }
            else
            {
                LogInfo($"GridSystem already available: Size={gridSystem.GetGridSize()}");
            }
            
            // Create default session data based on game manager settings
            var sessionData = CreateDefaultSessionData(gameManager);
            if (sessionData == null)
            {
                LogError("Failed to create default session data for early initialization");
                return;
            }
            
            LogInfo($"Created session data for {sessionData.playerCount} players");
            
            // Clear any existing pawns
            ClearExistingPawns();
            
            // Set prefab references if available
            if (gameManager.prefabReferences != null)
            {
                SetPrefabReferences(gameManager.prefabReferences);
                LogInfo("Set prefab references for early initialization");
                LogInfo($"Prefab count: {(gameManager.prefabReferences.playerPrefabs?.Length ?? 0)}");
            }
            else
            {
                LogWarning("No prefab references available - will create emergency pawns");
            }
            
            // Initialize pawns from session data
            InitializePawnsFromSession(sessionData);
            
            if (pawns.Count == 0)
            {
                LogError("No pawns were created during early initialization!");
                return;
            }
            
            LogInfo($"Successfully created {pawns.Count} pawns during early initialization");
            
            // Hide all pawns initially
            HideAllPawns();
            
            earlyInitialized = true;
            pawnsHidden = true;
            
            LogInfo($"Early initialization complete: {pawns.Count} pawns created and hidden");
        }
        
        void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        void Update()
        {
            UpdateDebugFields();
        }
        
        /// <summary>
        /// Updates inspector debug fields with current pawn states
        /// </summary>
        private void UpdateDebugFields()
        {
            // Update active pawn info
            if (ActivePawn != null)
            {
                activePawnInfo = $"{ActivePawn.PlayerData.playerName} (Index: {activePawnIndex}, Walls: {ActivePawn.PlayerData.wallsRemaining})";
            }
            else
            {
                activePawnInfo = "No active pawn";
            }
            
            // Update all pawns status
            if (pawns.Count != allPawnsStatus.Length)
            {
                allPawnsStatus = new string[pawns.Count];
            }
            
            for (int i = 0; i < pawns.Count; i++)
            {
                var pawn = pawns[i];
                if (pawn != null)
                {
                    allPawnsStatus[i] = $"{pawn.PlayerData.playerName} - Active: {pawn.IsActive}, Walls: {pawn.PlayerData.wallsRemaining}, Pos: {pawn.CurrentPosition}";
                }
                else
                {
                    allPawnsStatus[i] = "NULL PAWN";
                }
            }
        }

        

        /// <summary>
        /// Set prefab references (called by WallChessGameManager during initialization)
        /// </summary>
        public void SetPrefabReferences(PrefabReferences references)
        {
            prefabReferences = references;
            LogInfo("PrefabReferences assigned to PawnManager");
        }
        #endregion
        
        #region Initialization
        
        private void InitializeComponents()
        {
            // Auto-find required components
            if (sessionManager == null)                sessionManager = Object.FindFirstObjectByType<SessionManager>();
            
            if (gridSystem == null)                gridSystem = Object.FindFirstObjectByType<GridSystem>();
            
            // Create pawn container if not assigned
            if (pawnContainer == null)
            {
                GameObject container = new GameObject("PawnContainer");
                container.transform.SetParent(transform);
                pawnContainer = container.transform;
            }
            
            LogInfo("PawnManager components initialized");
        }
        
        /// <summary>
        /// Initialize pawns from session data when game starts
        /// </summary>
/// <summary>
        /// Initialize pawns from session data when game starts
        /// </summary>
        public void InitializePawnsFromSession(SessionData sessionData)
        {
            if (sessionData == null)
            {
                LogError("Cannot initialize pawns: SessionData is null");
                return;
            }
            
            LogInfo($"InitializePawnsFromSession called for {sessionData.playerCount} players");
            
            ClearExistingPawns();
            
            // Create pawns for each player in session
            for (int i = 0; i < sessionData.playerCount; i++)
            {
                PlayerData playerData = sessionData.GetPlayer(i);
                if (playerData != null)
                {
                    LogInfo($"Creating pawn for player {i}: {playerData.playerName}");
                    CreatePawn(playerData);
                }
                else
                {
                    LogError($"PlayerData is null for player index {i}");
                }
            }
            
            // Set initial active pawn
            if (pawns.Count > 0)
            {
                SetActivePawn(sessionData.currentPlayerIndex);
                LogInfo($"Set initial active pawn to index {sessionData.currentPlayerIndex}");
            }
            else
            {
                LogError("No pawns were created - cannot set active pawn");
            }
            
            LogInfo($"Initialized {pawns.Count} pawns from session data");
        }
        
private void CreatePawn(PlayerData playerData)
        {
            if (playerData == null)
            {
                LogError("Cannot create pawn: PlayerData is null");
                return;
            }
            
            LogInfo($"Creating pawn for {playerData.playerName} (index {playerData.playerIndex})");
            
            // Get appropriate prefab for this player
            GameObject prefab = GetPawnPrefab(playerData.playerIndex);
            if (prefab == null)
            {
                LogError($"No prefab available for player {playerData.playerIndex} - creating emergency pawn");
                CreateEmergencyPawnForPlayer(playerData);
                return;
            }
            
            LogInfo($"Using prefab: {prefab.name} for player {playerData.playerIndex}");
            
            // Instantiate pawn GameObject
            GameObject pawnObject = Instantiate(prefab, pawnContainer);
            pawnObject.name = $"Pawn_{playerData.playerIndex}_{playerData.playerName}";
            
            LogInfo($"Instantiated pawn object: {pawnObject.name}");
            
            // Create Pawn component and configure it
            Pawn pawn = pawnObject.GetComponent<Pawn>();
            if (pawn == null)
            {
                pawn = pawnObject.AddComponent<Pawn>();
                LogInfo("Added Pawn component to GameObject");
            }
            
            // Initialize pawn with player data
            if (gridSystem != null)
            {
                pawn.Initialize(playerData, gridSystem);
                LogInfo($"Initialized pawn with PlayerData and GridSystem");
            }
            else
            {
                LogError($"GridSystem is null - cannot initialize pawn properly");
                return;
            }
            
            // Position pawn at start position
            Vector3 worldPosition = gridSystem.GridToWorldPosition(playerData.startPosition);
            pawnObject.transform.position = worldPosition;
            LogInfo($"Positioned pawn at grid {playerData.startPosition} -> world {worldPosition}");
            
            // Configure visual appearance
            ConfigurePawnVisuals(pawn, playerData);
            
            // Add to pawn list
            pawns.Add(pawn);
            
            OnPawnInitialized?.Invoke(pawn);
            LogInfo($"Created pawn for {playerData.playerName} at {playerData.startPosition} (total pawns: {pawns.Count})");
        }

        /// <summary>
        /// Create emergency pawn for a specific player when no prefab is available
        /// </summary>
/// <summary>
        /// Create emergency pawn for a specific player when no prefab is available
        /// </summary>
        private void CreateEmergencyPawnForPlayer(PlayerData playerData)
        {
            if (playerData == null)
            {
                LogError("Cannot create emergency pawn: PlayerData is null");
                return;
            }
            
            LogInfo($"Creating emergency pawn for {playerData.playerName}");
            
            if (gridSystem == null)
            {
                LogError("Cannot create emergency pawn: GridSystem not found");
                return;
            }

            // Create primitive cube as emergency pawn
            GameObject emergencyPawn = GameObject.CreatePrimitive(PrimitiveType.Cube);
            emergencyPawn.name = $"EmergencyPawn_{playerData.playerIndex}_{playerData.playerName}";
            emergencyPawn.transform.SetParent(pawnContainer);
            
            // Scale down to appropriate size
            emergencyPawn.transform.localScale = Vector3.one * 0.8f;
            
            // Position at start location
            Vector3 worldPosition = gridSystem.GridToWorldPosition(playerData.startPosition);
            emergencyPawn.transform.position = worldPosition;
            
            // Color the emergency pawn
            var renderer = emergencyPawn.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = playerData.playerColor;
                renderer.material = mat;
            }
            
            // Add Pawn component
            Pawn pawn = emergencyPawn.AddComponent<Pawn>();
            pawn.Initialize(playerData, gridSystem);
            
            // Add to pawn manager list
            pawns.Add(pawn);
            
            LogInfo($"Created emergency pawn for {playerData.playerName} at {playerData.startPosition} (total pawns: {pawns.Count})");
        }

        
        private GameObject GetPawnPrefab(int playerIndex)
        {
            if (prefabReferences?.playerPrefabs == null) return null;
            
            // Cycle through available prefabs if we have more players than prefabs
            int prefabIndex = playerIndex % prefabReferences.playerPrefabs.Length;
            return prefabReferences.playerPrefabs[prefabIndex];
        }
        
        private void ConfigurePawnVisuals(Pawn pawn, PlayerData playerData)
        {
            // Apply player color
            Renderer renderer = pawn.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                // Use shared material to avoid leaks
                Material sharedMat = renderer.sharedMaterial;
                if (sharedMat != null)
                {
                    // Create a material instance only if we need to change color
                    Material coloredMaterial = new Material(sharedMat);
                    coloredMaterial.color = playerData.playerColor;
                    renderer.material = coloredMaterial;
                }
            }
            
            // Scale or modify pawn based on player type if needed
            if (playerData.playerType == PlayerType.AI)
            {
                // Could add special visual indicator for AI players
                pawn.gameObject.name += "_AI";
            }
        }
        
        #endregion
        
        #region Pawn Management
        
        /// <summary>
        /// Set the active pawn (whose turn it is)
        /// </summary>
        public void SetActivePawn(int pawnIndex)
        {
            if (pawnIndex < 0 || pawnIndex >= pawns.Count)
            {
                LogWarning($"Invalid pawn index: {pawnIndex}");
                return;
            }
            
            // Deactivate current pawn
            if (activePawnIndex >= 0 && activePawnIndex < pawns.Count)
            {
                pawns[activePawnIndex].SetActive(false);
            }
            
            // Activate new pawn
            activePawnIndex = pawnIndex;
            pawns[activePawnIndex].SetActive(true);
            
            OnActivePawnChanged?.Invoke(activePawnIndex);
            LogInfo($"Active pawn changed to: {pawns[activePawnIndex].PlayerData.playerName}");
        }
        
        /// <summary>
        /// Move to next player's turn
        /// </summary>
        public void NextTurn()
        {
            if (pawns.Count == 0) return;
            
            int nextIndex = (activePawnIndex + 1) % pawns.Count;
            SetActivePawn(nextIndex);
        }
        
        /// <summary>
        /// Get pawn by index
        /// </summary>
        public Pawn GetPawn(int index)
        {
            return index >= 0 && index < pawns.Count ? pawns[index] : null;
        }
        
        /// <summary>
        /// Get pawn at specific grid position
        /// </summary>
        public Pawn GetPawnAtPosition(Vector2Int gridPosition)
        {
            foreach (Pawn pawn in pawns)
            {
                if (pawn.CurrentPosition == gridPosition)
                    return pawn;
            }
            return null;
        }
        
        /// <summary>
        /// Move a pawn to new position
        /// </summary>
        public bool MovePawn(int pawnIndex, Vector2Int newPosition)
        {
            Pawn pawn = GetPawn(pawnIndex);
            if (pawn == null)
            {
                LogError($"Cannot move pawn: Invalid index {pawnIndex}");
                return false;
            }
            
            // Validate move
            if (!IsValidMove(pawn, newPosition))
            {
                LogWarning($"Invalid move for {pawn.PlayerData.playerName} to {newPosition}");
                return false;
            }
            
            // Execute move
            Vector2Int oldPosition = pawn.CurrentPosition;
            pawn.MoveTo(newPosition);
            
            OnPawnMoved?.Invoke(pawn);
            LogInfo($"Moved {pawn.PlayerData.playerName} from {oldPosition} to {newPosition}");
            return true;
        }
        
        /// <summary>
        /// Check if a move is valid for the given pawn
        /// </summary>
/// <summary>
        /// Check if a move is valid for the given pawn
        /// </summary>
        public bool IsValidMove(Pawn pawn, Vector2Int targetPosition)
        {
            if (pawn == null) return false;
            
            // Check if position is occupied by another pawn
            if (GetPawnAtPosition(targetPosition) != null)
                return false;
            
            // Use grid system to get valid moves and check if target is included
            if (gridSystem != null)
            {
                List<Vector2Int> validMoves = gridSystem.GetValidMoves(pawn.CurrentPosition);
                return validMoves.Contains(targetPosition);
            }
            
            return false;
        }
        
        /// <summary>
        /// Get all valid moves for a pawn
        /// </summary>
        public List<Vector2Int> GetValidMoves(int pawnIndex)
        {
            Pawn pawn = GetPawn(pawnIndex);
            if (pawn == null) return new List<Vector2Int>();
            
            if (gridSystem != null)
            {
                List<Vector2Int> moves = gridSystem.GetValidMoves(pawn.CurrentPosition);
                
                // Filter out positions occupied by other pawns
                moves.RemoveAll(pos => GetPawnAtPosition(pos) != null);
                
                return moves;
            }
            
            return new List<Vector2Int>();
        }
        
        #endregion
        
        #region Event Handling
        
        private void SubscribeToEvents()
        {
            // Listen for session events
            if (sessionManager != null)
            {
                SessionManager.OnSessionStarted += HandleSessionStarted;
                SessionManager.OnTurnStarted += HandleTurnStarted;
            }
        }
        
        private void UnsubscribeFromEvents()
        {
            if (sessionManager != null)
            {
                SessionManager.OnSessionStarted -= HandleSessionStarted;
                SessionManager.OnTurnStarted -= HandleTurnStarted;
            }
        }
        
        private void HandleSessionStarted(SessionData sessionData)
        {
            LogInfo("Session started - initializing pawns");
            InitializePawnsFromSession(sessionData);
        }
        
        private void HandleTurnStarted(int playerIndex, PlayerType playerType)
        {
            LogInfo($"Turn started for player {playerIndex} ({playerType})");
            SetActivePawn(playerIndex);
        }
        
        
        /// <summary>
        /// Show pawns after tile building is complete
        /// </summary>
/// <summary>
        /// Show pawns after tile building is complete
        /// </summary>
        public void ShowPawns()
        {
            LogInfo($"ShowPawns called - earlyInitialized: {earlyInitialized}, pawnsHidden: {pawnsHidden}, pawns.Count: {pawns.Count}");
            
            if (!earlyInitialized)
            {
                LogWarning("Cannot show pawns: not early initialized");
                return;
            }
            
            if (!pawnsHidden)
            {
                LogInfo("Pawns already visible");
                return;
            }
            
            if (pawns.Count == 0)
            {
                LogError("Cannot show pawns: no pawns exist!");
                return;
            }
            
            int shownCount = 0;
            foreach (var pawn in pawns)
            {
                if (pawn?.gameObject != null)
                {
                    pawn.gameObject.SetActive(true);
                    shownCount++;
                    LogInfo($"Showed pawn: {pawn.PlayerData?.playerName ?? "Unknown"} at {pawn.CurrentPosition}");
                }
                else
                {
                    LogError("Found null pawn or null gameObject in pawns list!");
                }
            }
            
            pawnsHidden = false;
            LogInfo($"Showed {shownCount}/{pawns.Count} pawns");
        }
        
        /// <summary>
        /// Hide all pawns (used during early initialization)
        /// </summary>
        public void HideAllPawns()
        {
            foreach (var pawn in pawns)
            {
                if (pawn?.gameObject != null)
                {
                    pawn.gameObject.SetActive(false);
                }
            }
            
            pawnsHidden = true;
            LogInfo($"Hidden {pawns.Count} pawns");
        }
        
        /// <summary>
        /// Check if pawns are initialized but hidden
        /// </summary>
        public bool ArePawnsInitializedButHidden()
        {
            return earlyInitialized && pawnsHidden;
        }
        
        /// <summary>
        /// Check if pawns are fully ready (initialized and visible)
        /// </summary>
        public bool ArePawnsFullyReady()
        {
            return earlyInitialized && !pawnsHidden && pawns.Count > 0;
        }
        
        /// <summary>
        /// Create default session data for early initialization
        /// </summary>
        private SessionData CreateDefaultSessionData(WallChessGameManager gameManager)
        {
            var sessionSettings = new SessionSettings
            {
                gridSize = gameManager.gridSize,
                wallsPerPlayer = gameManager.wallsPerPlayer,
                playerCount = gameManager.numberOfPlayers,
                turnTimeLimit = 30f,
                enableTurnTimer = false
            };

            // Configure players
            sessionSettings.playerConfigurations.Clear();
            for (int i = 0; i < sessionSettings.playerCount; i++)
            {
                var playerType = (i == 0) ? PlayerType.Human : PlayerType.AI;
                var playerName = $"Player {i + 1}";
                var playerColor = GetDefaultPlayerColor(i);
                
                var playerConfig = new PlayerConfiguration(playerType, playerName, playerColor);
                sessionSettings.playerConfigurations.Add(playerConfig);
            }

            return new SessionData(sessionSettings);
        }
        
        /// <summary>
        /// Get default player colors for early initialization
        /// </summary>
        private Color GetDefaultPlayerColor(int playerIndex)
        {
            Color[] colors = { Color.blue, Color.red, Color.green, Color.yellow };
            return playerIndex < colors.Length ? colors[playerIndex] : Color.white;
        }
        
        #endregion
        
        #region Cleanup
        
        private void ClearExistingPawns()
        {
            foreach (Pawn pawn in pawns)
            {
                if (pawn != null && pawn.gameObject != null)
                {
                    DestroyImmediate(pawn.gameObject);
                }
            }
            pawns.Clear();
            activePawnIndex = -1;
            
            LogInfo("Cleared existing pawns");
        }
        
        /// <summary>
        /// Clean up all pawns and reset state
        /// </summary>
public void Cleanup()
        {
            ClearExistingPawns();
            
            // Clear GridSystem cache since pawns are being cleared
            if (gridSystem != null)
            {
                gridSystem.ClearCaches();
            }
            
            LogInfo("PawnManager cleaned up");
        }
        
                
        #region Wall Management
        
        /// <summary>
        /// Get remaining walls for the currently active pawn
        /// </summary>
        public int GetCurrentPlayerWallsRemaining()
        {
            var activePawn = ActivePawn;
            return activePawn?.PlayerData.wallsRemaining ?? 0;
        }
        
        /// <summary>
        /// Get total walls allocated per player at game start
        /// </summary>
        public int GetWallsPerPlayer()
        {
                        if (sessionManager?.CurrentSession?.settings != null)
            {
                return sessionManager.CurrentSession.settings.wallsPerPlayer;
            }
            
            // Fallback to standard Quoridor rules
            return 10;
        }
        
        /// <summary>
        /// Get remaining walls for a specific player index
        /// </summary>
        public int GetPlayerWallsRemaining(int playerIndex)
        {
            var pawn = GetPawn(playerIndex);
            return pawn?.PlayerData.wallsRemaining ?? 0;
        }
        
        /// <summary>
        /// Check if the currently active player can place walls
        /// </summary>
        public bool CanCurrentPlayerPlaceWalls()
        {
            var activePawn = ActivePawn;
            if (activePawn == null) return false;
            
            bool hasWalls = activePawn.PlayerData.wallsRemaining > 0;
                        bool gameAllows = sessionManager?.IsSessionActive ?? true;
            
            return hasWalls && gameAllows;
        }
        
        /// <summary>
        /// Consume a wall from the active player's inventory
        /// Returns true if wall was successfully consumed
        /// </summary>
        public bool ConsumeWallFromActivePlayer()
        {
            var activePawn = ActivePawn;
            if (activePawn == null || activePawn.PlayerData.wallsRemaining <= 0)
            {
                LogWarning("Cannot consume wall: No active player or no walls remaining");
                return false;
            }
            
            activePawn.PlayerData.wallsRemaining--;
            LogInfo($"Wall consumed. {activePawn.PlayerData.playerName} has {activePawn.PlayerData.wallsRemaining} walls remaining");
            return true;
        }
        
        /// <summary>
        /// Set wall count for a specific player (useful for game setup or resets)
        /// </summary>
        public void SetPlayerWallCount(int playerIndex, int wallCount)
        {
            var pawn = GetPawn(playerIndex);
            if (pawn != null)
            {
                pawn.PlayerData.wallsRemaining = Mathf.Max(0, wallCount);
                LogInfo($"Set {pawn.PlayerData.playerName} wall count to {wallCount}");
            }
            else
            {
                LogWarning($"Cannot set wall count: Invalid player index {playerIndex}");
            }
        }
        
        /// <summary>
        /// Check if any player can still place walls
        /// </summary>
        public bool AnyPlayerCanPlaceWalls()
        {
            foreach (var pawn in pawns)
            {
                if (pawn.PlayerData.wallsRemaining > 0)
                    return true;
            }
            return false;
        }
        
        #endregion
        
#endregion
        
        #region Debug Utilities
        
        private void LogInfo(string message)
        {
            if (enableDebugLogs) 
                Debug.Log($"[PawnManager] {message}");
        }
        
        private void LogWarning(string message)
        {
            if (enableDebugLogs) 
                Debug.LogWarning($"[PawnManager] {message}");
        }
        
        private void LogError(string message)
        {
            Debug.LogError($"[PawnManager] {message}");
        }
        
        [ContextMenu("Debug/Print Pawn Info")]
        private void DebugPrintPawnInfo()
        {
            Debug.Log($"PawnManager Status:");
            Debug.Log($"- Total Pawns: {pawns.Count}");
            Debug.Log($"- Active Pawn Index: {activePawnIndex}");
            
            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                Debug.Log($"  Pawn {i}: {pawn.PlayerData.playerName} at {pawn.CurrentPosition} " +
                         $"({pawn.PlayerData.playerType}) - Active: {pawn.IsActive}");
            }
        }
        
        #endregion
    }
}