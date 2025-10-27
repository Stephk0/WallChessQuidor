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
            
            Debug.Log("[GameplayState] OnEnter called");
            
            if (!IsGameManagerValid())
            {
                Debug.LogError("[GameplayState] GameManager is not valid!");
                return;
            }
            
            // Get current state before changing
            var currentState = gameManager.GetCurrentState();
            Debug.Log($"[GameplayState] Current game state before change: {currentState}");
            
            // Always set to PlayerTurn when entering GameplayState
            gameManager.ChangeState(GameState.PlayerTurn);
            Debug.Log("[GameplayState] Changed game state to PlayerTurn");
            
            // Verify the change took effect
            var newState = gameManager.GetCurrentState();
            Debug.Log($"[GameplayState] Game state after change: {newState}");
            
            int activePlayerIndex = gameManager.GetActivePawnIndex();
            Debug.Log($"[GameplayState] Active player index: {activePlayerIndex}");
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