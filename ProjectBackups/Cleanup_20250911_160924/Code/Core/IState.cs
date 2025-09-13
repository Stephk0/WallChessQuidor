namespace WallChess.Core
{
    /// <summary>
    /// Interface for all game states in the FSM
    /// </summary>
    public interface IState
    {
        /// <summary>
        /// Called when entering this state
        /// </summary>
        void OnEnter();
        
        /// <summary>
        /// Called every frame while in this state
        /// </summary>
        void OnUpdate();
        
        /// <summary>
        /// Called when exiting this state
        /// </summary>
        void OnExit();
        
        /// <summary>
        /// State identifier for debugging
        /// </summary>
        string StateName { get; }
    }
}