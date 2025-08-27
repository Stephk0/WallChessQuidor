using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WallChess
{
    [DisallowMultipleComponent]
    public class AIOpponent : MonoBehaviour
    {
        public enum Difficulty { Beginner, Casual, Intermediate, Advanced, Expert }

        [Header("AI Settings")]
        [Tooltip("When checked, the opponent pawn is controlled by AI.")]
        public bool opponentIsAI = true;

        [Tooltip("Harder = more strategic walling and fewer beginner mistakes.")]
        public Difficulty difficulty = Difficulty.Intermediate;

        [Range(0f, 2f)]
        [Tooltip("Small delay so moves are readable in playtests.")]
        public float thinkDelaySeconds = 0.2f;

        [Header("Debug")]
        public bool logDecisions = false;

        WallChessGameManager gm;
        public PlayerControllerV2 playerCtrl;
        public WallManager wallMgr;
        public GridSystem grid; // grabbed via GameManager

        bool isThinking;

        void Awake()
        {
            gm = GetComponent<WallChessGameManager>();
            playerCtrl = GetComponent<PlayerControllerV2>();
            wallMgr = GetComponent<WallManager>();
        }

        void Start()
        {
            if (gm != null) grid = gm.GetGridSystem();
        }

        void Update()
        {
            if (!opponentIsAI || gm == null || gm.GetCurrentState() == GameState.GameOver) return;

            // Only act on the AI's turn and when no action is underway
            if (!isThinking &&
                gm.IsOpponentTurn() &&
                gm.CanInitiateMove() && // idle (not in movement or wall placement)
                !gm.IsInMovementState())
            {
                StartCoroutine(DecideAndPlay());
            }
        }

        #region Core loop
        IEnumerator DecideAndPlay()
        {
            isThinking = true;
            if (thinkDelaySeconds > 0f) yield return new WaitForSeconds(thinkDelaySeconds);

            // Evaluate current state
            Vector2Int myPos = gm.opponentPosition;
            Vector2Int themPos = gm.playerPosition;
            int myGoalRow = gm.gridSize - 1; // opponent wins when y == gridSize-1
            int theirGoalRow = 0;

            int mySP = ShortestPathLength(myPos, myGoalRow);
            int theirSP = ShortestPathLength(themPos, theirGoalRow);

            var parms = GetParams(difficulty);

            // Decide whether to attempt a wall or a move
            bool tryWall = gm.opponentWallsRemaining > 0 &&
                           gm.CanInitiateWallPlacement() &&
                           ShouldTryWall(parms, mySP, theirSP);

            if (tryWall && TryBestWall(parms, myPos, themPos, myGoalRow, theirGoalRow))
            {
                if (logDecisions) Debug.Log("[AI] Placed a wall.");
                isThinking = false;
                yield break;
            }

            // Otherwise, move along our best path (with small tie-breakers)
            TryBestMove(parms, myPos, myGoalRow, themPos);
            isThinking = false;
        }
        #endregion

        #region Movement
        void TryBestMove(AIParams parms, Vector2Int start, int myGoalRow, Vector2Int opponentPos)
        {
            var moves = playerCtrl.GetValidMoves(start);
            if (moves == null || moves.Count == 0) return;

            int bestScore = int.MinValue;
            Vector2Int best = moves[0];

            foreach (var m in moves)
            {
                // Primary: reduce our shortest path
                int sp = ShortestPathLength(m, myGoalRow);
                if (sp < 0) continue;

                // Secondary: prefer central files, avoid hugging edges too early
                int centerBias = -Mathf.Abs(m.x - (gm.gridSize / 2));

                // Tertiary: avoid stepping directly in front of opponent if it opens jumps
                int danger = (IsAdjacent(m, opponentPos) ? -1 : 0);

                int score = -sp * 10 + centerBias * 2 + danger;
                // Add a little noise on lower difficulties
                score += parms.moveJitter != 0 ? Mathf.RoundToInt(Random.Range(-parms.moveJitter, parms.moveJitter)) : 0;

                if (score > bestScore) { bestScore = score; best = m; }
            }

            // Execute
            gm.TryMovePawn(start, best);
            if (logDecisions) Debug.Log($"[AI] Move -> {best} (score {bestScore})");
        }

        static bool IsAdjacent(Vector2Int a, Vector2Int b)
            => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) == 1;
        #endregion

        #region Walls
        bool TryBestWall(AIParams parms, Vector2Int myPos, Vector2Int themPos, int myGoalRow, int theirGoalRow)
        {
            // Pull candidate set; if not available, consider a small local ring around the opponent
            var candidates = GetCandidateWallsNearOpponent(parms, themPos);
            if (candidates == null || candidates.Count == 0) return false;

            int baseTheirSP = ShortestPathLength(themPos, theirGoalRow);
            int baseMySP = ShortestPathLength(myPos, myGoalRow);

            int bestScore = int.MinValue;
            WallChoice best = default;
            bool found = false;

            foreach (var w in candidates)
            {
                // Simulate: mark both halves of the wall, score, then undo
                SimApply(w, true);
                int theirSP = ShortestPathLength(themPos, theirGoalRow);
                int mySP = ShortestPathLength(myPos, myGoalRow);
                SimApply(w, false);

                if (theirSP < 0 || mySP < 0) continue; // illegal (shouldn't happen if validation aligns)

                int deltaOpp = theirSP - baseTheirSP;
                int deltaMe = mySP - baseMySP;

                // Heuristic: maximize (opponent delay) while minimizing (self delay)
                int score = deltaOpp * 12 - deltaMe * 8;

                // Bonuses for “good” Quoridor habits:
                // • Make funnels/two-path threats (approx: increase opp path by >=2 without hurting us)
                if (deltaOpp >= 2 && deltaMe <= 1) score += 8;
                // • Prefer center to keep options open (wall centers near middle files/ranks)
                score += CenterBonus(w);

                // Penalties on lower difficulties to mimic human mistakes
                score += parms.wallJitter != 0 ? Mathf.RoundToInt(Random.Range(-parms.wallJitter, parms.wallJitter)) : 0;

                if (score > bestScore)
                {
                    bestScore = score;
                    best = new WallChoice(w);
                    found = true;
                }
            }

            if (!found) return false;

            // Threshold: only place a wall if it’s truly useful for this difficulty
            if (bestScore < parms.wallThresholdScore) return false;

            // Place it through WallManager (converts to GridSystem + decrements via GameManager callback)
            bool placed = wallMgr.TryPlaceWall(best.centerWorld);
            if (!placed && logDecisions) Debug.Log("[AI] TryPlaceWall failed; will move instead.");
            return placed;
        }

        struct WallChoice
        {
            public WallManager.Orientation orientation;
            public int x, y;
            public Vector3 centerWorld;

            public WallChoice(WallManager.WallInfo info)
            {
                orientation = info.orientation;
                x = info.x; y = info.y;
                centerWorld = info.position;
            }
        }

        List<WallManager.WallInfo> GetCandidateWallsNearOpponent(AIParams parms, Vector2Int themPos)
        {
            // Prefer a ring around the opponent’s shortest path region.
            var list = new List<WallManager.WallInfo>();

            // If WallManager exposes prefiltered valid positions, use it; otherwise scan a modest area.
            // (This project exposes GetValidWallPositions(); if missing at runtime, we fallback to a local sample.)
            try
            {
                var all = wallMgr.GetValidWallPositions(); // may be large; we’ll filter
                foreach (var w in all)
                {
                    if (NearGrid(themp: themPos, w))
                        list.Add(w);
                }
            }
            catch
            {
                // Fallback: sample 5x5 area around opponent in both orientations
                int r = Mathf.Clamp(parms.scanRadius, 1, 4);
                for (int dx = -r; dx <= r; dx++)
                    for (int dy = -r; dy <= r; dy++)
                    {
                        Vector2Int p = new Vector2Int(themPos.x + dx, themPos.y + dy);
                        // Convert candidate tile-adjacent coordinates into nearest gap index by clamping
                        int gx = Mathf.Clamp(p.x, 0, gm.gridSize - 2);
                        int gy = Mathf.Clamp(p.y, 0, gm.gridSize - 2);

                        // Build virtual infos; CanPlaceWall will be rechecked via simulation
                        list.Add(BuildInfo(WallManager.Orientation.Vertical, gx, gy));
                        list.Add(BuildInfo(WallManager.Orientation.Horizontal, gx, gy));
                    }
            }

            return list;
        }

        bool NearGrid(Vector2Int themp, WallManager.WallInfo w)
        {
            // Consider a wall “near” if within a Manhattan distance around the opponent pawn.
            int r = Mathf.Max(2, GetParams(difficulty).scanRadius);
            return Mathf.Abs(w.x - themp.x) + Mathf.Abs(w.y - themp.y) <= r + 1;
        }

        WallManager.WallInfo BuildInfo(WallManager.Orientation o, int x, int y)
        {
            // Recreate a WallInfo with center position/scale using WallManager’s helpers
            // We approximate the center position the same way WallManager does (grid spacing inferred from GridSystem).
            float spacing = gm.tileSize + gm.tileGap;
            float off = 0.5f;
            Vector3 center = (o == WallManager.Orientation.Horizontal)
                ? new Vector3((x + off) * spacing, (y + off) * spacing, -0.1f)
                : new Vector3((x + off) * spacing, (y + off) * spacing, -0.1f);

            Vector3 scale = (o == WallManager.Orientation.Horizontal)
                ? new Vector3(gm.tileSize * 2f + gm.tileGap, gm.wallThickness, gm.wallHeight)
                : new Vector3(gm.wallThickness, gm.tileSize * 2f + gm.tileGap, gm.wallHeight);

            return new WallManager.WallInfo(o, x, y, center, scale);
        }

        void SimApply(WallManager.WallInfo w, bool place)
        {
            // Toggle both halves, matching WallManager.OccupyWallPositions
            bool occ = place;
            if (w.orientation == WallManager.Orientation.Horizontal)
            {
                wallMgr.SetGapOccupiedForTesting(WallManager.Orientation.Horizontal, w.x, w.y, occ);
                wallMgr.SetGapOccupiedForTesting(WallManager.Orientation.Horizontal, w.x + 1, w.y, occ);
            }
            else
            {
                wallMgr.SetGapOccupiedForTesting(WallManager.Orientation.Vertical, w.x, w.y, occ);
                wallMgr.SetGapOccupiedForTesting(WallManager.Orientation.Vertical, w.x, w.y + 1, occ);
            }
        }

        int CenterBonus(WallManager.WallInfo w)
        {
            int cx = gm.gridSize / 2;
            int cy = gm.gridSize / 2;
            int fx = -Mathf.Abs(w.x - cx);
            int fy = -Mathf.Abs(w.y - cy);
            return (fx + fy);
        }
        #endregion

        #region Shortest path (BFS via PlayerControllerV2/WallManager rules)
        int ShortestPathLength(Vector2Int start, int goalRow)
        {
            // BFS over tiles using the same move legality as the PlayerController (respects walls)
            var q = new Queue<Vector2Int>();
            var seen = new HashSet<Vector2Int>();
            q.Enqueue(start);
            seen.Add(start);

            int steps = 0;
            while (q.Count > 0)
            {
                int layer = q.Count;
                for (int i = 0; i < layer; i++)
                {
                    var cur = q.Dequeue();
                    if (cur.y == goalRow) return steps;

                    foreach (var nxt in playerCtrl.GetValidMoves(cur))
                    {
                        if (!seen.Contains(nxt))
                        {
                            seen.Add(nxt);
                            q.Enqueue(nxt);
                        }
                    }
                }
                steps++;
            }
            return -1; // no path (should never happen due to rules)
        }
        #endregion

        #region Difficulty profiles
        struct AIParams
        {
            public int wallThresholdScore;  // minimum usefulness to actually wall
            public int moveJitter;          // randomness added to moves (±)
            public int wallJitter;          // randomness added to wall scores (±)
            public float wallBias;          // baseline probability to consider walls
            public int scanRadius;          // how far around opponent to look for walls
        }

        AIParams GetParams(Difficulty d)
        {
            switch (d)
            {
                case Difficulty.Beginner:
                    return new AIParams
                    {
                        wallThresholdScore = 10,
                        moveJitter = 3,
                        wallJitter = 6,
                        wallBias = 0.15f,
                        scanRadius = 2
                    }; // mostly runs forward; occasional random/bad walls

                case Difficulty.Casual:
                    return new AIParams
                    {
                        wallThresholdScore = 12,
                        moveJitter = 2,
                        wallJitter = 4,
                        wallBias = 0.30f,
                        scanRadius = 3
                    }; // mixes simple mirrors & basic blocks

                case Difficulty.Intermediate:
                    return new AIParams
                    {
                        wallThresholdScore = 14,
                        moveJitter = 1,
                        wallJitter = 2,
                        wallBias = 0.45f,
                        scanRadius = 3
                    }; // balances pathing vs. delaying opponent (wall economy)

                case Difficulty.Advanced:
                    return new AIParams
                    {
                        wallThresholdScore = 16,
                        moveJitter = 0,
                        wallJitter = 1,
                        wallBias = 0.55f,
                        scanRadius = 4
                    }; // favors center funnels, two-path threats, avoids self-blocking

                default: // Expert
                    return new AIParams
                    {
                        wallThresholdScore = 18,
                        moveJitter = 0,
                        wallJitter = 0,
                        wallBias = 0.65f,
                        scanRadius = 4
                    }; // highly selective, walls only when it nets ≥2 steps with ≤1 self penalty
            }
        }

        bool ShouldTryWall(AIParams p, int mySP, int theirSP)
        {
            // If we’re already clearly winning, run; if behind, wall more.
            int lead = theirSP - mySP;
            float baseChance = p.wallBias + Mathf.Clamp01((-lead) * 0.05f); // behind => more walls
            return Random.value < baseChance;
        }
        #endregion
    }
}
