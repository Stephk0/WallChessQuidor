using System.Collections.Generic;
using WallChess.Core.Data;

namespace WallChess.Core.Rules
{
    /// <summary>
    /// Result of attempting a pawn move.
    /// </summary>
    public readonly struct MoveResult
    {
        public readonly bool IsSuccess;
        public readonly bool IsVictory;
        public readonly int WinnerIndex;
        public readonly BoardPosition From;
        public readonly BoardPosition To;
        public readonly bool WasJump;
        public readonly string FailureReason;

        private MoveResult(bool success, bool victory, int winner, BoardPosition from, BoardPosition to, bool jump, string reason)
        {
            IsSuccess = success;
            IsVictory = victory;
            WinnerIndex = winner;
            From = from;
            To = to;
            WasJump = jump;
            FailureReason = reason;
        }

        public static MoveResult Success(BoardPosition from, BoardPosition to, bool wasJump) =>
            new(true, false, -1, from, to, wasJump, null);

        public static MoveResult Victory(BoardPosition from, BoardPosition to, int winner) =>
            new(true, true, winner, from, to, false, null);

        public static MoveResult Failure(string reason) =>
            new(false, false, -1, default, default, false, reason);
    }

    /// <summary>
    /// Result of attempting a wall placement.
    /// </summary>
    public readonly struct WallPlaceResult
    {
        public readonly bool IsSuccess;
        public readonly WallPlacement Wall;
        public readonly string FailureReason;

        private WallPlaceResult(bool success, WallPlacement wall, string reason)
        {
            IsSuccess = success;
            Wall = wall;
            FailureReason = reason;
        }

        public static WallPlaceResult Success(WallPlacement wall) =>
            new(true, wall, null);

        public static WallPlaceResult Failure(string reason) =>
            new(false, default, reason);
    }

    /// <summary>
    /// Interface for game rules engine.
    /// All methods are pure - they read state but never modify it.
    /// </summary>
    public interface IRulesEngine
    {
        // Movement validation
        bool IsValidMove(GameState state, int playerIndex, BoardPosition to);
        List<BoardPosition> GetValidMoves(GameState state, int playerIndex);

        // Wall placement validation
        bool CanPlaceWall(GameState state, int playerIndex, WallPlacement wall);
        List<WallPlacement> GetValidWallPlacements(GameState state, int playerIndex);

        // Victory checking
        int CheckWinner(GameState state);
        bool HasPlayerWon(GameState state, int playerIndex);

        // Pathfinding for validation and AI
        bool PathExists(GameState state, int playerIndex);
        int GetPathLength(GameState state, int playerIndex);
        List<BoardPosition> FindPath(GameState state, int playerIndex);
    }
}
