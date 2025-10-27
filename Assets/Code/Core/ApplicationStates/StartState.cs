using UnityEngine;

namespace WallChess.Core.ApplicationStates
{
    /// <summary>
    /// Initial application state - handles game initialization and loading
    /// Automatically transitions to MenuState when ready
    /// </summary>
    public class StartState : IState
    {
        public string StateName => "Start";
        
        private readonly StateMachine stateMachine;
        private bool initialized = false;
        
        public StartState(StateMachine stateMachine)
        {
            this.stateMachine = stateMachine;
        }
        
        public void OnEnter()
        {
            Debug.Log("StartState: Initializing application...");
            initialized = false;
            
            // Initialize core systems here
            InitializeApplication();
        }
        
        public void OnUpdate()
        {
            // Simple immediate transition for now
            // In a real application, you might wait for loading to complete
            if (!initialized)
            {
                initialized = true;
                Debug.Log("StartState: Initialization complete, transitioning to Menu");
                stateMachine.ChangeState<MenuState>();
            }
        }
        
        public void OnExit()
        {
            Debug.Log("StartState: Initialization complete");
        }
        
        private void InitializeApplication()
        {
            // Future: Initialize save system, settings, etc.
            Debug.Log("StartState: Core systems initialized");
        }
    }
}