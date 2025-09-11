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
            
            // Check if the initial click was within game board bounds
            if (!IsInitialClickWithinBounds())
            {
                Debug.Log("WallPlacementState: Initial click outside game board - cancelling placement");
                OnWallPlacementCancelled();
                return;
            }
            
            // Check if current player has walls remaining
            var activePawn = gameManager.GetActivePawn();
            if (activePawn == null || activePawn.wallsRemaining <= 0)
            {
                Debug.Log($"WallPlacementState: Player {gameManager.GetActivePawnIndex()} has no walls remaining - cancelling placement");
                OnWallPlacementCancelled();
                return;
            }
            
            // Set the legacy game state for compatibility
            gameManager.ChangeState(GameState.WallPlacement);
            
            placementInProgress = true;
            
            Debug.Log($"WallPlacementState: Wall placement initiated for player {gameManager.GetActivePawnIndex()} (has {activePawn.wallsRemaining} walls remaining)");
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
/// <summary>
        /// Check if the current player can place walls (includes limit checking)
        /// </summary>
        public bool CanPlaceWall()
        {
            if (!IsGameManagerValid()) return false;
            
            var activePawn = gameManager.GetActivePawn();
            if (activePawn == null)
            {
                Debug.LogWarning("WallPlacementState: No active pawn found");
                return false;
            }
            
            // Check wall limit
            if (activePawn.wallsRemaining <= 0)
            {
                Debug.Log($"WallPlacementState: Player {gameManager.GetActivePawnIndex()} has no walls remaining ({activePawn.wallsRemaining})");
                return false;
            }
            
            // Check if game allows wall placement
            bool canPlace = gameManager.CurrentPlayerHasWalls() && gameManager.CanPlaceWalls();
            
            if (!canPlace)
            {
                Debug.Log($"WallPlacementState: Game doesn't allow wall placement - CurrentPlayerHasWalls: {gameManager.CurrentPlayerHasWalls()}, CanPlaceWalls: {gameManager.CanPlaceWalls()}");
            }
            
            return canPlace;
        }
    

/// <summary>
        /// Check if the initial mouse click was within the game board boundaries
        /// </summary>
        private bool IsInitialClickWithinBounds()
        {
            Vector3 mouseWorldPos = GetMouseWorldPosition();
            
            var gridSystem = gameManager?.GetGridSystem();
            if (gridSystem == null)
            {
                Debug.LogError("WallPlacementState: GridSystem not found");
                return false;
            }
            
            // Use GridSystem's boundary checking
            bool isWithinBounds = gridSystem.IsWithinGridBounds(mouseWorldPos);
            
            Debug.Log($"WallPlacementState: Mouse position {mouseWorldPos} is {(isWithinBounds ? "within" : "outside")} grid bounds");
            
            return isWithinBounds;
        }
        
        /// <summary>
        /// Get current mouse position in world space
        /// </summary>
        private Vector3 GetMouseWorldPosition()
        {
            var cam = Camera.main;
            if (!cam) 
            {
                Debug.LogWarning("WallPlacementState: No main camera found");
                return Vector3.zero;
            }
            
            var ray = cam.ScreenPointToRay(Input.mousePosition);
            var plane = new Plane(Vector3.forward, new Vector3(0, 0, 0)); // Placement plane at Z=0
            
            if (plane.Raycast(ray, out float enter))
            {
                return ray.GetPoint(enter);
            }
            else
            {
                // Fallback method
                return cam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Mathf.Abs(cam.transform.position.z)));
            }
        }
}
}