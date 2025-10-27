using WallChess.Gameplay.Pawns;
using UnityEngine;

namespace WallChess.Core.ApplicationStates
{
    /// <summary>
    /// Application-level game over state - shows results and handles post-game options
    /// This is different from the gameplay GameOverState which handles victory detection
    /// </summary>
    public class ApplicationGameOverState : IState
    {
        public string StateName => "ApplicationGameOver";
        
        private readonly StateMachine stateMachine;
        private readonly WallChessGameManager gameManager;
        
        private int winningPlayer = -1;
        private float stateEnteredTime;
        private const float AUTO_RETURN_DELAY = 5f; // Auto return to menu after 5 seconds
        
        public ApplicationGameOverState(StateMachine stateMachine, WallChessGameManager gameManager)
        {
            this.stateMachine = stateMachine;
            this.gameManager = gameManager;
        }
        
        public void OnEnter()
        {
            Debug.Log("ApplicationGameOverState: Game over screen entered");
            stateEnteredTime = Time.time;
            
            // Determine the winning player
            DetermineWinner();
            
            // Show game over UI
            ShowGameOverUI();
            
            // Subscribe to victory events if not already handled
            SubscribeToEvents();
        }
        
        public void OnUpdate()
        {
            HandleGameOverInput();
            
            // Auto-return to menu after delay (optional)
            if (Time.time - stateEnteredTime > AUTO_RETURN_DELAY)
            {
                if (Input.anyKeyDown)
                {
                    ReturnToMenu();
                }
            }
        }
        
        public void OnExit()
        {
            Debug.Log("ApplicationGameOverState: Leaving game over screen");
            HideGameOverUI();
            UnsubscribeFromEvents();
        }
        
        private void DetermineWinner()
        {
            // Try to get winner info from the game manager
            if (gameManager != null)
            {
                // Check which player won based on game state
                for (int i = 0; i < gameManager.pawns.Count; i++)
                {
                    var pawn = gameManager.pawns[i];
                    
                    // Check victory condition for this pawn
                    bool hasWon = false;
                    if (pawn.winPosition.x == -1) // Any position on specified row
                    {
                        hasWon = (pawn.position.y == pawn.winPosition.y);
                    }
                    else if (pawn.winPosition.y == -1) // Any position on specified column
                    {
                        hasWon = (pawn.position.x == pawn.winPosition.x);
                    }
                    else // Specific position
                    {
                        hasWon = (pawn.position == pawn.winPosition);
                    }
                    
                    if (hasWon)
                    {
                        winningPlayer = i;
                        break;
                    }
                }
            }
            
            Debug.Log($"ApplicationGameOverState: Winner determined - Player {winningPlayer + 1}");
        }
        
        private void ShowGameOverUI()
        {
            // Future: Show game over UI with winner announcement, stats, options
            string winnerMessage = winningPlayer >= 0 ? 
                $"Player {winningPlayer + 1} Wins!" : 
                "Game Over";
                
            Debug.Log($"ApplicationGameOverState: {winnerMessage}");
            
            // Future UI elements:
            // - Victory animation
            // - Player statistics
            // - Play Again button
            // - Return to Menu button
            // - Save replay option
        }
        
        private void HideGameOverUI()
        {
            // Future: Hide game over UI elements
            Debug.Log("ApplicationGameOverState: Game over UI hidden");
        }
        
        private void HandleGameOverInput()
        {
            // Handle post-game input
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                PlayAgain();
            }
            
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.M))
            {
                ReturnToMenu();
            }
            
            if (Input.GetKeyDown(KeyCode.R))
            {
                PlayAgain();
            }
        }
        
        /// <summary>
        /// Start a new game with the same settings
        /// </summary>
        public void PlayAgain()
        {
            Debug.Log("ApplicationGameOverState: Starting new game...");
            stateMachine.ChangeState<ApplicationGameState>();
        }
        
        /// <summary>
        /// Return to main menu
        /// </summary>
        public void ReturnToMenu()
        {
            Debug.Log("ApplicationGameOverState: Returning to main menu...");
            stateMachine.ChangeState<MenuState>();
        }
        
        /// <summary>
        /// Open settings to change game configuration
        /// </summary>
        public void ChangeSettings()
        {
            Debug.Log("ApplicationGameOverState: Opening settings...");
            stateMachine.ChangeState<SettingsState>();
        }
        
        private void SubscribeToEvents()
        {
            WallChessGameManager.OnPlayerVictory += OnPlayerVictory;
        }
        
        private void UnsubscribeFromEvents()
        {
            WallChessGameManager.OnPlayerVictory -= OnPlayerVictory;
        }
        
        private void OnPlayerVictory(int playerIndex)
        {
            winningPlayer = playerIndex;
            Debug.Log($"ApplicationGameOverState: Victory event received - Player {playerIndex + 1} won");
            
            // Update UI if needed
            ShowGameOverUI();
        }
        
        /// <summary>
        /// Get formatted game results for UI display
        /// </summary>
        public string GetGameResults()
        {
            if (winningPlayer >= 0)
            {
                return $"Player {winningPlayer + 1} Wins!";
            }
            
            return "Game Over";
        }
        
        /// <summary>
        /// Get game statistics for display
        /// </summary>
        public string GetGameStatistics()
        {
            if (gameManager == null) return "No statistics available";
            
            // Future: Collect and return game statistics
            // - Turn count
            // - Walls used by each player
            // - Game duration
            // - Path efficiency
            
            return "Game completed successfully";
        }
    }
}