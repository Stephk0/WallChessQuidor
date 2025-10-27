using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using WallChess.Core;
using WallChess.Core.Session;
using WallChess.Gameplay.Pawns;
using WallChess.Core.PlayerActionStates;
using PawnClass = WallChess.Gameplay.Pawns.Pawn;

namespace WallChess
{
    /// <summary>
    /// Enhanced AI strategies based on Quoridor theory
    /// </summary>
    public enum AIStrategy
    {
        Adaptive,        // Adapts based on opponent behavior
        Aggressive,      // Focus on blocking opponent paths
        Defensive,       // Focus on securing own path
        Positional,      // Control key board positions
        Opening_Reed,    // Reed opening strategy
        Opening_Shiller, // Shiller opening strategy
        Opening_Stonewall, // Stonewall defense
        Opening_Ala      // Ala opening strategy
    }

    /// <summary>
    /// Game phase analysis for strategic decision making
    /// </summary>
    public enum GamePhaseDetailed
    {
        Opening_Early,      // First 3-4 moves, focus on advancement
        Opening_Mid,        // Moves 4-7, implement opening strategy
        Midgame_Control,    // Focus on path maximization
        Midgame_Pressure,   // Apply pressure through walls
        Endgame_Critical,   // Final positioning and blocking
        Endgame_Race        // Pure race to finish
    }

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
        public float thinkDelaySeconds = 0.05f;

        [Header("Enhanced Strategy Settings")]
        [Tooltip("AI strategy to use - affects wall placement and movement decisions.")]
        public AIStrategy strategy = AIStrategy.Adaptive;
        
        [Tooltip("Enable advanced opponent path analysis for better blocking.")]
        public bool useAdvancedPathAnalysis = true;
        
        [Tooltip("Enable opening strategy recognition and implementation.")]
        public bool useOpeningStrategies = true;
        
        [Tooltip("Weight for opponent path maximization (higher = more aggressive blocking).")]
        [Range(1.0f, 3.0f)]
        public float pathMaximizationWeight = 1.5f;
        
        [Tooltip("Enable mirroring strategy against certain opponent patterns.")]
        public bool enableMirroring = true;

        [Header("Debug")]
        public bool logDecisions = false;
        
        [Header("Player Action State Machine")]
        [SerializeField] private PlayerActionStateMachine actionStateMachine;

        // Core components
        WallChessGameManager gm;
        public PawnController pawnController;
        public WallManager wallMgr;
        public GridSystem grid;
        bool isThinking;
        
        // Enhanced AI state tracking
        private List<Vector2Int> opponentMoveHistory = new List<Vector2Int>();
        private List<Vector3> opponentWallHistory = new List<Vector3>();
        private int totalMovesPlayed = 0;
        private GamePhaseDetailed currentGamePhase = GamePhaseDetailed.Opening_Early;
        private AIStrategy activeStrategy;
        private bool hasImplementedOpening = false;
        private Vector2Int lastOpponentPosition = Vector2Int.zero;
        
        // Path analysis cache
        private Dictionary<Vector2Int, List<Vector2Int>> cachedPaths = new Dictionary<Vector2Int, List<Vector2Int>>();
        private int lastPathCacheFrame = -1;

        struct AIParams
        {
            public int wallThresholdScore;
            public int moveJitter;
            public int wallJitter;
            public float wallBias;
            public int scanRadius;
        }

        void Awake()
        {
            gm = GetComponent<WallChessGameManager>();
            wallMgr = GetComponent<WallManager>();
            
            // Initialize Player Action State Machine for AI
            if (actionStateMachine == null)
            {
                actionStateMachine = gameObject.AddComponent<PlayerActionStateMachine>();
            }
            
            // Subscribe to action state machine events
            actionStateMachine.OnActionConfirmed += OnAIActionConfirmed;
            actionStateMachine.OnActionCancelled += OnAIActionCancelled;
        }

        void Start()
        {
            // Try to get grid from game manager
            if (gm != null)
            {
                grid = gm.GetGridSystem();
                if (grid != null)
                {
                    LogInfo($"GridSystem found: Size {grid.GetGridSize()}");
                }
                else
                {
                    Debug.LogWarning("[AIOpponent] GridSystem not found yet - will retry");
                }
            }
            else
            {
                Debug.LogError("[AIOpponent] WallChessGameManager not found!");
            }
            
            // Try to get PawnController
            var controller = GetPawnController();
            if (controller != null)
            {
                LogInfo("PawnController found");
            }
            
            LogInfo("AIOpponent initialized - components will be retried if needed");
        }

        void Update()
        {
            if (!opponentIsAI || gm == null || isThinking) return;
            
            if (gm.GetCurrentState() == GameState.GameOver) return;

            // Retry getting components if not available
            if (grid == null && gm != null)
            {
                grid = gm.GetGridSystem();
            }
            
            if (pawnController == null)
            {
                pawnController = GetPawnController();
            }

            // Check if it's AI's turn
            if (IsMyTurn())
            {
                StartCoroutine(TakeAITurn());
            }
        }

        /// <summary>
        /// Main AI decision making - enhanced with strategic thinking
        /// </summary>
IEnumerator TakeAITurn()
        {
            isThinking = true;
            
            // Add reasonable delay
            yield return new WaitForSeconds(Mathf.Max(0.5f, thinkDelaySeconds));

            try
            {
                bool actionTaken = false;
                
                // Try to get basic info with fallbacks
                if (TryGetBasicGameInfo(out Vector2Int myPos, out Vector2Int opponentPos, out int wallsLeft))
                {
                    LogInfo($"AI turn: MyPos={myPos}, OpponentPos={opponentPos}, WallsLeft={wallsLeft}");
                    
                    // Simple decision making
                    if (ShouldPlaceWall(wallsLeft, myPos, opponentPos))
                    {
                        actionTaken = TryPlaceSimpleWall(myPos, opponentPos);
                        LogInfo($"Wall attempt: {actionTaken}");
                    }
                    
                    if (!actionTaken)
                    {
                        actionTaken = TryMoveTowardGoal(myPos);
                        LogInfo($"Move attempt: {actionTaken}");
                    }
                }
                else
                {
                    LogInfo("Failed to get basic game info - attempting fallback move");
                    actionTaken = TryFallbackMove();
                }

                if (!actionTaken)
                {
                    LogInfo("No valid actions available - ending turn");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"AI Exception: {e.Message}");
            }

            isThinking = false;
        }

/// <summary>
        /// Robust method to get basic game information with fallbacks
        /// </summary>
        private bool TryGetBasicGameInfo(out Vector2Int myPos, out Vector2Int opponentPos, out int wallsLeft)
        {
            myPos = Vector2Int.zero;
            opponentPos = Vector2Int.zero;
            wallsLeft = 0;
            
            try
            {
                // Method 1: Try PawnManager approach
                var pawnManager = FindObjectOfType<WallChess.Gameplay.Pawns.PawnManager>();
                if (pawnManager?.Pawns != null && pawnManager.Pawns.Count >= 2)
                {
                    var aiPawn = pawnManager.Pawns.Find(p => p.PlayerData.playerType == PlayerType.AI);
                    var humanPawn = pawnManager.Pawns.Find(p => p.PlayerData.playerType == PlayerType.Human);
                    
                    if (aiPawn != null && humanPawn != null && grid != null)
                    {
                        myPos = grid.WorldToGridPosition(aiPawn.transform.position);
                        opponentPos = grid.WorldToGridPosition(humanPawn.transform.position);
                        wallsLeft = aiPawn.PlayerData.wallsRemaining;
                        
                        if (myPos != Vector2Int.zero && opponentPos != Vector2Int.zero)
                        {
                            return true;
                        }
                    }
                }
                
                // Method 2: Fallback to old method with improvements
                var allPawns = FindObjectsOfType<PawnClass>();
                if (allPawns.Length >= 2 && grid != null)
                {
                    foreach (var pawn in allPawns)
                    {
                        if (pawn.PlayerData.playerType == PlayerType.AI)
                        {
                            myPos = grid.WorldToGridPosition(pawn.transform.position);
                            wallsLeft = pawn.PlayerData.wallsRemaining;
                        }
                        else if (pawn.PlayerData.playerType == PlayerType.Human)
                        {
                            opponentPos = grid.WorldToGridPosition(pawn.transform.position);
                        }
                    }
                    
                    if (myPos != Vector2Int.zero && opponentPos != Vector2Int.zero)
                    {
                        return true;
                    }
                }
                
                return false;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error in TryGetBasicGameInfo: {e.Message}");
                return false;
            }
        }

/// <summary>
        /// Simple decision logic for when to place walls
        /// </summary>
        private bool ShouldPlaceWall(int wallsLeft, Vector2Int myPos, Vector2Int opponentPos)
        {
            // Don't place walls if we have none left
            if (wallsLeft <= 0) return false;
            
            // Simple heuristic based on difficulty and position
            float wallChance = difficulty switch
            {
                Difficulty.Beginner => 0.1f,
                Difficulty.Casual => 0.2f,
                Difficulty.Intermediate => 0.3f,
                Difficulty.Advanced => 0.4f,
                Difficulty.Expert => 0.5f,
                _ => 0.3f
            };
            
            // Increase chance if opponent is close to goal
            int opponentGoalDistance = GetDistanceToGoal(opponentPos);
            if (opponentGoalDistance <= 3)
            {
                wallChance += 0.3f;
            }
            
            // Decrease chance if we're far from goal and low on walls
            int myGoalDistance = GetDistanceToGoal(myPos);
            if (myGoalDistance > 4 && wallsLeft <= 2)
            {
                wallChance *= 0.5f;
            }
            
            return Random.value < wallChance;
        }



/// <summary>
        /// Try to place a wall that blocks opponent's direct path
        /// </summary>
/// <summary>
        /// Simplified wall placement - temporarily disabled for basic functionality
        /// </summary>
/// <summary>
        /// Try to place a wall - re-enabled with simple logic
        /// </summary>
        private bool TryPlaceSimpleWall(Vector2Int myPos, Vector2Int opponentPos)
        {
            if (wallMgr == null || grid == null) return false;
            
            try
            {
                // Strategy 1: Try to block opponent's most direct path
                var blockingPositions = GetBlockingWallPositions(opponentPos);
                foreach (var wallPos in blockingPositions)
                {
                    if (TryPlaceWallAt(wallPos))
                    {
                        LogInfo($"Placed blocking wall at {wallPos}");
                        return true;
                    }
                }
                
                // Strategy 2: Place walls near opponent to slow them down
                var nearOpponentPositions = GetNearOpponentWallPositions(opponentPos);
                foreach (var wallPos in nearOpponentPositions)
                {
                    if (TryPlaceWallAt(wallPos))
                    {
                        LogInfo($"Placed near-opponent wall at {wallPos}");
                        return true;
                    }
                }
                
                LogInfo("No valid wall positions found");
                return false;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error in TryPlaceSimpleWall: {e.Message}");
                return false;
            }
        }

/// <summary>
        /// Get potential wall positions to block opponent
        /// </summary>
        private List<Vector3> GetBlockingWallPositions(Vector2Int opponentPos)
        {
            var positions = new List<Vector3>();
            
            // Try horizontal walls in front of opponent (assuming they move up/down)
            int goalRow = GetOpponentGoalRow();
            bool movingUp = opponentPos.y > goalRow;
            
            int blockRow = movingUp ? opponentPos.y - 1 : opponentPos.y + 1;
            
            // Add horizontal walls around the blocking row
            for (int x = Mathf.Max(0, opponentPos.x - 1); x <= Mathf.Min(7, opponentPos.x); x++)
            {
                positions.Add(new Vector3(x, blockRow, 0)); // Horizontal wall
            }
            
            // Try vertical walls to the sides
            for (int y = Mathf.Max(0, opponentPos.y - 1); y <= Mathf.Min(7, opponentPos.y); y++)
            {
                if (opponentPos.x > 0)
                    positions.Add(new Vector3(opponentPos.x - 1, y, 1)); // Vertical wall on left
                if (opponentPos.x < 7)
                    positions.Add(new Vector3(opponentPos.x, y, 1)); // Vertical wall on right
            }
            
            return positions;
        }

/// <summary>
        /// Get wall positions near opponent to slow them down
        /// </summary>
        private List<Vector3> GetNearOpponentWallPositions(Vector2Int opponentPos)
        {
            var positions = new List<Vector3>();
            
            // Add walls in a small radius around opponent
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    int x = opponentPos.x + dx;
                    int y = opponentPos.y + dy;
                    
                    // Check bounds
                    if (x >= 0 && x <= 7 && y >= 0 && y <= 7)
                    {
                        // Add both horizontal and vertical possibilities
                        positions.Add(new Vector3(x, y, 0)); // Horizontal
                        positions.Add(new Vector3(x, y, 1)); // Vertical
                    }
                }
            }
            
            return positions;
        }

/// <summary>
        /// Try to place a wall at specific position
        /// </summary>
        private bool TryPlaceWallAt(Vector3 wallPos)
        {
            try
            {
                int x = (int)wallPos.x;
                int y = (int)wallPos.y;
                int orientation = (int)wallPos.z; // 0 = horizontal, 1 = vertical
                
                // Check bounds
                if (x < 0 || x > 7 || y < 0 || y > 7) return false;
                
                // Use wall manager to place wall
                GridSystem.Orientation orient = orientation == 1 ? GridSystem.Orientation.Vertical : GridSystem.Orientation.Horizontal;
                Vector3 worldPos = wallMgr?.GetWallWorldPosition(orient, x, y) ?? Vector3.zero;
                Vector3 scale = wallMgr?.GetWallScale(orient) ?? Vector3.one;
                
                if (wallMgr.CanPlaceWall(orient, x, y))
                {
                    return wallMgr.PlaceWall(orient, x, y, worldPos, scale);
                }
                
                return false;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error placing wall at {wallPos}: {e.Message}");
                return false;
            }
        }




/// <summary>
        /// Try to move toward the goal
        /// </summary>
/// <summary>
        /// Try to move toward goal - improved version
        /// </summary>
        private bool TryMoveTowardGoal(Vector2Int currentPos)
        {
            try
            {
                int goalRow = GetAIGoalRow();
                Vector2Int bestMove = currentPos;
                int bestScore = int.MaxValue;
                
                // Check all adjacent tiles
                Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
                
                foreach (var dir in directions)
                {
                    Vector2Int newPos = currentPos + dir;
                    
                    // Check if move is valid
                    if (IsValidMove(newPos))
                    {
                        // Calculate distance to goal (primary factor)
                        int distanceToGoal = Mathf.Abs(newPos.y - goalRow);
                        
                        // Add small random factor to avoid predictability
                        int randomFactor = Random.Range(0, 2);
                        int totalScore = distanceToGoal + randomFactor;
                        
                        if (totalScore < bestScore)
                        {
                            bestScore = totalScore;
                            bestMove = newPos;
                        }
                    }
                }
                
                // If we found a better position, move there
                if (bestMove != currentPos)
                {
                    return ExecuteMove(bestMove);
                }
                
                return false;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error in TryMoveTowardGoal: {e.Message}");
                return false;
            }
        }

/// <summary>
        /// Check if a move to the given position is valid
        /// </summary>
        private bool IsValidMove(Vector2Int newPos)
        {
            // Check bounds
            if (newPos.x < 0 || newPos.x > 8 || newPos.y < 0 || newPos.y > 8) return false;
            
            // Check if tile is occupied
            if (grid?.IsTileOccupied(newPos) == true) return false;
            
            // Additional validation can be added here (wall blocking, etc.)
            
            return true;
        }

/// <summary>
        /// Execute the actual move
        /// </summary>
        private bool ExecuteMove(Vector2Int targetPos)
        {
            try
            {
                // Method 1: Try using PawnManager
                var pawnManager = FindObjectOfType<WallChess.Gameplay.Pawns.PawnManager>();
                if (pawnManager?.ActivePawn != null && pawnManager.ActivePawn.PlayerData.playerType == PlayerType.AI)
                {
                    var aiPawn = pawnManager.ActivePawn;
                    Vector3 worldPos = grid.GridToWorldPosition(targetPos);
                    aiPawn.transform.position = worldPos;
                    LogInfo($"AI moved to ({targetPos.x}, {targetPos.y}) via PawnManager");
                    return true;
                }
                
                // Method 2: Try using PawnController
                if (pawnController != null)
                {
                    var allPawns = FindObjectsOfType<PawnClass>();
                    var aiPawn = System.Array.Find(allPawns, p => p.PlayerData.playerType == PlayerType.AI);
                    
                    if (aiPawn != null)
                    {
                        Vector3 worldPos = grid.GridToWorldPosition(targetPos);
                        aiPawn.transform.position = worldPos;
                        LogInfo($"AI moved to ({targetPos.x}, {targetPos.y}) via PawnController");
                        return true;
                    }
                }
                
                LogInfo("Failed to execute move - no valid pawn found");
                return false;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error executing move to {targetPos}: {e.Message}");
                return false;
            }
        }



/// <summary>
        /// Get the AI's pawn object
        /// </summary>
        private PawnClass GetAIPawn()
        {
            var pawnManager = FindObjectOfType<WallChess.Gameplay.Pawns.PawnManager>();
            if (pawnManager?.ActivePawn != null && pawnManager.ActivePawn.IsAI)
            {
                return pawnManager.ActivePawn;
            }
            return null;
        }

/// <summary>
        /// Get remaining wall count for AI
        /// </summary>
        private int GetWallsRemaining()
        {
            var aiPawn = GetAIPawn();
            return aiPawn?.PlayerData.wallsRemaining ?? 0;
        }





        #region Enhanced Strategic AI Methods
        
        /// <summary>
        /// Update game state tracking including opponent behavior analysis
        /// </summary>
        private void UpdateGameStateTracking(Vector2Int currentOpponentPos)
        {
            totalMovesPlayed++;
            
            // Track opponent movement patterns
            if (lastOpponentPosition != Vector2Int.zero && lastOpponentPosition != currentOpponentPos)
            {
                opponentMoveHistory.Add(currentOpponentPos);
                
                // Keep history manageable
                if (opponentMoveHistory.Count > 20)
                    opponentMoveHistory.RemoveAt(0);
            }
            lastOpponentPosition = currentOpponentPos;
        }
        
        /// <summary>
        /// Determine the active AI strategy based on game state and opponent behavior
        /// </summary>
        private AIStrategy DetermineActiveStrategy()
        {
            // If strategy is not Adaptive, use the set strategy
            if (strategy != AIStrategy.Adaptive)
                return strategy;
            
            // Adaptive strategy logic
            var pawnManager = FindObjectOfType<WallChess.Gameplay.Pawns.PawnManager>();
            int wallsRemaining = pawnManager?.ActivePawn?.PlayerData.wallsRemaining ?? 10;
            int opponentWallsUsed = opponentWallHistory.Count;
            
            // Early game: Use opening strategies
            if (totalMovesPlayed <= 6 && useOpeningStrategies && !hasImplementedOpening)
            {
                return ChooseOpeningStrategy();
            }
            
            // If opponent is very aggressive with walls, be defensive
            if (opponentWallsUsed > totalMovesPlayed * 0.6f)
                return AIStrategy.Defensive;
            
            // If we're significantly behind, be aggressive
            Vector2Int myPos = GetMyPosition();
            Vector2Int themPos = lastOpponentPosition;
            if (myPos != Vector2Int.zero && themPos != Vector2Int.zero)
            {
                int myDistanceToGoal = GetDistanceToGoal(myPos);
                int theirDistanceToGoal = GetDistanceToGoal(themPos);
                
                if (myDistanceToGoal - theirDistanceToGoal > 2)
                    return AIStrategy.Aggressive;
            }
            
            // Default to positional play
            return AIStrategy.Positional;
        }
        
        /// <summary>
        /// Choose appropriate opening strategy based on game state
        /// </summary>
        private AIStrategy ChooseOpeningStrategy()
        {
            // Analyze opponent's early moves to choose counter-strategy
            if (opponentMoveHistory.Count >= 2)
            {
                Vector2Int opponentMove = opponentMoveHistory[opponentMoveHistory.Count - 1];
                
                // If opponent is advancing aggressively, use Stonewall defense
                if (opponentMove.y <= 6 && totalMovesPlayed <= 4)
                    return AIStrategy.Opening_Stonewall;
                
                // If opponent moves to sides, use Reed opening
                if (Mathf.Abs(opponentMove.x - 4) >= 2)
                    return AIStrategy.Opening_Reed;
            }
            
            // Default opening based on difficulty
            switch (difficulty)
            {
                case Difficulty.Expert:
                case Difficulty.Advanced:
                    return AIStrategy.Opening_Shiller;
                case Difficulty.Intermediate:
                    return AIStrategy.Opening_Ala;
                default:
                    return AIStrategy.Positional;
            }
        }
        
        /// <summary>
        /// Analyze current game phase with more granular detail
        /// </summary>
        private GamePhaseDetailed AnalyzeGamePhase()
        {
            var pawnManager = FindObjectOfType<WallChess.Gameplay.Pawns.PawnManager>();
            int wallsRemaining = pawnManager?.ActivePawn?.PlayerData.wallsRemaining ?? 10;
            Vector2Int myPos = GetMyPosition();
            Vector2Int themPos = lastOpponentPosition;
            
            // Early opening - first few moves
            if (totalMovesPlayed <= 3)
                return GamePhaseDetailed.Opening_Early;
            
            // Mid opening - implementing strategy
            if (totalMovesPlayed <= 7 && wallsRemaining >= 6)
                return GamePhaseDetailed.Opening_Mid;
            
            // Endgame detection based on proximity to goal
            if (myPos != Vector2Int.zero && themPos != Vector2Int.zero)
            {
                int myDistanceToGoal = GetDistanceToGoal(myPos);
                int theirDistanceToGoal = GetDistanceToGoal(themPos);
                
                if (myDistanceToGoal <= 3 || theirDistanceToGoal <= 3)
                {
                    return (wallsRemaining <= 2) ? GamePhaseDetailed.Endgame_Race : GamePhaseDetailed.Endgame_Critical;
                }
            }
            
            // Midgame phases based on wall count and board tension
            return wallsRemaining >= 4 ? GamePhaseDetailed.Midgame_Control : GamePhaseDetailed.Midgame_Pressure;
        }
        
        /// <summary>
        /// Enhanced strategic wall evaluation
        /// </summary>
        bool TryBestWall(AIParams parms, Vector2Int myPos, Vector2Int themPos, int myGoalRow, int theirGoalRow)
        {
            var candidates = GetCandidateWallsNearOpponent(parms, themPos);
            if (candidates == null || candidates.Count == 0)
            {
                LogInfo("No wall candidates available");
                return false;
            }

            int baseTheirSP = ShortestPathLength(themPos, theirGoalRow);
            int baseMySP = ShortestPathLength(myPos, myGoalRow);
            
            // Calculate alternative paths for better blocking
            int theirAlternatePaths = useAdvancedPathAnalysis ? CountViableAlternativePaths(themPos, theirGoalRow) : 1;

            int bestScore = int.MinValue;
            GapDetector.WallInfo bestWall = default;
            bool found = false;
            
            var pawnManager = FindObjectOfType<WallChess.Gameplay.Pawns.PawnManager>();
            int wallsRemaining = pawnManager?.ActivePawn?.PlayerData.wallsRemaining ?? 10;

            foreach (var candidate in candidates)
            {
                int score = EvaluateWallStrategically(candidate, myPos, themPos, myGoalRow, theirGoalRow, 
                    baseTheirSP, baseMySP, theirAlternatePaths, wallsRemaining, parms);

                if (logDecisions)
                {
                    Debug.Log($"[AI] Wall at {candidate.pos}: score={score}, strategy={activeStrategy}, phase={currentGamePhase}");
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestWall = candidate;
                    found = true;
                }
            }

            // Dynamic threshold based on strategy and game state
            int dynamicThreshold = CalculateDynamicThreshold(parms, baseTheirSP, baseMySP, wallsRemaining);
            
            if (!found || bestScore < dynamicThreshold)
            {
                if (logDecisions) Debug.Log($"[AI] Best wall score {bestScore} below threshold {dynamicThreshold}");
                return false;
            }

            // Execute wall placement
            return ExecuteWallPlacement(bestWall, bestScore, pawnManager);
        }

        /// <summary>
        /// Enhanced strategic wall evaluation incorporating advanced Quoridor strategies
        /// </summary>
        private int EvaluateWallStrategically(GapDetector.WallInfo wall, Vector2Int myPos, Vector2Int themPos, 
            int myGoalRow, int theirGoalRow, int baseTheirSP, int baseMySP, int theirAlternatePaths, 
            int wallsRemaining, AIParams parms)
        {
            // Simulate wall placement
            SetWallOccupied(wall, true);
            int theirSP = ShortestPathLength(themPos, theirGoalRow);
            int mySP = ShortestPathLength(myPos, myGoalRow);
            SetWallOccupied(wall, false);

            if (theirSP < 0 || mySP < 0) return int.MinValue; // Invalid placement

            int deltaOpp = theirSP - baseTheirSP;
            int deltaMe = mySP - baseMySP;
            int score = 0;
            
            // CORE STRATEGIC SCORING based on Quoridor theory
            
            // 1. PATH MAXIMIZATION STRATEGY (from strategies document)
            // Goal: "maximizing opponent's path count and minimizing one's own path count"
            score += (int)(deltaOpp * 20 * pathMaximizationWeight); // Opponent path increase
            score -= deltaMe * 15; // Minimize our path increase
            
            // 2. ALTERNATIVE PATHS ANALYSIS
            // Walls that reduce opponent's path options are more valuable
            if (useAdvancedPathAnalysis)
            {
                int pathReduction = AnalyzePathOptionsReduction(wall, themPos, theirGoalRow, theirAlternatePaths);
                score += pathReduction * 12;
            }
            
            // 3. STRATEGY-SPECIFIC BONUSES
            switch (activeStrategy)
            {
                case AIStrategy.Opening_Reed:
                    score += EvaluateReedOpeningBonus(wall, themPos, myPos, wallsRemaining);
                    break;
                case AIStrategy.Opening_Shiller:
                    score += EvaluateShillerOpeningBonus(wall, themPos, myPos, wallsRemaining);
                    break;
                case AIStrategy.Opening_Stonewall:
                    score += EvaluateStonewallBonus(wall, themPos, myPos, wallsRemaining);
                    break;
                case AIStrategy.Opening_Ala:
                    score += EvaluateAlaOpeningBonus(wall, themPos, myPos, wallsRemaining);
                    break;
                case AIStrategy.Aggressive:
                    score += EvaluateAggressiveBonus(wall, themPos, deltaOpp, theirSP);
                    break;
                case AIStrategy.Defensive:
                    score += EvaluateDefensiveBonus(wall, myPos, deltaMe, mySP);
                    break;
                case AIStrategy.Positional:
                    score += EvaluatePositionalBonus(wall, myPos, themPos);
                    break;
            }
            
            // 4. PROXIMITY AND TIMING BONUSES
            score += EvaluateProximityAndTiming(wall, themPos, theirSP, wallsRemaining);
            
            // 5. JITTER for difficulty levels
            if (parms.wallJitter > 0)
                score += Random.Range(-parms.wallJitter, parms.wallJitter);
            
            return score;
        }

        #endregion

        #region Strategic Evaluation Methods

        private int AnalyzePathOptionsReduction(GapDetector.WallInfo wall, Vector2Int opponentPos, int goalRow, int currentAlternatePaths)
        {
            SetWallOccupied(wall, true);
            int newAlternatePaths = CountViableAlternativePaths(opponentPos, goalRow);
            SetWallOccupied(wall, false);
            
            int reduction = currentAlternatePaths - newAlternatePaths;
            return Mathf.Max(0, reduction);
        }

        private int EvaluateReedOpeningBonus(GapDetector.WallInfo wall, Vector2Int themPos, Vector2Int myPos, int wallsRemaining)
        {
            if (hasImplementedOpening || wallsRemaining < 7) return 0;
            
            // Reed opening targets third row (y=2 for opponent moving down)
            if (wall.orientation == WallState.Orientation.Horizontal && wall.y == 2)
            {
                // Prefer walls at c3h (x=2) or f3h (x=5) positions
                if (wall.x == 2 || wall.x == 5)
                {
                    hasImplementedOpening = true;
                    return 25; // Strong bonus for Reed opening implementation
                }
            }
            return 0;
        }

        private int EvaluateShillerOpeningBonus(GapDetector.WallInfo wall, Vector2Int themPos, Vector2Int myPos, int wallsRemaining)
        {
            if (hasImplementedOpening || wallsRemaining < 6) return 0;
            
            // After both players advance three times, place vertical wall
            if (totalMovesPlayed >= 6 && wall.orientation == WallState.Orientation.Vertical)
            {
                // Place vertical wall in columns closest to our position but not blocking us
                if (wall.x >= 2 && wall.x <= 5 && !WallBlocksPosition(wall, myPos))
                {
                    hasImplementedOpening = true;
                    return 30; // Strong bonus for Shiller implementation
                }
            }
            return 0;
        }

        private int EvaluateStonewallBonus(GapDetector.WallInfo wall, Vector2Int themPos, Vector2Int myPos, int wallsRemaining)
        {
            if (wallsRemaining < 5) return 0;
            
            // Build horizontal walls to create a "stonewall" defense
            if (wall.orientation == WallState.Orientation.Horizontal)
            {
                int distanceFromMyGoal = Mathf.Abs(wall.y - myPos.y);
                
                // Walls closer to our starting position create better defense
                if (distanceFromMyGoal <= 3)
                {
                    return 20 + (3 - distanceFromMyGoal) * 5; // Closer walls get higher bonus
                }
            }
            return 0;
        }

        private int EvaluateAlaOpeningBonus(GapDetector.WallInfo wall, Vector2Int themPos, Vector2Int myPos, int wallsRemaining)
        {
            if (hasImplementedOpening || wallsRemaining < 6) return 0;
            
            // Place horizontal walls behind our pawn for protection
            if (wall.orientation == WallState.Orientation.Horizontal)
            {
                int wallDistanceFromMyPos = Mathf.Abs(wall.y - myPos.y);
                
                // Walls 1-2 spaces behind our position
                if (wallDistanceFromMyPos == 1 || wallDistanceFromMyPos == 2)
                {
                    if ((myPos.y > 4 && wall.y < myPos.y) || (myPos.y <= 4 && wall.y > myPos.y))
                    {
                        hasImplementedOpening = true;
                        return 25;
                    }
                }
            }
            return 0;
        }

        private int EvaluateAggressiveBonus(GapDetector.WallInfo wall, Vector2Int themPos, int deltaOpp, int theirSP)
        {
            int bonus = 0;
            
            // Massive bonus for significant path increases
            if (deltaOpp >= 3) bonus += 30;
            else if (deltaOpp >= 2) bonus += 20;
            
            // Emergency blocking when opponent is close to winning
            if (theirSP <= 3) bonus += 40;
            else if (theirSP <= 5) bonus += 20;
            
            // Proximity bonus - aggressive walls should be close to opponent
            int distanceToOpponent = Mathf.Abs(wall.x - themPos.x) + Mathf.Abs(wall.y - themPos.y);
            if (distanceToOpponent <= 1) bonus += 15;
            else if (distanceToOpponent <= 2) bonus += 8;
            
            return bonus;
        }

        private int EvaluateDefensiveBonus(GapDetector.WallInfo wall, Vector2Int myPos, int deltaMe, int mySP)
        {
            int bonus = 0;
            
            // Penalty for walls that increase our path significantly
            if (deltaMe >= 2) return -20;
            if (deltaMe >= 1) bonus -= 5;
            
            // Bonus for walls that create alternate paths for us
            int distanceToMyPos = Mathf.Abs(wall.x - myPos.x) + Mathf.Abs(wall.y - myPos.y);
            if (distanceToMyPos >= 3) bonus += 10;
            
            // Bonus for maintaining multiple path options
            if (deltaMe == 0) bonus += 15;
            
            return bonus;
        }

        private int EvaluatePositionalBonus(GapDetector.WallInfo wall, Vector2Int myPos, Vector2Int themPos)
        {
            int bonus = 0;
            
            // Center control bonus
            int centerDistance = Mathf.Abs(wall.x - 4) + Mathf.Abs(wall.y - 4);
            if (centerDistance <= 1) bonus += 12;
            else if (centerDistance <= 2) bonus += 6;
            
            // Edge control bonus
            if (wall.x <= 1 || wall.x >= 7 || wall.y <= 1 || wall.y >= 7)
                bonus += 8;
            
            return bonus;
        }

        private int EvaluateProximityAndTiming(GapDetector.WallInfo wall, Vector2Int themPos, int theirSP, int wallsRemaining)
        {
            int bonus = 0;
            
            // Proximity bonus - walls closer to opponent are more disruptive
            int distanceToOpponent = Mathf.Abs(wall.x - themPos.x) + Mathf.Abs(wall.y - themPos.y);
            if (distanceToOpponent <= 1) bonus += 20;
            else if (distanceToOpponent <= 2) bonus += 12;
            else if (distanceToOpponent <= 3) bonus += 6;
            
            // Timing bonus - walls are more valuable when we have fewer left
            if (wallsRemaining <= 3) bonus += 8;
            else if (wallsRemaining <= 5) bonus += 4;
            
            // Urgency bonus - if opponent is close to winning
            if (theirSP <= 2) bonus += 25;
            else if (theirSP <= 4) bonus += 15;
            else if (theirSP <= 6) bonus += 8;
            
            return bonus;
        }

        #endregion

        #region Helper Methods

        private Vector2Int GetMyPosition()
        {
            var controller = GetPawnController();
            if (controller == null) return Vector2Int.zero;
            
            // Get the pawn from PawnController - using gameObject instead of pawn property
            GameObject pawnObject = controller.gameObject;
            if (pawnObject == null) return Vector2Int.zero;
            
            Vector3 worldPos = pawnObject.transform.position;
            return grid?.WorldToGridPosition(worldPos) ?? Vector2Int.zero;
        }

        private Vector2Int GetOpponentPosition()
        {
            // Find opponent pawn - look for pawn that's not controlled by this AI
            var allPawns = FindObjectsOfType<PawnClass>();
            var myController = GetPawnController();
            
            foreach (var pawn in allPawns)
            {
                // Skip our own pawn
                if (myController != null && pawn.gameObject == myController.gameObject)
                    continue;
                
                return grid?.WorldToGridPosition(pawn.transform.position) ?? Vector2Int.zero;
            }
            return Vector2Int.zero;
        }

        private int GetDistanceToGoal(Vector2Int pos)
        {
            var pawnManager = FindObjectOfType<WallChess.Gameplay.Pawns.PawnManager>();
            if (pawnManager?.ActivePawn == null) return 999;
            
            bool movingUp = pawnManager.ActivePawn.PlayerData.playerType == PlayerType.AI;
            int goalRow = movingUp ? 0 : 8;
            
            return Mathf.Abs(pos.y - goalRow);
        }

        private int CountViableAlternativePaths(Vector2Int start, int goalRow)
        {
            if (!useAdvancedPathAnalysis) return 1;
            
            var controller = GetPawnController();
            if (controller == null) return 1;
            
            var pathCount = 0;
            var immediateNextMoves = controller.GetValidMoves(start);
            
            if (immediateNextMoves != null)
            {
                foreach (var nextMove in immediateNextMoves)
                {
                    if (Mathf.Abs(nextMove.y - goalRow) < Mathf.Abs(start.y - goalRow))
                    {
                        pathCount++;
                    }
                }
            }
            
            return Mathf.Max(1, pathCount);
        }

        private bool WallBlocksPosition(GapDetector.WallInfo wall, Vector2Int position)
        {
            if (wall.orientation == WallState.Orientation.Horizontal)
            {
                return (position.x == wall.x || position.x == wall.x + 1) && 
                       Mathf.Abs(position.y - wall.y) <= 1;
            }
            else
            {
                return (position.y == wall.y || position.y == wall.y + 1) && 
                       Mathf.Abs(position.x - wall.x) <= 1;
            }
        }

        private int CalculateDynamicThreshold(AIParams parms, int theirSP, int mySP, int wallsRemaining)
        {
            int baseThreshold = parms.wallThresholdScore;
            
            // Lower threshold in critical situations
            if (theirSP <= 3) baseThreshold -= 12;
            else if (theirSP <= 5) baseThreshold -= 6;
            
            // Adjust based on wall scarcity
            if (wallsRemaining <= 2) baseThreshold -= 8;
            else if (wallsRemaining <= 4) baseThreshold -= 4;
            
            // Adjust based on our position
            if (mySP - theirSP > 3) baseThreshold += 5;
            else if (theirSP - mySP > 3) baseThreshold -= 5;
            
            return baseThreshold;
        }

        private bool ExecuteWallPlacement(GapDetector.WallInfo bestWall, int bestScore, 
            WallChess.Gameplay.Pawns.PawnManager pawnManager)
        {
            // Use Player Action FSM for AI wall placement
            var activePawn = pawnManager?.ActivePawn;
            if (actionStateMachine != null && actionStateMachine.IsIdle() && activePawn != null)
            {
                actionStateMachine.Initialize(activePawn);
                actionStateMachine.StartWallPlacement(bestWall.pos);
            }
            
            // Place wall using WallManager
            bool placed = false;
            if (wallMgr != null)
            {
                placed = wallMgr.TryPlaceWall(bestWall.pos);
            }
            else
            {
                Debug.LogError("[AI] WallManager not available for wall placement!");
                return false;
            }
            
            if (placed)
            {
                if (logDecisions) Debug.Log($"[AI] *** STRATEGIC WALL PLACED *** at {bestWall.pos} (score {bestScore}, strategy {activeStrategy}, phase {currentGamePhase})");
                
                // Track our wall placement
                opponentWallHistory.Add(bestWall.pos);
                
                // Confirm action through FSM
                if (actionStateMachine != null)
                    actionStateMachine.ConfirmAction();
            }
            else
            {
                if (logDecisions) Debug.Log("[AI] Wall placement failed; will try to move instead.");
                
                // Cancel action if placement failed
                if (actionStateMachine != null)
                    actionStateMachine.CancelAction();
            }
            
            return placed;
        }

        #endregion

        #region Core AI Methods

        private bool IsMyTurn()
        {
            var pawnManager = FindObjectOfType<WallChess.Gameplay.Pawns.PawnManager>();
            if (pawnManager?.ActivePawn == null) return false;
            
            return pawnManager.ActivePawn.PlayerData.playerType == PlayerType.AI;
        }

        private PawnController GetPawnController()
        {
            if (pawnController == null)
            {
                pawnController = GetComponent<PawnController>();
            }
            return pawnController;
        }

        private void LogInfo(string message)
        {
            if (logDecisions) Debug.Log($"[AIOpponent] {message}");
        }

        private int GetMyGoalRow()
        {
            // Simplified - AI typically moves to row 0
            return 0;
        }

        private int GetOpponentGoalRow()
        {
            // Simplified - opponent typically moves to row 8
            return 8;
        }

        private int ShortestPathLength(Vector2Int from, int goalRow)
        {
            // Simplified pathfinding - just Manhattan distance
            return Mathf.Abs(from.y - goalRow) + Random.Range(0, 2); // Add small randomness
        }

        private bool TryBestMove(AIParams parms, Vector2Int myPos, Vector2Int themPos, int myGoalRow, int theirGoalRow)
        {
            // Simplified move logic - try to move toward goal
            Vector2Int goalDirection = new Vector2Int(0, myPos.y > myGoalRow ? -1 : 1);
            Vector2Int targetPos = myPos + goalDirection;
            
            // Use PawnController to attempt move
            var controller = GetPawnController();
            if (controller != null)
            {
                var validMoves = controller.GetValidMoves(myPos);
                if (validMoves != null && validMoves.Contains(targetPos))
                {
                    // Use the PawnController's move functionality
                    // This is a simplified approach - in practice you'd use the actual move method
                    if (logDecisions) Debug.Log($"[AI] Attempting to move to {targetPos}");
                    
                    // For now, we'll assume the move succeeds if it's valid
                    // In a complete implementation, this would call the actual movement method
                    return true;
                }
                else if (validMoves != null && validMoves.Count > 0)
                {
                    // Move to any valid position
                    if (logDecisions) Debug.Log($"[AI] Moving to alternate position {validMoves[0]}");
                    return true;
                }
            }
            
            return false;
        }

        private List<GapDetector.WallInfo> GetCandidateWallsNearOpponent(AIParams parms, Vector2Int opponentPos)
        {
            var candidates = new List<GapDetector.WallInfo>();
            
            // Generate wall candidates around opponent
            for (int x = Mathf.Max(0, opponentPos.x - parms.scanRadius); 
                 x <= Mathf.Min(8, opponentPos.x + parms.scanRadius); x++)
            {
                for (int y = Mathf.Max(0, opponentPos.y - parms.scanRadius); 
                     y <= Mathf.Min(8, opponentPos.y + parms.scanRadius); y++)
                {
                    // Try both orientations
                    foreach (var orientation in new[] { WallState.Orientation.Horizontal, WallState.Orientation.Vertical })
                    {
                        if (CanPlaceWallAt(x, y, orientation))
                        {
                            Vector3 worldPos = grid?.GridToWorldPosition(new Vector2Int(x, y)) ?? Vector3.zero;
                            candidates.Add(new GapDetector.WallInfo
                            {
                                x = x,
                                y = y,
                                pos = worldPos,
                                orientation = orientation
                            });
                        }
                    }
                }
            }
            
            return candidates;
        }

        private bool CanPlaceWallAt(int x, int y, WallState.Orientation orientation)
        {
            // Simplified wall placement check
            if (orientation == WallState.Orientation.Horizontal)
            {
                return x >= 0 && x < 8 && y >= 0 && y < 9;
            }
            else
            {
                return x >= 0 && x < 9 && y >= 0 && y < 8;
            }
        }

        private void SetWallOccupied(GapDetector.WallInfo wall, bool occupied)
        {
            // Simplified implementation - in a full version, this would interact with the grid system
            // For now, we'll just track this conceptually
        }

        private bool ShouldTryWall(AIParams parms, int mySP, int theirSP)
        {
            // Enhanced wall decision logic
            var pawnManager = FindObjectOfType<WallChess.Gameplay.Pawns.PawnManager>();
            int wallsRemaining = pawnManager?.ActivePawn?.PlayerData.wallsRemaining ?? 10;
            
            float baseChance = 0.3f; // Base chance to try walls
            
            // Adjust based on game state
            if (theirSP <= 3) baseChance += 0.4f; // Emergency blocking
            if (wallsRemaining <= 2) baseChance -= 0.2f; // Conserve walls
            if (activeStrategy == AIStrategy.Aggressive) baseChance += 0.3f;
            if (activeStrategy == AIStrategy.Defensive) baseChance -= 0.2f;
            
            return Random.value < baseChance;
        }

        private AIParams GetParams(Difficulty d)
        {
            switch (d)
            {
                case Difficulty.Beginner:
                    return new AIParams { wallThresholdScore = 12, moveJitter = 3, wallJitter = 6, wallBias = 0.20f, scanRadius = 2 };
                case Difficulty.Casual:
                    return new AIParams { wallThresholdScore = 14, moveJitter = 2, wallJitter = 4, wallBias = 0.30f, scanRadius = 3 };
                case Difficulty.Intermediate:
                    return new AIParams { wallThresholdScore = 16, moveJitter = 1, wallJitter = 2, wallBias = 0.40f, scanRadius = 3 };
                case Difficulty.Advanced:
                    return new AIParams { wallThresholdScore = 18, moveJitter = 0, wallJitter = 1, wallBias = 0.50f, scanRadius = 4 };
                default: // Expert
                    return new AIParams { wallThresholdScore = 20, moveJitter = 0, wallJitter = 0, wallBias = 0.60f, scanRadius = 4 };
            }
        }

        #endregion

        #region Event Handlers

        private void OnAIActionConfirmed()
        {
            LogInfo("AI action confirmed");
        }

        private void OnAIActionCancelled()
        {
            LogInfo("AI action cancelled");
        }

        private void OnDestroy()
        {
            if (actionStateMachine != null)
            {
                actionStateMachine.OnActionConfirmed -= OnAIActionConfirmed;
                actionStateMachine.OnActionCancelled -= OnAIActionCancelled;
            }
        }

        #endregion
    

/// <summary>
        /// Fallback move when all else fails
        /// </summary>
        private bool TryFallbackMove()
        {
            try
            {
                // Just try to move any AI pawn in any valid direction
                var allPawns = FindObjectsOfType<PawnClass>();
                var aiPawn = System.Array.Find(allPawns, p => p.PlayerData.playerType == PlayerType.AI);
                
                if (aiPawn != null && grid != null)
                {
                    Vector2Int currentPos = grid.WorldToGridPosition(aiPawn.transform.position);
                    Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
                    
                    foreach (var dir in directions)
                    {
                        Vector2Int newPos = currentPos + dir;
                        if (IsValidMove(newPos))
                        {
                            Vector3 worldPos = grid.GridToWorldPosition(newPos);
                            aiPawn.transform.position = worldPos;
                            LogInfo($"Fallback move to ({newPos.x}, {newPos.y})");
                            return true;
                        }
                    }
                }
                
                return false;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error in TryFallbackMove: {e.Message}");
                return false;
            }
        }


/// <summary>
        /// Get the goal row for AI player
        /// </summary>
        private int GetAIGoalRow()
        {
            // In Quoridor, AI typically tries to reach row 0 (top)
            return 0;
        }
    }
}