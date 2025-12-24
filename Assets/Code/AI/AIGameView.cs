using System.Collections.Generic;
using WallChess.Core;
using WallChess.Core.Data;
using WallChess.Core.Rules;

namespace WallChess.AI
{
    /// <summary>
    /// Adapter that provides read-only game view for AI.
    /// </summary>
    public class AIGameView : IAIGameView
    {
        private readonly GameState _state;
        private readonly QuoridorRules _rules;
        private readonly int _playerIndex;

        public AIGameView(GameState state, QuoridorRules rules, int playerIndex)
        {
            _state = state;
            _rules = rules;
            _playerIndex = playerIndex;
        }

        public int BoardWidth => _state.BoardWidth;
        public int BoardHeight => _state.BoardHeight;
        public int BoardSize => _state.BoardWidth; // Backwards compat
        public int MyPlayerIndex => _playerIndex;
        public int PlayerCount => _state.PlayerCount;

        public BoardPosition GetMyPosition()
        {
            return _state.Players[_playerIndex].CurrentPosition;
        }

        public BoardPosition GetOpponentPosition(int opponentOffset = 0)
        {
            int oppIndex = GetOpponentIndex(opponentOffset);
            return _state.Players[oppIndex].CurrentPosition;
        }

        public GoalDirection GetMyGoal()
        {
            return _state.Players[_playerIndex].Goal;
        }

        public GoalDirection GetOpponentGoal(int opponentOffset = 0)
        {
            int oppIndex = GetOpponentIndex(opponentOffset);
            return _state.Players[oppIndex].Goal;
        }

        public int GetMyWallsRemaining()
        {
            return _state.Players[_playerIndex].WallsRemaining;
        }

        public int GetOpponentWallsRemaining(int opponentOffset = 0)
        {
            int oppIndex = GetOpponentIndex(opponentOffset);
            return _state.Players[oppIndex].WallsRemaining;
        }

        public List<BoardPosition> GetMyValidMoves()
        {
            return _rules.GetValidMoves(_state, _playerIndex);
        }

        public List<WallPlacement> GetValidWallPlacements()
        {
            return _rules.GetValidWallPlacements(_state, _playerIndex);
        }

        public int GetMyPathLength()
        {
            return _rules.GetPathLength(_state, _playerIndex);
        }

        public int GetOpponentPathLength(int opponentOffset = 0)
        {
            int oppIndex = GetOpponentIndex(opponentOffset);
            return _rules.GetPathLength(_state, oppIndex);
        }

        public int GetPathLengthFrom(BoardPosition position, GoalDirection goal)
        {
            return Pathfinder.GetPathLength(_state, position, goal);
        }

        public bool WouldWallBlockPath(WallPlacement wall)
        {
            return Pathfinder.WouldBlockPaths(_state, wall);
        }

        public int GetPathLengthWithWall(WallPlacement wall, int playerIndex)
        {
            // Clone state, place wall, measure path
            var testState = _state.Clone();
            if (!testState.Walls.TryPlaceWall(wall))
                return -1;

            var player = testState.Players[playerIndex];
            return Pathfinder.GetPathLength(testState, player.CurrentPosition, player.Goal);
        }

        public GameState GetStateCopy()
        {
            return _state.Clone();
        }

        private int GetOpponentIndex(int offset)
        {
            // For 2-player: opponent is always the other player
            // For 4-player: offset allows selecting different opponents
            int oppIndex = (_playerIndex + 1 + offset) % _state.PlayerCount;
            if (oppIndex == _playerIndex)
                oppIndex = (_playerIndex + 1) % _state.PlayerCount;
            return oppIndex;
        }
    }
}
