using UnityEngine;
using System;
using WallChess.Core;

namespace WallChess.Core.Session
{
    /// <summary>
    /// Manages turn flow, timing, and player actions within a session
    /// Handles both human and AI player turns with proper validation
    /// </summary>
    public class TurnManager
    {
        private SessionManager sessionManager;
        private SessionData sessionData;
        private float turnStartTime;
        private bool waitingForAction;
        private ActionType pendingActionType;
        
        // Events
        public static event Action<int, PlayerType> OnTurnChanged; // playerIndex, playerType
        public static event Action<int, ActionType, bool> OnPlayerAction; // playerIndex, actionType, success
        public static event Action<int> OnTurnTimeout; // playerIndex
        
        // Properties
        public int CurrentPlayerIndex => sessionData?.currentPlayerIndex ?? -1;
        public int CurrentTurn => sessionData?.currentTurn ?? 0;
        public PlayerData CurrentPlayer => sessionData?.GetCurrentPlayer();
        public bool IsWaitingForAction => waitingForAction;
        public float TurnTimeRemaining => GetTurnTimeRemaining();
        
        public TurnManager(SessionManager manager)
        {
            sessionManager = manager;
        }
        
        /// <summary>
        /// Initialize with session data
        /// </summary>
        public void Initialize(SessionData data)
        {
            sessionData = data;
            waitingForAction = false;
            pendingActionType = ActionType.Idle;
        }
        
        /// <summary>
        /// Start the next player's turn
        /// </summary>
        public void StartNextTurn()
        {
            if (sessionData == null || !sessionData.isActive)
            {
                LogError("Cannot start turn - no active session");
                return;
            }
            
            var currentPlayer = sessionData.GetCurrentPlayer();
            if (currentPlayer == null)
            {
                LogError("Cannot start turn - no current player");
                return;
            }
            
            // Mark turn as started
            turnStartTime = Time.time;
            waitingForAction = true;
            pendingActionType = ActionType.Idle;
            
            LogInfo($"Turn {CurrentTurn}: Player {CurrentPlayerIndex} ({currentPlayer.playerType}) started");
            
            // Notify listeners
            OnTurnChanged?.Invoke(CurrentPlayerIndex, currentPlayer.playerType);
        }
        
        /// <summary>
        /// Process a player action request
        /// </summary>
        public bool ProcessPlayerAction(int playerIndex, ActionType actionType, object actionData)
        {
            // Validate request
            if (!ValidateActionRequest(playerIndex, actionType))
            {
                return false;
            }
            
            // Mark action as pending
            pendingActionType = actionType;
            waitingForAction = false; // Action is being processed
            
            // Delegate to appropriate handler based on action type
            bool success = false;
            try
            {
                success = ExecutePlayerAction(playerIndex, actionType, actionData);
            }
            catch (Exception e)
            {
                LogError($"Error executing player action: {e.Message}");
                success = false;
            }
            
            // Notify of action result
            OnPlayerAction?.Invoke(playerIndex, actionType, success);
            
            return success;
        }
        
        /// <summary>
        /// Complete the current turn
        /// </summary>
/// <summary>
        /// Complete the current turn
        /// </summary>
        public void CompleteTurn(bool isValidMove)
        {
            if (sessionData == null || !sessionData.isActive)
            {
                LogError("Cannot complete turn - no active session");
                return;
            }
            
            var currentPlayer = sessionData.GetCurrentPlayer();
            if (currentPlayer == null)
            {
                LogError("Cannot complete turn - no current player");
                return;
            }
            
            LogInfo($"Completing turn for Player {CurrentPlayerIndex}: {(isValidMove ? "Valid" : "Invalid")} move");
            
            if (isValidMove)
            {
                // Check for victory condition
                if (currentPlayer.HasWon())
                {
                    LogInfo($"Player {CurrentPlayerIndex} has won!");
                    // Victory handling will be done by SessionManager through legacy events
                    return;
                }
                
                // The legacy WallChessGameManager.EndTurn() will handle the actual turn switching
                // We just need to keep our data synchronized
                LogInfo($"Turn completed successfully for Player {CurrentPlayerIndex} - legacy system will handle turn switch");
            }
            else
            {
                LogWarning($"Invalid move for Player {CurrentPlayerIndex} - staying on same player");
                
                // Reset turn state for retry
                waitingForAction = true;
                pendingActionType = ActionType.Idle;
                turnStartTime = Time.time;
            }
        }
        
        /// <summary>
        /// Update turn state (called from SessionManager's Update)
        /// </summary>
        public void UpdateTurn()
        {
            if (sessionData == null || !sessionData.isActive) return;
            
            // Check for turn timeout
            if (sessionData.settings.enableTurnTimer && IsWaitingForAction)
            {
                if (GetTurnTimeRemaining() <= 0f)
                {
                    HandleTurnTimeout();
                }
            }
        }
        
        /// <summary>
        /// Sync turn manager to a specific player index (for legacy compatibility)
        /// </summary>
/// <summary>
        /// Sync turn manager to a specific player index (for legacy compatibility)
        /// </summary>
        public void SyncToPlayerIndex(int playerIndex)
        {
            if (sessionData == null || !sessionData.isActive)
            {
                LogError("Cannot sync - no active session");
                return;
            }
            
            if (playerIndex < 0 || playerIndex >= sessionData.playerCount)
            {
                LogError($"Invalid player index for sync: {playerIndex}");
                return;
            }
            
            if (CurrentPlayerIndex == playerIndex)
            {
                LogInfo($"TurnManager already synced to Player {playerIndex}");
                return; // Already synced
            }
            
            LogInfo($"Syncing turn manager from Player {CurrentPlayerIndex} to Player {playerIndex}");
            
            // End current player's turn
            sessionData.GetCurrentPlayer()?.EndTurn();
            
            // Set new current player
            sessionData.currentPlayerIndex = playerIndex;
            sessionData.GetCurrentPlayer()?.StartTurn();
            
            // Reset turn state WITHOUT firing additional events
            // The legacy system has already handled the turn change
            turnStartTime = Time.time;
            waitingForAction = true;
            pendingActionType = ActionType.Idle;
            
            LogInfo($"Turn manager synced to Player {playerIndex} - no additional events fired");
        }
        
        /// <summary>
        /// Clean up turn manager
        /// </summary>
        public void Cleanup()
        {
            waitingForAction = false;
            pendingActionType = ActionType.Idle;
            sessionData = null;
        }
        
        #region Private Methods
        
        private bool ValidateActionRequest(int playerIndex, ActionType actionType)
        {
            // Check session state
            if (sessionData == null || !sessionData.isActive)
            {
                LogError("Cannot process action - no active session");
                return false;
            }
            
            // Check if it's the correct player's turn
            if (playerIndex != CurrentPlayerIndex)
            {
                LogError($"Action rejected - not Player {playerIndex}'s turn (current: {CurrentPlayerIndex})");
                return false;
            }
            
            // Check if waiting for action
            if (!waitingForAction)
            {
                LogWarning($"Action rejected - not waiting for action (current state: {pendingActionType})");
                return false;
            }
            
            // Validate action type
            if (actionType == ActionType.Idle)
            {
                LogError("Invalid action type: Idle");
                return false;
            }
            
            return true;
        }
        
private bool ExecutePlayerAction(int playerIndex, ActionType actionType, object actionData)
        {
            var player = sessionData.GetPlayer(playerIndex);
            if (player == null)
            {
                LogError($"Player {playerIndex} not found");
                return false;
            }
            
            // Get the WallChessGameManager to execute the actual action
            var gameManager = sessionManager.GameManager;
            if (gameManager == null)
            {
                LogError("WallChessGameManager not found - cannot execute player action");
                return false;
            }
            
            bool success = false;
            
            switch (actionType)
            {
                case ActionType.MovingPawn:
                    success = ExecutePawnMoveViaGameManager(gameManager, player, actionData);
                    break;
                    
                case ActionType.PlacingWall:
                    success = ExecuteWallPlacementViaGameManager(gameManager, player, actionData);
                    break;
                    
                default:
                    LogError($"Unknown action type: {actionType}");
                    return false;
            }
            
            if (success)
            {
                LogInfo($"Action {actionType} executed successfully for Player {playerIndex}");
            }
            else
            {
                LogError($"Action {actionType} failed for Player {playerIndex}");
            }
            
            return success;
        }

private bool ExecutePawnMoveViaGameManager(WallChessGameManager gameManager, PlayerData player, object moveData)
        {
            if (moveData is Vector2Int newPosition)
            {
                                LogInfo($"Executing pawn move for Player {player.playerIndex} to {newPosition}");
                
                // Get current pawn position first 
                Vector2Int currentPosition = player?.currentPosition ?? Vector2Int.zero;
                
                // Use the existing game manager's pawn movement system
                bool success = gameManager.TryMovePawn(currentPosition, newPosition);
                
                if (success)
                {
                    // Update our session data to match
                    player.UpdatePosition(newPosition);
                }
                
                return success;
            }
            
            LogError("Invalid move data provided - expected Vector2Int");
            return false;
        }

private bool ExecuteWallPlacementViaGameManager(WallChessGameManager gameManager, PlayerData player, object wallData)
        {
            // Check if player has walls remaining
            if (player.wallsRemaining <= 0)
            {
                LogError($"Player {player.playerIndex} has no walls remaining");
                return false;
            }
            
            if (wallData is WallPlacementData placement)
            {
                LogInfo($"Executing wall placement for Player {player.playerIndex} at {placement.position} ({placement.orientation})");
                
                // Use the existing wall manager through the game manager
                // The actual wall placement logic should go through the existing WallManager
                // For now, just update the player data and let the legacy system handle it
                
                if (player.UseWall())
                {
                    LogInfo($"Wall used successfully. Player {player.playerIndex} has {player.wallsRemaining} walls remaining");
                    return true;
                }
            }
            
            LogError("Invalid wall placement data provided");
            return false;
        }


        
        private bool ExecutePawnMove(PlayerData player, object moveData)
        {
            // This would integrate with the existing movement system
            // For now, we'll delegate to the legacy system through SessionManager
            
            if (moveData is Vector2Int newPosition)
            {
                LogInfo($"Processing pawn move for Player {player.playerIndex} to {newPosition}");
                
                // Update player data
                player.UpdatePosition(newPosition);
                
                // The actual movement validation and execution should be handled by
                // the existing PlayerControllerV2 and GridSystem
                return true;
            }
            
            LogError("Invalid move data provided");
            return false;
        }
        
        private bool ExecuteWallPlacement(PlayerData player, object wallData)
        {
            // Check if player has walls remaining
            if (player.wallsRemaining <= 0)
            {
                LogError($"Player {player.playerIndex} has no walls remaining");
                return false;
            }
            
            if (wallData is WallPlacementData placement)
            {
                LogInfo($"Processing wall placement for Player {player.playerIndex} at {placement.position} ({placement.orientation})");
                
                // Use a wall
                if (player.UseWall())
                {
                    // The actual wall placement validation and execution should be handled by
                    // the existing WallManager and GridSystem
                    return true;
                }
            }
            
            LogError("Invalid wall placement data provided");
            return false;
        }
        
        private void HandleTurnTimeout()
        {
            var currentPlayer = sessionData.GetCurrentPlayer();
            if (currentPlayer == null) return;
            
            LogWarning($"Turn timeout for Player {CurrentPlayerIndex}");
            
            OnTurnTimeout?.Invoke(CurrentPlayerIndex);
            
            // For AI players, this might indicate a bug in AI logic
            if (currentPlayer.playerType == PlayerType.AI)
            {
                LogError("AI player timed out - this may indicate an AI bug");
            }
            
            // Force end turn without a valid move
            CompleteTurn(false);
        }
        
        private float GetTurnTimeRemaining()
        {
            if (sessionData == null || !sessionData.settings.enableTurnTimer || !waitingForAction)
                return float.MaxValue;
                
            float elapsed = Time.time - turnStartTime;
            return Mathf.Max(0f, sessionData.settings.turnTimeLimit - elapsed);
        }
        
        #endregion
        
        #region Logging
        
        private void LogInfo(string message)
        {
            Debug.Log($"[TurnManager] {message}");
        }
        
        private void LogWarning(string message)
        {
            Debug.LogWarning($"[TurnManager] {message}");
        }
        
        private void LogError(string message)
        {
            Debug.LogError($"[TurnManager] {message}");
        }
        
        #endregion
    }
    
    /// <summary>
    /// Data structure for wall placement actions
    /// </summary>
    [System.Serializable]
    public class WallPlacementData
    {
        public Vector2Int position;
        public WallOrientation orientation;
        
        public WallPlacementData(Vector2Int pos, WallOrientation orient)
        {
            position = pos;
            orientation = orient;
        }
    }
    
    /// <summary>
    /// Wall orientation enum (should match existing system)
    /// </summary>
    public enum WallOrientation
    {
        Horizontal,
        Vertical
    }
}