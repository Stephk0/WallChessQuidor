using UnityEngine;
using WallChess;

/// <summary>
/// Test script to verify wall placement bounds are correctly fixed
/// </summary>
public class WallBoundsTest : MonoBehaviour
{
    [Header("Bounds Test")]
    public bool runBoundsTest = true;
    
    private WallChessGameManager gameManager;
    private WallManager wallManager;
    
    void Start()
    {
        if (runBoundsTest)
        {
            StartCoroutine(RunBoundsTest());
        }
    }
    
    System.Collections.IEnumerator RunBoundsTest()
    {
        yield return new WaitForSeconds(1f); // Wait for initialization
        
        gameManager = FindObjectOfType<WallChessGameManager>();
        wallManager = gameManager?.GetWallManager();
        
        if (wallManager != null)
        {
            Debug.Log("=== Wall Bounds Test ===");
            TestBounds();
        }
        else
        {
            Debug.LogError("WallManager not found for bounds test!");
        }
    }
    
    void TestBounds()
    {
        int gridSize = gameManager.gridSize;
        float spacing = gameManager.tileSize + gameManager.tileGap;
        
        Debug.Log($"Testing bounds for {gridSize}x{gridSize} grid with spacing {spacing}");
        Debug.Log($"Valid gap area: {0.5f * spacing} to {(gridSize - 1.5f) * spacing}");
        
        // Test positions that should be VALID (in gaps between tiles)
        Vector3[] validPositions = {
            new Vector3(0.5f * spacing, 0.5f * spacing, 0f),        // First gap (between tiles 0,0 and 1,1)
            new Vector3(1.5f * spacing, 0.5f * spacing, 0f),        // Gap between columns 1-2
            new Vector3(0.5f * spacing, 1.5f * spacing, 0f),        // Gap between rows 1-2
            new Vector3(3.5f * spacing, 3.5f * spacing, 0f),        // Middle gap area
            new Vector3((gridSize-1.5f) * spacing, 0.5f * spacing, 0f),   // Last gap in X
            new Vector3(0.5f * spacing, (gridSize-1.5f) * spacing, 0f),   // Last gap in Y
        };
        
        // Test positions that should be INVALID (outside gaps or on tiles)
        Vector3[] invalidPositions = {
            new Vector3(0f, 0f, 0f),                                 // On tile (0,0)
            new Vector3(spacing, spacing, 0f),                      // On tile (1,1)
            new Vector3(gridSize * spacing, 0.5f * spacing, 0f),    // Outside board to right
            new Vector3(0.5f * spacing, gridSize * spacing, 0f),    // Outside board to top
            new Vector3(0f, 0.5f * spacing, 0f),                    // Outside gap area to left
            new Vector3(0.5f * spacing, 0f, 0f),                    // Outside gap area to bottom
            new Vector3((gridSize + 0.5f) * spacing, 0.5f * spacing, 0f),  // Way outside bounds
        };
        
        Debug.Log("Testing VALID gap positions (should allow placement):");
        for (int i = 0; i < validPositions.Length; i++)
        {
            Vector3 pos = validPositions[i];
            bool canPlace = TestSinglePosition(pos, true);
            Debug.Log($"  Gap {i+1}: {pos} -> {(canPlace ? "✓ VALID" : "✗ BLOCKED")}");
        }
        
        Debug.Log("Testing INVALID positions (should block placement):");
        for (int i = 0; i < invalidPositions.Length; i++)
        {
            Vector3 pos = invalidPositions[i];
            bool canPlace = TestSinglePosition(pos, false);
            Debug.Log($"  Position {i+1}: {pos} -> {(canPlace ? "✗ INCORRECTLY ALLOWED" : "✓ CORRECTLY BLOCKED")}");
        }
        
        Debug.Log("=== Bounds Test Complete ===\nWalls should now only be placeable in gaps between tiles!");
    }
    
    bool TestSinglePosition(Vector3 worldPos, bool shouldBeValid)
    {
        // Save initial state
        int initialWallCount = wallManager.GetManagedWallCount();
        
        // Try to place wall
        bool placed = wallManager.TryPlaceWall(worldPos);
        
        // Check if placement happened
        int finalWallCount = wallManager.GetManagedWallCount();
        bool actuallyPlaced = finalWallCount > initialWallCount;
        
        // Clean up if wall was placed
        if (actuallyPlaced)
        {
            wallManager.ClearAllWalls();
        }
        
        return actuallyPlaced;
    }
    
    [ContextMenu("Run Bounds Test")]
    public void ManualBoundsTest()
    {
        if (gameManager == null)
        {
            gameManager = FindObjectOfType<WallChessGameManager>();
            wallManager = gameManager?.GetWallManager();
        }
        
        if (wallManager != null)
        {
            TestBounds();
        }
        else
        {
            Debug.LogError("Cannot run manual bounds test - WallManager not found!");
        }
    }
}