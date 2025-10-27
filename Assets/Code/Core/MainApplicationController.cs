using UnityEngine;
using WallChess.Core.ApplicationStates;

namespace WallChess.Core
{
    /// <summary>
    /// Available application states for startup selection
    /// </summary>
    public enum ApplicationStartupState
    {
        Start,
        Menu,
        Settings,
        Game,
        GameOver
    }
    /// <summary>
    /// Main Application Controller - manages the high-level application flow
    /// Handles the main FSM: Start > MainMenu > Settings > Game > GameOver > MainMenu
    /// 
    /// This coordinates with WallChessGameManager for the actual gameplay.
    /// Add this component alongside WallChessGameManager (not replace it).
    /// </summary>
    public class MainApplicationController : MonoBehaviour
    {
        [Header("Application FSM")]
        [SerializeField] private bool enableApplicationFSM = true;
        [SerializeField] private bool debugApplicationFSM = true;
        [SerializeField] private ApplicationStartupState startupState = ApplicationStartupState.Start;
        [SerializeField] private bool startInMenuState = false; // Skip StartState for testing (deprecated - use startupState instead)
        
        [Header("References")]
        [SerializeField] private WallChessGameManager gameManager;
        [SerializeField] private StateMachine applicationStateMachine;
        
        // Application state instances
        private StartState startState;
        private MenuState menuState;
        private SettingsState settingsState;
        private ApplicationGameState gameState;
        private ApplicationGameOverState gameOverState;
        
        void Awake()
        {
            // Auto-find components if not assigned
            if (gameManager == null)
                gameManager = GetComponent<WallChessGameManager>();
            
            if (applicationStateMachine == null)
                applicationStateMachine = GetComponent<StateMachine>();
            
            // Create a separate StateMachine for application flow if needed
            if (applicationStateMachine == null)
            {
                // Create a separate GameObject for the application state machine
                var appStateMachineGO = new GameObject("ApplicationStateMachine");
                appStateMachineGO.transform.SetParent(transform);
                applicationStateMachine = appStateMachineGO.AddComponent<StateMachine>();
                Debug.Log("MainApplicationController: Created separate Application StateMachine");
            }
        }
        
        void Start()
        {
            if (enableApplicationFSM)
            {
                InitializeApplicationFSM();
            }
            else
            {
                Debug.Log("MainApplicationController: Application FSM disabled, using legacy flow");
                
                // If FSM disabled, let the original game manager handle everything
                if (gameManager != null)
                {
                    // Don't interfere with the original initialization
                }
            }
        }
        
        void InitializeApplicationFSM()
        {
            if (gameManager == null)
            {
                Debug.LogError("MainApplicationController: WallChessGameManager not found! Cannot initialize application FSM.");
                return;
            }
            
            // Create application state instances
            startState = new StartState(applicationStateMachine);
            menuState = new MenuState(applicationStateMachine, gameManager);
            settingsState = new SettingsState(applicationStateMachine, gameManager);
            gameState = new ApplicationGameState(applicationStateMachine, gameManager);
            gameOverState = new ApplicationGameOverState(applicationStateMachine, gameManager);
            
            // Register states with the application state machine
            applicationStateMachine.RegisterState(startState);
            applicationStateMachine.RegisterState(menuState);
            applicationStateMachine.RegisterState(settingsState);
            applicationStateMachine.RegisterState(gameState);
            applicationStateMachine.RegisterState(gameOverState);
            
            // Subscribe to game events to coordinate state transitions
            SubscribeToGameEvents();
            
            // Start the application flow based on selected startup state
            IState initialState = GetInitialState();
            
            applicationStateMachine.ChangeState(initialState);
            
            if (debugApplicationFSM)
                Debug.Log($"MainApplicationController: Started in {initialState.StateName}");
            
            if (debugApplicationFSM)
                Debug.Log("MainApplicationController: Application FSM initialized and running");
        }
        
        void SubscribeToGameEvents()
        {
            // Subscribe to game manager events to coordinate application flow
            WallChessGameManager.OnPlayerVictory += OnGameVictory;
            
            if (debugApplicationFSM)
                Debug.Log("MainApplicationController: Subscribed to game events");
        }
        
        void OnGameVictory(int winningPlayer)
        {
            if (debugApplicationFSM)
                Debug.Log($"MainApplicationController: Game victory detected - Player {winningPlayer}");
            
            // Transition to application game over state
            if (enableApplicationFSM && applicationStateMachine.IsInState<ApplicationGameState>())
            {
                applicationStateMachine.ChangeState(gameOverState);
            }
        }
        
        #region Public API for UI/External Systems
        
        /// <summary>
        /// Start a new game from menu or settings
        /// </summary>
        public void StartNewGame()
        {
            if (enableApplicationFSM)
            {
                applicationStateMachine.ChangeState(gameState);
            }
            else
            {
                Debug.LogWarning("MainApplicationController: Cannot start game - Application FSM is disabled");
            }
        }
        
        /// <summary>
        /// Return to main menu from any state
        /// </summary>
        public void ReturnToMainMenu()
        {
            if (enableApplicationFSM)
            {
                applicationStateMachine.ChangeState(menuState);
            }
            else
            {
                Debug.LogWarning("MainApplicationController: Cannot return to menu - Application FSM is disabled");
            }
        }
        
        /// <summary>
        /// Open settings menu
        /// </summary>
        public void OpenSettings()
        {
            if (enableApplicationFSM)
            {
                applicationStateMachine.ChangeState(settingsState);
            }
            else
            {
                Debug.LogWarning("MainApplicationController: Cannot open settings - Application FSM is disabled");
            }
        }
        
        /// <summary>
        /// Get current application state name for UI
        /// </summary>
        public string GetCurrentApplicationState()
        {
            return applicationStateMachine?.CurrentState?.StateName ?? "None";
        }
        
        /// <summary>
        /// Check if currently in game
        /// </summary>
        public bool IsInGameState()
        {
            return enableApplicationFSM && applicationStateMachine.IsInState<ApplicationGameState>();
        }
        
        /// <summary>
        /// Check if currently in menu
        /// </summary>
        public bool IsInMenuState()
        {
            return enableApplicationFSM && applicationStateMachine.IsInState<MenuState>();
        }
        
        #endregion
        
        #region Debug Methods
        
        [ContextMenu("Debug/Print Application State")]
        private void DebugPrintApplicationState()
        {
            Debug.Log($"Application State: {GetCurrentApplicationState()}");
            
            if (gameManager != null)
            {
                Debug.Log($"Game Manager State: {gameManager.GetCurrentState()}");
                Debug.Log($"Active Player: {gameManager.GetActivePawnIndex()}");
            }
        }
        
        [ContextMenu("Debug/Force Return to Menu")]
        private void DebugForceReturnToMenu()
        {
            ReturnToMainMenu();
        }
        
        [ContextMenu("Debug/Force Start Game")]
        private void DebugForceStartGame()
        {
            StartNewGame();
        }
        
        [ContextMenu("Debug/Toggle Application FSM")]
        private void DebugToggleApplicationFSM()
        {
            enableApplicationFSM = !enableApplicationFSM;
            Debug.Log($"Application FSM {(enableApplicationFSM ? "ENABLED" : "DISABLED")}");
            
            if (!enableApplicationFSM && applicationStateMachine != null)
            {
                // Clean exit from current state
                applicationStateMachine.CurrentState?.OnExit();
            }
        }
        
        [ContextMenu("Debug/Simulate Victory")]
        private void DebugSimulateVictory()
        {
            // Simulate a victory for testing
            OnGameVictory(0); // Player 0 wins
        }
        
        
        /// <summary>
        /// Get the initial state based on startup configuration
        /// </summary>
        private IState GetInitialState()
        {
            // Handle legacy startInMenuState for backward compatibility
            if (startInMenuState)
            {
                Debug.LogWarning("MainApplicationController: Using deprecated startInMenuState. Please use startupState dropdown instead.");
                return menuState;
            }
            
            // Use new dropdown selection
            return startupState switch
            {
                ApplicationStartupState.Start => startState,
                ApplicationStartupState.Menu => menuState,
                ApplicationStartupState.Settings => settingsState,
                ApplicationStartupState.Game => gameState,
                ApplicationStartupState.GameOver => gameOverState,
                _ => startState // Default fallback
            };
        }
        
        #endregion
        
        #region Editor Debug Methods
        
        [ContextMenu("Debug/Force Start State")]
        private void DebugForceStartState()
        {
            if (enableApplicationFSM && applicationStateMachine != null)
            {
                applicationStateMachine.ChangeState(startState);
                Debug.Log("Forced transition to StartState");
            }
        }
        
        [ContextMenu("Debug/Force Menu State")]
        private void DebugForceMenuState()
        {
            if (enableApplicationFSM && applicationStateMachine != null)
            {
                applicationStateMachine.ChangeState(menuState);
                Debug.Log("Forced transition to MenuState");
            }
        }
        
        [ContextMenu("Debug/Force Settings State")]
        private void DebugForceSettingsState()
        {
            if (enableApplicationFSM && applicationStateMachine != null)
            {
                applicationStateMachine.ChangeState(settingsState);
                Debug.Log("Forced transition to SettingsState");
            }
        }
        
        [ContextMenu("Debug/Force Game State")]
        private void DebugForceGameState()
        {
            if (enableApplicationFSM && applicationStateMachine != null)
            {
                applicationStateMachine.ChangeState(gameState);
                Debug.Log("Forced transition to GameState");
            }
        }
        
        [ContextMenu("Debug/Force GameOver State")]
        private void DebugForceGameOverState()
        {
            if (enableApplicationFSM && applicationStateMachine != null)
            {
                applicationStateMachine.ChangeState(gameOverState);
                Debug.Log("Forced transition to GameOverState");
            }
        }
        
        [ContextMenu("Debug/Cycle Through All States")]
        private void DebugCycleStates()
        {
            if (!enableApplicationFSM || applicationStateMachine == null) return;
            
            StartCoroutine(CycleStatesCoroutine());
        }
        
        private System.Collections.IEnumerator CycleStatesCoroutine()
        {
            var states = new IState[] { startState, menuState, settingsState, gameState, gameOverState };
            
            foreach (var state in states)
            {
                applicationStateMachine.ChangeState(state);
                Debug.Log($"Cycling to: {state.StateName}");
                yield return new WaitForSeconds(2f);
            }
            
            Debug.Log("State cycling complete");
        }

        
        #endregion
        
        void OnDestroy()
        {
            // Unsubscribe from events
            WallChessGameManager.OnPlayerVictory -= OnGameVictory;
            
            if (debugApplicationFSM)
                Debug.Log("MainApplicationController: Cleaned up and unsubscribed from events");
        }
        
        void OnValidate()
        {
            // Ensure we have a game manager reference in editor
            if (gameManager == null)
            {
                gameManager = GetComponent<WallChessGameManager>();
            }
        }
    }
}