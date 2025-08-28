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
        public GridSystem grid;

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
            if (!opponentIsAI || gm is null || gm.GetCurrentState() == GameState.GameOver) return;

            if (!isThinking &&
                gm.IsOpponentTurn() &&
                gm.CanInitiateMove() && 
                !gm.IsInMovementState())
            {
                StartCoroutine(DecideAndPlay());
            }
        }

        #region Core Loop
        IEnumerator DecideAndPlay()
        {
            isThinking = true;
            if (thinkDelaySeconds > 0f) yield return new WaitForSeconds(thinkDelaySeconds);

            Vector2Int myPos = gm.opponentPosition;
            Vector2Int themPos = gm.playerPosition;
            int myGoalRow = 0; 
            int theirGoalRow = gm.gridSize - 1;

            int mySP = ShortestPathLength(myPos, myGoalRow);
            int theirSP = ShortestPathLength(themPos, theirGoalRow);

            var parms = GetParams(difficulty);

            bool tryWall = gm.opponentWallsRemaining > 0 &&
                           gm.CanInitiateWallPlacement() &&
                           ShouldTryWall(parms, mySP, theirSP);

            if (tryWall && TryBestWall(parms, myPos, themPos, myGoalRow, theirGoalRow))
            {
                if (logDecisions) Debug.Log("[AI] Placed a wall.");
                isThinking = false;
                yield break;
            }

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
                int sp = ShortestPathLength(m, myGoalRow);
                if (sp < 0) continue;

                int centerBias = -Mathf.Abs(m.x - (gm.gridSize / 2));
                int danger = (IsAdjacent(m, opponentPos) ? -1 : 0);

                int score = -sp * 10 + centerBias * 2 + danger;
                score += parms.moveJitter != 0 ? Mathf.RoundToInt(Random.Range(-parms.moveJitter, parms.moveJitter)) : 0;

                if (score > bestScore) { bestScore = score; best = m; }
            }

            gm.TryMovePawn(start, best);
            if (logDecisions) Debug.Log($"[AI] Move -> {best} (score {bestScore})");
        }

        static bool IsAdjacent(Vector2Int a, Vector2Int b)
            => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) == 1;
        #endregion

        #region Walls
        bool TryBestWall(AIParams parms, Vector2Int myPos, Vector2Int themPos, int myGoalRow, int theirGoalRow)
        {
            var candidates = GetCandidateWallsNearOpponent(parms, themPos);
            if (candidates == null || candidates.Count == 0) return false;

            int baseTheirSP = ShortestPathLength(themPos, theirGoalRow);
            int baseMySP = ShortestPathLength(myPos, myGoalRow);

            int bestScore = int.MinValue;
            Vector3 bestPosition = Vector3.zero;
            bool found = false;

            foreach (var candidate in candidates)
            {
                // Simulate placement using temporary occupancy
                SetWallOccupied(candidate, true);
                int theirSP = ShortestPathLength(themPos, theirGoalRow);
                int mySP = ShortestPathLength(myPos, myGoalRow);
                SetWallOccupied(candidate, false);

                if (theirSP < 0 || mySP < 0) continue; 

                int deltaOpp = theirSP - baseTheirSP;
                int deltaMe = mySP - baseMySP;

                int score = deltaOpp * 12 - deltaMe * 8;

                if (deltaOpp >= 2 && deltaMe <= 1) score += 8;
                score += CenterBonus(candidate);
                score += parms.wallJitter != 0 ? Mathf.RoundToInt(Random.Range(-parms.wallJitter, parms.wallJitter)) : 0;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestPosition = candidate.pos;
                    found = true;
                }
            }

            if (!found || bestScore < parms.wallThresholdScore) return false;

            bool placed = wallMgr.TryPlaceWall(bestPosition);
            if (!placed && logDecisions) Debug.Log("[AI] Wall placement failed; will move instead.");
            return placed;
        }

        List<GapDetector.WallInfo> GetCandidateWallsNearOpponent(AIParams parms, Vector2Int themPos)
        {
            var candidates = new List<GapDetector.WallInfo>();
            int radius = Mathf.Clamp(parms.scanRadius, 1, 4);
            float spacing = gm.tileSize + gm.tileGap;

            // Generate candidate positions around opponent
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    Vector2Int gridPos = new Vector2Int(themPos.x + dx, themPos.y + dy);
                    Vector3 worldPos = new Vector3(gridPos.x * spacing, gridPos.y * spacing, 0f);

                    // Try both orientations at each candidate location
                    AddCandidateIfValid(worldPos, candidates);
                    AddCandidateIfValid(worldPos + Vector3.right * spacing * 0.5f, candidates);
                    AddCandidateIfValid(worldPos + Vector3.up * spacing * 0.5f, candidates);
                }
            }

            return candidates;
        }

        void AddCandidateIfValid(Vector3 worldPos, List<GapDetector.WallInfo> candidates)
        {
            // Use GapDetector to find nearest valid wall at this position
            var gapDetector = GetGapDetector();
            if (gapDetector != null && gapDetector.TryFind(worldPos, gm, out var info))
            {
                // Check if this wall can potentially be placed
                var validator = GetWallValidator();
                if (validator != null && validator.CanPlace(info))
                {
                    candidates.Add(info);
                }
            }
        }

        GapDetector GetGapDetector() => wallMgr?.GetGapDetector();
        WallValidator GetWallValidator() => wallMgr?.GetWallValidator();
        WallState GetWallState() => wallMgr?.GetWallState();

        void SetWallOccupied(GapDetector.WallInfo wall, bool occupied)
        {
            var state = GetWallState();
            if (state == null) return;

            if (wall.orientation == WallState.Orientation.Horizontal)
            {
                state.SetOccupied(WallState.Orientation.Horizontal, wall.x, wall.y, occupied);
                state.SetOccupied(WallState.Orientation.Horizontal, wall.x + 1, wall.y, occupied);
            }
            else
            {
                state.SetOccupied(WallState.Orientation.Vertical, wall.x, wall.y, occupied);
                state.SetOccupied(WallState.Orientation.Vertical, wall.x, wall.y + 1, occupied);
            }
        }

        int CenterBonus(GapDetector.WallInfo w)
        {
            int cx = gm.gridSize / 2;
            int cy = gm.gridSize / 2;
            int fx = -Mathf.Abs(w.x - cx);
            int fy = -Mathf.Abs(w.y - cy);
            return (fx + fy);
        }
        #endregion

        #region Pathfinding
        int ShortestPathLength(Vector2Int start, int goalRow)
        {
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
            return -1;
        }
        #endregion

        #region Difficulty Profiles
        struct AIParams
        {
            public int wallThresholdScore;  
            public int moveJitter;          
            public int wallJitter;          
            public float wallBias;          
            public int scanRadius;          
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
                    };

                case Difficulty.Casual:
                    return new AIParams
                    {
                        wallThresholdScore = 12,
                        moveJitter = 2,
                        wallJitter = 4,
                        wallBias = 0.30f,
                        scanRadius = 3
                    };

                case Difficulty.Intermediate:
                    return new AIParams
                    {
                        wallThresholdScore = 14,
                        moveJitter = 1,
                        wallJitter = 2,
                        wallBias = 0.45f,
                        scanRadius = 3
                    };

                case Difficulty.Advanced:
                    return new AIParams
                    {
                        wallThresholdScore = 16,
                        moveJitter = 0,
                        wallJitter = 1,
                        wallBias = 0.55f,
                        scanRadius = 4
                    };

                default: // Expert
                    return new AIParams
                    {
                        wallThresholdScore = 18,
                        moveJitter = 0,
                        wallJitter = 0,
                        wallBias = 0.65f,
                        scanRadius = 4
                    };
            }
        }

        bool ShouldTryWall(AIParams p, int mySP, int theirSP)
        {
            int lead = theirSP - mySP;
            float baseChance = p.wallBias + Mathf.Clamp01((-lead) * 0.05f);
            return Random.value < baseChance;
        }
        #endregion
    }
}