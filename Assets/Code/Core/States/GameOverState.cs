using UnityEngine;

namespace WallChess.Core.States
{
    /// <summary>
    /// State for when the game has ended (victory or draw)
    /// Corresponds to the original GameOver state in WallChessGameManager
    /// </summary>
    public class GameOverState : BaseState
    {
        public override string StateName => "GameOver";
        
        private int winningPlayerIndex = -1;
        
        public GameOverState(WallChessGameManager gameManager, StateMachine stateMachine) 
            : base(gameManager, stateMachine)
        {
        }
        
        public override void OnEnter()
        {
            base.OnEnter();
            
            if (!IsGameManagerValid()) return;
            
            // Set the legacy game state for compatibility
            gameManager.ChangeState(GameState.GameOver);
            
            // Determine the winning player
            DetermineWinner();
            
            // Clear any active highlights or UI elements
            CleanupGameUI();
            
            Debug.Log($"GameOverState: Game ended. Winner: Player {winningPlayerIndex}");
        }
        
        public override void OnUpdate()
        {
            // Game over state typically doesn't need frame updates
            // But we can handle restart input or other end-game logic here
        }
        
        public override void OnExit()
        {
            base.OnExit();
            
            // Reset for potential new game
            winningPlayerIndex = -1;
        }
        
        /// <summary>
        /// Determine which player won the game
        /// </summary>
        private void DetermineWinner()
        {
            if (!IsGameManagerValid()) return;
            
            // Check each pawn to see if they've reached their win position
            for (int i = 0; i < gameManager.pawns.Count; i++)
            {
                var pawn = gameManager.pawns[i];
                
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
                    winningPlayerIndex = i;
                    break;
                }
            }
        }
        
        /// <summary>
        /// Clean up game UI elements
        /// </summary>
        private void CleanupGameUI()
        {
            var highlightManager = gameManager?.GetHighlightManager();
            highlightManager?.ClearValidMoveHighlights();
        }
        
        /// <summary>
        /// Get the winning player index
        /// </summary>
        public int GetWinningPlayer() => winningPlayerIndex;
        
        /// <summary>
        /// Restart the game (transition to a new game state)
        /// </summary>
        public void RestartGame()
        {
            Debug.Log("GameOverState: Restarting game");
            
            // For now, transition back to gameplay
            // In a full implementation, this might go to a menu or initialization state
            TransitionTo<GameplayState>();
        }
    }
}