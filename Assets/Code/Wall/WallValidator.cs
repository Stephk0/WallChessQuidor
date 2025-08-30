using UnityEngine;
using System.Collections.Generic;
using WallChess.Grid;
// Force recompile trigger

namespace WallChess
{
    /// <summary>
    /// Validates wall placements and path constraints using GridSystem.
    /// REFACTORED: Now uses on-demand pathfinding validation when walls are placed.
    /// No more pre-calculation - checks paths immediately when needed.
    /// </summary>
    public class WallValidator
    {
        private readonly GridSystem gridSystem;
        private readonly WallChessGameManager gameManager;

        public WallValidator(GridSystem grid, WallChessGameManager gm)
        {
            gridSystem = grid;
            gameManager = gm;
            Debug.Log("WallValidator initialized with on-demand pathfinding validation");
        }

        /// <summary>
        /// Check if a wall can be placed at the given position
        /// Uses on-demand pathfinding validation
        /// </summary>
        public bool CanPlace(GridSystem.Orientation orientation, int x, int y)
        {
            if (gameManager == null) return false;
            if (!gameManager.CanPlaceWalls()) return false;
            if (!gameManager.CurrentPlayerHasWalls()) return false;

            // Check basic placement validity (already occupied by other walls)
            if (!gridSystem.CanPlaceWall(orientation, x, y)) return false;
            
            // NEW: Check if wall would block all paths for any player using temporary placement
            if (WouldBlockAllPaths(orientation, x, y)) return false;

            return true;
        }
        
        // Legacy method for compatibility with GapDetector.WallInfo
        public bool CanPlace(GapDetector.WallInfo w)
        {
            return CanPlace(
                w.orientation == WallState.Orientation.Horizontal ? GridSystem.Orientation.Horizontal : GridSystem.Orientation.Vertical,
                w.x, w.y
            );
        }

        /// <summary>
        /// NEW: Test if placing a wall would block all paths for any player
        /// Uses temporary wall placement and immediate pathfinding validation
        /// </summary>
        private bool WouldBlockAllPaths(GridSystem.Orientation orientation, int x, int y)
        {
            // Temporarily place the wall without triggering events
            Vector3 dummyPos = Vector3.zero;
            Vector3 dummyScale = Vector3.one;
            var wallInfo = new GridSystem.WallInfo(orientation, x, y, dummyPos, dummyScale);
            
            // Place wall temporarily without triggering OnWallPlaced event
            bool placed = gridSystem.PlaceWall(wallInfo, false);
            if (!placed) 
            {
                return true; // Can't place means it would interfere with existing walls
            }

            // Test if ALL players still have paths to their target sides
            bool anyPlayerBlocked = !GridPathfinder.AllPawnsHaveValidPaths(gridSystem, gameManager);

            // Remove temporary wall immediately
            gridSystem.RemoveWallOccupancy(orientation, x, y);

            if (anyPlayerBlocked)
            {
                Debug.Log($"Wall at {orientation} ({x},{y}) would block player paths - placement denied");
            }

            return anyPlayerBlocked;
        }
        
        // Legacy method for compatibility  
        private bool WouldBlockPaths(GapDetector.WallInfo w)
        {
            return WouldBlockAllPaths(
                w.orientation == WallState.Orientation.Horizontal ? GridSystem.Orientation.Horizontal : GridSystem.Orientation.Vertical,
                w.x, w.y
            );
        }
        
        /// <summary>
        /// NEW: Validate all current pawn paths after a wall has been placed
        /// Called after wall placement to ensure game state is still valid
        /// </summary>
        public bool ValidateAllPawnPaths()
        {
            return GridPathfinder.AllPawnsHaveValidPaths(gridSystem, gameManager);
        }
        
        /// <summary>
        /// NEW: Get shortest path length for a specific pawn to their goal
        /// Returns -1 if no path exists
        /// </summary>
        public int GetShortestPathLength(WallChessGameManager.PawnData pawn)
        {
            if (pawn == null || gameManager == null) return -1;
            
            Vector2Int currentPos = pawn.position;
            List<Vector2Int> goalTiles = GetGoalTiles(pawn);
            
            int shortestLength = int.MaxValue;
            bool foundPath = false;
            
            // Find shortest path to any goal tile
            foreach (Vector2Int goalTile in goalTiles)
            {
                // Skip if target tile is occupied by another pawn
                if (gridSystem.IsTileOccupied(goalTile) && goalTile != currentPos)
                    continue;
                
                int pathLength = GridPathfinder.GetPathLength(gridSystem, currentPos, goalTile);
                if (pathLength >= 0 && pathLength < shortestLength)
                {
                    shortestLength = pathLength;
                    foundPath = true;
                }
            }
            
            return foundPath ? shortestLength : -1;
        }
        
        /// <summary>
        /// NEW: Get all possible goal tiles for a pawn based on their starting position
        /// </summary>
        private List<Vector2Int> GetGoalTiles(WallChessGameManager.PawnData pawn)
        {
            List<Vector2Int> goalTiles = new List<Vector2Int>();
            Vector2Int startPos = pawn.startPosition;
            int gridSize = gameManager.gridSize;
            
            if (startPos.y == 0) // Started at bottom row (y=0)
            {
                // Goal is any tile on top row (y=gridSize-1)
                for (int x = 0; x < gridSize; x++)
                {
                    goalTiles.Add(new Vector2Int(x, gridSize - 1));
                }
            }
            else if (startPos.y == gridSize - 1) // Started at top row (y=gridSize-1)
            {
                // Goal is any tile on bottom row (y=0)
                for (int x = 0; x < gridSize; x++)
                {
                    goalTiles.Add(new Vector2Int(x, 0));
                }
            }
            else if (startPos.x == 0) // Started at left column (x=0)
            {
                // Goal is any tile on right column (x=gridSize-1)
                for (int y = 0; y < gridSize; y++)
                {
                    goalTiles.Add(new Vector2Int(gridSize - 1, y));
                }
            }
            else if (startPos.x == gridSize - 1) // Started at right column (x=gridSize-1)
            {
                // Goal is any tile on left column (x=0)
                for (int y = 0; y < gridSize; y++)
                {
                    goalTiles.Add(new Vector2Int(0, y));
                }
            }
            
            return goalTiles;
        }
        
        #region Debug Methods
        
        /// <summary>
        /// Debug method to test pathfinding for all pawns
        /// </summary>
        public void DebugPrintAllPawnPaths()
        {
            Debug.Log("=== PAWN PATHFINDING DEBUG ===");
            
            if (gameManager?.pawns == null)
            {
                Debug.Log("No pawns found in gameManager");
                return;
            }
            
            for (int i = 0; i < gameManager.pawns.Count; i++)
            {
                var pawn = gameManager.pawns[i];
                int pathLength = GetShortestPathLength(pawn);
                
                Debug.Log($"Pawn {i} at {pawn.position}: " + 
                         (pathLength >= 0 ? $"Shortest path = {pathLength} moves" : "No path available"));
            }
        }
        
        /// <summary>
        /// Debug method to validate current game state
        /// </summary>
        public void DebugValidateGameState()
        {
            bool allValid = ValidateAllPawnPaths();
            Debug.Log($"Game state validation: {(allValid ? "VALID" : "INVALID - Some pawns have no path to goal")}");
            
            if (!allValid)
            {
                DebugPrintAllPawnPaths();
            }
        }
        
        #endregion
    }
}
