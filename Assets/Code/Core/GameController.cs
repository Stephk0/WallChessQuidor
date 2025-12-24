using UnityEngine;
using WallChess.Core.Data;
using WallChess.Core.Config;
using WallChess.Core.Rules;

namespace WallChess.Core
{
    /// <summary>
    /// Main game controller. Thin orchestration layer that coordinates subsystems.
    /// Single entry point for all game actions.
    /// </summary>
    public class GameController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private BoardPreset gamePreset;
        [SerializeField] private BoardConfig boardConfig;
        [SerializeField] private GameRulesConfig rulesConfig;

        // Core state
        public GameState State { get; private set; }
        public BoardConfig Board => boardConfig;
        public GameRulesConfig Rules => rulesConfig;

        // Rules engine
        private QuoridorRules _rules;

        // Singleton access (optional)
        public static GameController Instance { get; private set; }

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            GameEvents.OnRestartRequested += HandleRestartRequest;
            GameEvents.OnUndoRequested += HandleUndoRequest;
        }

        private void OnDisable()
        {
            GameEvents.OnRestartRequested -= HandleRestartRequest;
            GameEvents.OnUndoRequested -= HandleUndoRequest;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                GameEvents.ClearAll();
            }
        }

        private void HandleRestartRequest()
        {
            if (gamePreset != null)
            {
                Initialize(gamePreset);
                StartGame();
            }
        }

        private void HandleUndoRequest()
        {
            // Undo not yet implemented - placeholder for future
            Debug.Log("Undo requested but not implemented");
        }

        private void Start()
        {
            if (gamePreset != null)
            {
                Initialize(gamePreset);
            }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Initialize game from a preset.
        /// </summary>
        public void Initialize(BoardPreset preset)
        {
            gamePreset = preset;
            boardConfig = preset.boardConfig;
            rulesConfig = preset.rulesConfig;

            State = preset.CreateGameState();
            _rules = new QuoridorRules(rulesConfig);

            GameEvents.RaiseGameInitialized(State);
        }

        /// <summary>
        /// Initialize game with custom state.
        /// </summary>
        public void Initialize(GameState state, BoardConfig board, GameRulesConfig rules)
        {
            State = state;
            boardConfig = board;
            rulesConfig = rules;
            _rules = new QuoridorRules(rules);

            GameEvents.RaiseGameInitialized(State);
        }

        /// <summary>
        /// Start the game (transition from NotStarted to Playing).
        /// </summary>
        public void StartGame()
        {
            if (State == null)
            {
                Debug.LogError("GameController: Cannot start game - not initialized");
                return;
            }

            if (State.Phase != GamePhase.NotStarted)
            {
                Debug.LogWarning("GameController: Game already started");
                return;
            }

            State.StartGame();
            GameEvents.RaiseGameStarted();
            GameEvents.RaiseTurnStarted(State.CurrentPlayerIndex);
        }

        #endregion

        #region Player Actions

        /// <summary>
        /// Attempt to move a pawn. Called by input system.
        /// </summary>
        public MoveResult TryMovePawn(int playerIndex, BoardPosition to)
        {
            // Validate game state
            if (!State.IsPlaying)
                return MoveResult.Failure("Game not in progress");

            if (State.CurrentPlayerIndex != playerIndex)
                return MoveResult.Failure("Not your turn");

            // Validate move
            if (!_rules.IsValidMove(State, playerIndex, to))
                return MoveResult.Failure("Invalid move");

            // Execute move
            var player = State.GetPlayer(playerIndex);
            var from = player.CurrentPosition;
            bool isJump = !from.IsAdjacent(to);

            player.MoveTo(to);
            State.History.RecordMove(new MoveRecord(State.CurrentTurn, playerIndex, from, to, isJump));

            GameEvents.RaisePawnMoved(playerIndex, from, to, isJump);
            GameEvents.RaiseValidMovesCleared();

            // Check win condition
            if (_rules.HasPlayerWon(State, playerIndex))
            {
                State.SetWinner(playerIndex);
                GameEvents.RaiseGameOver(playerIndex);
                return MoveResult.Victory(from, to, playerIndex);
            }

            // End turn
            EndTurn();
            return MoveResult.Success(from, to, isJump);
        }

        /// <summary>
        /// Attempt to place a wall. Called by input system.
        /// </summary>
        public WallPlaceResult TryPlaceWall(int playerIndex, BoardPosition position, WallOrientation orientation)
        {
            var wall = new WallPlacement(position, orientation, playerIndex, State.CurrentTurn);
            return TryPlaceWall(playerIndex, wall);
        }

        /// <summary>
        /// Attempt to place a wall. Called by input system.
        /// </summary>
        public WallPlaceResult TryPlaceWall(int playerIndex, WallPlacement wall)
        {
            // Validate game state
            if (!State.IsPlaying)
                return WallPlaceResult.Failure("Game not in progress");

            if (State.CurrentPlayerIndex != playerIndex)
                return WallPlaceResult.Failure("Not your turn");

            // Validate wall placement
            if (!_rules.CanPlaceWall(State, playerIndex, wall))
                return WallPlaceResult.Failure("Invalid wall placement");

            // Execute placement
            var player = State.GetPlayer(playerIndex);

            if (!State.Walls.TryPlaceWall(wall))
                return WallPlaceResult.Failure("Wall placement conflict");

            player.TryUseWall();
            State.History.RecordMove(new MoveRecord(State.CurrentTurn, playerIndex, wall));

            GameEvents.RaiseWallPlaced(playerIndex, wall);
            GameEvents.RaiseWallCountChanged(playerIndex, player.WallsRemaining);
            GameEvents.RaiseValidMovesCleared();

            // End turn
            EndTurn();
            return WallPlaceResult.Success(wall);
        }

        #endregion

        #region Turn Management

        private void EndTurn()
        {
            int previousPlayer = State.CurrentPlayerIndex;
            GameEvents.RaiseTurnEnded(previousPlayer);

            State.NextTurn();
            GameEvents.RaiseTurnStarted(State.CurrentPlayerIndex);
        }

        /// <summary>
        /// Pause the game.
        /// </summary>
        public void Pause()
        {
            if (State?.Phase == GamePhase.Playing)
            {
                State.Pause();
                GameEvents.RaiseGamePaused();
            }
        }

        /// <summary>
        /// Resume the game from pause.
        /// </summary>
        public void Resume()
        {
            if (State?.Phase == GamePhase.Paused)
            {
                State.Resume();
                GameEvents.RaiseGameResumed();
            }
        }

        #endregion

        #region Query Methods

        /// <summary>
        /// Get valid moves for a player. For input highlighting.
        /// </summary>
        public System.Collections.Generic.List<BoardPosition> GetValidMoves(int playerIndex)
        {
            if (State == null || _rules == null)
                return new System.Collections.Generic.List<BoardPosition>();

            return _rules.GetValidMoves(State, playerIndex);
        }

        /// <summary>
        /// Check if a wall placement is valid. For preview feedback.
        /// </summary>
        public bool CanPlaceWall(int playerIndex, BoardPosition position, WallOrientation orientation)
        {
            if (State == null || _rules == null)
                return false;

            var wall = new WallPlacement(position, orientation, playerIndex, State.CurrentTurn);
            return _rules.CanPlaceWall(State, playerIndex, wall);
        }

        /// <summary>
        /// Get current player's pawn position.
        /// </summary>
        public BoardPosition GetCurrentPawnPosition()
        {
            return State?.CurrentPlayer?.CurrentPosition ?? default;
        }

        /// <summary>
        /// Check if it's a specific player's turn.
        /// </summary>
        public bool IsPlayerTurn(int playerIndex)
        {
            return State?.IsPlaying == true && State.CurrentPlayerIndex == playerIndex;
        }

        /// <summary>
        /// Check if current player is AI.
        /// </summary>
        public bool IsCurrentPlayerAI()
        {
            return State?.CurrentPlayer?.IsAI ?? false;
        }

        /// <summary>
        /// Get path length for a player (for AI evaluation).
        /// </summary>
        public int GetPathLength(int playerIndex)
        {
            return _rules?.GetPathLength(State, playerIndex) ?? -1;
        }

        #endregion

        #region Debug

        [ContextMenu("Debug: Print Game State")]
        private void DebugPrintState()
        {
            if (State == null)
            {
                Debug.Log("No game state");
                return;
            }

            Debug.Log($"Board: {State.BoardWidth}x{State.BoardHeight}, Phase: {State.Phase}, Turn: {State.CurrentTurn}, Player: {State.CurrentPlayerIndex}");
            foreach (var p in State.Players)
            {
                Debug.Log($"  P{p.PlayerIndex} '{p.DisplayName}': {p.CurrentPosition}, Walls: {p.WallsRemaining}, Type: {p.Type}");
            }
            Debug.Log($"Walls placed: {State.Walls.WallCount}");
        }

        #endregion
    }
}
