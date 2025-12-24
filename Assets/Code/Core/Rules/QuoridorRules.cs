using System.Collections.Generic;
using WallChess.Core.Data;
using WallChess.Core.Config;

namespace WallChess.Core.Rules
{
    /// <summary>
    /// Quoridor-specific rules implementation.
    /// Pure C# - no Unity dependencies except config references.
    /// </summary>
    public class QuoridorRules : IRulesEngine
    {
        private readonly bool _allowJumping;
        private readonly bool _allowDiagonalJumps;
        private readonly bool _enforcePathToGoal;

        public QuoridorRules(GameRulesConfig config = null)
        {
            _allowJumping = config?.allowJumping ?? true;
            _allowDiagonalJumps = config?.allowDiagonalJumps ?? true;
            _enforcePathToGoal = config?.enforcePathToGoal ?? true;
        }

        // ========================================
        // MOVEMENT VALIDATION
        // ========================================

        public bool IsValidMove(GameState state, int playerIndex, BoardPosition to)
        {
            var validMoves = GetValidMoves(state, playerIndex);
            return validMoves.Contains(to);
        }

        public List<BoardPosition> GetValidMoves(GameState state, int playerIndex)
        {
            var player = state.GetPlayer(playerIndex);
            if (player == null) return new List<BoardPosition>();

            var moves = new List<BoardPosition>(8);
            var from = player.CurrentPosition;

            // Check all 4 cardinal directions
            CheckDirection(state, from, 0, 1, moves, playerIndex);  // North
            CheckDirection(state, from, 0, -1, moves, playerIndex); // South
            CheckDirection(state, from, 1, 0, moves, playerIndex);  // East
            CheckDirection(state, from, -1, 0, moves, playerIndex); // West

            return moves;
        }

        private void CheckDirection(GameState state, BoardPosition from, int dx, int dy, List<BoardPosition> moves, int playerIndex)
        {
            var to = from.Offset(dx, dy);

            // Check bounds
            if (!state.IsValidPosition(to))
                return;

            // Check wall blocking
            if (state.Walls.IsMovementBlocked(from, to))
                return;

            // Check occupancy
            var occupant = state.GetPlayerAtPosition(to);

            if (occupant == null)
            {
                // Empty tile - valid move
                moves.Add(to);
                return;
            }

            // Tile occupied by another player
            if (!_allowJumping || occupant.PlayerIndex == playerIndex)
                return;

            // Try to jump over opponent
            var jumpTarget = to.Offset(dx, dy);

            // Can we jump straight?
            if (state.IsValidPosition(jumpTarget) &&
                !state.Walls.IsMovementBlocked(to, jumpTarget) &&
                !state.IsPositionOccupied(jumpTarget))
            {
                moves.Add(jumpTarget);
                return;
            }

            // Straight jump blocked - try diagonal jumps
            if (!_allowDiagonalJumps)
                return;

            // Diagonal directions perpendicular to original movement
            int perpDx1, perpDy1, perpDx2, perpDy2;
            if (dx != 0) // Moving horizontally, try vertical diagonals
            {
                perpDx1 = 0; perpDy1 = 1;
                perpDx2 = 0; perpDy2 = -1;
            }
            else // Moving vertically, try horizontal diagonals
            {
                perpDx1 = 1; perpDy1 = 0;
                perpDx2 = -1; perpDy2 = 0;
            }

            TryDiagonalJump(state, to, perpDx1, perpDy1, moves);
            TryDiagonalJump(state, to, perpDx2, perpDy2, moves);
        }

        private void TryDiagonalJump(GameState state, BoardPosition from, int dx, int dy, List<BoardPosition> moves)
        {
            var diagonal = from.Offset(dx, dy);

            if (state.IsValidPosition(diagonal) &&
                !state.Walls.IsMovementBlocked(from, diagonal) &&
                !state.IsPositionOccupied(diagonal))
            {
                moves.Add(diagonal);
            }
        }

        // ========================================
        // WALL PLACEMENT VALIDATION
        // ========================================

        public bool CanPlaceWall(GameState state, int playerIndex, WallPlacement wall)
        {
            var player = state.GetPlayer(playerIndex);
            if (player == null) return false;

            // Check player has walls remaining
            if (!player.HasWallsRemaining)
                return false;

            // Check wall position is valid
            if (!IsValidWallPosition(wall, state.BoardWidth, state.BoardHeight))
                return false;

            // Check wall doesn't overlap existing walls
            if (WouldOverlap(state, wall))
                return false;

            // Check wall doesn't block all paths
            if (_enforcePathToGoal && Pathfinder.WouldBlockPaths(state, wall))
                return false;

            return true;
        }

        public List<WallPlacement> GetValidWallPlacements(GameState state, int playerIndex)
        {
            var valid = new List<WallPlacement>();
            var player = state.GetPlayer(playerIndex);

            if (player == null || !player.HasWallsRemaining)
                return valid;

            int maxX = state.BoardWidth - 2;  // Walls span 2 tiles horizontally
            int maxY = state.BoardHeight - 2; // Walls span 2 tiles vertically

            for (int x = 0; x <= maxX; x++)
            {
                for (int y = 0; y <= maxY; y++)
                {
                    var pos = new BoardPosition(x, y);

                    // Try horizontal wall
                    var hWall = new WallPlacement(pos, WallOrientation.Horizontal, playerIndex, state.CurrentTurn);
                    if (CanPlaceWall(state, playerIndex, hWall))
                        valid.Add(hWall);

                    // Try vertical wall
                    var vWall = new WallPlacement(pos, WallOrientation.Vertical, playerIndex, state.CurrentTurn);
                    if (CanPlaceWall(state, playerIndex, vWall))
                        valid.Add(vWall);
                }
            }

            return valid;
        }

        private bool IsValidWallPosition(WallPlacement wall, int boardWidth, int boardHeight)
        {
            int maxX = boardWidth - 2;  // Walls span 2 tiles horizontally
            int maxY = boardHeight - 2; // Walls span 2 tiles vertically
            return wall.Position.X >= 0 && wall.Position.X <= maxX &&
                   wall.Position.Y >= 0 && wall.Position.Y <= maxY;
        }

        private bool WouldOverlap(GameState state, WallPlacement wall)
        {
            var (gap1, gap2) = wall.GetOccupiedGaps();
            var intersection = wall.GetCenterIntersection();

            // Check if gaps already occupied by same orientation wall
            if (state.Walls.IsGapOccupied(gap1, wall.Orientation) ||
                state.Walls.IsGapOccupied(gap2, wall.Orientation))
                return true;

            // Check intersection (walls of any orientation can't share center)
            if (state.Walls.IsIntersectionOccupied(intersection))
                return true;

            return false;
        }

        // ========================================
        // VICTORY DETECTION
        // ========================================

        public int CheckWinner(GameState state)
        {
            for (int i = 0; i < state.PlayerCount; i++)
            {
                if (HasPlayerWon(state, i))
                    return i;
            }
            return -1;
        }

        public bool HasPlayerWon(GameState state, int playerIndex)
        {
            var player = state.GetPlayer(playerIndex);
            return player?.HasReachedGoal(state.BoardWidth, state.BoardHeight) ?? false;
        }

        // ========================================
        // PATHFINDING WRAPPERS
        // ========================================

        public bool PathExists(GameState state, int playerIndex)
        {
            var player = state.GetPlayer(playerIndex);
            if (player == null) return false;
            return Pathfinder.PathExists(state, player.CurrentPosition, player.Goal);
        }

        public int GetPathLength(GameState state, int playerIndex)
        {
            var player = state.GetPlayer(playerIndex);
            if (player == null) return -1;
            return Pathfinder.GetPathLength(state, player.CurrentPosition, player.Goal);
        }

        public List<BoardPosition> FindPath(GameState state, int playerIndex)
        {
            var player = state.GetPlayer(playerIndex);
            if (player == null) return new List<BoardPosition>();
            return Pathfinder.FindPath(state, player.CurrentPosition, player.Goal);
        }
    }
}
