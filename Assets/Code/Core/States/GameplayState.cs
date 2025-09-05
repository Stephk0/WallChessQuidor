using UnityEngine;

namespace WallChess.Core.States
{
    /// <summary>
    /// Main gameplay state - handles player turns, input processing, and game flow
    /// Corresponds to the original PlayerTurn state in WallChessGameManager
    /// </summary>
    public class GameplayState : BaseState
    {
        public override string StateName => "Gameplay";
        
        public GameplayState(WallChessGameManager gameManager, StateMachine stateMachine) 
            : base(gameManager, stateMachine)
        {
        }
        
public override void OnEnter()
        {
            base.OnEnter();
            
            if (!IsGameManagerValid()) return;
            
            // Only set the legacy game state if it's not already PlayerTurn
            // This prevents interfering with ongoing turn management
            if (gameManager.GetCurrentState() != GameState.PlayerTurn)
            {
                gameManager.ChangeState(GameState.PlayerTurn);
                Debug.Log("GameplayState: Legacy state set to PlayerTurn");
            }
            
            // DO NOT force active player changes here - let the turn management system handle it
            // The SetActivePlayer logic should only be called by the turn management flow
            
            int activePlayerIndex = gameManager.GetActivePawnIndex();
            Debug.Log($"GameplayState: Entered with active player {activePlayerIndex}");
        }
        
        public override void OnUpdate()
        {
            if (!IsGameManagerValid()) return;
            
            // Check for victory condition every frame
            if (gameManager.CheckVictory())
            {
                TransitionTo<GameOverState>();
                return;
            }
            
            // Handle input and game logic is managed by existing systems
            // PlayerControllerV2, WallManager, etc. will continue to work
            // This state just coordinates the high-level flow
        }
        
        public override void OnExit()
        {
            base.OnExit();
            
            // Clean up any highlights or UI elements when leaving gameplay
            var highlightManager = gameManager?.GetHighlightManager();
            highlightManager?.ClearValidMoveHighlights();
        }
        
        /// <summary>
        /// Called by external systems when a pawn move is initiated
        /// </summary>
        public void OnPawnMoveInitiated()
        {
            TransitionTo<PawnMovingState>();
        }
        
        /// <summary>
        /// Called by external systems when wall placement is initiated
        /// </summary>
        public void OnWallPlacementInitiated()
        {
            TransitionTo<WallPlacementState>();
        }
    }
}