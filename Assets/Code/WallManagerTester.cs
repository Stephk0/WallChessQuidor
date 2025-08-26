using UnityEngine;
using WallChess;

/// <summary>
/// Test script to verify WallManager refactoring and integration
/// </summary>
public class WallManagerTester : MonoBehaviour
{
    [Header("Test Settings")]
    public bool runTests = true;
    public bool showDebugInfo = true;
    
    private WallChessGameManager gameManager;
    private WallManager wallManager;
    
    void Start()
    {
        if (runTests)
        {
            StartCoroutine(RunTestSequence());
        }
    }
    
    System.Collections.IEnumerator RunTestSequence()
    {
        yield return new WaitForSeconds(1f); // Wait for initialization
        
        gameManager = FindObjectOfType<WallChessGameManager>();
        if (gameManager != null)
        {
            wallManager = gameManager.GetWallManager();
            
            if (wallManager != null)
            {
                Debug.Log("=== WallManager Integration Tests ===");
                TestBasicFunctionality();
                yield return new WaitForSeconds(2f);
                
                TestAdvancedFeatures();
                yield return new WaitForSeconds(2f);
                
                TestGameIntegration();
                
                Debug.Log("=== All Tests Complete ===");
            }
            else
            {
                Debug.LogError("WallManager not found!");
            }
        }
        else
        {
            Debug.LogError("WallChessGameManager not found!");
        }
    }
    
    void TestBasicFunctionality()
    {
        Debug.Log("Testing basic wall placement functionality...");
        
        // Test 1: Wall placement at specific coordinates
        Vector3 testPos1 = new Vector3(2f, 1f, 0f);
        bool placed1 = wallManager.TryPlaceWall(testPos1);
        Debug.Log($"Test 1 - Basic placement: {(placed1 ? "PASS" : "FAIL")}");
        
        // Test 2: Verify wall count
        int wallCount = wallManager.GetManagedWallCount();
        Debug.Log($"Test 2 - Wall count tracking: {(wallCount == 1 ? "PASS" : "FAIL")} (Count: {wallCount})");
        
        // Test 3: Gap occupancy check
        bool occupied = wallManager.IsGapOccupied(WallManager.Orientation.Horizontal, 1, 1);
        Debug.Log($"Test 3 - Gap occupancy: {(occupied ? "PASS" : "FAIL")}");
    }
    
    void TestAdvancedFeatures()
    {
        Debug.Log("Testing advanced features from ImprovedWallPlacer...");
        
        // Test 4: Lane detection and orientation locking
        Vector3 testPos2 = new Vector3(1.5f, 2.5f, 0f);
        bool placed2 = wallManager.TryPlaceWall(testPos2);
        Debug.Log($"Test 4 - Advanced gap detection: {(placed2 ? "PASS" : "FAIL")}");
        
        // Test 5: Crossing prevention
        Vector3 crossingPos = new Vector3(2f, 2f, 0f);
        bool blocked = !wallManager.TryPlaceWall(crossingPos);
        Debug.Log($"Test 5 - Crossing prevention: {(blocked ? "PASS" : "FAIL")}");
        
        // Test 6: Valid wall positions enumeration
        var validPositions = wallManager.GetValidWallPositions();
        Debug.Log($"Test 6 - Valid positions count: {validPositions.Count}");
    }
    
    void TestGameIntegration()
    {
        Debug.Log("Testing game manager integration...");
        
        // Test 7: Wall count integration
        int initialPlayerWalls = gameManager.playerWallsRemaining;
        Vector3 testPos3 = new Vector3(3f, 3f, 0f);
        bool placed3 = wallManager.TryPlaceWall(testPos3);
        int finalPlayerWalls = gameManager.playerWallsRemaining;
        
        bool wallCountDecremented = (initialPlayerWalls - finalPlayerWalls) == (placed3 ? 1 : 0);
        Debug.Log($"Test 7 - Wall count integration: {(wallCountDecremented ? "PASS" : "FAIL")}");
        
        // Test 8: Movement blocking
        Vector2Int testMove = new Vector2Int(2, 2);
        bool blocked = wallManager.IsMovementBlocked(new Vector2Int(1, 2), testMove);
        Debug.Log($"Test 8 - Movement blocking: {(blocked ? "PASS" : "FAIL")}");
    }
    
    [ContextMenu("Clear All Test Walls")]
    void ClearTestWalls()
    {
        if (wallManager != null)
        {
            wallManager.ClearAllWalls();
            Debug.Log("Test walls cleared");
        }
    }
    
    [ContextMenu("Run Tests Again")]
    void RunTestsAgain()
    {
        if (gameManager != null)
        {
            ClearTestWalls();
            StartCoroutine(RunTestSequence());
        }
    }
}