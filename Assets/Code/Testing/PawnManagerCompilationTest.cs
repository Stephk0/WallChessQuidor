using UnityEngine;
using WallChess.Gameplay.Pawns;

namespace WallChess.Testing
{
    /// <summary>
    /// Simple test to verify PawnManager compiles correctly
    /// </summary>
    public class PawnManagerCompilationTest : MonoBehaviour
    {
        void Start()
        {
            // This will only compile if PawnManager has no errors
            var pawnManager = Object.FindObjectOfType<PawnManager>();
            
            if (pawnManager != null)
            {
                Debug.Log("✅ PawnManager compilation test PASSED - no compilation errors");
                
                // Test if the new methods exist
                bool canEarlyInit = pawnManager.ArePawnsInitializedButHidden();
                Debug.Log($"✅ Early initialization methods available: {canEarlyInit}");
            }
            else
            {
                Debug.Log("ℹ️ PawnManager not found in scene (this is OK for compilation test)");
            }
        }
    }
}
