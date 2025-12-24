using System;

namespace WallChess.Core.Data
{
    /// <summary>
    /// Type of player controller.
    /// </summary>
    public enum PlayerType : byte
    {
        Human = 0,
        AI = 1,
        Remote = 2 // Future: networked player
    }

    /// <summary>
    /// AI difficulty level.
    /// </summary>
    public enum AIDifficulty : byte
    {
        Easy = 0,
        Medium = 1,
        Hard = 2,
        Expert = 3
    }

    /// <summary>
    /// Goal direction for determining win condition.
    /// </summary>
    public enum GoalDirection : byte
    {
        North = 0, // Reach top row (Y = boardHeight - 1)
        South = 1, // Reach bottom row (Y = 0)
        East = 2,  // Reach right column (X = boardWidth - 1)
        West = 3   // Reach left column (X = 0)
    }

    /// <summary>
    /// Complete player state. Mutable during gameplay.
    /// </summary>
    [Serializable]
    public class PlayerState
    {
        // Immutable identity
        public readonly int PlayerIndex;
        public readonly string DisplayName;
        public readonly PlayerType Type;
        public readonly AIDifficulty Difficulty;
        public readonly string AIPersonality;

        // Board positions
        public readonly BoardPosition StartPosition;
        public readonly GoalDirection Goal;

        // Mutable game state
        public BoardPosition CurrentPosition { get; private set; }
        public int WallsRemaining { get; private set; }
        public int MoveCount { get; private set; }
        public int WallsPlaced { get; private set; }

        public PlayerState(
            int playerIndex,
            string displayName,
            PlayerType type,
            BoardPosition startPosition,
            GoalDirection goal,
            int initialWalls,
            AIDifficulty difficulty = AIDifficulty.Medium,
            string aiPersonality = "Balanced")
        {
            PlayerIndex = playerIndex;
            DisplayName = displayName;
            Type = type;
            StartPosition = startPosition;
            Goal = goal;
            Difficulty = difficulty;
            AIPersonality = aiPersonality;

            CurrentPosition = startPosition;
            WallsRemaining = initialWalls;
            MoveCount = 0;
            WallsPlaced = 0;
        }

        public bool IsHuman => Type == PlayerType.Human;
        public bool IsAI => Type == PlayerType.AI;
        public bool HasWallsRemaining => WallsRemaining > 0;

        /// <summary>
        /// Checks if player has reached their goal (non-square board version).
        /// </summary>
        public bool HasReachedGoal(int boardWidth, int boardHeight)
        {
            return Goal switch
            {
                GoalDirection.North => CurrentPosition.Y == boardHeight - 1,
                GoalDirection.South => CurrentPosition.Y == 0,
                GoalDirection.East => CurrentPosition.X == boardWidth - 1,
                GoalDirection.West => CurrentPosition.X == 0,
                _ => false
            };
        }

        /// <summary>
        /// Checks if player has reached their goal (square board version).
        /// </summary>
        public bool HasReachedGoal(int gridSize) => HasReachedGoal(gridSize, gridSize);

        /// <summary>
        /// Gets the target row/column value for pathfinding (non-square board version).
        /// </summary>
        public int GetGoalValue(int boardWidth, int boardHeight)
        {
            return Goal switch
            {
                GoalDirection.North => boardHeight - 1,
                GoalDirection.South => 0,
                GoalDirection.East => boardWidth - 1,
                GoalDirection.West => 0,
                _ => 0
            };
        }

        /// <summary>
        /// Gets the target row/column value for pathfinding (square board version).
        /// </summary>
        public int GetGoalValue(int gridSize) => GetGoalValue(gridSize, gridSize);

        public void MoveTo(BoardPosition newPosition)
        {
            CurrentPosition = newPosition;
            MoveCount++;
        }

        public bool TryUseWall()
        {
            if (WallsRemaining <= 0) return false;
            WallsRemaining--;
            WallsPlaced++;
            return true;
        }

        public void RefundWall()
        {
            WallsRemaining++;
            if (WallsPlaced > 0) WallsPlaced--;
        }

        public void Reset(int initialWalls)
        {
            CurrentPosition = StartPosition;
            MoveCount = 0;
            WallsRemaining = initialWalls;
            WallsPlaced = 0;
        }

        /// <summary>
        /// Creates a deep copy for AI simulation.
        /// </summary>
        public PlayerState Clone()
        {
            var clone = new PlayerState(
                PlayerIndex, DisplayName, Type, StartPosition, Goal,
                WallsRemaining, Difficulty, AIPersonality);
            clone.CurrentPosition = CurrentPosition;
            clone.MoveCount = MoveCount;
            clone.WallsPlaced = WallsPlaced;
            return clone;
        }
    }
}
