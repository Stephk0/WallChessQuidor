using UnityEngine;
using WallChess.Core;

namespace WallChess.Testing
{
    /// <summary>
    /// Simple test script to verify wall placement and turn advancement
    /// </summary>
    public class WallTurnTest : MonoBehaviour
    {
        [Header("Test Settings")]
        [SerializeField] private KeyCode testKey = KeyCode.T;
        [SerializeField] private Vector2Int wallPosition = new Vector2Int(4, 4);
        
        private WallChessGameManager gameManager;
        private WallManager wallManager;
        
        private void Start()
        {
            gameManager = FindFirstObjectByType<WallChessGameManager>();
            if (gameManager != null)
            {
                wallManager = gameManager.GetWallManager();
                
                // Subscribe to events for debugging
                WallChessGameManager.OnWallPlacedComplete += OnWallPlaced;
                WallChessGameManager.OnPlayerTurnChanged += OnTurnChanged;
                
                Debug.Log("WallTurnTest: Ready! Press T to test wall placement and turn advancement.");
            }
        }
        
        private void Update()
        {
            if (Input.GetKeyDown(testKey))
            {
                TestWallPlacement();
            }
            
            if (Input.GetKeyDown(KeyCode.I))
            {
                PrintGameInfo();
            }
        }
        
        private void TestWallPlacement()
        {
            Debug.Log("=== TESTING WALL PLACEMENT AND TURN ADVANCEMENT ===");
            
            if (gameManager == null || wallManager == null)
            {
                Debug.LogError("Game components not initialized!");
                return;
            }
            
            // Print current state
            PrintGameInfo();
            
            // Force PlayerTurn state if needed
            if (gameManager.GetCurrentState() != GameState.PlayerTurn)
            {
                Debug.Log("Forcing game to PlayerTurn state...");
                gameManager.ChangeState(GameState.PlayerTurn);
            }
            
            // Try wall placement
            Vector3 worldPos = wallManager.GetWallWorldPosition(GridSystem.Orientation.Horizontal, wallPosition.x, wallPosition.y);
            Debug.Log($"Attempting wall placement at world position: {worldPos}");
            
            bool success = wallManager.TryPlaceWall(worldPos);
            Debug.Log($"Wall placement result: {(success ? "SUCCESS" : "FAILED")}");
            
            if (success)
            {
                Debug.Log("Wall placed! Turn should advance automatically...");
                Invoke(nameof(CheckAfterPlacement), 0.2f);
            }
        }
        
        private void CheckAfterPlacement()
        {
            Debug.Log("=== CHECKING RESULTS AFTER WALL PLACEMENT ===");
            PrintGameInfo();
        }
        
        private void PrintGameInfo()
        {
            if (gameManager == null) return;
            
            Debug.Log($"--- GAME STATE INFO ---");
            Debug.Log($"State: {gameManager.GetCurrentState()}");
            Debug.Log($"Action: {gameManager.GetCurrentAction()}");
            Debug.Log($"Active Player: {gameManager.GetActivePawnIndex()}");
            
            var activePawn = gameManager.GetActivePawnFromManager();
            if (activePawn != null)
            {
                Debug.Log($"Player: {activePawn.PlayerData?.playerName}, Walls: {activePawn.PlayerData?.wallsRemaining}");
            }
        }
        
        // Event handlers
        private void OnWallPlaced(WallPlacementResult result)
        {
            Debug.Log($"*** EVENT: Wall placed by Player {result.playerIndex} ***");
            Debug.Log($"Wall: {result.wallInfo.orientation} at ({result.wallInfo.x},{result.wallInfo.y})");
            Debug.Log($"Walls remaining: {result.remainingWalls}, Turn ended: {result.turnEnded}");
        }
        
        private void OnTurnChanged(int newPlayerIndex)
        {
            Debug.Log($"*** EVENT: Turn changed to Player {newPlayerIndex} ***");
        }
        
        private void OnDestroy()
        {
            WallChessGameManager.OnWallPlacedComplete -= OnWallPlaced;
            WallChessGameManager.OnPlayerTurnChanged -= OnTurnChanged;
        }
    }
}
