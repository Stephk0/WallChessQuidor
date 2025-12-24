using System;
using WallChess.Core.Data;

namespace WallChess.Core
{
    /// <summary>
    /// Central event definitions for game state changes.
    /// Views subscribe to these events to update their display.
    /// Events are the ONLY way presentation layer learns about state changes.
    /// </summary>
    public static class GameEvents
    {
        // ========================================
        // GAME LIFECYCLE
        // ========================================

        /// <summary>Fired when game initializes and is ready to play. Parameter is initial GameState.</summary>
        public static event Action<Data.GameState> OnGameInitialized;

        /// <summary>Fired when game starts (transitions from NotStarted to Playing).</summary>
        public static event Action OnGameStarted;

        /// <summary>Fired when game is paused.</summary>
        public static event Action OnGamePaused;

        /// <summary>Fired when game resumes from pause.</summary>
        public static event Action OnGameResumed;

        /// <summary>Fired when game ends. Parameter is winner player index.</summary>
        public static event Action<int> OnGameOver;

        // ========================================
        // TURN MANAGEMENT
        // ========================================

        /// <summary>Fired at start of a player's turn. Parameter is player index.</summary>
        public static event Action<int> OnTurnStarted;

        /// <summary>Fired at end of a player's turn. Parameter is player index.</summary>
        public static event Action<int> OnTurnEnded;

        /// <summary>Fired every tick during turn timer. Parameter is time remaining.</summary>
        public static event Action<float> OnTurnTimerTick;

        // ========================================
        // PLAYER ACTIONS
        // ========================================

        /// <summary>
        /// Fired when a player moves their pawn.
        /// Parameters: playerIndex, fromPosition, toPosition, wasJump
        /// </summary>
        public static event Action<int, BoardPosition, BoardPosition, bool> OnPawnMoved;

        /// <summary>
        /// Fired when a wall is placed.
        /// Parameters: playerIndex, wall
        /// </summary>
        public static event Action<int, WallPlacement> OnWallPlaced;

        // ========================================
        // STATE QUERIES (for UI updates)
        // ========================================

        /// <summary>Fired when a player's wall count changes. Parameters: playerIndex, newCount.</summary>
        public static event Action<int, int> OnWallCountChanged;

        /// <summary>Fired when valid moves are calculated. Parameter is list of valid positions.</summary>
        public static event Action<System.Collections.Generic.List<BoardPosition>> OnValidMovesCalculated;

        /// <summary>Fired when valid moves should be cleared.</summary>
        public static event Action OnValidMovesCleared;

        /// <summary>Alias for OnValidMovesCalculated - used by views.</summary>
        public static event Action<System.Collections.Generic.List<BoardPosition>> OnValidMovesUpdated;

        // ========================================
        // UI REQUESTS (from views to controller)
        // ========================================

        /// <summary>Fired when UI requests game restart.</summary>
        public static event Action OnRestartRequested;

        /// <summary>Fired when UI requests return to main menu.</summary>
        public static event Action OnMainMenuRequested;

        /// <summary>Fired when UI requests undo.</summary>
        public static event Action OnUndoRequested;

        // ========================================
        // INPUT FEEDBACK
        // ========================================

        /// <summary>Fired when a pawn is selected. Parameter is player index.</summary>
        public static event Action<int> OnPawnSelected;

        /// <summary>Fired when pawn selection is cancelled.</summary>
        public static event Action OnPawnDeselected;

        /// <summary>Fired when wall placement mode is entered.</summary>
        public static event Action OnWallPlacementStarted;

        /// <summary>Fired when wall placement mode is exited.</summary>
        public static event Action OnWallPlacementCancelled;

        /// <summary>Fired when wall preview position updates. Parameters: position, orientation, isValid.</summary>
        public static event Action<BoardPosition, WallOrientation, bool> OnWallPreviewUpdated;

        // ========================================
        // RAISE METHODS (called by GameController only)
        // ========================================

        internal static void RaiseGameInitialized(Data.GameState state) => OnGameInitialized?.Invoke(state);
        internal static void RaiseGameStarted() => OnGameStarted?.Invoke();
        internal static void RaiseGamePaused() => OnGamePaused?.Invoke();
        internal static void RaiseGameResumed() => OnGameResumed?.Invoke();
        internal static void RaiseGameOver(int winner) => OnGameOver?.Invoke(winner);

        internal static void RaiseTurnStarted(int player) => OnTurnStarted?.Invoke(player);
        internal static void RaiseTurnEnded(int player) => OnTurnEnded?.Invoke(player);
        internal static void RaiseTurnTimerTick(float timeRemaining) => OnTurnTimerTick?.Invoke(timeRemaining);

        internal static void RaisePawnMoved(int player, BoardPosition from, BoardPosition to, bool wasJump) =>
            OnPawnMoved?.Invoke(player, from, to, wasJump);
        internal static void RaiseWallPlaced(int player, WallPlacement wall) =>
            OnWallPlaced?.Invoke(player, wall);

        internal static void RaiseWallCountChanged(int player, int count) =>
            OnWallCountChanged?.Invoke(player, count);
        internal static void RaiseValidMovesCalculated(System.Collections.Generic.List<BoardPosition> moves)
        {
            OnValidMovesCalculated?.Invoke(moves);
            OnValidMovesUpdated?.Invoke(moves); // Also raise the alias
        }
        internal static void RaiseValidMovesCleared() => OnValidMovesCleared?.Invoke();

        // UI Request methods (called from Views)
        public static void RequestRestart() => OnRestartRequested?.Invoke();
        public static void RequestMainMenu() => OnMainMenuRequested?.Invoke();
        public static void RequestUndo() => OnUndoRequested?.Invoke();

        internal static void RaisePawnSelected(int player) => OnPawnSelected?.Invoke(player);
        internal static void RaisePawnDeselected() => OnPawnDeselected?.Invoke();
        internal static void RaiseWallPlacementStarted() => OnWallPlacementStarted?.Invoke();
        internal static void RaiseWallPlacementCancelled() => OnWallPlacementCancelled?.Invoke();
        internal static void RaiseWallPreviewUpdated(BoardPosition pos, WallOrientation orient, bool valid) =>
            OnWallPreviewUpdated?.Invoke(pos, orient, valid);

        /// <summary>
        /// Clears all event handlers. Call on scene unload to prevent memory leaks.
        /// </summary>
        public static void ClearAll()
        {
            OnGameInitialized = null;
            OnGameStarted = null;
            OnGamePaused = null;
            OnGameResumed = null;
            OnGameOver = null;

            OnTurnStarted = null;
            OnTurnEnded = null;
            OnTurnTimerTick = null;

            OnPawnMoved = null;
            OnWallPlaced = null;

            OnWallCountChanged = null;
            OnValidMovesCalculated = null;
            OnValidMovesCleared = null;
            OnValidMovesUpdated = null;

            OnRestartRequested = null;
            OnMainMenuRequested = null;
            OnUndoRequested = null;

            OnPawnSelected = null;
            OnPawnDeselected = null;
            OnWallPlacementStarted = null;
            OnWallPlacementCancelled = null;
            OnWallPreviewUpdated = null;
        }
    }
}
