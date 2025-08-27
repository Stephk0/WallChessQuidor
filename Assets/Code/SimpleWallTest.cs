using UnityEngine;

namespace WallChess
{
    public class SimpleWallTest : MonoBehaviour
    {
        void Start()
        {
            // Wait 1 second then run test
            Invoke(nameof(RunTest), 1f);
        }
        
        void RunTest()
        {
            Debug.Log("=== SIMPLE WALL BLOCKING TEST ===");
            
            var wallManager = FindObjectOfType<WallManager>();
            var playerController = FindObjectOfType<PlayerControllerV2>();
            
            if (wallManager == null || playerController == null)
            {
                Debug.LogError("Cannot find WallManager or PlayerController!");
                return;
            }
            
            // Clear all walls first
            wallManager.ClearAllWalls();
            
            // Test pawn at position (4,4) 
            Vector2Int pawn = new Vector2Int(4, 4);
            Debug.Log($"Testing pawn at {pawn}");
            
            // Test 1: Place vertical wall to the RIGHT (gap at x=4)
            Debug.Log("--- Test 1: RIGHT wall ---");
            wallManager.SetGapOccupiedForTesting(WallManager.Orientation.Vertical, 4, 4, true);
            wallManager.SetGapOccupiedForTesting(WallManager.Orientation.Vertical, 4, 5, true);
            
            Vector2Int rightTarget = new Vector2Int(5, 4);
            bool rightBlocked = wallManager.IsMovementBlocked(pawn, rightTarget);
            Debug.Log($"Movement RIGHT to {rightTarget}: {(rightBlocked ? "BLOCKED" : "ALLOWED")} (should be BLOCKED)");
            
            // Test 2: Clear and place wall to LEFT (gap at x=3)
            wallManager.ClearAllWalls();
            Debug.Log("--- Test 2: LEFT wall ---");
            wallManager.SetGapOccupiedForTesting(WallManager.Orientation.Vertical, 3, 4, true);
            wallManager.SetGapOccupiedForTesting(WallManager.Orientation.Vertical, 3, 5, true);
            
            Vector2Int leftTarget = new Vector2Int(3, 4);
            bool leftBlocked = wallManager.IsMovementBlocked(pawn, leftTarget);
            Debug.Log($"Movement LEFT to {leftTarget}: {(leftBlocked ? "BLOCKED" : "ALLOWED")} (should be BLOCKED)");
            
            Debug.Log("=== TEST COMPLETE ===");
        }
    }
}