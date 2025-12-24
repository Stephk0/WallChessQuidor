using System.Collections.Generic;
using WallChess.Core.Data;
using WallChess.Core.Config;
using UnityEngine;

namespace WallChess.AI.Strategies
{
    /// <summary>
    /// Balanced AI: Switches between offense and defense based on game state.
    /// </summary>
    public class BalancedStrategy : IAIStrategy
    {
        public string StrategyName => "Balanced";

        public AIDecision Decide(IAIGameView game, AIEvaluationParams p)
        {
            int myPath = game.GetMyPathLength();
            int theirPath = game.GetOpponentPathLength();

            // Determine if we should try to place a wall
            bool shouldTryWall = ShouldTryWall(game, p, myPath, theirPath);

            if (shouldTryWall && game.GetMyWallsRemaining() > p.WallReserve)
            {
                var wallDecision = FindBestWall(game, p, myPath, theirPath);
                if (wallDecision.HasValue)
                    return wallDecision.Value;
            }

            // Default to best move
            return FindBestMove(game, p, myPath);
        }

        private bool ShouldTryWall(IAIGameView game, AIEvaluationParams p, int myPath, int theirPath)
        {
            // Don't place walls if we're winning by a lot
            if (myPath <= theirPath - 3) return false;

            // More likely to place wall if opponent is close to winning
            if (theirPath <= 3) return true;

            // Consider wall if we're behind or close
            if (myPath >= theirPath - 1)
            {
                // Random factor based on aggression
                return Random.value < p.AggressionWeight * 0.4f;
            }

            return false;
        }

        private AIDecision? FindBestWall(IAIGameView game, AIEvaluationParams p, int myPath, int theirPath)
        {
            var validWalls = game.GetValidWallPlacements();
            if (validWalls.Count == 0) return null;

            WallPlacement? bestWall = null;
            float bestScore = p.WallThreshold; // Minimum improvement needed

            foreach (var wall in validWalls)
            {
                // Check if wall would block paths
                if (game.WouldWallBlockPath(wall)) continue;

                // Calculate impact
                int newTheirPath = game.GetPathLengthWithWall(wall, GetOpponentIndex(game));
                int newMyPath = game.GetPathLengthWithWall(wall, game.MyPlayerIndex);

                if (newTheirPath < 0 || newMyPath < 0) continue; // Invalid

                // Score = opponent path increase - my path increase
                int theirIncrease = newTheirPath - theirPath;
                int myIncrease = newMyPath - myPath;
                float score = theirIncrease * p.AggressionWeight - myIncrease;

                // Add jitter
                score += (Random.value - 0.5f) * 2f * p.WallJitter * 10f;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestWall = wall;
                }
            }

            if (bestWall.HasValue)
            {
                return AIDecision.CreateWallPlacement(
                    bestWall.Value,
                    Mathf.Clamp01(bestScore / 10f),
                    $"Block opponent (score: {bestScore:F1})"
                );
            }

            return null;
        }

        private AIDecision FindBestMove(IAIGameView game, AIEvaluationParams p, int currentPath)
        {
            var moves = game.GetMyValidMoves();
            if (moves.Count == 0)
            {
                // No moves available - shouldn't happen in valid Quoridor
                return AIDecision.CreateMove(game.GetMyPosition(), 0f, "No moves available");
            }

            BoardPosition bestMove = moves[0];
            float bestScore = float.MinValue;

            var myGoal = game.GetMyGoal();
            int centerX = game.BoardWidth / 2;
            int centerY = game.BoardHeight / 2;
            int maxDist = (game.BoardWidth + game.BoardHeight) / 2;

            foreach (var move in moves)
            {
                // Calculate path length from this position
                int newPath = game.GetPathLengthFrom(move, myGoal);
                if (newPath < 0) continue;

                // Score based on path improvement
                float score = (currentPath - newPath) * 10f * p.PathWeight;

                // Center control bonus
                int distFromCenter = Mathf.Abs(move.X - centerX) + Mathf.Abs(move.Y - centerY);
                score += (maxDist - distFromCenter) * p.CenterWeight;

                // Distance from opponent (avoid getting blocked)
                var oppPos = game.GetOpponentPosition();
                int distFromOpp = Mathf.Abs(move.X - oppPos.X) + Mathf.Abs(move.Y - oppPos.Y);
                if (distFromOpp <= 1)
                    score -= 2f; // Penalty for being adjacent

                // Add jitter
                score += (Random.value - 0.5f) * 2f * p.MoveJitter * 10f;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                }
            }

            return AIDecision.CreateMove(
                bestMove,
                Mathf.Clamp01(bestScore / 20f + 0.5f),
                $"Best move (score: {bestScore:F1})"
            );
        }

        private int GetOpponentIndex(IAIGameView game)
        {
            return (game.MyPlayerIndex + 1) % game.PlayerCount;
        }
    }
}
