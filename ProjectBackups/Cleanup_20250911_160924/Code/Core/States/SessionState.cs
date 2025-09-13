using UnityEngine;
using WallChess.Core.Session;

namespace WallChess.Core.States
{
    /// <summary>
    /// State that manages active gameplay session
    /// Integrates SessionManager with the StateMachine system
    /// </summary>
    public class SessionState : BaseState
    {
        private SessionManager sessionManager;
        private bool sessionInitialized;
        
        public SessionState(WallChessGameManager gameManager, StateMachine stateMachine) 
            : base(gameManager, stateMachine)
        {
            
        }
        
        public override string StateName => "Session";
        
        public override void OnEnter()
        {
            base.OnEnter();
            
            LogInfo("Entering session state");
            
            // Find or create SessionManager
            InitializeSessionManager();
            
            // Start session if not already active
            StartSessionIfNeeded();
        }
        
        public override void OnUpdate()
        {
            base.OnUpdate();
            
            // SessionManager handles its own update logic
            // This state mainly acts as a bridge between FSM and session system
            
            // Check for state transitions based on session state
            CheckForStateTransitions();
        }
        
        public override void OnExit()
        {
            base.OnExit();
            
            LogInfo("Exiting session state");
            
            // Don't end session here as it might be transitioning to GameOverState
            // Let SessionManager handle session lifecycle
        }
        
        #region Session Management
        
        private void InitializeSessionManager()
        {
            if (sessionManager == null)
            {
                // Try to find existing SessionManager
                sessionManager = gameManager.GetComponent<SessionManager>();
                
                if (sessionManager == null)
                {
                    // Create SessionManager if it doesn't exist
                    sessionManager = gameManager.gameObject.AddComponent<SessionManager>();
                    LogInfo("Created SessionManager component");
                }
            }
            
            sessionInitialized = sessionManager != null;
            
            if (!sessionInitialized)
            {
                LogError("Failed to initialize SessionManager");
            }
        }
        
        private void StartSessionIfNeeded()
        {
            if (!sessionInitialized || sessionManager.IsSessionActive)
            {
                return;
            }
            
            // Create session settings based on current game manager configuration
            var settings = CreateSessionSettingsFromGameManager();
            
            // Start the session
            bool success = sessionManager.StartSession(settings);
            
            if (!success)
            {
                LogError("Failed to start session - transitioning to error state");
                // Could transition to an error state or back to menu
            }
            else
            {
                LogInfo("Session started successfully");
                SubscribeToSessionEvents();
            }
        }
        
        private SessionSettings CreateSessionSettingsFromGameManager()
        {
            var settings = new SessionSettings
            {
                gridSize = gameManager.gridSize,
                wallsPerPlayer = gameManager.wallsPerPlayer,
                playerCount = gameManager.numberOfPlayers
            };
            
            // Configure players based on game manager settings
            settings.playerConfigurations.Clear();
            
            for (int i = 0; i < settings.playerCount; i++)
            {
                PlayerType playerType = PlayerType.Human;
                
                // Check if this should be an AI player
                // This logic should match your existing AI configuration
                if (i > 0) // For now, make all non-first players AI
                {
                    playerType = PlayerType.AI;
                }
                
                var config = new PlayerConfiguration(playerType);
                config.playerName = playerType == PlayerType.AI ? $"AI Player {i + 1}" : $"Player {i + 1}";
                
                // Set colors (this should match your existing color system)
                config.playerColor = GetPlayerColor(i);
                
                settings.playerConfigurations.Add(config);
            }
            
            return settings;
        }
        
        private Color GetPlayerColor(int playerIndex)
        {
            // Default colors - should match your existing system
            Color[] colors = { Color.blue, Color.red, Color.green, Color.yellow };
            return playerIndex < colors.Length ? colors[playerIndex] : Color.white;
        }
        
        #endregion
        
        #region Event Handling
        
        private void SubscribeToSessionEvents()
        {
            if (sessionManager == null) return;
            
            SessionManager.OnSessionComplete += HandleSessionComplete;
            SessionManager.OnTurnStarted += HandleTurnStarted;
        }
        
        private void UnsubscribeFromSessionEvents()
        {
            SessionManager.OnSessionComplete -= HandleSessionComplete;
            SessionManager.OnTurnStarted -= HandleTurnStarted;
        }
        
        private void HandleSessionComplete(SessionEndReason reason, int winnerIndex)
        {
            LogInfo($"Session completed: {reason}, Winner: {(winnerIndex >= 0 ? $"Player {winnerIndex}" : "None")}");
            
            // Transition to appropriate end state
            switch (reason)
            {
                case SessionEndReason.PlayerVictory:
                case SessionEndReason.AIVictory:
                    stateMachine.ChangeState<GameOverState>();
                    break;
                    
                case SessionEndReason.PlayerQuit:
                    // Could transition to pause or menu state
                    break;
                    
                case SessionEndReason.Error:
                case SessionEndReason.Timeout:
                case SessionEndReason.Disconnect:
                    // Could transition to error state or game over
                    stateMachine.ChangeState<GameOverState>();
                    break;
            }
        }
        
        private void HandleTurnStarted(int playerIndex, PlayerType playerType)
        {
            LogInfo($"Turn started for Player {playerIndex} ({playerType})");
            
            // Update game manager state if needed for legacy compatibility
            if (gameManager != null && gameManager.activePlayerIndex != playerIndex)
            {
                gameManager.activePlayerIndex = playerIndex;
            }
        }
        
        #endregion
        
        #region State Transitions
        
        private void CheckForStateTransitions()
        {
            if (!sessionInitialized || !sessionManager.IsSessionActive)
            {
                return;
            }
            
            // Check if we need to transition to specific sub-states
            // This depends on your existing state machine structure
            
            var currentPlayer = sessionManager.GetCurrentPlayer();
            if (currentPlayer == null) return;
            
            // Example: Transition to PawnMovingState or WallPlacementState based on current action
            // This would depend on your input system and how actions are initiated
        }
        
        #endregion
        
        #region Public Interface
        
        /// <summary>
        /// Get the current session manager
        /// </summary>
        public SessionManager GetSessionManager()
        {
            return sessionManager;
        }
        
        /// <summary>
        /// Check if session is active and ready
        /// </summary>
        public bool IsSessionReady()
        {
            return sessionInitialized && sessionManager != null && sessionManager.IsSessionActive;
        }
        
        /// <summary>
        /// Request player action through session manager
        /// </summary>
        public bool RequestPlayerAction(int playerIndex, ActionType actionType, object actionData)
        {
            if (!IsSessionReady())
            {
                LogWarning("Session not ready for player action");
                return false;
            }
            
            return sessionManager.RequestPlayerAction(playerIndex, actionType, actionData);
        }
        
        /// <summary>
        /// Complete current turn
        /// </summary>
        public void CompleteTurn(bool isValidMove)
        {
            if (!IsSessionReady())
            {
                LogWarning("Session not ready to complete turn");
                return;
            }
            
            sessionManager.CompleteTurn(isValidMove);
        }
        
        #endregion
        
        #region Logging
        
        private void LogInfo(string message)
        {
            Debug.Log($"[SessionState] {message}");
        }
        
        private void LogWarning(string message)
        {
            Debug.LogWarning($"[SessionState] {message}");
        }
        
        private void LogError(string message)
        {
            Debug.LogError($"[SessionState] {message}");
        }
        
        #endregion
        
        #region Cleanup
        
        ~SessionState()
        {
            UnsubscribeFromSessionEvents();
        }
        
        #endregion
    }
}