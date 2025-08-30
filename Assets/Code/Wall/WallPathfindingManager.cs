using UnityEngine;
using System.Collections.Generic;
using WallChess.Grid;

namespace WallChess.Wall
{
    /// <summary>
    /// DEPRECATED: Wall pathfinding manager with pre-calculation approach.
    /// 
    /// This class is now deprecated in favor of on-demand pathfinding validation
    /// performed directly in WallValidator and GridPathfinder.
    /// 
    /// The new approach:
    /// - Uses temporary wall placement to test pathfinding
    /// - Validates paths immediately when walls are placed
    /// - No pre-calculation or caching needed
    /// - More accurate and eliminates timing issues
    /// 
    /// Use GridPathfinder.AllPawnsHaveValidPaths() instead of this class.
    /// </summary>
    [System.Obsolete("WallPathfindingManager is deprecated. Use GridPathfinder.AllPawnsHaveValidPaths() and WallValidator for on-demand pathfinding validation instead.")]
    public class WallPathfindingManager
    {
        private readonly GridSystem gridSystem;
        private readonly WallChessGameManager gameManager;

        public WallPathfindingManager(GridSystem grid, WallChessGameManager gm)
        {
            gridSystem = grid;
            gameManager = gm;
            
            Debug.LogWarning("WallPathfindingManager is deprecated. Please update your code to use the new on-demand pathfinding approach in GridPathfinder and WallValidator.");
        }

        #region Deprecated API - For Compatibility Only

        [System.Obsolete("Use WallValidator.CanPlace() which performs on-demand validation instead.")]
        public bool WouldBlockPaths(GridSystem.Orientation orientation, int x, int y)
        {
            // Fallback to new on-demand validation approach
            return !GridPathfinder.AllPawnsHaveValidPaths(gridSystem, gameManager);
        }

        [System.Obsolete("Pre-calculation is no longer used. Pathfinding is now validated on-demand.")]
        public void RecalculateBlockedGaps()
        {
            Debug.LogWarning("RecalculateBlockedGaps() is deprecated. The new system validates paths on-demand without pre-calculation.");
        }

        [System.Obsolete("Pre-calculation is no longer used. Use on-demand pathfinding instead.")]
        public void InvalidatePathfindingData()
        {
            Debug.LogWarning("InvalidatePathfindingData() is deprecated. The new system doesn't cache pathfinding data.");
        }

        [System.Obsolete("Pre-calculation is no longer used. Use GridPathfinder.AllPawnsHaveValidPaths() instead.")]
        public HashSet<Vector2Int> GetBlockedHorizontalGaps()
        {
            Debug.LogWarning("GetBlockedHorizontalGaps() is deprecated. Use WallValidator.CanPlace() for validation instead.");
            return new HashSet<Vector2Int>();
        }

        [System.Obsolete("Pre-calculation is no longer used. Use GridPathfinder.AllPawnsHaveValidPaths() instead.")]
        public HashSet<Vector2Int> GetBlockedVerticalGaps()
        {
            Debug.LogWarning("GetBlockedVerticalGaps() is deprecated. Use WallValidator.CanPlace() for validation instead.");
            return new HashSet<Vector2Int>();
        }

        [System.Obsolete("Pre-calculation is no longer used.")]
        public bool NeedsUpdate()
        {
            return false; // Always false since we don't pre-calculate anymore
        }

        [System.Obsolete("Use WallValidator.DebugPrintAllPawnPaths() instead.")]
        public void DebugPrintBlockedGaps()
        {
            Debug.Log("=== DEPRECATED METHOD ===");
            Debug.Log("DebugPrintBlockedGaps() is deprecated.");
            Debug.Log("Use WallValidator.DebugPrintAllPawnPaths() for pathfinding debug information.");
            Debug.Log("Use WallValidator.DebugValidateGameState() to validate current game state.");
        }

        public void Dispose()
        {
            // Nothing to dispose since we don't maintain state anymore
        }

        #endregion
    }
}
