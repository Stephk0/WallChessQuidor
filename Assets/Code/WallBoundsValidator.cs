using UnityEngine;

namespace WallChess
{
    /// <summary>
    /// Test script to validate that wall placement bounds are correctly constrained
    /// Press V to run validation tests
    /// </summary>
    public class WallBoundsValidator : MonoBehaviour
    {
        [Header("Debug Settings")]
        public bool enableDebugLogs = true;
        
        private WallManager wallManager;
        private WallChessGameManager gameManager;

        void Start()
        {
            wallManager = FindFirstObjectByType<WallManager>();
            gameManager = FindFirstObjectByType<WallChessGameManager>();
            
            if (wallManager == null)
                Debug.LogError("WallBoundsValidator: WallManager not found!");
            if (gameManager == null)
                Debug.LogError("WallBoundsValidator: WallChessGameManager not found!");
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.V))
            {
                ValidateWallBounds();
            }
        }

        void ValidateWallBounds()
        {
            if (wallManager == null || gameManager == null)
            {
                Debug.LogError("[BOUNDS VALIDATOR] Missing required components!");
                return;
            }
            
            Debug.Log("=== WALL BOUNDS VALIDATION TEST ===");
            
            int gridSize = gameManager.gridSize;
            Debug.Log($"Grid size: {gridSize}x{gridSize}");
            
            // Get all valid wall positions and check they're within bounds
            var validPositions = wallManager.GetValidWallPositions();
            Debug.Log($"Found {validPositions.Count} valid wall positions");
            
            foreach (var wallInfo in validPositions)
            {
                if (wallInfo.orientation == WallManager.Orientation.Horizontal)
                {
                    // Check horizontal wall doesn't extend beyond right edge
                    if (wallInfo.x + 1 >= gridSize)
                    {
                        Debug.LogError($"[VALIDATION FAILED] Horizontal wall at ({wallInfo.x},{wallInfo.y}) extends beyond board!");
                    }
                }
                else
                {
                    // Check vertical wall doesn't extend beyond top edge  
                    if (wallInfo.y + 1 >= gridSize)
                    {
                        Debug.LogError($"[VALIDATION FAILED] Vertical wall at ({wallInfo.x},{wallInfo.y}) extends beyond board!");
                    }
                }
            }
            
            Debug.Log("=== BOUNDS VALIDATION COMPLETE ===");
        }
    }
}
