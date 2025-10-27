// Force recompilation v2
using UnityEngine;
using WallChess.Core.States;
using WallChess.Core.Session;
using WallChess.Gameplay.Pawns;

        // Force Unity compilation refresh
        
namespace WallChess.Core
{
    /// <summary>
    /// Manages session states for proper game flow coordination
    /// Handles the sequence: BuildTiles → SpawnPawns → ActiveGameplay → GameOver
    /// Integrates with existing WallChessGameManager while providing clean state management
    /// </summary>
    [RequireComponent(typeof(StateMachine))]
        // Force compilation refresh
    
public class SessionStateManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private bool autoStartSession = true;
        [SerializeField] private bool debugLogs = true;
        [SerializeField] private float stateTransitionDelay = 0.1f;

        [Header("References")]
        [SerializeField] private WallChessGameManager gameManager;
        [SerializeField] private StateMachine stateMachine;

        // State instances
        private BuildTilesState buildTilesState;
        private SpawnPawnsState spawnPawnsState;
        private GameplayState gameplayState;
        private GameOverState gameOverState;

        // Session tracking
        private bool sessionStarted = false;
        private bool sessionCompleted = false;
        
        // Events
        public static System.Action<string> OnSessionStateChanged;
        public static System.Action OnSessionReady;
        public static System.Action<int> OnSessionCompleted;

        #region Unity Lifecycle

        void Awake()
        {
            InitializeComponents();
            InitializeStates();
        }

        void Start()
        {
            if (autoStartSession)
            {
                StartSession();
            }
        }

        void Update()
        {
            // Allow manual state monitoring and debugging
            if (debugLogs && stateMachine != null && stateMachine.CurrentState != null)
            {
                // Log state changes (could be optimized to only log on change)
            }
        }

        void OnDestroy()
        {
            // Cleanup if needed
            if (sessionStarted && !sessionCompleted)
            {
                EndSession();
            }
        }

        #endregion

        #region Initialization

        private void InitializeComponents()
        {
            // Auto-find components if not assigned
            if (gameManager == null)
            {
                gameManager = GetComponent<WallChessGameManager>();
                if (gameManager == null)
                {
                    gameManager = Object.FindFirstObjectByType<WallChessGameManager>();
                }
            }

            // IMPORTANT: SessionStateManager needs its own dedicated StateMachine
            // Don't share with MainApplicationController or GameStateController
            if (stateMachine == null)
            {
                // Always create a fresh StateMachine specifically for session management
                GameObject sessionSMObject = new GameObject("SessionStateMachine");
                sessionSMObject.transform.SetParent(transform);
                stateMachine = sessionSMObject.AddComponent<StateMachine>();
                LogInfo("Created dedicated StateMachine for session management");
            }

            if (gameManager == null)
            {
                LogError("WallChessGameManager not found! SessionStateManager requires GameManager.");
                return;
            }

            LogInfo("Components initialized successfully");
        }

        private void InitializeStates()
        {
            if (gameManager == null || stateMachine == null)
            {
                LogError("Cannot initialize states - missing required components");
                return;
            }

            // Create state instances
            buildTilesState = new BuildTilesState(gameManager, stateMachine);
            spawnPawnsState = new SpawnPawnsState(gameManager, stateMachine);  
            gameplayState = new GameplayState(gameManager, stateMachine);
            gameOverState = new GameOverState(gameManager, stateMachine);

            // Register states with state machine
            stateMachine.RegisterState(buildTilesState);
            stateMachine.RegisterState(spawnPawnsState);
            stateMachine.RegisterState((gameplayState));
            stateMachine.RegisterState(gameOverState);

            // Subscribe to state machine events
            stateMachine.OnStateChanged += HandleStateChanged;

            LogInfo("Session states initialized and registered");
        }

        #endregion

        #region Session Management

        /// <summary>
        /// Start the game session with proper state flow
        /// </summary>
        /// <summary>
        /// Start the game session with proper state flow
        /// </summary>
        public void StartSession()
        {
            if (sessionStarted)
            {
                LogWarning("Session already started");
                return;
            }

            if (stateMachine == null)
            {
                LogError("Cannot start session - StateMachine not available");
                return;
            }

            LogInfo("Starting game session with state management");

            // Initialize the game manager if not already done
            if (gameManager != null)
            {
                // Ensure grid system is initialized before starting states
                EnsureGameManagerInitialized();
                
                // ADDED: Initialize pawns early and hide them
                InitializePawnsEarly();
            }

            // Start with BuildTiles state
            bool success = stateMachine.ChangeState<BuildTilesState>();
            if (success)
            {
                sessionStarted = true;
                LogInfo("Session started successfully - entered BuildTiles state");
            }
            else
            {
                LogError("Failed to start session - could not transition to BuildTiles state");
            }
        }
        
        /// <summary>
        /// Initialize pawns early so they're ready when needed
        /// </summary>
        private void InitializePawnsEarly()
        {
            var pawnManager = Object.FindObjectOfType<PawnManager>();
            if (pawnManager == null)
            {
                // Create PawnManager if it doesn't exist
                GameObject pawnManagerObj = new GameObject("PawnManager");
                if (gameManager != null)
                {
                    pawnManagerObj.transform.SetParent(gameManager.transform);
                }
                pawnManager = pawnManagerObj.AddComponent<PawnManager>();
                LogInfo("Created PawnManager for early initialization");
            }

            // Trigger early initialization
            pawnManager.EarlyInitializePawns();
            LogInfo("Early pawn initialization completed");
        }

        /// <summary>
        /// End the current session
        /// </summary>
        public void EndSession()
        {
            if (!sessionStarted || sessionCompleted)
            {
                LogWarning("No active session to end");
                return;
            }

            LogInfo("Ending game session");

            sessionCompleted = true;
            sessionStarted = false;

            // Clean up session state
            OnSessionCompleted?.Invoke(-1); // No winner specified for manual end

            LogInfo("Session ended successfully");
        }

        /// <summary>
        /// Restart the current session
        /// </summary>
        public void RestartSession()
        {
            LogInfo("Restarting game session");

            if (sessionStarted)
            {
                EndSession();
            }

            // Reset session flags
            sessionCompleted = false;

            // Clean up game manager state
            if (gameManager != null)
            {
                gameManager.CleanupGameSession();
            }

            // Small delay to ensure cleanup is complete
            Invoke(nameof(StartSession), stateTransitionDelay);
        }

        #endregion

        #region State Management Integration

        private void EnsureGameManagerInitialized()
        {
            if (gameManager == null) return;

            // Make sure grid system exists before states need it
            var gridSystem = gameManager.GetGridSystem();
            if (gridSystem == null)
            {
                LogInfo("Initializing GameManager before session states");
                gameManager.InitializeGame();
            }
        }

        private void HandleStateChanged(IState oldState, IState newState)
        {
            string oldStateName = oldState?.StateName ?? "None";
            string newStateName = newState?.StateName ?? "None";
            
            LogInfo($"Session state changed: {oldStateName} → {newStateName}");
            
            // Update game manager state to match session state
            UpdateGameManagerState(newStateName);
            
            // Notify listeners
            OnSessionStateChanged?.Invoke(newStateName);

            // Check if we've reached a ready state
            if (newState is GameplayState)
            {
                LogInfo("Session is ready for gameplay");
                OnSessionReady?.Invoke();
            }

            // Check if session is completed
            if (newState is GameOverState && !sessionCompleted)
            {
                HandleSessionCompletion();
            }
        }

private void UpdateGameManagerState(string sessionStateName)
        {
            if (gameManager == null) return;

            // Map session states to game manager states
            GameState gameState = GameState.GameStart;

            switch (sessionStateName)
            {
                case "BuildTiles":
                    gameState = GameState.BuildTiles;
                    break;
                case "SpawnPawns":
                    gameState = GameState.GameStart; // Transitional state
                    break;
                case "Gameplay": // FIXED: Was "ActiveGameplay" but state is named "Gameplay"
                case "ActiveGameplay": // Keep for backwards compatibility
                    gameState = GameState.PlayerTurn;
                    break;
                case "GameOver":
                    gameState = GameState.GameOver;
                    break;
            }

            if (gameManager.GetCurrentState() != gameState)
            {
                gameManager.ChangeState(gameState);
                LogInfo($"Updated GameManager state to: {gameState}");
            }
        }

        private void HandleSessionCompletion()
        {
            LogInfo("Session completed - transitioning to end state");
            
            sessionCompleted = true;
            
            // Could trigger cleanup or transition to menu here
            // For now, just mark as completed
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// Check if session is currently active
        /// </summary>
        public bool IsSessionActive()
        {
            return sessionStarted && !sessionCompleted;
        }

        /// <summary>
        /// Get the current session state name
        /// </summary>
        public string GetCurrentStateName()
        {
            return stateMachine?.CurrentState?.StateName ?? "None";
        }

        /// <summary>
        /// Check if the session is ready for gameplay
        /// </summary>
        public bool IsSessionReady()
        {
            return IsSessionActive() && stateMachine?.CurrentState is GameplayState;
        }

        /// <summary>
        /// Force transition to a specific state (for debugging)
        /// </summary>
        public bool ForceStateTransition<T>() where T : class, IState
        {
            if (stateMachine == null) return false;
            return stateMachine.ChangeState<T>();
        }

        /// <summary>
        /// Skip current state animation/transition (if applicable)
        /// </summary>
        public void SkipCurrentState()
        {
            var currentState = stateMachine?.CurrentState;
            
            if (currentState is BuildTilesState buildState)
            {
                buildState.SkipAnimation();
            }
        }

        #endregion

        #region Context Menu Actions

        [ContextMenu("Session/Start Session")]
        private void MenuStartSession()
        {
            if (Application.isPlaying)
            {
                StartSession();
            }
        }

        [ContextMenu("Session/End Session")]
        private void MenuEndSession()
        {
            if (Application.isPlaying)
            {
                EndSession();
            }
        }

        [ContextMenu("Session/Restart Session")]
        private void MenuRestartSession()
        {
            if (Application.isPlaying)
            {
                RestartSession();
            }
        }

        [ContextMenu("Session/Skip Current State")]
        private void MenuSkipCurrentState()
        {
            if (Application.isPlaying)
            {
                SkipCurrentState();
            }
        }

                        /// <summary>
        /// Force proper session state sequence for debugging
        /// </summary>
        [ContextMenu("Debug/Force Proper Session Flow")]
        private void MenuForcePrperSessionFlow()
        {
            if (Application.isPlaying)
            {
                Debug.Log("Forcing proper session flow: BuildTiles → SpawnPawns → ActiveGameplay");
                
                // Reset and restart session with proper flow
                sessionStarted = false;
                sessionCompleted = false;
                
                // Start with BuildTiles
                stateMachine.ChangeState<BuildTilesState>();
            }
        }

        
[ContextMenu("Debug/Force Spawn Pawns State")]
        private void MenuForceSpawnPawns()
        {
            if (Application.isPlaying)
            {
                ForceStateTransition<SpawnPawnsState>();
            }
        }

        
[ContextMenu("Debug/Print Session Status")]
        private void MenuPrintStatus()
        {
            Debug.Log($"Session Status:\n" +
                     $"Started: {sessionStarted}\n" +
                     $"Completed: {sessionCompleted}\n" +
                     $"Current State: {GetCurrentStateName()}\n" +
                     $"Ready: {IsSessionReady()}");
        }

        #endregion

        #region Logging

        private void LogInfo(string message)
        {
            if (debugLogs) Debug.Log($"[SessionStateManager] {message}");
        }

        private void LogWarning(string message)
        {
            if (debugLogs) Debug.LogWarning($"[SessionStateManager] {message}");
        }

        private void LogError(string message)
        {
            Debug.LogError($"[SessionStateManager] {message}");
        }

        #endregion
    }
}