using UnityEngine;
using System.Collections.Generic;

namespace WallChess.Testing
{
    /// <summary>
    /// Test script to validate wall placement pathfinding logic
    /// Tests if walls properly block paths and prevents invalid placements
    /// </summary>
    public class PathfindingTester : MonoBehaviour
    {
        [Header("Test Settings")]
        [SerializeField] private bool runTestsOnStart = true;
        [SerializeField] private bool verboseLogging = true;
        
        private WallChessGameManager gameManager;
        private GridSystem gridSystem;
        private WallManager wallManager;
        
        void Start()
        {
            if (runTestsOnStart)
            {
                // Wait a frame for initialization
                Invoke(nameof(RunPathfindingTests), 0.1f);
            }
        }
        
        [ContextMenu("Run Pathfinding Tests")]
        public void RunPathfindingTests()
        {
            // Get components
            gameManager = FindFirstObjectByType<WallChessGameManager>();
            if (gameManager == null)
            {
                Debug.LogError("PathfindingTester: No WallChessGameManager found!");
                return;
            }
            
            gridSystem = gameManager.GetGridSystem();
            wallManager = gameManager.GetWallManager();
            
            if (gridSystem == null || wallManager == null)
            {
                Debug.LogError("PathfindingTester: Missing GridSystem or WallManager!");
                return;
            }
            
            Debug.Log("=== PATHFINDING TESTS STARTED ===");
            
            // Clear any existing walls first
            wallManager.ClearAllWalls();
            
            // Test 1: Basic pathfinding without walls
            TestBasicPathfinding();
            
            // Test 2: Wall that shouldn't block paths
            TestValidWallPlacement();
            
            // Test 3: Wall that WOULD block a player's path (should be rejected)
            TestInvalidWallPlacement();
            
            // Test 4: Complex scenario with multiple walls
            TestComplexScenario();
            
            Debug.Log("=== PATHFINDING TESTS COMPLETED ===");
        }
        
        private void TestBasicPathfinding()
        {
            Debug.Log("--- Test 1: Basic Pathfinding ---");
            
            foreach (var pawn in gameManager.pawns)
            {
                Vector2Int currentPos = pawn.position;
                Vector2Int startPos = pawn.startPosition;
                
                // Get goal tiles based on starting position
                List<Vector2Int> goalTiles = GetGoalTilesForPawn(pawn);
                
                Debug.Log($"Pawn at {currentPos} (started at {startPos}) has {goalTiles.Count} goal tiles");
                
                // Test if path exists to at least one goal tile
                bool hasPath = false;
                foreach (var goalTile in goalTiles)
                {
                    if (gridSystem.PathExists(currentPos, goalTile))
                    {
                        hasPath = true;
                        if (verboseLogging)
                            Debug.Log($"  ✓ Path exists from {currentPos} to goal {goalTile}");
                        break;
                    }
                }
                
                if (!hasPath)
                {
                    Debug.LogError($"  ✗ NO PATH found for pawn at {currentPos} to any goal tile!");
                }
                else
                {
                    Debug.Log($"  ✓ Pawn at {currentPos} has valid path to goal");
                }
            }
        }
        
        private void TestValidWallPlacement()
        {
            Debug.Log("--- Test 2: Valid Wall Placement ---");
            
            // Try placing a wall that shouldn't block anyone's path
            // Place horizontal wall at position (1,2) - this should be safe
            int testX = 1, testY = 2;
            GridSystem.Orientation testOrientation = GridSystem.Orientation.Horizontal;
            
            bool canPlace = wallManager.CanPlaceWall(testOrientation, testX, testY);
            Debug.Log($"Can place {testOrientation} wall at ({testX},{testY}): {canPlace}");
            
            if (canPlace)
            {
                Vector3 worldPos = wallManager.GetWallWorldPosition(testOrientation, testX, testY);
                Vector3 scale = wallManager.GetWallScale(testOrientation);
                bool placed = wallManager.PlaceWall(testOrientation, testX, testY, worldPos, scale);
                
                if (placed)
                {
                    Debug.Log($"  ✓ Successfully placed {testOrientation} wall at ({testX},{testY})");
                    
                    // Verify all players still have paths
                    foreach (var pawn in gameManager.pawns)
                    {
                        List<Vector2Int> goalTiles = GetGoalTilesForPawn(pawn);
                        bool hasPath = false;
                        
                        foreach (var goalTile in goalTiles)
                        {
                            if (gridSystem.PathExists(pawn.position, goalTile))
                            {
                                hasPath = true;
                                break;
                            }
                        }
                        
                        Debug.Log($"    Pawn at {pawn.position} still has path: {hasPath}");
                    }
                }
                else
                {
                    Debug.LogError($"  ✗ Failed to place wall even though CanPlaceWall returned true!");
                }
            }
        }
        
        private void TestInvalidWallPlacement()
        {
            Debug.Log("--- Test 3: Invalid Wall Placement (Should Block Path) ---");
            
            // Clear walls first
            wallManager.ClearAllWalls();
            
            // Try to create a scenario where a wall would block a player's path
            // For a 7x7 grid, let's try to block Player 0 (at bottom) from reaching top
            
            // First, place walls to narrow the path significantly
            List<(GridSystem.Orientation, int, int)> setupWalls = new List<(GridSystem.Orientation, int, int)>
            {
                (GridSystem.Orientation.Horizontal, 0, 4),  // Block left side
                (GridSystem.Orientation.Horizontal, 2, 4),  // Block center-left
                (GridSystem.Orientation.Horizontal, 4, 4),  // Block center-right
            };
            
            foreach (var (orientation, x, y) in setupWalls)
            {
                if (wallManager.CanPlaceWall(orientation, x, y))
                {
                    Vector3 worldPos = wallManager.GetWallWorldPosition(orientation, x, y);
                    Vector3 scale = wallManager.GetWallScale(orientation);
                    wallManager.PlaceWall(orientation, x, y, worldPos, scale);
                    Debug.Log($"  Setup wall: {orientation} at ({x},{y})");
                }
            }
            
            // Now try to place a wall that would completely block the remaining path
            int blockingX = 5, blockingY = 4;
            GridSystem.Orientation blockingOrientation = GridSystem.Orientation.Horizontal;
            
            bool canPlaceBlocking = wallManager.CanPlaceWall(blockingOrientation, blockingX, blockingY);
            Debug.Log($"Can place blocking {blockingOrientation} wall at ({blockingX},{blockingY}): {canPlaceBlocking}");
            
            if (canPlaceBlocking)
            {
                Debug.LogError($"  ✗ VALIDATION FAILED: Wall placement should be blocked but was allowed!");
            }
            else
            {
                Debug.Log($"  ✓ Validation correctly rejected wall that would block paths");
            }
        }
        
        private void TestComplexScenario()
        {
            Debug.Log("--- Test 4: Complex Scenario ---");
            
            // Clear walls first
            wallManager.ClearAllWalls();
            
            // Create a complex maze-like scenario and ensure pathfinding works
            List<(GridSystem.Orientation, int, int)> complexWalls = new List<(GridSystem.Orientation, int, int)>
            {
                (GridSystem.Orientation.Vertical, 1, 1),
                (GridSystem.Orientation.Horizontal, 2, 2),
                (GridSystem.Orientation.Vertical, 4, 1),
            };
            
            foreach (var (orientation, x, y) in complexWalls)
            {
                bool canPlace = wallManager.CanPlaceWall(orientation, x, y);
                if (canPlace)
                {
                    Vector3 worldPos = wallManager.GetWallWorldPosition(orientation, x, y);
                    Vector3 scale = wallManager.GetWallScale(orientation);
                    wallManager.PlaceWall(orientation, x, y, worldPos, scale);
                    Debug.Log($"  Placed complex wall: {orientation} at ({x},{y})");
                }
                else
                {
                    Debug.Log($"  Could not place wall: {orientation} at ({x},{y}) - validation blocked it");
                }
            }
            
            // Verify all players still have paths after complex placement
            foreach (var pawn in gameManager.pawns)
            {
                List<Vector2Int> goalTiles = GetGoalTilesForPawn(pawn);
                bool hasPath = false;
                Vector2Int reachableGoal = Vector2Int.zero;
                
                foreach (var goalTile in goalTiles)
                {
                    if (gridSystem.PathExists(pawn.position, goalTile))
                    {
                        hasPath = true;
                        reachableGoal = goalTile;
                        break;
                    }
                }
                
                if (hasPath)
                {
                    Debug.Log($"  ✓ Pawn at {pawn.position} can reach goal at {reachableGoal}");
                    
                    // Get and log the actual path
                    var path = gridSystem.FindPath(pawn.position, reachableGoal);
                    if (path != null)
                    {
                        Debug.Log($"    Path length: {path.Count}, Path: {string.Join(" -> ", path)}");
                    }
                }
                else
                {
                    Debug.LogError($"  ✗ Pawn at {pawn.position} BLOCKED from reaching any goal!");
                }
            }
        }
        
        /// <summary>
        /// Get goal tiles for a pawn based on their starting position
        /// </summary>
        private List<Vector2Int> GetGoalTilesForPawn(WallChessGameManager.PawnData pawn)
        {
            List<Vector2Int> goalTiles = new List<Vector2Int>();
            Vector2Int startPos = pawn.startPosition;
            int gridSize = gameManager.gridSize;
            
            if (startPos.y == 0) // Started at bottom, goal is top row
            {
                for (int x = 0; x < gridSize; x++)
                    goalTiles.Add(new Vector2Int(x, gridSize - 1));
            }
            else if (startPos.y == gridSize - 1) // Started at top, goal is bottom row
            {
                for (int x = 0; x < gridSize; x++)
                    goalTiles.Add(new Vector2Int(x, 0));
            }
            else if (startPos.x == 0) // Started at left, goal is right column
            {
                for (int y = 0; y < gridSize; y++)
                    goalTiles.Add(new Vector2Int(gridSize - 1, y));
            }
            else if (startPos.x == gridSize - 1) // Started at right, goal is left column
            {
                for (int y = 0; y < gridSize; y++)
                    goalTiles.Add(new Vector2Int(0, y));
            }
            
            return goalTiles;
        }
        
        [ContextMenu("Clear All Walls")]
        public void ClearAllWalls()
        {
            if (wallManager != null)
            {
                wallManager.ClearAllWalls();
                Debug.Log("All walls cleared");
            }
        }
        
        [ContextMenu("Test Specific Wall")]
        public void TestSpecificWall()
        {
            // Test a specific wall placement
            int x = 2, y = 3;
            GridSystem.Orientation orientation = GridSystem.Orientation.Horizontal;
            
            bool canPlace = wallManager.CanPlaceWall(orientation, x, y);
            Debug.Log($"Can place {orientation} wall at ({x},{y}): {canPlace}");
        }
    }
}