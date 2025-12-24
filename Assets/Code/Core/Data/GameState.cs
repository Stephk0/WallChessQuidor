using System;

namespace WallChess.Core.Data
{
    /// <summary>
    /// Current phase of the game.
    /// </summary>
    public enum GamePhase : byte
    {
        NotStarted = 0,
        Playing = 1,
        Paused = 2,
        GameOver = 3
    }

    /// <summary>
    /// Complete, serializable game state.
    /// This is the SINGLE SOURCE OF TRUTH for all game data.
    /// </summary>
    [Serializable]
    public class GameState
    {
        // Configuration (immutable after game start)
        public readonly int BoardWidth;
        public readonly int BoardHeight;
        public readonly int PlayerCount;

        /// <summary>
        /// Backwards compatibility. Returns BoardWidth. Use BoardWidth/BoardHeight for non-square boards.
        /// </summary>
        public int BoardSize => BoardWidth;

        // Dynamic state
        public GamePhase Phase { get; private set; }
        public int CurrentTurn { get; private set; }
        public int CurrentPlayerIndex { get; private set; }
        public int? WinnerIndex { get; private set; }

        // Core state objects
        public readonly PlayerState[] Players;
        public readonly WallState Walls;
        public readonly MoveHistory History;

        // Timing
        public DateTime GameStartTime { get; private set; }
        public DateTime? GameEndTime { get; private set; }
        public float TurnStartTime { get; private set; }

        public GameState(int boardWidth, int boardHeight, PlayerState[] players)
        {
            if (boardWidth < 5 || boardWidth > 15)
                throw new ArgumentException("Board width must be between 5 and 15", nameof(boardWidth));
            if (boardHeight < 5 || boardHeight > 15)
                throw new ArgumentException("Board height must be between 5 and 15", nameof(boardHeight));
            if (players == null || players.Length < 2 || players.Length > 4)
                throw new ArgumentException("Player count must be 2-4", nameof(players));

            BoardWidth = boardWidth;
            BoardHeight = boardHeight;
            PlayerCount = players.Length;
            Players = players;
            Walls = new WallState();
            History = new MoveHistory();

            Phase = GamePhase.NotStarted;
            CurrentTurn = 0;
            CurrentPlayerIndex = 0;
            WinnerIndex = null;
        }

        /// <summary>
        /// Backwards compatibility constructor for square boards.
        /// </summary>
        public GameState(int boardSize, PlayerState[] players)
            : this(boardSize, boardSize, players)
        {
        }

        // Accessors
        public PlayerState CurrentPlayer => Players[CurrentPlayerIndex];
        public bool IsGameOver => Phase == GamePhase.GameOver;
        public bool IsPlaying => Phase == GamePhase.Playing;

        public PlayerState GetPlayer(int index) =>
            index >= 0 && index < Players.Length ? Players[index] : null;

        /// <summary>
        /// Checks if a board position is valid.
        /// </summary>
        public bool IsValidPosition(BoardPosition pos) =>
            pos.X >= 0 && pos.X < BoardWidth && pos.Y >= 0 && pos.Y < BoardHeight;

        /// <summary>
        /// Gets the player at a specific position, or null if empty.
        /// </summary>
        public PlayerState GetPlayerAtPosition(BoardPosition pos)
        {
            foreach (var player in Players)
                if (player.CurrentPosition == pos)
                    return player;
            return null;
        }

        /// <summary>
        /// Checks if a position is occupied by any player.
        /// </summary>
        public bool IsPositionOccupied(BoardPosition pos) =>
            GetPlayerAtPosition(pos) != null;

        // State transitions
        public void StartGame()
        {
            if (Phase != GamePhase.NotStarted)
                throw new InvalidOperationException("Game already started");

            Phase = GamePhase.Playing;
            CurrentTurn = 1;
            CurrentPlayerIndex = 0;
            GameStartTime = DateTime.UtcNow;
            TurnStartTime = UnityEngine.Time.time;
        }

        public void NextTurn()
        {
            int previousPlayer = CurrentPlayerIndex;
            CurrentPlayerIndex = (CurrentPlayerIndex + 1) % PlayerCount;

            // Increment turn counter when we wrap back to player 0
            if (CurrentPlayerIndex == 0)
                CurrentTurn++;

            TurnStartTime = UnityEngine.Time.time;
        }

        public void SetWinner(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= PlayerCount)
                throw new ArgumentException("Invalid player index", nameof(playerIndex));

            WinnerIndex = playerIndex;
            Phase = GamePhase.GameOver;
            GameEndTime = DateTime.UtcNow;
        }

        public void Pause()
        {
            if (Phase == GamePhase.Playing)
                Phase = GamePhase.Paused;
        }

        public void Resume()
        {
            if (Phase == GamePhase.Paused)
                Phase = GamePhase.Playing;
        }

        /// <summary>
        /// Creates a complete deep copy for AI simulation.
        /// </summary>
        public GameState Clone()
        {
            var clonedPlayers = new PlayerState[Players.Length];
            for (int i = 0; i < Players.Length; i++)
                clonedPlayers[i] = Players[i].Clone();

            var clone = new GameState(BoardWidth, BoardHeight, clonedPlayers);

            // Copy walls
            foreach (var wall in Walls.PlacedWalls)
                clone.Walls.TryPlaceWall(wall);

            // Copy state
            clone.Phase = Phase;
            clone.CurrentTurn = CurrentTurn;
            clone.CurrentPlayerIndex = CurrentPlayerIndex;
            clone.WinnerIndex = WinnerIndex;
            clone.GameStartTime = GameStartTime;
            clone.GameEndTime = GameEndTime;

            return clone;
        }
    }
}
