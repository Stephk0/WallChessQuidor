using UnityEngine;
using WallChess.Core.Session;

namespace WallChess.Core
{
    /// <summary>
    /// Example integration script showing how to use SessionManager with existing systems
    /// This demonstrates the key integration points and can be used as a template
    /// </summary>
    public class SessionIntegrationExample : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private SessionManager sessionManager;
        [SerializeField] private WallChessGameManager gameManager;
        [SerializeField] private AIOpponent aiOpponent;
        [SerializeField] private PlayerControllerV2 playerController;
        
        [Header("Settings")]
        [SerializeField] private bool startSessionOnPlay = true;
        [SerializeField] private bool enableDebugLogs = true;
        
        void Start()
        {
            if (startSessionOnPlay)
            {
                StartExampleSession();
            }
            
            SubscribeToEvents();
        }
        
        void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
        
        #region Example Session Management
        
        /// <summary>
        /// Example: Start a 2-player session (Human vs AI)
        /// </summary>
        [ContextMenu("Start Human vs AI Session")]
        public void StartExampleSession()
        {
            if (sessionManager == null)
            {
                LogError("SessionManager reference not set!");
                return;
            }
            
            // Create session settings
            var settings = SessionSettings.CreateDefault(); // Human vs AI
            settings.gridSize = gameManager != null ? gameManager.gridSize : 9;
            settings.wallsPerPlayer = gameManager != null ? gameManager.wallsPerPlayer : 9;
            
            // Start the session
            bool success = sessionManager.StartSession(settings);
            
            if (success)
            {
                LogInfo("Example session started successfully!");
            }
            else
            {
                LogError("Failed to start example session");
            }
        }
        
        /// <summary>
        /// Example: Start a 2-player human session
        /// </summary>
        [ContextMenu("Start Human vs Human Session")]
        public void StartTwoPlayerSession()
        {
            if (sessionManager == null)
            {
                LogError("SessionManager reference not set!");
                return;
            }
            
            var settings = SessionSettings.CreateTwoPlayer();
            settings.gridSize = gameManager != null ? gameManager.gridSize : 9;
            
            bool success = sessionManager.StartSession(settings);
            LogInfo(success ? "Two player session started!" : "Failed to start two player session");
        }
        
        #endregion
        
        #region Event Handling Examples
        
        private void SubscribeToEvents()
        {
            // Session Manager events
            SessionManager.OnSessionStarted += HandleSessionStarted;
            SessionManager.OnSessionEnded += HandleSessionEnded;
            SessionManager.OnTurnStarted += HandleTurnStarted;
            SessionManager.OnTurnCompleted += HandleTurnCompleted;
            
            // Turn Manager events
            TurnManager.OnTurnChanged += HandleTurnChanged;
            TurnManager.OnPlayerAction += HandlePlayerAction;
            
            // Legacy Game Manager events (for compatibility)
            WallChessGameManager.OnPlayerTurnChanged += HandleLegacyTurnChanged;
            WallChessGameManager.OnPlayerVictory += HandleLegacyVictory;
        }
        
        private void UnsubscribeFromEvents()
        {
            SessionManager.OnSessionStarted -= HandleSessionStarted;
            SessionManager.OnSessionEnded -= HandleSessionEnded;
            SessionManager.OnTurnStarted -= HandleTurnStarted;
            SessionManager.OnTurnCompleted -= HandleTurnCompleted;
            
            TurnManager.OnTurnChanged -= HandleTurnChanged;
            TurnManager.OnPlayerAction -= HandlePlayerAction;
            
            WallChessGameManager.OnPlayerTurnChanged -= HandleLegacyTurnChanged;
            WallChessGameManager.OnPlayerVictory -= HandleLegacyVictory;
        }
        
        private void HandleSessionStarted(SessionData sessionData)
        {
            LogInfo($"Session started with {sessionData.playerCount} players");
            
            // Example: Update UI, initialize game components, etc.
            if (gameManager != null)
            {
                // Sync game manager settings if needed
                gameManager.numberOfPlayers = sessionData.playerCount;
                gameManager.activePlayerIndex = sessionData.currentPlayerIndex;
            }
        }
        
        private void HandleSessionEnded(SessionData sessionData)
        {
            LogInfo($"Session ended: {sessionData.endReason}");
            
            var stats = sessionData.GetStats();
            LogInfo(stats.ToString());
            
            // Example: Show end game screen, save statistics, etc.
        }
        
        private void HandleTurnStarted(int playerIndex, PlayerType playerType)
        {
            LogInfo($"Turn started: Player {playerIndex} ({playerType})");
            
            // Example: Update UI turn indicator, enable/disable controls, etc.
            if (playerType == PlayerType.Human && playerController != null)
            {
                // Enable player controls
                playerController.enabled = true;
            }
            else if (playerType == PlayerType.AI && aiOpponent != null)
            {
                // AI will handle its own turn
                playerController.enabled = false;
            }
        }
        
        private void HandleTurnCompleted(int playerIndex, bool isValidMove)
        {
            LogInfo($"Turn completed: Player {playerIndex}, Valid: {isValidMove}");
            
            // Example: Update UI, play sound effects, etc.
        }
        
        private void HandleTurnChanged(int newPlayerIndex, PlayerType playerType)
        {
            LogInfo($"Turn changed to Player {newPlayerIndex} ({playerType})");
            
            // Example: Update visual indicators, camera focus, etc.
        }
        
        private void HandlePlayerAction(int playerIndex, ActionType actionType, bool success)
        {
            LogInfo($"Player {playerIndex} action {actionType}: {(success ? "Success" : "Failed")}");
            
            // Example: Play animations, sound effects, update counters, etc.
        }
        
        // Legacy event handlers for compatibility
        private void HandleLegacyTurnChanged(int playerIndex)
        {
            LogInfo($"Legacy turn change: Player {playerIndex}");
            
            // This ensures compatibility with existing systems that depend on legacy events
        }
        
        private void HandleLegacyVictory(int winnerIndex)
        {
            LogInfo($"Legacy victory: Player {winnerIndex}");
            
            // This ensures existing victory handling still works
        }
        
        #endregion
        
        #region Example Player Action Integration
        
        /// <summary>
        /// Example: How to request a pawn move through SessionManager
        /// This would typically be called by your input system or player controller
        /// </summary>
        public void ExampleRequestPawnMove(Vector2Int newPosition)
        {
            if (sessionManager == null || !sessionManager.IsSessionActive)
            {
                LogWarning("Cannot request pawn move - no active session");
                return;
            }
            
            var currentPlayer = sessionManager.GetCurrentPlayer();
            if (currentPlayer == null)
            {
                LogWarning("No current player for pawn move");
                return;
            }
            
            // Only allow human players to make moves through this example
            if (currentPlayer.playerType != PlayerType.Human)
            {
                LogWarning("Not a human player's turn");
                return;
            }
            
            // Request the move through SessionManager
            bool success = sessionManager.RequestPlayerAction(
                currentPlayer.playerIndex, 
                ActionType.MovingPawn, 
                newPosition
            );
            
            if (success)
            {
                LogInfo($"Pawn move requested to {newPosition}");
                
                // Complete the turn after move is processed
                // In real implementation, this would be called after move validation and execution
                sessionManager.CompleteTurn(true);
            }
            else
            {
                LogWarning("Failed to request pawn move");
            }
        }
        
        /// <summary>
        /// Example: How to request a wall placement through SessionManager
        /// </summary>
        public void ExampleRequestWallPlacement(Vector2Int position, WallOrientation orientation)
        {
            if (sessionManager == null || !sessionManager.IsSessionActive)
            {
                LogWarning("Cannot request wall placement - no active session");
                return;
            }
            
            var currentPlayer = sessionManager.GetCurrentPlayer();
            if (currentPlayer == null || currentPlayer.playerType != PlayerType.Human)
            {
                LogWarning("Not a human player's turn or no current player");
                return;
            }
            
            // Create wall placement data
            var wallData = new WallPlacementData(position, orientation);
            
            // Request wall placement through SessionManager
            bool success = sessionManager.RequestPlayerAction(
                currentPlayer.playerIndex,
                ActionType.PlacingWall,
                wallData
            );
            
            if (success)
            {
                LogInfo($"Wall placement requested at {position} ({orientation})");
                sessionManager.CompleteTurn(true);
            }
            else
            {
                LogWarning("Failed to request wall placement");
            }
        }
        
        #endregion
        
        #region Utility Methods
        
        private void LogInfo(string message)
        {
            if (enableDebugLogs) Debug.Log($"[SessionIntegration] {message}");
        }
        
        private void LogWarning(string message)
        {
            if (enableDebugLogs) Debug.LogWarning($"[SessionIntegration] {message}");
        }
        
        private void LogError(string message)
        {
            Debug.LogError($"[SessionIntegration] {message}");
        }
        
        #endregion
        
        #region Debug Menu Items
        
        [ContextMenu("Debug/Print Session Status")]
        private void DebugPrintSessionStatus()
        {
            if (sessionManager == null)
            {
                Debug.Log("No SessionManager reference");
                return;
            }
            
            if (sessionManager.IsSessionActive)
            {
                var session = sessionManager.CurrentSession;
                var currentPlayer = sessionManager.GetCurrentPlayer();
                
                Debug.Log($"Session Active:\n" +
                         $"Players: {session.playerCount}\n" +
                         $"Current Turn: {session.currentTurn}\n" +
                         $"Current Player: {currentPlayer?.playerIndex} ({currentPlayer?.playerType})\n" +
                         $"Walls Remaining: {currentPlayer?.wallsRemaining}");
            }
            else
            {
                Debug.Log("No active session");
            }
        }
        
        [ContextMenu("Debug/End Current Session")]
        private void DebugEndSession()
        {
            if (sessionManager != null && sessionManager.IsSessionActive)
            {
                sessionManager.EndSession(SessionEndReason.PlayerQuit);
                LogInfo("Session ended manually");
            }
            else
            {
                LogInfo("No active session to end");
            }
        }
        
        #endregion
    }
}