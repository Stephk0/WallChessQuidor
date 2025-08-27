using UnityEngine;

namespace WallChess
{
    public class WallBlockingDebugger : MonoBehaviour
    {
        private WallChessGameManager gameManager;
        private WallManager wallManager;
        private PlayerControllerV2 playerController;
        
        void Start()
        {
            gameManager = FindObjectOfType<WallChessGameManager>();
            wallManager = FindObjectOfType<WallManager>();
            playerController = FindObjectOfType<PlayerControllerV2>();
            
            // Wait a frame then run tests
            Invoke(nameof(RunDebugTests), 0.1f);
        }
        
        void RunDebugTests()
        {
            if (gameManager == null || wallManager == null) 
            {
                Debug.LogError("Missing required components for debug test");
                return;
            }
            
            Debug.Log("=== WALL BLOCKING DEBUG TEST START ===");
            
            // Test case: Pawn at (4,4) with vertical walls placed to left and right
            Vector2Int testPawn = new Vector2Int(4, 4);
            
            Debug.Log($"Testing pawn at position {testPawn}");
            
            // Test 1: Place a vertical wall to the RIGHT of the pawn (should block rightward movement)
            Debug.Log("\n--- Test 1: Vertical wall to the RIGHT ---");
            TestVerticalWallBlocking(testPawn, true); // true = wall to right
            
            // Test 2: Place a vertical wall to the LEFT of the pawn (should block leftward movement)
            Debug.Log("\n--- Test 2: Vertical wall to the LEFT ---");
            TestVerticalWallBlocking(testPawn, false); // false = wall to left
            
            Debug.Log("\n=== WALL BLOCKING DEBUG TEST END ===");
        }
        
        void TestVerticalWallBlocking(Vector2Int pawnPos, bool wallToRight)
        {
            // Clear existing walls first
            wallManager.ClearAllWalls();
            
            // Determine wall gap position
            int wallGapX = wallToRight ? pawnPos.x : pawnPos.x - 1;
            int wallGapY = pawnPos.y;
            
            Debug.Log($"Pawn at tile ({pawnPos.x},{pawnPos.y})");
            Debug.Log($"Placing vertical wall at gap ({wallGapX},{wallGapY}) - wall to {(wallToRight ? "RIGHT" : "LEFT")}");
            
            // Manually set the wall gaps (simulate wall placement)
            wallManager.SetGapOccupiedForTesting(WallManager.Orientation.Vertical, wallGapX, wallGapY, true);
            wallManager.SetGapOccupiedForTesting(WallManager.Orientation.Vertical, wallGapX, wallGapY + 1, true);
            
            Debug.Log($"Set vertical gaps: ({wallGapX},{wallGapY}) and ({wallGapX},{wallGapY + 1}) to occupied");
            
            // Test movement in all directions
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            string[] dirNames = { "UP", "DOWN", "LEFT", "RIGHT" };
            
            for (int i = 0; i < directions.Length; i++)
            {
                Vector2Int targetPos = pawnPos + directions[i];
                bool isBlocked = wallManager.IsMovementBlocked(pawnPos, targetPos);
                
                Debug.Log($"Movement {dirNames[i]} to ({targetPos.x},{targetPos.y}): {(isBlocked ? "BLOCKED" : "ALLOWED")}");
                
                // Also test with PlayerController logic
                if (playerController != null)
                {
                    var validMoves = playerController.GetValidMoves(pawnPos);
                    bool inValidMoves = validMoves.Contains(targetPos);
                    Debug.Log($"  PlayerController says move {dirNames[i]} is: {(inValidMoves ? "VALID" : "INVALID")}");
                }
            }
            
            // Check gap occupancy directly
            Debug.Log("\n--- Gap Occupancy Check ---");
            for (int checkY = wallGapY; checkY <= wallGapY + 1; checkY++)
            {
                bool occupied = wallManager.IsGapOccupied(WallManager.Orientation.Vertical, wallGapX, checkY);
                Debug.Log($"Vertical gap ({wallGapX},{checkY}): {(occupied ? "OCCUPIED" : "FREE")}");
            }
        }
        
        void Update()
        {
            // Press T to run tests
            if (Input.GetKeyDown(KeyCode.T))
            {
                RunDebugTests();
            }
        }
    }
}