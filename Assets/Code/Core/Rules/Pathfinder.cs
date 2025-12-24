using System.Collections.Generic;
using WallChess.Core.Data;

namespace WallChess.Core.Rules
{
    /// <summary>
    /// BFS pathfinder for Quoridor board.
    /// Pure C# - no Unity dependencies.
    /// </summary>
    public static class Pathfinder
    {
        private static readonly BoardPosition[] Directions = new BoardPosition[]
        {
            new(0, 1),  // North
            new(0, -1), // South
            new(1, 0),  // East
            new(-1, 0)  // West
        };

        /// <summary>
        /// Checks if a path exists from a position to the goal.
        /// </summary>
        public static bool PathExists(GameState state, BoardPosition start, GoalDirection goal)
        {
            var visited = new HashSet<BoardPosition>();
            var queue = new Queue<BoardPosition>();

            queue.Enqueue(start);
            visited.Add(start);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                if (IsAtGoal(current, goal, state.BoardWidth, state.BoardHeight))
                    return true;

                foreach (var dir in Directions)
                {
                    var next = current.Offset(dir.X, dir.Y);

                    if (!next.IsWithinBounds(state.BoardWidth, state.BoardHeight))
                        continue;

                    if (visited.Contains(next))
                        continue;

                    if (state.Walls.IsMovementBlocked(current, next))
                        continue;

                    // Don't consider pawn occupancy for path validation
                    visited.Add(next);
                    queue.Enqueue(next);
                }
            }

            return false;
        }

        /// <summary>
        /// Finds the shortest path length to the goal. Returns -1 if no path exists.
        /// </summary>
        public static int GetPathLength(GameState state, BoardPosition start, GoalDirection goal)
        {
            var visited = new HashSet<BoardPosition>();
            var queue = new Queue<(BoardPosition pos, int dist)>();

            queue.Enqueue((start, 0));
            visited.Add(start);

            while (queue.Count > 0)
            {
                var (current, dist) = queue.Dequeue();

                if (IsAtGoal(current, goal, state.BoardWidth, state.BoardHeight))
                    return dist;

                foreach (var dir in Directions)
                {
                    var next = current.Offset(dir.X, dir.Y);

                    if (!next.IsWithinBounds(state.BoardWidth, state.BoardHeight))
                        continue;

                    if (visited.Contains(next))
                        continue;

                    if (state.Walls.IsMovementBlocked(current, next))
                        continue;

                    visited.Add(next);
                    queue.Enqueue((next, dist + 1));
                }
            }

            return -1; // No path
        }

        /// <summary>
        /// Finds the shortest path to the goal. Returns empty list if no path.
        /// </summary>
        public static List<BoardPosition> FindPath(GameState state, BoardPosition start, GoalDirection goal)
        {
            var visited = new Dictionary<BoardPosition, BoardPosition?>(); // position -> parent
            var queue = new Queue<BoardPosition>();

            queue.Enqueue(start);
            visited[start] = null;

            BoardPosition? goalPos = null;

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                if (IsAtGoal(current, goal, state.BoardWidth, state.BoardHeight))
                {
                    goalPos = current;
                    break;
                }

                foreach (var dir in Directions)
                {
                    var next = current.Offset(dir.X, dir.Y);

                    if (!next.IsWithinBounds(state.BoardWidth, state.BoardHeight))
                        continue;

                    if (visited.ContainsKey(next))
                        continue;

                    if (state.Walls.IsMovementBlocked(current, next))
                        continue;

                    visited[next] = current;
                    queue.Enqueue(next);
                }
            }

            if (!goalPos.HasValue)
                return new List<BoardPosition>();

            // Reconstruct path
            var path = new List<BoardPosition>();
            var pos = goalPos;
            while (pos.HasValue)
            {
                path.Add(pos.Value);
                pos = visited[pos.Value];
            }

            path.Reverse();
            return path;
        }

        /// <summary>
        /// Checks if all players have valid paths to their goals.
        /// Used for wall placement validation.
        /// </summary>
        public static bool AllPlayersHavePaths(GameState state)
        {
            foreach (var player in state.Players)
            {
                if (!PathExists(state, player.CurrentPosition, player.Goal))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Checks if placing a wall would block all paths for any player.
        /// </summary>
        public static bool WouldBlockPaths(GameState state, WallPlacement wall)
        {
            // Temporarily place wall
            if (!state.Walls.TryPlaceWall(wall))
                return true; // Can't even place it

            bool blocked = !AllPlayersHavePaths(state);

            // Remove the wall
            state.Walls.TryRemoveLastWall();

            return blocked;
        }

        private static bool IsAtGoal(BoardPosition pos, GoalDirection goal, int boardWidth, int boardHeight)
        {
            return goal switch
            {
                GoalDirection.North => pos.Y == boardHeight - 1,
                GoalDirection.South => pos.Y == 0,
                GoalDirection.East => pos.X == boardWidth - 1,
                GoalDirection.West => pos.X == 0,
                _ => false
            };
        }
    }
}
