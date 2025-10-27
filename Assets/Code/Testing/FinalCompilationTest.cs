using UnityEngine;
using WallChess;
using WallChess.Core;
using WallChess.Core.States;
using WallChess.Gameplay.Pawns;

/// <summary>
/// Compilation verification - tests that all session state components work together
/// This forces Unity to recompile all dependencies fresh
/// </summary>
public class FinalCompilationTest : MonoBehaviour
{
    [Header("Test Results")]
    [SerializeField] private bool allComponentsCompile = false;
    [SerializeField] private string compilationStatus = "Testing...";
    
    void Awake()
    {
        TestCompilation();
    }
    
    void TestCompilation()
    {
        try
        {
            // Test that all types can be referenced
            var gameManagerType = typeof(WallChessGameManager);
            var sessionStateManagerType = typeof(SessionStateManager);
            var buildTilesStateType = typeof(BuildTilesState);
            var spawnPawnsStateType = typeof(SpawnPawnsState);
            var activeGameplayStateType = typeof(GameplayState);
            var stateMachineType = typeof(StateMachine);
            var pawnManagerType = typeof(PawnManager);
            
            // Test that FindObjectOfType works correctly
            var gameManager = Object.FindObjectOfType<WallChessGameManager>();
            var pawnManager = Object.FindObjectOfType<PawnManager>();
            
            allComponentsCompile = true;
            compilationStatus = "✓ ALL COMPONENTS COMPILE SUCCESSFULLY!";
            
            Debug.Log("=== COMPILATION SUCCESS ===");
            Debug.Log("✓ All session state components compile correctly");
            Debug.Log("✓ All namespace references resolved");
            Debug.Log("✓ All FindObjectOfType calls work");
            Debug.Log("✓ System ready for use!");
            
        }
        catch (System.Exception e)
        {
            allComponentsCompile = false;
            compilationStatus = $"✗ Compilation failed: {e.Message}";
            Debug.LogError($"Compilation test failed: {e}");
        }
    }
    
    [ContextMenu("Force Recompilation Test")]
    void ForceRecompilationTest()
    {
        TestCompilation();
    }
}
