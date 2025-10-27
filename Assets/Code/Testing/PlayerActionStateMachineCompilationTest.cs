using UnityEngine;
using WallChess.Core.PlayerActionStates;

namespace WallChess.Testing
{
    /// <summary>
    /// Forces PlayerActionStateMachine compilation
    /// </summary>
    public class PlayerActionStateMachineCompilationTest : MonoBehaviour
    {
        private void TestCompilation()
        {
            var fsm = gameObject.AddComponent<PlayerActionStateMachine>();
            Debug.Log($"PlayerActionStateMachine test: {fsm != null}");
        }
    }
}
