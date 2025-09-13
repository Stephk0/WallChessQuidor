using UnityEngine;
using WallChess.Core.Session;

namespace WallChess.Core
{
    /// <summary>
    /// Integrates SessionManager with the legacy WallChessGameManager
    /// Ensures proper turn management and communication between systems
    /// </summary>
    [RequireComponent(typeof(SessionManager))]
    public class SessionIntegrator : MonoBehaviour
    {
        [Header("Integration Settings")]
        [SerializeField] private bool autoStartSession = true;
        [SerializeField] private bool debugLogs = true;
        
        private SessionManager sessionManager;
        private WallChessGameManager gameManager;
        
        void Awake()
        {
            InitializeComponents();
        }
        
        void Start()
        {
            if (autoStartSession)
            {
                StartDefaultSession();
            }
        }
        
        private void InitializeComponents()
        {
            sessionManager = GetComponent<SessionManager>();
            gameManager = GetComponent<WallChessGameManager>();
            
            if (sessionManager == null)
            {
                LogError("SessionManager component not found!");
                return;
            }
            
            if (gameManager == null)
            {
                LogError("WallChessGameManager component not found!");
                return;
            }
            
            LogInfo("SessionIntegrator initialized successfully");
        }
        
        /// <summary>
        /// Start a default 2-player session (Player vs AI)
        /// </summary>
        public void StartDefaultSession()
        {
            if (sessionManager == null || gameManager == null)
            {
                LogError("Cannot start session - missing components");
                return;
            }
            
            // Create session settings based on the game manager's settings
            var settings = CreateSessionSettingsFromGameManager();
            
            // Start the session
            bool success = sessionManager.StartSession(settings);
            
            if (success)
            {
                LogInfo("Default session started successfully");
            }
            else
            {
                LogError("Failed to start default session");
            }
        }
        
        /// <summary>
        /// Create session settings that match the WallChessGameManager configuration
        /// </summary>
        private SessionSettings CreateSessionSettingsFromGameManager()
        {
            var settings = new SessionSettings()
            {
                gridSize = gameManager.gridSize,
                wallsPerPlayer = gameManager.wallsPerPlayer,
                playerCount = gameManager.numberOfPlayers,
                turnTimeLimit = 30f,
                enableTurnTimer = false
            };
            
            // Configure players based on the game manager's pawn data
            settings.playerConfigurations.Clear();
            
            for (int i = 0; i < gameManager.numberOfPlayers; i++)
            {
                PlayerType playerType = PlayerType.Human;
                string playerName = $"Player {i + 1}";
                Color playerColor = GetPlayerColor(i);
                
                // Check if this should be an AI player
                if (i == 1 && gameManager.numberOfPlayers == 2)
                {
                    // Second player in 2-player game is typically AI
                    playerType = PlayerType.AI;
                    playerName = "AI Opponent";
                }
                
                var config = new PlayerConfiguration(playerType, playerName, playerColor);
                settings.playerConfigurations.Add(config);
            }
            
            LogInfo($"Created session settings: {settings.playerCount} players, {settings.gridSize}x{settings.gridSize} grid");
            
            return settings;
        }
        
        /// <summary>
        /// Get appropriate color for player index
        /// </summary>
        private Color GetPlayerColor(int playerIndex)
        {
            Color[] colors = { Color.blue, Color.red, Color.green, Color.yellow };
            return playerIndex < colors.Length ? colors[playerIndex] : Color.white;
        }
        
        /// <summary>
        /// Manually start a session with custom settings
        /// </summary>
        public void StartSession(SessionSettings customSettings)
        {
            if (sessionManager == null)
            {
                LogError("SessionManager not available");
                return;
            }
            
            bool success = sessionManager.StartSession(customSettings);
            LogInfo($"Custom session start: {(success ? "Success" : "Failed")}");
        }
        
        /// <summary>
        /// End the current session
        /// </summary>
        public void EndCurrentSession()
        {
            if (sessionManager == null)
            {
                LogError("SessionManager not available");
                return;
            }
            
            sessionManager.EndSession(SessionEndReason.PlayerQuit);
            LogInfo("Session ended by user request");
        }
        
        #region Context Menu Commands
        
        [ContextMenu("Start Default Session")]
        private void Ctx_StartDefaultSession()
        {
            StartDefaultSession();
        }
        
        [ContextMenu("End Current Session")]
        private void Ctx_EndCurrentSession()
        {
            EndCurrentSession();
        }
        
        [ContextMenu("Debug Session Info")]
        private void Ctx_DebugSessionInfo()
        {
            if (sessionManager?.CurrentSession != null)
            {
                var session = sessionManager.CurrentSession;
                Debug.Log($"Session Active: {session.isActive}\\n" +
                         $"Current Player: {session.currentPlayerIndex}\\n" +
                         $"Turn: {session.currentTurn}\\n" +
                         $"Players: {session.playerCount}");
            }
            else
            {
                Debug.Log("No active session");
            }
        }
        
        #endregion
        
        #region Logging
        
        private void LogInfo(string message)
        {
            if (debugLogs) Debug.Log($"[SessionIntegrator] {message}");
        }
        
        private void LogError(string message)
        {
            Debug.LogError($"[SessionIntegrator] {message}");
        }
        
        #endregion
    }
}