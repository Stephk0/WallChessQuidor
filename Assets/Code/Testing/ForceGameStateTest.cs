using UnityEngine;
using WallChess.Core;

namespace WallChess.Testing
{
    /// <summary>
    /// Simple script to force game state and test wall placement
    /// </summary>
    public class ForceGameStateTest : MonoBehaviour
    {
        private void Update()
        {
            // Press F1 to force PlayerTurn state and test wall placement
            if (Input.GetKeyDown(KeyCode.F1))
            {
                TestWallPlacementWithStateForce();
            }
            
            // Press F2 to print current game state
            if (Input.GetKeyDown(KeyCode.F2))
            {
                PrintCurrentState();
            }
        }
        
        private void TestWallPlacementWithStateForce()
        {
            Debug.Log("=== FORCE GAME STATE AND TEST WALL PLACEMENT ===");
            
            var gameManager = FindFirstObjectByType<WallChessGameManager>();
            if (gameManager == null)
            {
                Debug.LogError("GameManager not found!");
                return;
            }
            
            // Force game into PlayerTurn state
            Debug.Log($"Current state: {gameManager.GetCurrentState()}, forcing to PlayerTurn...");
            gameManager.ChangeState(GameState.PlayerTurn);
            Debug.Log($"New state: {gameManager.GetCurrentState()}");
            
            // Get WallManager and test wall placement
            var wallManager = gameManager.GetWallManager();
            if (wallManager == null)
            {
                Debug.LogError("WallManager not found!");
                return;
            }
            
            // Try to place a wall at a test position
            Vector3 testPosition = wallManager.GetWallWorldPosition(GridSystem.Orientation.Horizontal, 4, 4);
            Debug.Log($"Attempting to place wall at position {testPosition}");
            
            bool success = wallManager.TryPlaceWall(testPosition);
            Debug.Log($"Wall placement result: {(success ? "SUCCESS" : "FAILED")}");
            
            if (success)
            {
                Debug.Log("Wall placed successfully! Checking turn advancement in 0.5 seconds...");
                Invoke(nameof(CheckTurnAdvancement), 0.5f);
            }
        }
        
        private void CheckTurnAdvancement()
        {
            var gameManager = FindFirstObjectByType<WallChessGameManager>();
            if (gameManager != null)
            {
                Debug.Log($"=== TURN ADVANCEMENT CHECK ===");
                Debug.Log($"Current game state: {gameManager.GetCurrentState()}");
                Debug.Log($"Active player index: {gameManager.GetActivePawnIndex()}");
                
                var activePawn = gameManager.GetActivePawnFromManager();
                if (activePawn != null)
                {
                    Debug.Log($"Active player: {activePawn.PlayerData?.playerName}");
                    Debug.Log($"Walls remaining: {activePawn.PlayerData?.wallsRemaining}");
                }
            }
        }
        
        private void PrintCurrentState()
        {
            var gameManager = FindFirstObjectByType<WallChessGameManager>();
            if (gameManager != null)
            {
                Debug.Log($"=== CURRENT GAME STATE ===");
                Debug.Log($"Game State: {gameManager.GetCurrentState()}");
                Debug.Log($"Active Player: {gameManager.GetActivePawnIndex()}");
                Debug.Log($"Debug Mode: {gameManager.debugMode}");
                
                var activePawn = gameManager.GetActivePawnFromManager();
                if (activePawn != null)
                {
                    Debug.Log($"Player: {activePawn.PlayerData?.playerName}, Walls: {activePawn.PlayerData?.wallsRemaining}");
                }
            }
        }
    }
}
