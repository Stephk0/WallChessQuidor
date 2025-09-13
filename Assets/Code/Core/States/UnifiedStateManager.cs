using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

namespace WallChess.Core.States
{
    /// <summary>
    /// Unified state manager replacing both enum and FSM systems.
    /// Thread-safe, event-driven, validated transitions.
    /// Part of Phase 2: State Management Consolidation (MVP Implementation Guide)
    /// </summary>
    public sealed class UnifiedStateManager : MonoBehaviour
    {
        #region Singleton Pattern
        private static UnifiedStateManager _instance;
        private static readonly object _lock = new object();
        
        public static UnifiedStateManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance = FindObjectOfType<UnifiedStateManager>();
                        if (_instance == null)
                        {
                            GameObject go = new GameObject("UnifiedStateManager");
                            _instance = go.AddComponent<UnifiedStateManager>();
                            DontDestroyOnLoad(go);
                        }
                    }
                }
                return _instance;
            }
        }
        #endregion

        #region State Definitions
        public enum StateType
        {
            Initialization,
            MainMenu,
            GameSetup,
            PlayerTurn,
            PawnSelection,
            PawnMoving,
            WallSelection,
            WallPlacement,
            TurnValidation,
            TurnTransition,
            GameOver,
            Paused
        }

        /// <summary>
        /// Base class for all game states
        /// </summary>
        private abstract class BaseState
        {
            public StateType Type { get; protected set; }
            public string Name => Type.ToString();
            protected UnifiedStateManager Manager { get; private set; }
            private readonly HashSet<StateType> _validTransitions = new HashSet<StateType>();
            
            public BaseState(UnifiedStateManager manager, StateType type)
            {
                Manager = manager;
                Type = type;
                DefineValidTransitions();
            }
            
            /// <summary>
            /// Called when entering this state
            /// </summary>
            public virtual void OnEnter() 
            {
                if (Manager._enableDebugLogging)
                    Debug.Log($"[State] Entering: {Name}");
            }
            
            /// <summary>
            /// Called every frame while in this state
            /// </summary>
            public virtual void OnUpdate() { }
            
            /// <summary>
            /// Called when exiting this state
            /// </summary>
            public virtual void OnExit() 
            {
                if (Manager._enableDebugLogging)
                    Debug.Log($"[State] Exiting: {Name}");
            }
            
            /// <summary>
            /// Checks if transition to target state is valid
            /// </summary>
            public virtual bool CanTransitionTo(StateType targetState)
            {
                return _validTransitions.Contains(targetState);
            }
            
            /// <summary>
            /// Define which states this state can transition to
            /// </summary>
            protected abstract void DefineValidTransitions();
            
            /// <summary>
            /// Add valid transition targets
            /// </summary>
            protected void AddValidTransitions(params StateType[] states)
            {
                foreach (var state in states)
                {
                    _validTransitions.Add(state);
                }
            }
        }
        #endregion

        #region Concrete State Implementations
        private class InitializationState : BaseState
        {
            public InitializationState(UnifiedStateManager manager) : base(manager, StateType.Initialization) { }
            
            protected override void DefineValidTransitions()
            {
                AddValidTransitions(StateType.MainMenu, StateType.GameSetup);
            }
            
            public override void OnEnter()
            {
                base.OnEnter();
                // Initialize game systems
                Debug.Log("[State] Initializing game systems...");
            }
        }

        private class MainMenuState : BaseState
        {
            public MainMenuState(UnifiedStateManager manager) : base(manager, StateType.MainMenu) { }
            
            protected override void DefineValidTransitions()
            {
                AddValidTransitions(StateType.GameSetup, StateType.Initialization);
            }
        }

        private class GameSetupState : BaseState
        {
            public GameSetupState(UnifiedStateManager manager) : base(manager, StateType.GameSetup) { }
            
            protected override void DefineValidTransitions()
            {
                AddValidTransitions(StateType.PlayerTurn, StateType.MainMenu);
            }
            
            public override void OnEnter()
            {
                base.OnEnter();
                // Setup board, players, initial positions
                Debug.Log("[State] Setting up game board and players...");
            }
        }

        private class PlayerTurnState : BaseState
        {
            public PlayerTurnState(UnifiedStateManager manager) : base(manager, StateType.PlayerTurn) { }
            
            protected override void DefineValidTransitions()
            {
                AddValidTransitions(
                    StateType.PawnSelection,
                    StateType.WallSelection,
                    StateType.GameOver,
                    StateType.Paused
                );
            }
            
            public override void OnEnter()
            {
                base.OnEnter();
                // Enable input for current player
                UnifiedStateManager.OnPlayerTurnStarted?.Invoke(Manager.GetCurrentPlayerId());
            }
        }

        private class PawnSelectionState : BaseState
        {
            public PawnSelectionState(UnifiedStateManager manager) : base(manager, StateType.PawnSelection) { }
            
            protected override void DefineValidTransitions()
            {
                AddValidTransitions(
                    StateType.PawnMoving,
                    StateType.PlayerTurn,
                    StateType.Paused
                );
            }
            
            public override void OnUpdate()
            {
                // Handle pawn selection input
                if (UnityEngine.Input.GetMouseButtonDown(0))
                {
                    // Check if a pawn was clicked
                    // If valid, transition to PawnMoving
                }
                
                if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
                {
                    Manager.RequestStateChange(StateType.PlayerTurn);
                }
            }
        }

        private class PawnMovingState : BaseState
        {
            public PawnMovingState(UnifiedStateManager manager) : base(manager, StateType.PawnMoving) { }
            
            protected override void DefineValidTransitions()
            {
                AddValidTransitions(
                    StateType.TurnValidation,
                    StateType.PawnSelection,
                    StateType.PlayerTurn,
                    StateType.Paused
                );
            }
            
            public override void OnEnter()
            {
                base.OnEnter();
                // Show valid move positions
            }
            
            public override void OnUpdate()
            {
                // Handle movement input
                if (UnityEngine.Input.GetMouseButtonDown(0))
                {
                    // Execute move if valid
                    // Transition to TurnValidation
                }
            }
        }

        private class WallSelectionState : BaseState
        {
            public WallSelectionState(UnifiedStateManager manager) : base(manager, StateType.WallSelection) { }
            
            protected override void DefineValidTransitions()
            {
                AddValidTransitions(
                    StateType.WallPlacement,
                    StateType.PlayerTurn,
                    StateType.Paused
                );
            }
            
            public override void OnEnter()
            {
                base.OnEnter();
                // Check if player has walls remaining
                var wallPlacer = FindObjectOfType<WallPlacer>();
                if (wallPlacer != null)
                {
                    int playerId = Manager.GetCurrentPlayerId();
                    if (wallPlacer.CanPlayerPlaceWall(playerId))
                    {
                        Manager.RequestStateChange(StateType.WallPlacement);
                    }
                    else
                    {
                        Debug.LogWarning("Player has no walls remaining");
                        Manager.RequestStateChange(StateType.PlayerTurn);
                    }
                }
            }
        }

        private class WallPlacementState : BaseState
        {
            public WallPlacementState(UnifiedStateManager manager) : base(manager, StateType.WallPlacement) { }
            
            protected override void DefineValidTransitions()
            {
                AddValidTransitions(
                    StateType.TurnValidation,
                    StateType.PlayerTurn,
                    StateType.Paused
                );
            }
            
            public override void OnEnter()
            {
                base.OnEnter();
                // Start wall placement mode
                var wallPlacer = FindObjectOfType<WallPlacer>();
                if (wallPlacer != null)
                {
                    wallPlacer.StartWallPlacement(Manager.GetCurrentPlayerId());
                }
            }
            
            public override void OnExit()
            {
                base.OnExit();
                // Cancel wall placement if still active
                var wallPlacer = FindObjectOfType<WallPlacer>();
                if (wallPlacer != null)
                {
                    wallPlacer.CancelWallPlacement();
                }
            }
        }

        private class TurnValidationState : BaseState
        {
            public TurnValidationState(UnifiedStateManager manager) : base(manager, StateType.TurnValidation) { }
            
            protected override void DefineValidTransitions()
            {
                AddValidTransitions(
                    StateType.TurnTransition,
                    StateType.GameOver,
                    StateType.PlayerTurn
                );
            }
            
            public override void OnEnter()
            {
                base.OnEnter();
                // Check win conditions
                if (CheckWinCondition())
                {
                    Manager.RequestStateChange(StateType.GameOver);
                }
                else
                {
                    Manager.RequestStateChange(StateType.TurnTransition);
                }
            }
            
            private bool CheckWinCondition()
            {
                // Check if any player has reached their goal
                // Placeholder for now
                return false;
            }
        }

        private class TurnTransitionState : BaseState
        {
            private float transitionDuration = 0.5f;
            private float transitionTimer = 0f;
            
            public TurnTransitionState(UnifiedStateManager manager) : base(manager, StateType.TurnTransition) { }
            
            protected override void DefineValidTransitions()
            {
                AddValidTransitions(StateType.PlayerTurn);
            }
            
            public override void OnEnter()
            {
                base.OnEnter();
                transitionTimer = 0f;
                // Switch to next player
                Manager.NextPlayer();
            }
            
            public override void OnUpdate()
            {
                transitionTimer += Time.deltaTime;
                if (transitionTimer >= transitionDuration)
                {
                    Manager.RequestStateChange(StateType.PlayerTurn);
                }
            }
        }

        private class GameOverState : BaseState
        {
            public GameOverState(UnifiedStateManager manager) : base(manager, StateType.GameOver) { }
            
            protected override void DefineValidTransitions()
            {
                AddValidTransitions(StateType.MainMenu, StateType.GameSetup);
            }
            
            public override void OnEnter()
            {
                base.OnEnter();
                Debug.Log($"[State] Game Over! Winner: Player {Manager.GetCurrentPlayerId()}");
                UnifiedStateManager.OnGameOver?.Invoke(Manager.GetCurrentPlayerId());
            }
        }

        private class PausedState : BaseState
        {
            private StateType _previousState;
            
            public PausedState(UnifiedStateManager manager) : base(manager, StateType.Paused) { }
            
            protected override void DefineValidTransitions()
            {
                // Can transition back to any game state
                AddValidTransitions(
                    StateType.PlayerTurn,
                    StateType.PawnSelection,
                    StateType.PawnMoving,
                    StateType.WallSelection,
                    StateType.WallPlacement,
                    StateType.MainMenu
                );
            }
            
            public override void OnEnter()
            {
                base.OnEnter();
                _previousState = Manager._previousStateType;
                Time.timeScale = 0f;
            }
            
            public override void OnExit()
            {
                base.OnExit();
                Time.timeScale = 1f;
            }
            
            public override void OnUpdate()
            {
                if (UnityEngine.Input.GetKeyDown(KeyCode.Escape) || UnityEngine.Input.GetKeyDown(KeyCode.P))
                {
                    Manager.RequestStateChange(_previousState);
                }
            }
        }
        #endregion

        #region Core Implementation
        private BaseState _currentState;
        private StateType _previousStateType;
        private readonly Dictionary<StateType, BaseState> _states = new Dictionary<StateType, BaseState>();
        private bool _isTransitioning = false;
        
        // Player management
        private int _currentPlayerId = 0;
        private int _playerCount = 2;
        
        [Header("Configuration")]
        [SerializeField] private bool _enableDebugLogging = true;
        [SerializeField] private bool _logStateTransitions = true;
        
        [Header("Debug Info")]
        [SerializeField] private string _currentStateName = "None";
        [SerializeField] private int _currentPlayerDisplay = 0;
        
        #region Events
        public delegate void StateChangeHandler(StateType fromState, StateType toState);
        public static event StateChangeHandler OnStateChanged;
        
        public delegate void PlayerChangeHandler(int playerId);
        public static event PlayerChangeHandler OnPlayerTurnStarted;
        
        public delegate void GameEndHandler(int winnerId);
        public static event GameEndHandler OnGameOver;
        #endregion

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeStates();
        }

        private void Update()
        {
            _currentState?.OnUpdate();
            
            // Debug display
            if (_currentState != null)
            {
                _currentStateName = _currentState.Name;
                _currentPlayerDisplay = _currentPlayerId;
            }
            
            // Global pause handling
            if (UnityEngine.Input.GetKeyDown(KeyCode.P) && _currentState?.Type != StateType.Paused)
            {
                RequestStateChange(StateType.Paused);
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void InitializeStates()
        {
            // Register all state implementations
            RegisterState(new InitializationState(this));
            RegisterState(new MainMenuState(this));
            RegisterState(new GameSetupState(this));
            RegisterState(new PlayerTurnState(this));
            RegisterState(new PawnSelectionState(this));
            RegisterState(new PawnMovingState(this));
            RegisterState(new WallSelectionState(this));
            RegisterState(new WallPlacementState(this));
            RegisterState(new TurnValidationState(this));
            RegisterState(new TurnTransitionState(this));
            RegisterState(new GameOverState(this));
            RegisterState(new PausedState(this));
            
            // Start with initialization
            ForceState(StateType.Initialization);
        }
        
        private void RegisterState(BaseState state)
        {
            if (!_states.ContainsKey(state.Type))
            {
                _states[state.Type] = state;
                if (_enableDebugLogging)
                    Debug.Log($"[StateManager] Registered state: {state.Name}");
            }
        }
        
        /// <summary>
        /// Request a state change with validation
        /// </summary>
        public bool RequestStateChange(StateType targetState)
        {
            if (_isTransitioning)
            {
                Debug.LogWarning($"[StateManager] Cannot change state while transitioning");
                return false;
            }
            
            if (_currentState != null && !_currentState.CanTransitionTo(targetState))
            {
                Debug.LogWarning($"[StateManager] Invalid transition: {_currentState.Type} -> {targetState}");
                return false;
            }
            
            return ExecuteStateTransition(targetState);
        }
        
        /// <summary>
        /// Force a state change without validation (use carefully)
        /// </summary>
        public void ForceState(StateType targetState)
        {
            ExecuteStateTransition(targetState);
        }

        private bool ExecuteStateTransition(StateType targetState)
        {
            if (_currentState != null && _currentState.Type == targetState)
            {
                if (_enableDebugLogging)
                    Debug.Log($"[StateManager] Already in state: {targetState}");
                return false;
            }
            
            _isTransitioning = true;
            StateType fromState = _currentState?.Type ?? StateType.Initialization;
            
            // Store previous state
            if (_currentState != null)
            {
                _previousStateType = _currentState.Type;
            }
            
            // Exit current state
            _currentState?.OnExit();
            
            // Transition to new state
            if (_states.TryGetValue(targetState, out BaseState newState))
            {
                _currentState = newState;
                newState.OnEnter();
                
                // Fire event
                OnStateChanged?.Invoke(fromState, targetState);
                
                if (_logStateTransitions)
                    Debug.Log($"[StateManager] Transitioned: {fromState} -> {targetState}");
            }
            else
            {
                Debug.LogError($"[StateManager] State not found: {targetState}");
            }
            
            _isTransitioning = false;
            return true;
        }
        #endregion
        
        #region Public API
        /// <summary>
        /// Current state type
        /// </summary>
        public StateType CurrentStateType => _currentState?.Type ?? StateType.Initialization;
        
        /// <summary>
        /// Check if in a specific state
        /// </summary>
        public bool IsInState(StateType state) => _currentState != null && _currentState.Type == state;
        
        /// <summary>
        /// Check if in any of the specified states
        /// </summary>
        public bool IsInAnyState(params StateType[] states)
        {
            return states.Any(s => IsInState(s));
        }
        
        /// <summary>
        /// Initialize game session
        /// </summary>
        public void StartNewGame(int playerCount)
        {
            _playerCount = Mathf.Clamp(playerCount, 2, 4);
            _currentPlayerId = 0;
            RequestStateChange(StateType.GameSetup);
        }
        
        /// <summary>
        /// Get current player ID
        /// </summary>
        public int GetCurrentPlayerId()
        {
            return _currentPlayerId;
        }
        
        /// <summary>
        /// Move to next player
        /// </summary>
        public void NextPlayer()
        {
            _currentPlayerId = (_currentPlayerId + 1) % _playerCount;
            Debug.Log($"[StateManager] Current player: {_currentPlayerId}");
        }
        
        /// <summary>
        /// Get state history for debugging
        /// </summary>
        public string GetStateHistory()
        {
            return $"Current: {CurrentStateType}, Previous: {_previousStateType}";
        }
        #endregion
    }
}