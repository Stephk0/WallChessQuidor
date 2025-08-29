using UnityEngine;
using System.Collections.Generic;

namespace WallChess
{
    /// <summary>
    /// Validates wall placements and path constraints using GridSystem.
    /// </summary>
    public class WallValidator
    {
        private readonly GridSystem gridSystem;
        private readonly WallChessGameManager gameManager;

        public WallValidator(GridSystem grid, WallChessGameManager gm)
        {
            gridSystem = grid;
            gameManager = gm;
        }

        public bool CanPlace(GridSystem.Orientation orientation, int x, int y)
        {
            if (gameManager == null) return false;
            if (!gameManager.CanPlaceWalls()) return false;
            if (!gameManager.CurrentPlayerHasWalls()) return false;

            // Check basic placement validity
            if (!gridSystem.CanPlaceWall(orientation, x, y)) return false;

            // Check if wall would cross existing walls
            if (WouldCross(orientation, x, y)) return false;
            
            // Check if wall would block all paths for any player
            if (WouldBlockPaths(orientation, x, y)) return false;

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

        bool WouldCross(GridSystem.Orientation orientation, int x, int y)
        {
            // With intersection tracking in GridSystem.CanPlaceWall(), crossing is automatically detected
            // This method is now primarily for legacy compatibility
            return false; // CanPlaceWall() handles intersection checking
        }
        
        // Legacy method for compatibility
        bool WouldCross(GapDetector.WallInfo w)
        {
            return WouldCross(
                w.orientation == WallState.Orientation.Horizontal ? GridSystem.Orientation.Horizontal : GridSystem.Orientation.Vertical,
                w.x, w.y
            );
        }

        bool WouldBlockPaths(GridSystem.Orientation orientation, int x, int y)
        {
            // Double check: Don't even test if wall can't be placed
            if (!gridSystem.CanPlaceWall(orientation, x, y))
                return true; // Already blocked/occupied
            
            // Temporarily place the wall in GridSystem WITHOUT triggering events
            Vector3 dummyPos = Vector3.zero;
            Vector3 dummyScale = Vector3.one;
            var wallInfo = new GridSystem.WallInfo(orientation, x, y, dummyPos, dummyScale);
            
            // FIXED: Place wall temporarily without triggering OnWallPlaced event
            bool placed = gridSystem.PlaceWall(wallInfo, false); // false = don't trigger events
            if (!placed) 
            {
                Debug.LogWarning($"WouldBlockPaths: Failed to place temporary wall at {orientation} ({x},{y}) - this should not happen after CanPlaceWall check");
                return true; // Can't place means blocked
            }

            // FIXED: Test if ALL players still have paths to ANY tile on their target side
            bool anyPlayerBlocked = false;
            
            foreach (var pawn in gameManager.pawns)
            {
                if (!HasPathToTargetSide(pawn))
                {
                    anyPlayerBlocked = true;
                    break;
                }
            }

            // FIXED: Remove temporary wall using the new GridSystem method
            gridSystem.RemoveWallOccupancy(orientation, x, y);

            return anyPlayerBlocked;
        }
        
        // Legacy method for compatibility  
        bool WouldBlockPaths(GapDetector.WallInfo w)
        {
            return WouldBlockPaths(
                w.orientation == WallState.Orientation.Horizontal ? GridSystem.Orientation.Horizontal : GridSystem.Orientation.Vertical,
                w.x, w.y
            );
        }
        
        // REMOVED: ClearTemporaryWall method - now using GridSystem.RemoveWallOccupancy()
        
        /// <summary>
        /// FIXED: Check if a pawn has a path to ANY tile on their target side
        /// According to Quoridor rules, players can reach any square on the opposite side
        /// </summary>
        private bool HasPathToTargetSide(WallChessGameManager.PawnData pawn)
        {
            Vector2Int currentPos = pawn.position;
            int targetRow = GetTargetRow(pawn);
            int gridSize = gameManager.gridSize;
            
            // For side-to-side movement (4-player games), check target column instead
            if (IsHorizontalMovement(pawn))
            {
                int targetCol = GetTargetColumn(pawn);
                // Test if there's a path to ANY tile on the target column
                for (int row = 0; row < gridSize; row++)
                {
                    Vector2Int targetTile = new Vector2Int(targetCol, row);
                    
                    // Skip if target tile is occupied by another pawn
                    if (gridSystem.IsTileOccupied(targetTile))
                    {
                        // Check if it's occupied by this same pawn (already at goal)
                        if (targetTile == currentPos)
                            return true;
                        continue;
                    }
                    
                    // Check if path exists to this target tile
                    if (gridSystem.PathExists(currentPos, targetTile))
                    {
                        return true; // Found at least one reachable tile on target side
                    }
                }
            }
            else
            {
                // Test if there's a path to ANY tile on the target row
                for (int col = 0; col < gridSize; col++)
                {
                    Vector2Int targetTile = new Vector2Int(col, targetRow);
                    
                    // Skip if target tile is occupied by another pawn
                    if (gridSystem.IsTileOccupied(targetTile))
                    {
                        // Check if it's occupied by this same pawn (already at goal)
                        if (targetTile == currentPos)
                            return true;
                        continue;
                    }
                    
                    // Check if path exists to this target tile
                    if (gridSystem.PathExists(currentPos, targetTile))
                    {
                        return true; // Found at least one reachable tile on target side
                    }
                }
            }
            
            return false; // No reachable tiles on target side
        }
        
        /// <summary>
        /// FIXED: Get the target row for a given pawn based on their starting position
        /// Player starting at bottom (y=0) needs to reach top (y=gridSize-1)
        /// Player starting at top (y=gridSize-1) needs to reach bottom (y=0)
        /// </summary>
        private int GetTargetRow(WallChessGameManager.PawnData pawn)
        {
            // If pawn started at bottom row, target is top row
            if (pawn.startPosition.y == 0)
                return gameManager.gridSize - 1;
            
            // If pawn started at top row, target is bottom row
            if (pawn.startPosition.y == gameManager.gridSize - 1)
                return 0;
            
            // For 4-player games with side positions, use middle as default target
            return pawn.startPosition.y == 0 ? gameManager.gridSize - 1 : 0;
        }
        
        /// <summary>
        /// Check if pawn movement is horizontal (left-to-right or right-to-left)
        /// This applies to 4-player games where some pawns start on the sides
        /// </summary>
        private bool IsHorizontalMovement(WallChessGameManager.PawnData pawn)
        {
            // Pawns starting at left or right edges move horizontally
            return pawn.startPosition.x == 0 || pawn.startPosition.x == gameManager.gridSize - 1;
        }
        
        /// <summary>
        /// Get the target column for horizontal-moving pawns (4-player games)
        /// </summary>
        private int GetTargetColumn(WallChessGameManager.PawnData pawn)
        {
            // If pawn started at left edge, target is right edge
            if (pawn.startPosition.x == 0)
                return gameManager.gridSize - 1;
            
            // If pawn started at right edge, target is left edge
            if (pawn.startPosition.x == gameManager.gridSize - 1)
                return 0;
            
            // Default fallback
            return pawn.startPosition.x == 0 ? gameManager.gridSize - 1 : 0;
        }
        
        private Vector2Int GetPlayerGoalPosition(bool isPlayer)
        {
            int goalY = isPlayer ? gameManager.gridSize - 1 : 0;
            return new Vector2Int(gameManager.gridSize / 2, goalY);
        }

        // Removed old pathfinding methods - now using GridSystem.PathExists() and GridSystem.FindPath()
        // This provides better integration with the unified grid system
    }
}
