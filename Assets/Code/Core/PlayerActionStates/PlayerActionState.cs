using UnityEngine;

namespace WallChess.Core.PlayerActionStates
{
    /// <summary>
    /// Player Action States - Third FSM layer for individual player actions
    /// Handles: Idle → MovingPawn/PlacingWall/PowerUp → Cancel/Confirm → Idle
    /// </summary>
    public enum PlayerActionState
    {
        Idle,
        MovingPawn,
        PlacingWall,
        UsingPowerup,
        Confirming,
        Canceling
    }
    
    /// <summary>
    /// Base interface for player action state handlers
    /// </summary>
    public interface IPlayerActionState
    {
        void OnEnter();
        void OnUpdate();
        void OnExit();
        bool CanTransitionTo(PlayerActionState newState);
    }
}
