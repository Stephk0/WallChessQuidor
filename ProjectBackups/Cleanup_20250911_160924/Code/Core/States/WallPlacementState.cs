using UnityEngine;

namespace WallChess.Core.States
{
    /// <summary>
    /// State for when a player is placing a wall
    /// Corresponds to the original WallPlacement state in WallChessGameManager
    /// </summary>
    public class WallPlacementState : BaseState
    {
        public override string StateName => "WallPlacement";
        
        private bool placementInProgress = false;
        
        public WallPlacementState(WallChessGameManager gameManager, StateMachine stateMachine) 
            : base(gameManager, stateMachine)
        {
        }
        
        public override void OnEnter()
        {
            base.OnEnter();
            
            if (!IsGameManagerValid()) return;
            
            // Set the legacy game state for compatibility
            gameManager.ChangeState(GameState.WallPlacement);
            
            placementInProgress = true;
            
            var activePawn = gameManager.GetActivePawn();
            if (activePawn != null)
            {
                Debug.Log($"WallPlacementState: Wall placement initiated for player {gameManager.GetActivePawnIndex()} (has {activePawn.wallsRemaining} walls)");
            }
        }
        
        public override void OnUpdate()
        {
            if (!IsGameManagerValid()) return;
            
            // Check if placement is still in progress
            if (!placementInProgress)
            {
                // Return to gameplay state - this will handle turn changes
                TransitionTo<GameplayState>();
            }
        }
        
        public override void OnExit()
        {
            base.OnExit();
            placementInProgress = false;
        }
        
        /// <summary>
        /// Called by external systems when a wall is successfully placed
        /// </summary>
        public void OnWallPlaced()
        {
            Debug.Log("WallPlacementState: Wall placed successfully");
            placementInProgress = false;
            
            // The gameManager will handle turn switching via its existing event system
        }
        
        /// <summary>
        /// Called by external systems when wall placement is cancelled
        /// </summary>
        public void OnWallPlacementCancelled()
        {
            Debug.Log("WallPlacementState: Wall placement cancelled");
            placementInProgress = false;
        }
        
        /// <summary>
        /// Check if the current player can place walls
        /// </summary>
        public bool CanPlaceWall()
        {
            if (!IsGameManagerValid()) return false;
            
            return gameManager.CurrentPlayerHasWalls();
        }
    }
}