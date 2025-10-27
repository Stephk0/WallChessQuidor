using UnityEngine;
using WallChess.Core;

public class SimpleTest : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("SPACE pressed - testing wall placement");
            TestWallPlacement();
        }
    }
    
    void TestWallPlacement()
    {
        var gameManager = FindFirstObjectByType<WallChess.WallChessGameManager>();
        if (gameManager != null)
        {
            Debug.Log($"Current State: {gameManager.GetCurrentState()}, Debug Mode: {gameManager.debugMode}");
            
            // Force PlayerTurn state
            gameManager.ChangeState(GameState.PlayerTurn);
            
            var wallManager = gameManager.GetWallManager();
            if (wallManager != null)
            {
                Vector3 pos = wallManager.GetWallWorldPosition(WallChess.GridSystem.Orientation.Horizontal, 3, 3);
                bool success = wallManager.TryPlaceWall(pos);
                Debug.Log($"Wall placement: {success}");
            }
        }
    }
}
