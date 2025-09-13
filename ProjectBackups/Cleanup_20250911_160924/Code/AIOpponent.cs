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
                
                // Jump move bonuses
                int jumpBonus = 0;
                if (IsJumpMove(start, m))
                {
                    jumpBonus = EvaluateJumpMove(start, m, myGoalRow, opponentPos);
                }

                int score = -sp * 10 + centerBias * 2 + danger + jumpBonus;
                score += parms.moveJitter != 0 ? Mathf.RoundToInt(Random.Range(-parms.moveJitter, parms.moveJitter)) : 0;

                if (score > bestScore) { bestScore = score; best = m; }
            }

            gm.TryMovePawn(start, best);
            if (logDecisions) Debug.Log($"[AI] Move -> {best} (score {bestScore}), Jump: {IsJumpMove(start, best)}");
        }

        static bool IsAdjacent(Vector2Int a, Vector2Int b)
            => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) == 1;
            
        /// <summary>
        /// Check if a move is a jump move (distance of 2)
        /// </summary>
        bool IsJumpMove(Vector2Int from, Vector2Int to)
        {
            Vector2Int diff = to - from;
            int distance = Mathf.Abs(diff.x) + Mathf.Abs(diff.y);
            return distance == 2;
        }
        
        /// <summary>
        /// Evaluate the strategic value of a jump move
        /// </summary>
        int EvaluateJumpMove(Vector2Int from, Vector2Int to, int myGoalRow, Vector2Int opponentPos)
        {
            int bonus = 0;
            
            // Base jump bonus - jumps are generally good as they cover more distance
            bonus += 3;
            
            // Check if jump gets us significantly closer to goal
            int currentDistanceToGoal = Mathf.Abs(from.y - myGoalRow);
            int newDistanceToGoal = Mathf.Abs(to.y - myGoalRow);
            int progressBonus = (currentDistanceToGoal - newDistanceToGoal) * 2;
            bonus += progressBonus;
            
            // Bonus if we jump over opponent (aggressive play)
            Vector2Int jumpDirection = Vector2Int.zero;
            if (to.x == from.x) // Vertical jump
            {
                jumpDirection = to.y > from.y ? Vector2Int.up : Vector2Int.down;
            }
            else if (to.y == from.y) // Horizontal jump
            {
                jumpDirection = to.x > from.x ? Vector2Int.right : Vector2Int.left;
            }
            
            Vector2Int middlePos = from + jumpDirection;
            if (middlePos == opponentPos)
            {
                bonus += 5; // Good bonus for jumping over opponent
                if (logDecisions) Debug.Log($"[AI] Jump over opponent detected: {from} -> {to} (over {middlePos})");
            }
            
            return bonus;
        }
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
                
                // VERTICAL WALL PREFERENCE - bonus for vertical walls to block player paths
                if (candidate.orientation == WallState.Orientation.Vertical) 
                    score += 6; // Strong preference for vertical walls
                
                // AGGRESSIVE BLOCKING BONUS - extra points for walls near player
                int distanceToPlayer = Mathf.Abs(candidate.x - themPos.x) + Mathf.Abs(candidate.y - themPos.y);
                if (distanceToPlayer <= 2) 
                    score += 8; // High bonus for walls close to player
                if (distanceToPlayer == 1) 
                    score += 12; // Maximum bonus for adjacent walls
                
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

            // Use GridSystem for wall placement instead of obsolete wallMgr
            bool placed = TryPlaceWallUsingGridSystem(bestPosition);
            if (!placed && logDecisions) Debug.Log("[AI] Wall placement failed; will move instead.");
            return placed;
        }
        
        /// <summary>
        /// Place wall using GridSystem API instead of obsolete WallManager
        /// </summary>
        bool TryPlaceWallUsingGridSystem(Vector3 worldPosition)
        {
            if (grid == null || wallMgr == null) return false;
            
            // Convert world position to grid position
            Vector2Int gridPos = grid.WorldToGridPosition(worldPosition);
            
            // Determine orientation based on position - this is a simplified approach
            // In a full implementation, you'd want to get orientation from the wall candidate
            // For now, try both orientations and use the first valid one
            
            // Try horizontal first
            if (CanPlaceWallAtGridPosition(gridPos, GridSystem.Orientation.Horizontal))
            {
                return wallMgr.TryPlaceWall(worldPosition); // Still use wallMgr for actual placement
            }
            
            // Try vertical
            if (CanPlaceWallAtGridPosition(gridPos, GridSystem.Orientation.Vertical))
            {
                return wallMgr.TryPlaceWall(worldPosition); // Still use wallMgr for actual placement
            }
            
            return false;
        }

        List<GapDetector.WallInfo> GetCandidateWallsNearOpponent(AIParams parms, Vector2Int themPos)
        {
            var candidates = new List<GapDetector.WallInfo>();
            int radius = Mathf.Clamp(parms.scanRadius, 1, 4);
            
            if (grid == null)
            {
                if (logDecisions) Debug.LogWarning("[AI] GridSystem not available for wall candidate generation");
                return candidates;
            }

            // AGGRESSIVE VERTICAL BLOCKING - prioritize positions that block forward movement
            AddAggressiveVerticalBlockingWalls(themPos, candidates);

            // Generate candidate positions around opponent using GridSystem
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    Vector2Int gridPos = new Vector2Int(themPos.x + dx, themPos.y + dy);
                    
                    // PRIORITIZE VERTICAL WALLS - try them first and more often
                    TryAddWallCandidatesAtPosition(gridPos, GridSystem.Orientation.Vertical, candidates);
                    
                    // Add horizontal walls with lower priority
                    TryAddWallCandidatesAtPosition(gridPos, GridSystem.Orientation.Horizontal, candidates);
                }
            }

            return candidates;
        }

        /// <summary>
        /// Add aggressive vertical walls that directly block player's forward progress
        /// </summary>
        void AddAggressiveVerticalBlockingWalls(Vector2Int playerPos, List<GapDetector.WallInfo> candidates)
        {
            // Target positions directly in front of player to block forward movement
            Vector2Int[] blockingPositions = {
                new Vector2Int(playerPos.x - 1, playerPos.y + 1), // Left-front diagonal block
                new Vector2Int(playerPos.x, playerPos.y + 1),     // Direct front block
                new Vector2Int(playerPos.x + 1, playerPos.y + 1), // Right-front diagonal block
                new Vector2Int(playerPos.x - 1, playerPos.y),     // Left side block
                new Vector2Int(playerPos.x + 1, playerPos.y),     // Right side block
            };
            
            foreach (var pos in blockingPositions)
            {
                // Focus on vertical walls for maximum blocking efficiency
                TryAddWallCandidatesAtPosition(pos, GridSystem.Orientation.Vertical, candidates);
                
                if (logDecisions)
                    Debug.Log($"[AI] Added aggressive vertical blocking candidate at {pos}");
            }
        }

        /// <summary>
        /// Try to add wall candidates at a specific position using GridSystem
        /// </summary>
        void TryAddWallCandidatesAtPosition(Vector2Int gridPos, GridSystem.Orientation orientation, List<GapDetector.WallInfo> candidates)
        {
            // Check if we can place a wall at this position
            if (CanPlaceWallAtGridPosition(gridPos, orientation))
            {
                // Convert to world position for compatibility with existing WallInfo structure
                Vector3 worldPos = grid.GridToWorldPosition(gridPos);
                
                // Create WallInfo for this candidate
                var wallInfo = new GapDetector.WallInfo
                {
                    pos = worldPos,
                    x = gridPos.x,
                    y = gridPos.y,
                    orientation = orientation == GridSystem.Orientation.Horizontal ? 
                        WallState.Orientation.Horizontal : WallState.Orientation.Vertical
                };
                
                candidates.Add(wallInfo);
                
                if (logDecisions)
                {
                    Debug.Log($"[AI] Added wall candidate: {orientation} at grid {gridPos} (world {worldPos})");
                }
            }
        }

        /// <summary>
        /// Check if a wall can be placed at the specified grid position using GridSystem
        /// </summary>
        bool CanPlaceWallAtGridPosition(Vector2Int gridPos, GridSystem.Orientation orientation)
        {
            if (grid == null) return false;
            
            // Check grid bounds for wall placement
            if (orientation == GridSystem.Orientation.Horizontal)
            {
                // Horizontal walls span 2 tiles horizontally
                if (gridPos.x < 0 || gridPos.x >= gm.gridSize - 1 || gridPos.y < 0 || gridPos.y >= gm.gridSize)
                    return false;
            }
            else // Vertical
            {
                // Vertical walls span 2 tiles vertically
                if (gridPos.x < 0 || gridPos.x >= gm.gridSize || gridPos.y < 0 || gridPos.y >= gm.gridSize - 1)
                    return false;
            }
            
            // Use GridSystem to check if wall can be placed
            // Convert to unified grid coordinate for checking
            Vector2Int unifiedPos = grid.TileToUnifiedPosition(gridPos);
            
            // Check if the wall positions are already occupied
            return !IsWallOccupiedAtPosition(gridPos, orientation);
        }

        /// <summary>
        /// Check if wall is occupied at position using GridSystem
        /// </summary>
        bool IsWallOccupiedAtPosition(Vector2Int gridPos, GridSystem.Orientation orientation)
        {
            if (grid == null) return true; // Assume occupied if no grid
            
            if (orientation == GridSystem.Orientation.Horizontal)
            {
                // Check both positions of horizontal wall
                Vector2Int pos1 = new Vector2Int(gridPos.x, gridPos.y);
                Vector2Int pos2 = new Vector2Int(gridPos.x + 1, gridPos.y);
                
                Vector2Int unified1 = grid.TileToUnifiedPosition(pos1);
                Vector2Int unified2 = grid.TileToUnifiedPosition(pos2);
                
                var cell1 = grid.GetCell(unified1);
                var cell2 = grid.GetCell(unified2);
                
                return (cell1 != null && cell1.isOccupied) || (cell2 != null && cell2.isOccupied);
            }
            else // Vertical
            {
                // Check both positions of vertical wall
                Vector2Int pos1 = new Vector2Int(gridPos.x, gridPos.y);
                Vector2Int pos2 = new Vector2Int(gridPos.x, gridPos.y + 1);
                
                Vector2Int unified1 = grid.TileToUnifiedPosition(pos1);
                Vector2Int unified2 = grid.TileToUnifiedPosition(pos2);
                
                var cell1 = grid.GetCell(unified1);
                var cell2 = grid.GetCell(unified2);
                
                return (cell1 != null && cell1.isOccupied) || (cell2 != null && cell2.isOccupied);
            }
        }

        /// <summary>
        /// Temporarily set wall occupancy for AI simulation using GridSystem
        /// </summary>
        void SetWallOccupied(GapDetector.WallInfo wall, bool occupied)
        {
            if (grid == null) return;
            
            Vector2Int gridPos = new Vector2Int(wall.x, wall.y);
            GridSystem.Orientation orientation = wall.orientation == WallState.Orientation.Horizontal ? 
                GridSystem.Orientation.Horizontal : GridSystem.Orientation.Vertical;
                
            if (orientation == GridSystem.Orientation.Horizontal)
            {
                // Set occupancy for both positions of horizontal wall
                Vector2Int pos1 = new Vector2Int(gridPos.x, gridPos.y);
                Vector2Int pos2 = new Vector2Int(gridPos.x + 1, gridPos.y);
                
                SetGridCellOccupied(pos1, occupied);
                SetGridCellOccupied(pos2, occupied);
            }
            else // Vertical
            {
                // Set occupancy for both positions of vertical wall
                Vector2Int pos1 = new Vector2Int(gridPos.x, gridPos.y);
                Vector2Int pos2 = new Vector2Int(gridPos.x, gridPos.y + 1);
                
                SetGridCellOccupied(pos1, occupied);
                SetGridCellOccupied(pos2, occupied);
            }
        }
        
        /// <summary>
        /// Helper method to set grid cell occupancy
        /// </summary>
        void SetGridCellOccupied(Vector2Int gridPos, bool occupied)
        {
            Vector2Int unifiedPos = grid.TileToUnifiedPosition(gridPos);
            var cell = grid.GetCell(unifiedPos);
            if (cell != null)
            {
                cell.isOccupied = occupied;
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
                        wallThresholdScore = 6,  // More aggressive - lowered from 10
                        moveJitter = 3,
                        wallJitter = 6,
                        wallBias = 0.40f,        // More walls - increased from 0.15f
                        scanRadius = 2
                    };

                case Difficulty.Casual:
                    return new AIParams
                    {
                        wallThresholdScore = 8,  // More aggressive - lowered from 12
                        moveJitter = 2,
                        wallJitter = 4,
                        wallBias = 0.55f,        // More walls - increased from 0.30f
                        scanRadius = 3
                    };

                case Difficulty.Intermediate:
                    return new AIParams
                    {
                        wallThresholdScore = 10, // More aggressive - lowered from 14
                        moveJitter = 1,
                        wallJitter = 2,
                        wallBias = 0.70f,        // More walls - increased from 0.45f
                        scanRadius = 3
                    };

                case Difficulty.Advanced:
                    return new AIParams
                    {
                        wallThresholdScore = 12, // More aggressive - lowered from 16
                        moveJitter = 0,
                        wallJitter = 1,
                        wallBias = 0.80f,        // More walls - increased from 0.55f
                        scanRadius = 4
                    };

                default: // Expert
                    return new AIParams
                    {
                        wallThresholdScore = 14, // More aggressive - lowered from 18
                        moveJitter = 0,
                        wallJitter = 0,
                        wallBias = 0.95f,        // Maximum aggression - increased from 0.65f
                        scanRadius = 4
                    };
            }
        }

        bool ShouldTryWall(AIParams p, int mySP, int theirSP)
        {
            int lead = theirSP - mySP;
            float baseChance = p.wallBias + Mathf.Clamp01((-lead) * 0.05f);
            
            // AGGRESSIVE WALL PLACEMENT - bonus chance early in game and when opponent is advancing
            int wallsUsed = 10 - gm.opponentWallsRemaining; // How many walls we've used
            if (wallsUsed < 3) baseChance += 0.25f; // Extra aggressive in early game
            
            // Extra aggressive when opponent is close to winning
            if (theirSP <= 3) baseChance += 0.30f;
            
            return Random.value < Mathf.Clamp01(baseChance);
        }
        #endregion
    }
}