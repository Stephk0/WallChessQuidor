using UnityEngine;

namespace WallChess.Core.States
{
    /// <summary>
    /// Base class for all game states, providing common functionality
    /// </summary>
    public abstract class BaseState : IState
    {
        protected WallChessGameManager gameManager;
        protected StateMachine stateMachine;
        
        /// <summary>
        /// Constructor with required dependencies
        /// </summary>
        public BaseState(WallChessGameManager gameManager, StateMachine stateMachine)
        {
            this.gameManager = gameManager;
            this.stateMachine = stateMachine;
        }
        
        /// <summary>
        /// State name for debugging - override in derived classes
        /// </summary>
        public abstract string StateName { get; }
        
        /// <summary>
        /// Called when entering this state
        /// </summary>
        public virtual void OnEnter()
        {
            UnityEngine.Debug.Log($"Entering {StateName} state");
        }
        
        /// <summary>
        /// Called every frame while in this state
        /// </summary>
        public virtual void OnUpdate()
        {
            // Base implementation does nothing
            // Override in derived classes for frame-by-frame logic
        }
        
        /// <summary>
        /// Called when exiting this state
        /// </summary>
        public virtual void OnExit()
        {
            UnityEngine.Debug.Log($"Exiting {StateName} state");
        }
        
        /// <summary>
        /// Helper to transition to another state
        /// </summary>
        protected bool TransitionTo<T>() where T : class, IState
        {
            return stateMachine.ChangeState<T>();
        }
        
        /// <summary>
        /// Helper to check if game manager is valid
        /// </summary>
        protected bool IsGameManagerValid()
        {
            if (gameManager == null)
            {
                Debug.LogError($"{StateName}: GameManager is null");
                return false;
            }
            return true;
        }
    }
}