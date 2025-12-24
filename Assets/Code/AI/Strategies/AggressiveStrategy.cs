using System.Collections.Generic;
using WallChess.Core.Data;
using WallChess.Core.Config;
using UnityEngine;

namespace WallChess.AI.Strategies
{
    /// <summary>
    /// Aggressive AI: Prioritizes blocking opponent over advancing.
    /// </summary>
    public class AggressiveStrategy : IAIStrategy
    {
        public string StrategyName => "Aggressive";

        public AIDecision Decide(IAIGameView game, AIEvaluationParams p)
        {
            int myPath = game.GetMyPathLength();
            int theirPath = game.GetOpponentPathLength();

            // Aggressive: always try to place wall first if opponent is within range
            if (game.GetMyWallsRemaining() > 0 && theirPath <= p.WallRadius + 2)
            {
                var wallDecision = FindBestBlockingWall(game, p, theirPath);
                if (wallDecision.HasValue)
                    return wallDecision.Value;
            }

            // Fall back to advancing
            return FindAdvanceMove(game, p, myPath);
        }

        private AIDecision? FindBestBlockingWall(IAIGameView game, AIEvaluationParams p, int theirPath)
        {
            var validWalls = game.GetValidWallPlacements();
            if (validWalls.Count == 0) return null;

            var oppPos = game.GetOpponentPosition();
            WallPlacement? bestWall = null;
            int bestDamage = 0;

            foreach (var wall in validWalls)
            {
                // Skip walls far from opponent
                int distToOpp = Mathf.Abs(wall.Position.X - oppPos.X) + Mathf.Abs(wall.Position.Y - oppPos.Y);
                if (distToOpp > p.WallRadius) continue;

                // Check if wall would block all paths
                if (game.WouldWallBlockPath(wall)) continue;

                // Calculate damage to opponent
                int newTheirPath = game.GetPathLengthWithWall(wall, GetOpponentIndex(game));
                if (newTheirPath < 0) continue;

                int damage = newTheirPath - theirPath;

                // Add aggression-weighted jitter
                damage += Mathf.RoundToInt((Random.value - 0.3f) * p.WallJitter * 5f);

                if (damage > bestDamage)
                {
                    bestDamage = damage;
                    bestWall = wall;
                }
            }

            if (bestWall.HasValue && bestDamage >= 1)
            {
                return AIDecision.CreateWallPlacement(
                    bestWall.Value,
                    Mathf.Clamp01(bestDamage / 5f),
                    $"Aggressive block (+{bestDamage} to opponent path)"
                );
            }

            return null;
        }

        private AIDecision FindAdvanceMove(IAIGameView game, AIEvaluationParams p, int currentPath)
        {
            var moves = game.GetMyValidMoves();
            if (moves.Count == 0)
            {
                return AIDecision.CreateMove(game.GetMyPosition(), 0f, "No moves");
            }

            BoardPosition bestMove = moves[0];
            float bestScore = float.MinValue;
            var myGoal = game.GetMyGoal();

            foreach (var move in moves)
            {
                int newPath = game.GetPathLengthFrom(move, myGoal);
                if (newPath < 0) continue;

                // Aggressive: strongly prioritize path reduction
                float score = (currentPath - newPath) * 15f;

                // Bonus for jumping (aggressive move)
                int dist = Mathf.Abs(move.X - game.GetMyPosition().X) + Mathf.Abs(move.Y - game.GetMyPosition().Y);
                if (dist > 1) score += 5f; // Jump bonus

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
                Mathf.Clamp01(bestScore / 30f + 0.5f),
                $"Aggressive advance (score: {bestScore:F1})"
            );
        }

        private int GetOpponentIndex(IAIGameView game)
        {
            return (game.MyPlayerIndex + 1) % game.PlayerCount;
        }
    }
}
