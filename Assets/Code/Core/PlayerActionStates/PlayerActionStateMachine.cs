using UnityEngine;
using PawnClass = WallChess.Gameplay.Pawns.Pawn;

namespace WallChess.Core.PlayerActionStates
{
    /// <summary>
    /// Player Action State Machine - Manages player action flow
    /// Third FSM layer: Idle → Action → Confirm/Cancel → Idle
    /// </summary>
    public class PlayerActionStateMachine : MonoBehaviour
    {
        public PlayerActionState CurrentState { get; private set; } = PlayerActionState.Idle;
        
        public System.Action<PlayerActionState, PlayerActionState> OnStateChanged;
        public System.Action OnActionConfirmed;
        public System.Action OnActionCancelled;
        
        [Header("Debug")]
        [SerializeField] private bool debugLogs = true;
        
        private PawnClass controlledPawn;
        private Vector2Int? pendingMoveTarget;
        private Vector3? pendingWallPosition;
        
        public void Initialize(PawnClass pawn)
        {
            controlledPawn = pawn;
            CurrentState = PlayerActionState.Idle;
            LogInfo($"PlayerActionStateMachine initialized for {pawn?.PlayerData.playerName}");
        }
        
        public void TransitionTo(PlayerActionState newState)
        {
            if (CurrentState == newState) return;
            
            PlayerActionState previousState = CurrentState;
            
            // Exit current state
            OnExitState(CurrentState);
            
            // Change state
            CurrentState = newState;
            
            // Enter new state
            OnEnterState(newState);
            
            // Fire event
            OnStateChanged?.Invoke(previousState, newState);
            
            LogInfo($"State: {previousState} → {newState}");
        }
        
        private void OnEnterState(PlayerActionState state)
        {
            switch (state)
            {
                case PlayerActionState.Idle:
                    // Clear any pending actions
                    pendingMoveTarget = null;
                    pendingWallPosition = null;
                    break;
                    
                case PlayerActionState.MovingPawn:
                    LogInfo("Started moving pawn");
                    break;
                    
                case PlayerActionState.PlacingWall:
                    LogInfo("Started placing wall");
                    break;
                    
                case PlayerActionState.Confirming:
                    LogInfo("Confirming action");
                    break;
                    
                case PlayerActionState.Canceling:
                    LogInfo("Canceling action");
                    break;
            }
        }
        
        private void OnExitState(PlayerActionState state)
        {
            // Cleanup for each state
        }
        
        public void StartPawnMove(Vector2Int targetPosition)
        {
            if (CurrentState != PlayerActionState.Idle)
            {
                LogWarning($"Cannot start pawn move from state {CurrentState}");
                return;
            }
            
            pendingMoveTarget = targetPosition;
            TransitionTo(PlayerActionState.MovingPawn);
        }
        
        public void StartWallPlacement(Vector3 wallPosition)
        {
            if (CurrentState != PlayerActionState.Idle)
            {
                LogWarning($"Cannot start wall placement from state {CurrentState}");
                return;
            }
            
            pendingWallPosition = wallPosition;
            TransitionTo(PlayerActionState.PlacingWall);
        }
        
        public void ConfirmAction()
        {
            if (CurrentState == PlayerActionState.Idle)
            {
                LogWarning("No action to confirm");
                return;
            }
            
            TransitionTo(PlayerActionState.Confirming);
            
            // Fire confirmed event (will advance turn in game session FSM)
            OnActionConfirmed?.Invoke();
            
            // Return to idle
            TransitionTo(PlayerActionState.Idle);
        }
        
        public void CancelAction()
        {
            if (CurrentState == PlayerActionState.Idle)
            {
                LogWarning("No action to cancel");
                return;
            }
            
            TransitionTo(PlayerActionState.Canceling);
            
            // Fire cancelled event
            OnActionCancelled?.Invoke();
            
            // Return to idle
            TransitionTo(PlayerActionState.Idle);
        }
        
        public Vector2Int? GetPendingMoveTarget() => pendingMoveTarget;
        public Vector3? GetPendingWallPosition() => pendingWallPosition;
        
        public bool IsIdle() => CurrentState == PlayerActionState.Idle;
        public bool IsPerformingAction() => CurrentState != PlayerActionState.Idle;
        
        private void LogInfo(string message)
        {
            if (debugLogs) Debug.Log($"[PlayerActionFSM] {message}");
        }
        
        private void LogWarning(string message)
        {
            if (debugLogs) Debug.LogWarning($"[PlayerActionFSM] {message}");
        }
    }
}
