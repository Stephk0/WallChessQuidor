using UnityEngine;
using System.Collections.Generic;
using System;
using WallChess.Core.States;

namespace WallChess.Core.Session
{
    /// <summary>
    /// Manages game session state, turns, and coordinates between player and AI systems
    /// Communicates with StateMachine for state transitions and orchestrates gameplay flow
    /// </summary>
    [DisallowMultipleComponent]
    public class SessionManager : MonoBehaviour
    {
        [Header("Session State - Read Only")]
        [SerializeField, Tooltip("Current session status")] private string sessionStatus = "No Session";
        [SerializeField, Tooltip("Current turn information")] private string currentTurnInfo = "No Turn";
        [SerializeField, Tooltip("Session statistics")] private string sessionStats = "No Stats";
        
        [Header("Session Configuration")]
        [SerializeField] private int maxPlayers = 4;
        [SerializeField] private bool debugLogs = true;
        
        [Header("Turn Settings")]
        [SerializeField] private float turnTimeout = 30f;
        [SerializeField] private bool enableTurnTimer = false;
        
                
        // Public properties for system integration
        public WallChessGameManager GameManager => gameManager;
        public AIOpponent AIOpponent => aiOpponent;
        
[Header("References")]
        [SerializeField] private StateMachine stateMachine;
        [SerializeField] private WallChessGameManager gameManager;
        [SerializeField] private AIOpponent aiOpponent;
        
        // Session state
        private SessionData sessionData;
        private TurnManager turnManager;
        
        // Events for system communication
        public static event Action<SessionData> OnSessionStarted;
        public static event Action<SessionData> OnSessionEnded;
        public static event Action<int, PlayerType> OnTurnStarted;
        public static event Action<int, bool> OnTurnCompleted; // playerIndex, isValidMove
        public static event Action<SessionEndReason, int> OnSessionComplete; // reason, winnerIndex
        
        public SessionData CurrentSession => sessionData;
        public TurnManager TurnManager => turnManager;
        public bool IsSessionActive => sessionData != null && sessionData.isActive;
        
        #region Unity Lifecycle
        
        void Awake()
        {
            InitializeComponents();
            InitializeManagers();
        }
        
        void Start()
        {
            SubscribeToEvents();
        }
        
        void Update()
        {
            if (IsSessionActive)
            {
                turnManager?.UpdateTurn();
            }
            
            UpdateDebugFields();
        }
        
        /// <summary>
        /// Updates inspector debug fields with current session state
        /// </summary>
        private void UpdateDebugFields()
        {
            // Update session status
            if (IsSessionActive)
            {
                sessionStatus = $"Active - {sessionData.playerCount} players";
            }
            else
            {
                sessionStatus = "No active session";
            }
            
            // Update turn info
            if (IsSessionActive && turnManager != null)
            {
                var currentPlayer = GetCurrentPlayer();
                currentTurnInfo = $"Turn {turnManager.CurrentTurn}: {currentPlayer?.playerName ?? "Unknown"} ({currentPlayer?.playerType ?? PlayerType.Human})";
            }
            else
            {
                currentTurnInfo = "No active turn";
            }
            
            // Update session stats
            if (IsSessionActive)
            {
                sessionStats = $"Turn: {turnManager?.CurrentTurn ?? 0}, Timer: {(enableTurnTimer ? "Enabled" : "Disabled")}";
            }
            else
            {
                sessionStats = "No session statistics";
            }
        }
        
        void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
        
        #endregion
        
        #region Initialization
        
        private void InitializeComponents()
        {
            // Auto-find components if not assigned
            if (stateMachine == null)
                stateMachine = GetComponent<StateMachine>();
                
            if (gameManager == null)
                gameManager = FindObjectOfType<WallChessGameManager>();
                
            if (aiOpponent == null)
                aiOpponent = FindObjectOfType<AIOpponent>();
                
            if (stateMachine == null)
            {
                LogError("StateMachine not found! SessionManager requires StateMachine component.");
                return;
            }
        }
        
        private void InitializeManagers()
        {
            turnManager = new TurnManager(this);
        }
        
        #endregion
        
        #region Session Management
        
        /// <summary>
        /// Start a new game session with specified settings
        /// </summary>
        public bool StartSession(SessionSettings settings)
        {
            if (IsSessionActive)
            {
                LogWarning("Session already active. End current session first.");
                return false;
            }
            
            // Validate settings
            if (!ValidateSessionSettings(settings))
            {
                LogError("Invalid session settings provided.");
                return false;
            }
            
            // Create session data
            sessionData = new SessionData(settings);
            
            // Initialize turn manager with session
            turnManager.Initialize(sessionData);
            
            // Transition to gameplay state
            if (stateMachine != null)
            {
                if (!stateMachine.ChangeState<GameplayState>())
                {
                    LogError("Failed to transition to GameplayState");
                    return false;
                }
            }
            
            LogInfo($"Session started with {sessionData.playerCount} players");
            OnSessionStarted?.Invoke(sessionData);
            
            // Start first turn
            turnManager.StartNextTurn();
            
            return true;
        }
        
        /// <summary>
        /// End the current session
        /// </summary>
        public void EndSession(SessionEndReason reason, int winnerIndex = -1)
        {
            if (!IsSessionActive)
            {
                LogWarning("No active session to end.");
                return;
            }
            
            // Mark session as inactive
            sessionData.isActive = false;
            sessionData.endReason = reason;
            sessionData.winnerIndex = winnerIndex;
            
            LogInfo($"Session ended: {reason}, Winner: {(winnerIndex >= 0 ? $"Player {winnerIndex}" : "None")}");
            
            // Notify systems
            OnSessionEnded?.Invoke(sessionData);
            OnSessionComplete?.Invoke(reason, winnerIndex);
            
            // Transition to appropriate state
            TransitionToEndState(reason);
            
            // Clean up
            turnManager?.Cleanup();
        }
        
        private void TransitionToEndState(SessionEndReason reason)
        {
            if (stateMachine == null) return;
            
            switch (reason)
            {
                case SessionEndReason.PlayerVictory:
                case SessionEndReason.AIVictory:
                    stateMachine.ChangeState<GameOverState>();
                    break;
                    
                case SessionEndReason.Timeout:
                case SessionEndReason.Disconnect:
                case SessionEndReason.Error:
                    // Could transition to error state or back to menu
                    stateMachine.ChangeState<GameOverState>();
                    break;
                    
                case SessionEndReason.PlayerQuit:
                    // Transition back to menu or pause state
                    break;
            }
        }
        
        #endregion
        
        #region Turn Management Interface
        
        /// <summary>
        /// Request to execute a player move (pawn movement or wall placement)
        /// </summary>
/// <summary>
        /// Request to execute a player move (pawn movement or wall placement)
        /// </summary>
        public bool RequestPlayerAction(int playerIndex, ActionType actionType, object actionData)
        {
            if (!IsSessionActive)
            {
                LogWarning("No active session for player action.");
                return false;
            }
            
            // Let the TurnManager handle the action through existing game systems
            bool success = turnManager.ProcessPlayerAction(playerIndex, actionType, actionData);
            
            if (success)
            {
                // Complete the turn through existing systems - let WallChessGameManager handle it
                LogInfo($"Player {playerIndex} action {actionType} successful - letting legacy system handle turn completion");
            }
            
            return success;
        }
        
        /// <summary>
        /// Notify that current player's turn is complete
        /// </summary>
/// <summary>
        /// Notify that current player's turn is complete
        /// </summary>
        public void CompleteTurn(bool isValidMove)
        {
            if (!IsSessionActive) return;
            
            LogInfo($"Turn completion requested: {(isValidMove ? "Valid" : "Invalid")} move");
            
            // Let the TurnManager handle turn completion
            turnManager.CompleteTurn(isValidMove);
        }
        
        /// <summary>
        /// Get current active player
        /// </summary>
        public PlayerData GetCurrentPlayer()
        {
            if (!IsSessionActive) return null;
            return sessionData.GetCurrentPlayer();
        }
        
        /// <summary>
        /// Check if it's an AI player's turn
        /// </summary>
        public bool IsAITurn()
        {
            if (!IsSessionActive) return false;
            var currentPlayer = GetCurrentPlayer();
            return currentPlayer != null && currentPlayer.playerType == PlayerType.AI;
        }
        
        #endregion
        
        #region Event Handling
        
        private void SubscribeToEvents()
        {
            // Subscribe to game manager events
            if (gameManager != null)
            {
                WallChessGameManager.OnPlayerTurnChanged += HandleLegacyTurnChange;
                WallChessGameManager.OnPlayerVictory += HandleLegacyPlayerVictory;
            }
            
            // Subscribe to turn manager events
            TurnManager.OnTurnChanged += HandleTurnChanged;
            TurnManager.OnPlayerAction += HandlePlayerAction;
        }
        
        private void UnsubscribeFromEvents()
        {
            if (gameManager != null)
            {
                WallChessGameManager.OnPlayerTurnChanged -= HandleLegacyTurnChange;
                WallChessGameManager.OnPlayerVictory -= HandleLegacyPlayerVictory;
            }
            
            TurnManager.OnTurnChanged -= HandleTurnChanged;
            TurnManager.OnPlayerAction -= HandlePlayerAction;
        }
        
private void HandleLegacyTurnChange(int playerIndex)
        {
            LogInfo($"Legacy turn change detected: Player {playerIndex}");
            
            // Update our session data to match the legacy system
            if (IsSessionActive)
            {
                var currentPlayer = sessionData.GetCurrentPlayer();
                if (currentPlayer != null)
                {
                    currentPlayer.EndTurn();
                }
                
                // Update session data
                sessionData.currentPlayerIndex = playerIndex;
                
                var newPlayer = sessionData.GetCurrentPlayer();
                if (newPlayer != null)
                {
                    newPlayer.StartTurn();
                }
                
                // Sync TurnManager
                if (turnManager.CurrentPlayerIndex != playerIndex)
                {
                    turnManager.SyncToPlayerIndex(playerIndex);
                }
                
                // Notify systems of the turn change
                OnTurnStarted?.Invoke(playerIndex, newPlayer?.playerType ?? PlayerType.Human);
                
                // If it's AI turn, request AI to make move
                if (newPlayer != null && newPlayer.playerType == PlayerType.AI && aiOpponent != null)
                {
                    RequestAIAction();
                }
            }
        }
        
        private void HandleLegacyPlayerVictory(int winnerIndex)
        {
            LogInfo($"Legacy victory detected: Player {winnerIndex}");
            
            var reason = sessionData.GetPlayer(winnerIndex).playerType == PlayerType.AI 
                ? SessionEndReason.AIVictory 
                : SessionEndReason.PlayerVictory;
                
            EndSession(reason, winnerIndex);
        }
        
        private void HandleTurnChanged(int newPlayerIndex, PlayerType playerType)
        {
            LogInfo($"Turn changed to Player {newPlayerIndex} ({playerType})");
            
            OnTurnStarted?.Invoke(newPlayerIndex, playerType);
            
            // If it's AI turn, request AI to make move
            if (playerType == PlayerType.AI && aiOpponent != null)
            {
                RequestAIAction();
            }
        }
        
        private void HandlePlayerAction(int playerIndex, ActionType actionType, bool success)
        {
            LogInfo($"Player {playerIndex} action {actionType}: {(success ? "Success" : "Failed")}");
            
            OnTurnCompleted?.Invoke(playerIndex, success);
            
            // Check for victory conditions
            if (success && CheckVictoryCondition(playerIndex))
            {
                var playerType = sessionData.GetPlayer(playerIndex).playerType;
                var reason = playerType == PlayerType.AI ? SessionEndReason.AIVictory : SessionEndReason.PlayerVictory;
                EndSession(reason, playerIndex);
            }
        }
        
        #endregion
        
        #region AI Integration
        
private void RequestAIAction()
        {
            if (aiOpponent == null)
            {
                LogWarning("AI opponent not found, skipping AI turn.");
                return;
            }
            
            LogInfo("Requesting AI to make move...");
            
            // The AI opponent should automatically detect it's their turn and make a move
            // through the existing AIOpponent.Update() logic or similar
            // No need to explicitly call anything here, just log the request
        }
        
        #endregion
        
        #region Validation and Helpers
        
        private bool ValidateSessionSettings(SessionSettings settings)
        {
            if (settings == null) return false;
            if (settings.playerCount < 2 || settings.playerCount > maxPlayers) return false;
            if (settings.playerConfigurations == null || settings.playerConfigurations.Count != settings.playerCount) return false;
            
            return true;
        }
        
        private bool CheckVictoryCondition(int playerIndex)
        {
            // Delegate to game manager's existing victory checking logic
            if (gameManager == null) return false;
            
            // This would integrate with existing victory checking
            // For now, we'll rely on the legacy system's OnPlayerVictory event
            return false;
        }
        
        #endregion
        
        #region Debug and Utilities
        
        private void LogInfo(string message)
        {
            if (debugLogs) Debug.Log($"[SessionManager] {message}");
        }
        
        private void LogWarning(string message)
        {
            if (debugLogs) Debug.LogWarning($"[SessionManager] {message}");
        }
        
        private void LogError(string message)
        {
            Debug.LogError($"[SessionManager] {message}");
        }
        
        [ContextMenu("Debug/Print Session Info")]
        private void DebugPrintSessionInfo()
        {
            if (IsSessionActive)
            {
                Debug.Log($"Session Info:\n" +
                         $"Players: {sessionData.playerCount}\n" +
                         $"Current Player: {turnManager.CurrentPlayerIndex}\n" +
                         $"Turn: {turnManager.CurrentTurn}\n" +
                         $"State: {stateMachine?.CurrentState?.StateName ?? "Unknown"}");
            }
            else
            {
                Debug.Log("No active session");
            }
        }
        
        [ContextMenu("Debug/Force End Session")]
        private void DebugForceEndSession()
        {
            EndSession(SessionEndReason.PlayerQuit);
        }
        
        #endregion
    }
}