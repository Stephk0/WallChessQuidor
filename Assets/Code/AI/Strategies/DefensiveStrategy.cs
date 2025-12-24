using System.Collections.Generic;
using WallChess.Core.Data;
using WallChess.Core.Config;
using UnityEngine;

namespace WallChess.AI.Strategies
{
    /// <summary>
    /// Defensive AI: Prioritizes advancing while saving walls for emergencies.
    /// </summary>
    public class DefensiveStrategy : IAIStrategy
    {
        public string StrategyName => "Defensive";

        public AIDecision Decide(IAIGameView game, AIEvaluationParams p)
        {
            int myPath = game.GetMyPathLength();
            int theirPath = game.GetOpponentPathLength();

            // Defensive: only place walls if significantly behind
            bool shouldPlaceWall = myPath > theirPath + 2 &&
                                   game.GetMyWallsRemaining() > p.WallReserve + 1;

            if (shouldPlaceWall)
            {
                var wallDecision = FindDefensiveWall(game, p, myPath, theirPath);
                if (wallDecision.HasValue)
                    return wallDecision.Value;
            }

            // Focus on safe advancement
            return FindSafestMove(game, p, myPath);
        }

        private AIDecision? FindDefensiveWall(IAIGameView game, AIEvaluationParams p, int myPath, int theirPath)
        {
            var validWalls = game.GetValidWallPlacements();
            if (validWalls.Count == 0) return null;

            WallPlacement? bestWall = null;
            float bestScore = 0f;

            foreach (var wall in validWalls)
            {
                if (game.WouldWallBlockPath(wall)) continue;

                int newTheirPath = game.GetPathLengthWithWall(wall, GetOpponentIndex(game));
                int newMyPath = game.GetPathLengthWithWall(wall, game.MyPlayerIndex);

                if (newTheirPath < 0 || newMyPath < 0) continue;

                // Defensive: ensure we don't hurt ourselves
                int myIncrease = newMyPath - myPath;
                if (myIncrease > 1) continue; // Don't self-harm

                int theirIncrease = newTheirPath - theirPath;

                // Score based on net improvement
                float score = theirIncrease - myIncrease * 2f; // Penalize self-harm more

                // Add jitter
                score += (Random.value - 0.5f) * p.WallJitter * 5f;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestWall = wall;
                }
            }

            if (bestWall.HasValue && bestScore >= p.WallThreshold)
            {
                return AIDecision.CreateWallPlacement(
                    bestWall.Value,
                    Mathf.Clamp01(bestScore / 5f),
                    $"Defensive wall (score: {bestScore:F1})"
                );
            }

            return null;
        }

        private AIDecision FindSafestMove(IAIGameView game, AIEvaluationParams p, int currentPath)
        {
            var moves = game.GetMyValidMoves();
            if (moves.Count == 0)
            {
                return AIDecision.CreateMove(game.GetMyPosition(), 0f, "No moves");
            }

            BoardPosition bestMove = moves[0];
            float bestScore = float.MinValue;
            var myGoal = game.GetMyGoal();
            var oppPos = game.GetOpponentPosition();
            int boardWidth = game.BoardWidth;
            int boardHeight = game.BoardHeight;

            foreach (var move in moves)
            {
                int newPath = game.GetPathLengthFrom(move, myGoal);
                if (newPath < 0) continue;

                // Base score: path improvement
                float score = (currentPath - newPath) * 10f * p.PathWeight;

                // Defensive bonus: distance from opponent
                int distFromOpp = Mathf.Abs(move.X - oppPos.X) + Mathf.Abs(move.Y - oppPos.Y);
                score += distFromOpp * 0.5f; // Prefer keeping distance

                // Edge avoidance (harder to get blocked on edges)
                bool onEdge = move.X == 0 || move.X == boardWidth - 1 ||
                              move.Y == 0 || move.Y == boardHeight - 1;
                if (!onEdge) score += 1f;

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
                $"Safe advance (score: {bestScore:F1})"
            );
        }

        private int GetOpponentIndex(IAIGameView game)
        {
            return (game.MyPlayerIndex + 1) % game.PlayerCount;
        }
    }
}
