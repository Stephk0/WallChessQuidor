using System;
using System.Collections.Generic;

namespace WallChess.Core.Data
{
    /// <summary>
    /// Type of action taken in a turn.
    /// </summary>
    public enum MoveType : byte
    {
        PawnMove = 0,
        PawnJump = 1,      // Jump over opponent
        WallPlacement = 2
    }

    /// <summary>
    /// Immutable record of a single move/action.
    /// </summary>
    [Serializable]
    public readonly struct MoveRecord
    {
        public readonly int TurnNumber;
        public readonly int PlayerIndex;
        public readonly MoveType Type;
        public readonly BoardPosition FromPosition;
        public readonly BoardPosition ToPosition;
        public readonly WallPlacement Wall;
        public readonly bool HasWall;
        public readonly long TimestampTicks;

        // Pawn move constructor
        public MoveRecord(int turn, int player, BoardPosition from, BoardPosition to, bool isJump)
        {
            TurnNumber = turn;
            PlayerIndex = player;
            Type = isJump ? MoveType.PawnJump : MoveType.PawnMove;
            FromPosition = from;
            ToPosition = to;
            Wall = default;
            HasWall = false;
            TimestampTicks = DateTime.UtcNow.Ticks;
        }

        // Wall placement constructor
        public MoveRecord(int turn, int player, WallPlacement wall)
        {
            TurnNumber = turn;
            PlayerIndex = player;
            Type = MoveType.WallPlacement;
            FromPosition = default;
            ToPosition = default;
            Wall = wall;
            HasWall = true;
            TimestampTicks = DateTime.UtcNow.Ticks;
        }

        public override string ToString()
        {
            if (Type == MoveType.WallPlacement)
                return $"T{TurnNumber}: P{PlayerIndex} placed {Wall}";
            return $"T{TurnNumber}: P{PlayerIndex} {FromPosition}->{ToPosition}" +
                   (Type == MoveType.PawnJump ? " (jump)" : "");
        }
    }

    /// <summary>
    /// Complete move history for a game. Supports undo/replay.
    /// </summary>
    [Serializable]
    public class MoveHistory
    {
        private readonly List<MoveRecord> _moves = new(100);

        public IReadOnlyList<MoveRecord> Moves => _moves;
        public int MoveCount => _moves.Count;
        public MoveRecord? LastMove => _moves.Count > 0 ? _moves[^1] : null;

        public void RecordMove(MoveRecord move)
        {
            _moves.Add(move);
        }

        public MoveRecord? PopLastMove()
        {
            if (_moves.Count == 0) return null;
            var move = _moves[^1];
            _moves.RemoveAt(_moves.Count - 1);
            return move;
        }

        public IEnumerable<MoveRecord> GetMovesForPlayer(int playerIndex)
        {
            foreach (var move in _moves)
                if (move.PlayerIndex == playerIndex)
                    yield return move;
        }

        public void Clear() => _moves.Clear();

        public MoveHistory Clone()
        {
            var clone = new MoveHistory();
            foreach (var move in _moves)
                clone.RecordMove(move);
            return clone;
        }
    }
}
