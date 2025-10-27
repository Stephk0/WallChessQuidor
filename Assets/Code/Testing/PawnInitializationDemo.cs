using UnityEngine;
using WallChess.Core;
using WallChess.Core.Session;
using WallChess.Core.ApplicationStates;
using WallChess.Gameplay.Pawns;

namespace WallChess.Testing
{
    /// <summary>
    /// Demo script showing how to initialize the game and pawns using the state machine
    /// This demonstrates the proper integration of PawnManager with SessionManager and StateMachine
    /// 
    /// USAGE:
    /// 1. Attach this to a GameObject in the scene
    /// 2. Press Space to start a 2-player game (Human vs AI)
    /// 3. Press 1-4 to start different player configurations
    /// 4. Press R to restart current game
    /// 5. Press M to return to menu
    /// </summary>
    public class PawnInitializationDemo : MonoBehaviour
    {
        [Header("Required Components")]
        [SerializeField] private StateMachine stateMachine;
        [SerializeField] private SessionManager sessionManager;
        [SerializeField] private WallChessGameManager gameManager;
        
        [Header("Demo Settings")]
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private bool autoFindComponents = true;
        
        private PawnManager pawnManager;
        
        #region Unity Lifecycle
        
        void Awake()
        {
            if (autoFindComponents)
            {
                FindRequiredComponents();
            }
            
            InitializeStateMachine();
        }
        
        void Start()
        {
            SubscribeToEvents();
            LogInfo("PawnInitializationDemo ready. Press SPACE to start a game.");
        }
        
        void Update()
        {
            HandleDemoInput();
        }
        
        void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
        
        #endregion
        
        #region Initialization
        
        private void FindRequiredComponents()
        {
            if (stateMachine == null)
                stateMachine = FindObjectOfType<StateMachine>();
            
            if (sessionManager == null)
                sessionManager = FindObjectOfType<SessionManager>();
            
            if (gameManager == null)
                gameManager = FindObjectOfType<WallChessGameManager>();
            
            LogInfo("Auto-found components:");
            LogInfo($"- StateMachine: {(stateMachine != null ? "Found" : "Missing")}");
            LogInfo($"- SessionManager: {(sessionManager != null ? "Found" : "Missing")}");
            LogInfo($"- GameManager: {(gameManager != null ? "Found" : "Missing")}");
        }
        
        private void InitializeStateMachine()
        {
            if (stateMachine == null)
            {
                LogError("StateMachine not found! Cannot initialize demo.");
                return;
            }
            
            // Register application states
            stateMachine.RegisterState(new MenuState(stateMachine, gameManager));
            stateMachine.RegisterState(new ApplicationGameState(stateMachine, gameManager));
            stateMachine.RegisterState(new ApplicationGameOverState(stateMachine, gameManager));
            
            // Start in menu state
            stateMachine.ChangeState<MenuState>();
            
            LogInfo("StateMachine initialized with application states");
        }
        
        #endregion
        
        #region Input Handling
        
        private void HandleDemoInput()
        {
            // Start 2-player game (Human vs AI)
            if (Input.GetKeyDown(KeyCode.Space))
            {
                StartTwoPlayerGame();
            }
            
            // Start 2-player human vs human
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                StartTwoPlayerHumanGame();
            }
            
            // Start 4-player game
            if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                StartFourPlayerGame();
            }
            
            // Restart current game
            if (Input.GetKeyDown(KeyCode.R))
            {
                RestartCurrentGame();
            }
            
            // Return to menu
            if (Input.GetKeyDown(KeyCode.M))
            {
                ReturnToMenu();
            }
            
            // Debug: Print current state
            if (Input.GetKeyDown(KeyCode.P))
            {
                PrintCurrentState();
            }
        }
        
        #endregion
        
        #region Game Starting Methods
        
        /// <summary>
        /// Start a standard 2-player game: Human vs AI
        /// </summary>
        public void StartTwoPlayerGame()
        {
            LogInfo("Starting 2-player game (Human vs AI)...");
            
            var settings = SessionSettings.CreateDefault();
            StartGameWithSettings(settings);
        }
        
        /// <summary>
        /// Start a 2-player human vs human game
        /// </summary>
        public void StartTwoPlayerHumanGame()
        {
            LogInfo("Starting 2-player game (Human vs Human)...");
            
            var settings = SessionSettings.CreateTwoPlayer();
            StartGameWithSettings(settings);
        }
        
        /// <summary>
        /// Start a 4-player game with AI opponents
        /// </summary>
        public void StartFourPlayerGame()
        {
            LogInfo("Starting 4-player game...");
            
            var settings = SessionSettings.CreateFourPlayer();
            StartGameWithSettings(settings);
        }
        
        /// <summary>
        /// Generic method to start game with custom settings
        /// </summary>
        public void StartGameWithSettings(SessionSettings settings)
        {
            if (sessionManager == null)
            {
                LogError("Cannot start game: SessionManager not found!");
                return;
            }
            
            // Transition to game state first
            if (stateMachine != null)
            {
                stateMachine.ChangeState<ApplicationGameState>();
            }
            
            // Start the session (this will trigger pawn initialization)
            bool success = sessionManager.StartSession(settings);
            
            if (success)
            {
                LogInfo($"Game started successfully with {settings.playerCount} players");
                
                // Get reference to pawn manager for demo purposes
                pawnManager = gameManager?.GetComponent<PawnManager>();
                if (pawnManager != null)
                {
                    LogInfo($"PawnManager found with {pawnManager.PawnCount} pawns");
                }
            }
            else
            {
                LogError("Failed to start game session");
            }
        }
        
        /// <summary>
        /// Restart the current game session
        /// </summary>
        public void RestartCurrentGame()
        {
            LogInfo("Restarting current game...");
            
            var gameState = stateMachine?.GetState<ApplicationGameState>();
            if (gameState != null)
            {
                gameState.RestartCurrentGame();
            }
            else
            {
                LogWarning("Cannot restart: Not in game state");
            }
        }
        
        /// <summary>
        /// Return to main menu
        /// </summary>
        public void ReturnToMenu()
        {
            LogInfo("Returning to menu...");
            
            if (stateMachine != null)
            {
                stateMachine.ChangeState<MenuState>();
            }
        }
        
        #endregion
        
        #region Event Handling
        
        private void SubscribeToEvents()
        {
            // Listen for session events
            SessionManager.OnSessionStarted += HandleSessionStarted;
            SessionManager.OnTurnStarted += HandleTurnStarted;
            SessionManager.OnSessionComplete += HandleSessionComplete;
            
            // Listen for pawn events
            PawnManager.OnActivePawnChanged += HandleActivePawnChanged;
            PawnManager.OnPawnMoved += HandlePawnMoved;
            PawnManager.OnPawnInitialized += HandlePawnInitialized;
        }
        
        private void UnsubscribeFromEvents()
        {
            SessionManager.OnSessionStarted -= HandleSessionStarted;
            SessionManager.OnTurnStarted -= HandleTurnStarted;
            SessionManager.OnSessionComplete -= HandleSessionComplete;
            
            PawnManager.OnActivePawnChanged -= HandleActivePawnChanged;
            PawnManager.OnPawnMoved -= HandlePawnMoved;
            PawnManager.OnPawnInitialized -= HandlePawnInitialized;
        }
        
        private void HandleSessionStarted(SessionData sessionData)
        {
            LogInfo($"Session started: {sessionData.playerCount} players");
            
            // Print player information
            for (int i = 0; i < sessionData.playerCount; i++)
            {
                var player = sessionData.GetPlayer(i);
                LogInfo($"- Player {i}: {player.playerName} ({player.playerType}) at {player.startPosition}");
            }
        }
        
        private void HandleTurnStarted(int playerIndex, PlayerType playerType)
        {
            LogInfo($"Turn started: Player {playerIndex} ({playerType})");
        }
        
        private void HandleSessionComplete(SessionEndReason reason, int winnerIndex)
        {
            LogInfo($"Session complete: {reason}, Winner: {(winnerIndex >= 0 ? $"Player {winnerIndex}" : "None")}");
        }
        
        private void HandleActivePawnChanged(int pawnIndex)
        {
            LogInfo($"Active pawn changed to: {pawnIndex}");
            
            if (pawnManager != null)
            {
                var activePawn = pawnManager.ActivePawn;
                if (activePawn != null)
                {
                    LogInfo($"Active pawn: {activePawn.PlayerData.playerName} at {activePawn.CurrentPosition}");
                }
            }
        }
        
        private void HandlePawnMoved(WallChess.Gameplay.Pawns.Pawn pawn)
        {
            LogInfo($"Pawn moved: {pawn.PlayerData.playerName} to {pawn.CurrentPosition}");
            
            // Check for victory
            if (pawn.HasWon())
            {
                LogInfo($"VICTORY! {pawn.PlayerData.playerName} has won the game!");
            }
        }
        
        private void HandlePawnInitialized(WallChess.Gameplay.Pawns.Pawn pawn)
        {
            LogInfo($"Pawn initialized: {pawn.PlayerData.playerName} ({pawn.PlayerData.playerType})");
        }
        
        #endregion
        
        #region Debug Methods
        
        [ContextMenu("Print Current State")]
        public void PrintCurrentState()
        {
            LogInfo("=== CURRENT STATE SUMMARY ===");
            
            // State Machine info
            if (stateMachine != null)
            {
                LogInfo($"Current State: {stateMachine.CurrentState?.StateName ?? "None"}");
            }
            
            // Session info
            if (sessionManager != null && sessionManager.IsSessionActive)
            {
                var session = sessionManager.CurrentSession;
                LogInfo($"Session Active: {session.playerCount} players, Turn {session.currentTurn}");
                LogInfo($"Current Player: {session.currentPlayerIndex}");
            }
            else
            {
                LogInfo("No active session");
            }
            
            // Pawn info
            if (pawnManager != null)
            {
                LogInfo($"PawnManager: {pawnManager.PawnCount} pawns, Active: {pawnManager.ActivePawnIndex}");
                
                for (int i = 0; i < pawnManager.PawnCount; i++)
                {
                    var pawn = pawnManager.GetPawn(i);
                    if (pawn != null)
                    {
                        LogInfo($"  Pawn {i}: {pawn.ToString()}");
                    }
                }
            }
            else
            {
                LogInfo("No PawnManager found");
            }
        }
        
        [ContextMenu("Demo Controls Help")]
        public void PrintDemoControls()
        {
            LogInfo("=== DEMO CONTROLS ===");
            LogInfo("SPACE - Start 2-player game (Human vs AI)");
            LogInfo("1 - Start 2-player game (Human vs Human)");
            LogInfo("4 - Start 4-player game");
            LogInfo("R - Restart current game");
            LogInfo("M - Return to menu");
            LogInfo("P - Print current state");
        }
        
        private void LogInfo(string message)
        {
            if (enableDebugLogs)
                Debug.Log($"[PawnDemo] {message}");
        }
        
        private void LogWarning(string message)
        {
            if (enableDebugLogs)
                Debug.LogWarning($"[PawnDemo] {message}");
        }
        
        private void LogError(string message)
        {
            Debug.LogError($"[PawnDemo] {message}");
        }
        
        #endregion
    }
}