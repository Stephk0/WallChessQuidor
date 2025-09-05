using UnityEngine;

namespace WallChess.Core.States
{
    /// <summary>
    /// State for when a pawn is in the process of moving
    /// Corresponds to the original PawnMoving state in WallChessGameManager
    /// </summary>
    public class PawnMovingState : BaseState
    {
        public override string StateName => "PawnMoving";
        
        private bool moveInProgress = false;
        
        public PawnMovingState(WallChessGameManager gameManager, StateMachine stateMachine) 
            : base(gameManager, stateMachine)
        {
        }
        
        public override void OnEnter()
        {
            base.OnEnter();
            
            if (!IsGameManagerValid()) return;
            
            // Set the legacy game state for compatibility
            gameManager.ChangeState(GameState.PawnMoving);
            
            moveInProgress = true;
            
            Debug.Log($"PawnMovingState: Pawn movement initiated for player {gameManager.GetActivePawnIndex()}");
        }
        
public override void OnUpdate()
        {
            if (!IsGameManagerValid()) return;
            
            // DO NOT automatically transition back to GameplayState
            // Let the turn management system handle state changes properly
            // The move completion will be handled by CompletePawnMovement() → EndTurn() flow
            
            // Only check for victory if explicitly requested
            if (!moveInProgress && gameManager.CheckVictory())
            {
                TransitionTo<GameOverState>();
            }
            
            // Let the legacy system handle turn changes and state transitions
            // We just monitor the state here without interfering
        }
        
        public override void OnExit()
        {
            base.OnExit();
            moveInProgress = false;
        }
        
        /// <summary>
        /// Called by external systems when the move is completed
        /// </summary>
/// <summary>
        /// Called by external systems when the move is completed
        /// </summary>
        public void OnPawnMoveCompleted()
        {
            Debug.Log("PawnMovingState: Pawn move completed - letting legacy system handle turn management");
            moveInProgress = false;
            
            // DO NOT transition back to GameplayState here
            // Let the legacy CompletePawnMovement() → EndTurn() → SetActivePlayer() flow handle everything
            // The GameStateController will sync the FSM state after the turn change completes
        }
        
        /// <summary>
        /// Called by external systems if the move is cancelled
        /// </summary>
        public void OnPawnMoveCancelled()
        {
            Debug.Log("PawnMovingState: Pawn move cancelled");
            moveInProgress = false;
        }
    }
}