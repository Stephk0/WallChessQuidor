using UnityEngine;
using System.Collections.Generic;

namespace WallChess
{
    /// <summary>
    /// Debug helper to test the unified wall system and validate fixes
    /// </summary>
    public class WallSystemTester : MonoBehaviour
    {
        [Header("Test Controls")]
        public KeyCode testValidMovesKey = KeyCode.F1;
        public KeyCode testWallPlacementKey = KeyCode.F2;
        public KeyCode testBoundaryWallsKey = KeyCode.F3;
        
        public WallChessGameManager gameManager;
        public GridSystem gridSystem;
        public WallManager wallManager;
        
        void Start()
        {
            gameManager = FindFirstObjectByType<WallChessGameManager>();
            gridSystem = gameManager?.GetGridSystem();
            wallManager = gameManager?.GetWallManager();
        }
        
        void Update()
        {
            if (Input.GetKeyDown(testValidMovesKey))
            {
                TestValidMoves();
            }
            
            if (Input.GetKeyDown(testWallPlacementKey))
            {
                TestWallPlacement();
            }
            
            if (Input.GetKeyDown(testBoundaryWallsKey))
            {
                TestBoundaryWalls();
            }
        }
        
        void TestValidMoves()
        {
            Debug.Log("=== TESTING VALID MOVES (F1) ===");
            
            if (gridSystem == null)
            {
                Debug.LogError("GridSystem not found!");
                return;
            }
            
            int gridSize = gridSystem.GetGridSize();
            Debug.Log($"Grid size: {gridSize}x{gridSize}");
            
            // Test moves from center position
            Vector2Int centerPos = new Vector2Int(gridSize / 2, gridSize / 2);
            List<Vector2Int> validMoves = gridSystem.GetValidMoves(centerPos);
            
            Debug.Log($"Valid moves from center position {centerPos}: [{string.Join(", ", validMoves)}]");
            
            // Check if any moves are outside bounds
            foreach (var move in validMoves)
            {
                if (move.x < 0 || move.x >= gridSize || move.y < 0 || move.y >= gridSize)
                {
                    Debug.LogError($"INVALID MOVE DETECTED: {move} is outside {gridSize}x{gridSize} bounds!");
                }
            }
        }
        
        void TestWallPlacement()
        {
            Debug.Log("=== TESTING WALL PLACEMENT (F2) ===");
            
            if (wallManager == null || gridSystem == null)
            {
                Debug.LogError("WallManager or GridSystem not found!");
                return;
            }
            
            int gridSize = gridSystem.GetGridSize();
            int validHorizontal = 0, validVertical = 0;
            int totalHorizontal = 0, totalVertical = 0;
            
            // Test horizontal walls
            for (int x = 0; x < gridSize - 1; x++)
            {
                for (int y = 0; y < gridSize; y++)
                {
                    totalHorizontal++;
                    if (wallManager.CanPlaceWall(GridSystem.Orientation.Horizontal, x, y))
                    {
                        validHorizontal++;
                    }
                }
            }
            
            // Test vertical walls
            for (int x = 0; x < gridSize; x++)
            {
                for (int y = 0; y < gridSize - 1; y++)
                {
                    totalVertical++;
                    if (wallManager.CanPlaceWall(GridSystem.Orientation.Vertical, x, y))
                    {
                        validVertical++;
                    }
                }
            }
            
            Debug.Log($"Wall placement test results:");
            Debug.Log($"Horizontal walls: {validHorizontal}/{totalHorizontal} positions available");
            Debug.Log($"Vertical walls: {validVertical}/{totalVertical} positions available");
        }
        
        void TestBoundaryWalls()
        {
            Debug.Log("=== TESTING BOUNDARY WALLS (F3) ===");
            
            if (wallManager == null || gridSystem == null)
            {
                Debug.LogError("WallManager or GridSystem not found!");
                return;
            }
            
            int gridSize = gridSystem.GetGridSize();
            
            // Test boundary walls
            Debug.Log("Testing boundary wall positions:");
            
            for (int x = 0; x < gridSize - 1; x++)
            {
                bool canPlaceTop = wallManager.CanPlaceWall(GridSystem.Orientation.Horizontal, x, gridSize - 1);
                bool canPlaceBottom = wallManager.CanPlaceWall(GridSystem.Orientation.Horizontal, x, 0);
                Debug.Log($"  H-Wall ({x},top): {(canPlaceTop ? "YES" : "no")}, ({x},bottom): {(canPlaceBottom ? "YES" : "no")}");
            }
            
            for (int y = 0; y < gridSize - 1; y++)
            {
                bool canPlaceRight = wallManager.CanPlaceWall(GridSystem.Orientation.Vertical, gridSize - 1, y);
                bool canPlaceLeft = wallManager.CanPlaceWall(GridSystem.Orientation.Vertical, 0, y);
                Debug.Log($"  V-Wall (right,{y}): {(canPlaceRight ? "YES" : "no")}, (left,{y}): {(canPlaceLeft ? "YES" : "no")}");
            }
        }
        
        void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label("Wall System Tester");
            GUILayout.Label($"F1: Test Valid Moves");
            GUILayout.Label($"F2: Test Wall Placement");
            GUILayout.Label($"F3: Test Boundary Walls");
            
            if (gridSystem != null)
            {
                GUILayout.Label($"Grid Size: {gridSystem.GetGridSize()}x{gridSystem.GetGridSize()}");
            }
            GUILayout.EndArea();
        }
    }
}
