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

            // Test if both players still have paths to their goals
            bool playerHasPath = gridSystem.PathExists(gameManager.playerPosition, GetPlayerGoalPosition(true));
            bool opponentHasPath = gridSystem.PathExists(gameManager.opponentPosition, GetPlayerGoalPosition(false));

            // FIXED: Remove temporary wall using the new GridSystem method
            gridSystem.RemoveWallOccupancy(orientation, x, y);

            return !(playerHasPath && opponentHasPath);
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
        
        private Vector2Int GetPlayerGoalPosition(bool isPlayer)
        {
            int goalY = isPlayer ? gameManager.gridSize - 1 : 0;
            return new Vector2Int(gameManager.gridSize / 2, goalY);
        }

        // Removed old pathfinding methods - now using GridSystem.PathExists() and GridSystem.FindPath()
        // This provides better integration with the unified grid system
    }
}
