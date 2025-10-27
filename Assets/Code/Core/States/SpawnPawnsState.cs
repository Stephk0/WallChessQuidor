using UnityEngine;
using WallChess.Core.Session;
using WallChess.Gameplay.Pawns;
using GamePawn = WallChess.Gameplay.Pawns.Pawn;

// Force Unity recompilation
namespace WallChess.Core.States
{
    /// <summary>
    /// State that handles pawn spawning after tiles are built
    /// Initializes PawnManager and spawns pawns based on session settings
    /// Transitions to ActiveGameplayState when pawns are ready
    /// </summary>
    public class SpawnPawnsState : BaseState
    {
        private PawnManager pawnManager;
        private bool pawnsSpawned = false;
        private bool controllersInitialized = false;

        public SpawnPawnsState(WallChessGameManager gameManager, StateMachine stateMachine) 
            : base(gameManager, stateMachine)
        {
        }

        public override string StateName => "SpawnPawns";

        public override void OnEnter()
        {
            base.OnEnter();
            
            LogInfo("Entering SpawnPawns state - preparing to spawn players");
            
            // Initialize pawn manager and spawn pawns
            InitializePawnManager();
            
            // Initialize pawn controller after pawns are spawned
            InitializePawnController();
            
            // Setup initial game state
            SetupInitialGameState();
            
            // Immediately transition to gameplay (could add delay if needed)
            LogInfo("Pawns spawned successfully - transitioning to ActiveGameplay state");
            stateMachine.ChangeState<GameplayState>();
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            // This state typically transitions immediately, but could add validation here
        }

        public override void OnExit()
        {
            base.OnExit();
            
            LogInfo("Exiting SpawnPawns state - game ready for play");
        }

        #region Pawn Management

private void InitializePawnManager()
        {
            // Find existing PawnManager
            pawnManager = Object.FindObjectOfType<PawnManager>();
            if (pawnManager == null)
            {
                GameObject pawnManagerObj = new GameObject("PawnManager");
                pawnManagerObj.transform.SetParent(gameManager.transform);
                pawnManager = pawnManagerObj.AddComponent<PawnManager>();
                LogInfo("Created new PawnManager");
            }

            // Check if pawns were already initialized early
            if (pawnManager.ArePawnsInitializedButHidden())
            {
                LogInfo("Pawns already initialized early - just showing them");
                
                // Validate prefab references are still set
                if (gameManager.prefabReferences != null)
                {
                    pawnManager.SetPrefabReferences(gameManager.prefabReferences);
                }
                
                // Show the hidden pawns
                pawnManager.ShowPawns();
                
                pawnsSpawned = true;
                LogInfo($"Successfully revealed {pawnManager.PawnCount} early-initialized pawns");
                return; // IMPORTANT: Return here to avoid fallback initialization
            }

            // Fallback to normal initialization if early init wasn't done
            LogWarning("Pawns were not early initialized - falling back to normal initialization");
            InitializePawnsNormally();
        }
        
        /// <summary>
        /// Normal pawn initialization (fallback when early init wasn't done)
        /// </summary>
        private void InitializePawnsNormally()
        {
            // Validate prefab references BEFORE attempting spawn
            if (gameManager.prefabReferences == null)
            {
                LogError("CRITICAL: PrefabReferences is null in WallChessGameManager!");
                LogError("Please assign a PrefabReferences ScriptableObject in the inspector.");
                CreateEmergencyPawns();
                return;
            }

            if (gameManager.prefabReferences.playerPrefabs == null || gameManager.prefabReferences.playerPrefabs.Length == 0)
            {
                LogError("CRITICAL: No player prefabs assigned in PrefabReferences!");
                LogError("Please assign player prefabs in the PrefabReferences ScriptableObject.");
                CreateEmergencyPawns();
                return;
            }

            // Check if any player prefabs are assigned
            bool hasPrefabs = false;
            for (int i = 0; i < gameManager.prefabReferences.playerPrefabs.Length; i++)
            {
                if (gameManager.prefabReferences.playerPrefabs[i] != null)
                {
                    hasPrefabs = true;
                    break;
                }
            }

            if (!hasPrefabs)
            {
                LogError("CRITICAL: All player prefabs are null in PrefabReferences!");
                CreateEmergencyPawns();
                return;
            }

            // Set prefab references
            pawnManager.SetPrefabReferences(gameManager.prefabReferences);
            LogInfo("Assigned prefab references to PawnManager");

            // Create session data and initialize pawns
            var sessionData = CreateSessionDataFromGameManager();
            if (sessionData == null)
            {
                LogError("Failed to create session data!");
                CreateEmergencyPawns();
                return;
            }

            pawnManager.InitializePawnsFromSession(sessionData);
            
            pawnsSpawned = true;
            LogInfo($"Successfully spawned {pawnManager.PawnCount} pawns normally");

            // Validate that pawns were actually created
            if (pawnManager.PawnCount == 0)
            {
                LogError("No pawns were created! Creating emergency pawns.");
                CreateEmergencyPawns();
            }
        }

        private void InitializePawnController()
        {
            if (!pawnsSpawned)
            {
                LogError("Cannot initialize PawnController - pawns not spawned yet");
                return;
            }

            // Remove any old pawn controllers
            var oldController = gameManager.GetComponent<PawnController>();
            if (oldController != null)
            {
                Object.DestroyImmediate(oldController);
                LogInfo("Removed old PawnController");
            }

            // Add and initialize new PawnController
            var pawnController = gameManager.gameObject.AddComponent<PawnController>();
            
            // Get required dependencies
            var gridSystem = gameManager.GetGridSystem();
            var highlightManager = gameManager.GetComponent<HighlightManager>();
            
            if (highlightManager == null)
            {
                highlightManager = gameManager.gameObject.AddComponent<HighlightManager>();
                highlightManager.Initialize(gameManager.highlightPrefab, gameManager.highlightConfirmPrefab);
                LogInfo("Created and initialized HighlightManager");
            }

            // Initialize pawn controller
            if (pawnManager != null && gridSystem != null)
            {
                pawnController.Initialize(pawnManager, gridSystem, highlightManager);
                controllersInitialized = true;
                LogInfo("PawnController initialized successfully");
            }
            else
            {
                LogError("Failed to initialize PawnController - missing dependencies");
            }
        }

        private void SetupInitialGameState()
        {
            if (!pawnsSpawned || !controllersInitialized)
            {
                LogError("Cannot setup game state - pawns or controllers not ready");
                return;
            }

            var gridSystem = gameManager.GetGridSystem();
            if (gridSystem == null)
            {
                LogError("GridSystem not found - cannot setup initial occupancy");
                return;
            }

            // Set initial tile occupancy based on pawn positions
            foreach (var pawn in pawnManager.Pawns)
            {
                gridSystem.SetTileOccupied(pawn.CurrentPosition, true);
                LogInfo($"Set tile {pawn.CurrentPosition} as occupied by {pawn.PlayerData.playerName}");
            }

            // Initialize WallValidator with current pawns
            var wallValidator = new WallValidator(gridSystem, gameManager);
            LogInfo("WallValidator initialized");

            // Set the first player as active
            if (pawnManager.PawnCount > 0)
            {
                pawnManager.SetActivePawn(0);
                gameManager.activePlayerIndex = 0;
                LogInfo($"Set active player to: {pawnManager.ActivePawn?.PlayerData.playerName}");
            }
        }

        private SessionData CreateSessionDataFromGameManager()
        {
            // Create session settings based on game manager configuration
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
                var playerColor = GetPlayerColor(i);
                
                var playerConfig = new PlayerConfiguration(playerType, playerName, playerColor);
                sessionSettings.playerConfigurations.Add(playerConfig);
            }

            return new SessionData(sessionSettings);
        }

        private Color GetPlayerColor(int playerIndex)
        {
            Color[] playerColors = {
                Color.blue,      // Player 1
                Color.red,       // Player 2  
                Color.green,     // Player 3
                Color.yellow     // Player 4
            };
            
            return playerIndex < playerColors.Length ? playerColors[playerIndex] : Color.white;
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// Check if pawns are spawned and ready
        /// </summary>
        public bool ArePawnsReady()
        {
            return pawnsSpawned && controllersInitialized;
        }

        /// <summary>
        /// Get the spawned pawn manager
        /// </summary>
        public PawnManager GetPawnManager()
        {
            return pawnManager;
        }

        #endregion

        
        #region Emergency Pawn Creation
        
        /// <summary>
        /// Create basic pawns when prefabs are missing
        /// </summary>
        private void CreateEmergencyPawns()
        {
            LogInfo("Creating emergency pawns with primitive objects");

            if (pawnManager == null) return;

            // Create basic session data
            var sessionSettings = new SessionSettings
            {
                gridSize = gameManager.gridSize,
                wallsPerPlayer = gameManager.wallsPerPlayer,
                playerCount = 2, // Start with 2 players
                turnTimeLimit = 30f,
                enableTurnTimer = false
            };

            // Configure minimal player setup
            sessionSettings.playerConfigurations.Clear();
            for (int i = 0; i < 2; i++)
            {
                var playerType = (i == 0) ? PlayerType.Human : PlayerType.AI;
                var playerName = $"Player {i + 1}";
                var playerColor = (i == 0) ? Color.blue : Color.red;
                
                var playerConfig = new PlayerConfiguration(playerType, playerName, playerColor);
                sessionSettings.playerConfigurations.Add(playerConfig);
            }

            var emergencySessionData = new SessionData(sessionSettings);

            // Create emergency pawns manually
            CreateEmergencyPawnObjects(emergencySessionData);
            
            pawnsSpawned = true;
            LogInfo("Emergency pawns created successfully");
        }

private void CreateEmergencyPawnObjects(SessionData sessionData)
        {
            var gridSystem = gameManager.GetGridSystem();
            if (gridSystem == null)
            {
                LogError("Cannot create emergency pawns: GridSystem not found");
                return;
            }

            for (int i = 0; i < sessionData.playerCount; i++)
            {
                PlayerData playerData = sessionData.GetPlayer(i);
                if (playerData == null) continue;

                // Create primitive cube as emergency pawn
                GameObject emergencyPawn = GameObject.CreatePrimitive(PrimitiveType.Cube);
                emergencyPawn.name = $"EmergencyPawn_{i}_{playerData.playerName}";
                emergencyPawn.transform.SetParent(pawnManager.transform);
                
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
                
                // Add Pawn component using alias to avoid namespace conflicts
                GamePawn pawn = emergencyPawn.AddComponent<GamePawn>();
                pawn.Initialize(playerData, gridSystem);
                
                // Add to pawn manager manually using reflection
                var pawnsField = typeof(PawnManager).GetField("pawns", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (pawnsField != null)
                {
                    var pawnsList = pawnsField.GetValue(pawnManager) as System.Collections.Generic.List<GamePawn>;
                    pawnsList?.Add(pawn);
                }
                
                LogInfo($"Created emergency pawn for {playerData.playerName} at {playerData.startPosition}");
            }

            // Set the first pawn as active
            if (sessionData.playerCount > 0)
            {
                pawnManager.SetActivePawn(0);
            }
        }

        #endregion
        
        #region Diagnostics
        
        [ContextMenu("Debug/Print Diagnostics")]
        public void PrintDiagnostics()
        {
            Debug.Log("=== SPAWN PAWNS STATE DIAGNOSTICS ===");
            Debug.Log($"Game Manager: {(gameManager != null ? "✅" : "❌")}");
            Debug.Log($"PrefabReferences: {(gameManager?.prefabReferences != null ? "✅" : "❌")}");
            
            if (gameManager?.prefabReferences?.playerPrefabs != null)
            {
                Debug.Log($"Player Prefabs Count: {gameManager.prefabReferences.playerPrefabs.Length}");
                for (int i = 0; i < gameManager.prefabReferences.playerPrefabs.Length; i++)
                {
                    var prefab = gameManager.prefabReferences.playerPrefabs[i];
                    Debug.Log($"  Prefab {i}: {(prefab != null ? prefab.name : "NULL")}");
                }
            }
            
            Debug.Log($"Pawn Manager: {(pawnManager != null ? "✅" : "❌")}");
            Debug.Log($"Pawns Spawned: {pawnsSpawned}");
            Debug.Log($"Controllers Initialized: {controllersInitialized}");
            
            if (pawnManager != null)
            {
                Debug.Log($"Pawn Count: {pawnManager.PawnCount}");
                Debug.Log($"Active Pawn: {pawnManager.ActivePawn?.PlayerData.playerName ?? "None"}");
            }
        }
        
        [ContextMenu("Debug/Force Emergency Pawns")]
        public void ForceEmergencyPawns()
        {
            CreateEmergencyPawns();
        }
        
        #endregion

        
#region Logging

        private void LogInfo(string message)
        {
            Debug.Log($"[SpawnPawnsState] {message}");
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"[SpawnPawnsState] {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"[SpawnPawnsState] {message}");
        }

        #endregion
    }
}