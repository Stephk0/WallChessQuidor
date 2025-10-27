// Force compilation refresh - should resolve namespace issues
using UnityEngine;
using WallChess.Gameplay.Pawns;
using WallChess.Core;

namespace WallChess.Testing
{
    /// <summary>
    /// Simple compilation test to force Unity to recognize the proper namespaces
    /// </summary>
    public class CompilationForceRefresh : MonoBehaviour
    {
        void Start()
        {
            // Test namespace visibility
            var pawnManager = Object.FindObjectOfType<PawnManager>();
            var sessionManager = Object.FindObjectOfType<SessionStateManager>();
            
            if (pawnManager != null && sessionManager != null)
            {
                Debug.Log("✅ All namespaces resolved correctly in CompilationForceRefresh");
            }
            else
            {
                Debug.LogWarning("❌ Namespace resolution issue in CompilationForceRefresh");
            }
        }
    }
}
