using WallChess.Core.Data;
using WallChess.Core.Config;

namespace WallChess.AI
{
    /// <summary>
    /// Type of AI action.
    /// </summary>
    public enum AIActionType
    {
        Move,
        PlaceWall
    }

    /// <summary>
    /// Result of AI decision-making.
    /// </summary>
    public struct AIDecision
    {
        public AIActionType ActionType;
        public BoardPosition MoveTarget;       // For Move action
        public WallPlacement WallPlacement;    // For PlaceWall action
        public float Confidence;               // 0-1 score for debugging
        public string Reasoning;               // Debug info

        public static AIDecision CreateMove(BoardPosition target, float confidence = 1f, string reason = null)
        {
            return new AIDecision
            {
                ActionType = AIActionType.Move,
                MoveTarget = target,
                Confidence = confidence,
                Reasoning = reason ?? "Move selected"
            };
        }

        public static AIDecision CreateWallPlacement(WallPlacement wall, float confidence = 1f, string reason = null)
        {
            return new AIDecision
            {
                ActionType = AIActionType.PlaceWall,
                WallPlacement = wall,
                Confidence = confidence,
                Reasoning = reason ?? "Wall placement selected"
            };
        }

        public override string ToString()
        {
            return ActionType == AIActionType.Move
                ? $"Move to {MoveTarget} ({Confidence:P0}): {Reasoning}"
                : $"Place {WallPlacement} ({Confidence:P0}): {Reasoning}";
        }
    }

    /// <summary>
    /// Read-only view of game state for AI analysis.
    /// Prevents AI from mutating game state.
    /// </summary>
    public interface IAIGameView
    {
        int BoardWidth { get; }
        int BoardHeight { get; }
        int BoardSize { get; } // Backwards compat - returns BoardWidth
        int MyPlayerIndex { get; }
        int PlayerCount { get; }

        BoardPosition GetMyPosition();
        BoardPosition GetOpponentPosition(int opponentOffset = 0);
        GoalDirection GetMyGoal();
        GoalDirection GetOpponentGoal(int opponentOffset = 0);

        int GetMyWallsRemaining();
        int GetOpponentWallsRemaining(int opponentOffset = 0);

        System.Collections.Generic.List<BoardPosition> GetMyValidMoves();
        System.Collections.Generic.List<WallPlacement> GetValidWallPlacements();

        int GetMyPathLength();
        int GetOpponentPathLength(int opponentOffset = 0);
        int GetPathLengthFrom(BoardPosition position, GoalDirection goal);

        bool WouldWallBlockPath(WallPlacement wall);
        int GetPathLengthWithWall(WallPlacement wall, int playerIndex);

        GameState GetStateCopy(); // For deep simulation
    }

    /// <summary>
    /// Strategy interface for AI personalities.
    /// </summary>
    public interface IAIStrategy
    {
        string StrategyName { get; }

        /// <summary>
        /// Evaluate the current game state and decide on an action.
        /// </summary>
        AIDecision Decide(IAIGameView gameView, AIEvaluationParams parameters);
    }
}
