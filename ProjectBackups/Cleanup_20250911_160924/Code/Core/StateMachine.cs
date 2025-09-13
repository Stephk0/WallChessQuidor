using UnityEngine;
using System.Collections.Generic;

namespace WallChess.Core
{
    /// <summary>
    /// Generic finite state machine for managing game states
    /// Designed to work alongside existing WallChessGameManager during refactor
    /// </summary>
    public class StateMachine : MonoBehaviour
    {
        private IState currentState;
        private Dictionary<System.Type, IState> states = new Dictionary<System.Type, IState>();
        
        [Header("Debug")]
        [SerializeField] private string currentStateName = "None";
        [SerializeField] private bool enableDebugLogs = true;

        /// <summary>
        /// Current active state
        /// </summary>
        public IState CurrentState => currentState;
        
        /// <summary>
        /// Register a state with the state machine
        /// </summary>
        public void RegisterState<T>(T state) where T : class, IState
        {
            var type = typeof(T);
            if (!states.ContainsKey(type))
            {
                states[type] = state;
                if (enableDebugLogs)
                    UnityEngine.Debug.Log($"StateMachine: Registered state {type.Name}");
            }
            else
            {
                Debug.LogWarning($"StateMachine: State {type.Name} already registered");
            }
        }
        
        /// <summary>
        /// Transition to a specific state type
        /// </summary>
        public bool ChangeState<T>() where T : class, IState
        {
            var type = typeof(T);
            if (states.TryGetValue(type, out IState newState))
            {
                return ChangeState(newState);
            }
            
            Debug.LogError($"StateMachine: State {type.Name} not registered");
            return false;
        }
        
        /// <summary>
        /// Transition to a specific state instance
        /// </summary>
        public bool ChangeState(IState newState)
        {
            if (newState == null)
            {
                Debug.LogError("StateMachine: Cannot change to null state");
                return false;
            }
            
            // Exit current state
            if (currentState != null)
            {
                if (enableDebugLogs)
                    Debug.Log($"StateMachine: Exiting state {currentState.StateName}");
                currentState.OnExit();
            }
            
            // Change to new state
            var previousStateName = currentState?.StateName ?? "None";
            currentState = newState;
            currentStateName = currentState.StateName;
            
            if (enableDebugLogs)
                Debug.Log($"StateMachine: Transitioning from {previousStateName} to {currentStateName}");
            
            // Enter new state
            currentState.OnEnter();
            
            return true;
        }
        
        /// <summary>
        /// Get a registered state of specific type
        /// </summary>
        public T GetState<T>() where T : class, IState
        {
            var type = typeof(T);
            if (states.TryGetValue(type, out IState state))
            {
                return state as T;
            }
            return null;
        }
        
        /// <summary>
        /// Check if currently in a specific state type
        /// </summary>
        public bool IsInState<T>() where T : class, IState
        {
            return currentState is T;
        }
        
        void Update()
        {
            // Update current state
            currentState?.OnUpdate();
        }
        
        void OnDestroy()
        {
            // Clean exit from current state
            if (currentState != null)
            {
                currentState.OnExit();
                currentState = null;
            }
        }

        #region Debug Methods
        [ContextMenu("Debug/Print Current State")]
        private void DebugPrintCurrentState()
        {
            Debug.Log($"Current State: {currentStateName}");
        }
        
        [ContextMenu("Debug/Print All Registered States")]
        private void DebugPrintAllStates()
        {
            Debug.Log($"Registered States ({states.Count}):");
            foreach (var kvp in states)
            {
                Debug.Log($"- {kvp.Key.Name}: {kvp.Value.StateName}");
            }
        }
        #endregion
    }
}