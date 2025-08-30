using System.Collections.Generic;
using UnityEngine;

namespace WallChess.Grid
{
    public static class GridPathfinder
    {
        private class PathNode
        {
            public Vector2Int position;
            public PathNode parent;
            public float gCost; // Distance from start
            public float hCost; // Heuristic distance to end
            public float FCost => gCost + hCost;
            
            public PathNode(Vector2Int pos)
            {
                position = pos;
                parent = null;
                gCost = 0;
                hCost = 0;
            }
        }
        
        /// <summary>
        /// Find path between two tile positions using A* on unified grid
        /// Pawns move 2 cells at a time (crossing gaps)
        /// FIXED: Proper boundary checking and wall detection
        /// UPDATED: Ignores pawn occupancy, only considers walls as blocking
        /// </summary>
        public static bool PathExists(GridSystem gridSystem, Vector2Int fromTile, Vector2Int toTile)
        {
            var path = FindPath(gridSystem, fromTile, toTile);
            return path != null && path.Count > 0;
        }
        
        /// <summary>
        /// Find the shortest path between two tile positions
        /// Returns null if no path exists
        /// FIXED: Proper boundary and wall checking
        /// UPDATED: Ignores pawn occupancy, only considers walls as blocking
        /// </summary>
        public static List<Vector2Int> FindPath(GridSystem gridSystem, Vector2Int fromTile, Vector2Int toTile)
        {
            // FIXED: Validate tile positions first
            if (!IsValidTilePosition(gridSystem, fromTile) || !IsValidTilePosition(gridSystem, toTile))
            {
                Debug.LogWarning($"GridPathfinder: Invalid tile positions - from:{fromTile}, to:{toTile}");
                return null;
            }
            
            // Convert to unified positions
            Vector2Int startUnified = gridSystem.TileToUnifiedPosition(fromTile);
            Vector2Int endUnified = gridSystem.TileToUnifiedPosition(toTile);
            
            // A* algorithm
            List<PathNode> openSet = new List<PathNode>();
            HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();
            
            PathNode startNode = new PathNode(startUnified);
            startNode.hCost = GetDistance(startUnified, endUnified);
            openSet.Add(startNode);
            
            while (openSet.Count > 0)
            {
                // Get node with lowest F cost
                PathNode currentNode = openSet[0];
                for (int i = 1; i < openSet.Count; i++)
                {
                    if (openSet[i].FCost < currentNode.FCost || 
                        (openSet[i].FCost == currentNode.FCost && openSet[i].hCost < currentNode.hCost))
                    {
                        currentNode = openSet[i];
                    }
                }
                
                openSet.Remove(currentNode);
                closedSet.Add(currentNode.position);
                
                // Check if we reached the target
                if (currentNode.position == endUnified)
                {
                    return RetracePath(gridSystem, startNode, currentNode);
                }
                
                // Check neighbors (4 directions, moving 2 cells at a time)
                Vector2Int[] directions = {
                    new Vector2Int(0, 2),   // Up
                    new Vector2Int(0, -2),  // Down
                    new Vector2Int(2, 0),   // Right
                    new Vector2Int(-2, 0)   // Left
                };
                
                foreach (Vector2Int dir in directions)
                {
                    Vector2Int neighborPos = currentNode.position + dir;
                    Vector2Int gapPos = currentNode.position + (dir / 2);
                    
                    // Skip if already visited
                    if (closedSet.Contains(neighborPos))
                        continue;
                    
                    // FIXED: Check if neighbor is valid tile position within bounds
                    Vector2Int neighborTilePos = gridSystem.UnifiedToTilePosition(neighborPos);
                    if (!IsValidTilePosition(gridSystem, neighborTilePos))
                        continue;
                    
                    // Check if neighbor unified position is valid
                    if (!gridSystem.IsValidUnifiedPosition(neighborPos))
                        continue;
                    
                    GridSystem.GridCell neighborCell = gridSystem.GetCell(neighborPos);
                    if (neighborCell == null || !neighborCell.IsTile)
                        continue;
                    
                    // UPDATED: Ignore pawn occupancy on tiles for pathfinding
                    // Pawns can be jumped over, so we don't consider tile occupancy as blocking
                    // Only walls in gaps will block movement
                    
                    // FIXED: Check if gap is blocked by wall
                    if (!gridSystem.IsValidUnifiedPosition(gapPos))
                        continue;
                        
                    GridSystem.GridCell gapCell = gridSystem.GetCell(gapPos);
                    if (gapCell == null || gapCell.isOccupied)
                        continue;
                    
                    // Calculate costs
                    float newGCost = currentNode.gCost + 1; // Each move has cost of 1
                    
                    // Find existing node or create new one
                    PathNode neighborNode = openSet.Find(n => n.position == neighborPos);
                    if (neighborNode == null)
                    {
                        neighborNode = new PathNode(neighborPos);
                        neighborNode.parent = currentNode;
                        neighborNode.gCost = newGCost;
                        neighborNode.hCost = GetDistance(neighborPos, endUnified);
                        openSet.Add(neighborNode);
                    }
                    else if (newGCost < neighborNode.gCost)
                    {
                        // Found better path to this node
                        neighborNode.parent = currentNode;
                        neighborNode.gCost = newGCost;
                    }
                }
            }
            
            // No path found
            return null;
        }
        
        /// <summary>
        /// FIXED: Proper tile position validation within grid bounds
        /// </summary>
        private static bool IsValidTilePosition(GridSystem gridSystem, Vector2Int tilePos)
        {
            int gridSize = gridSystem.GetGridSize();
            return tilePos.x >= 0 && tilePos.x < gridSize && 
                   tilePos.y >= 0 && tilePos.y < gridSize;
        }
        
        private static float GetDistance(Vector2Int a, Vector2Int b)
        {
            // Manhattan distance (divided by 2 since we move 2 cells at a time)
            return (Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y)) / 2f;
        }
        
        private static List<Vector2Int> RetracePath(GridSystem gridSystem, PathNode startNode, PathNode endNode)
        {
            List<Vector2Int> path = new List<Vector2Int>();
            PathNode currentNode = endNode;
            
            while (currentNode != startNode)
            {
                // Convert unified position back to tile position
                Vector2Int tilePos = gridSystem.UnifiedToTilePosition(currentNode.position);
                path.Add(tilePos);
                currentNode = currentNode.parent;
            }
            
            path.Reverse();
            return path;
        }
        
        /// <summary>
        /// Get the shortest path length between two tiles
        /// Returns -1 if no path exists
        /// </summary>
        public static int GetPathLength(GridSystem gridSystem, Vector2Int fromTile, Vector2Int toTile)
        {
            var path = FindPath(gridSystem, fromTile, toTile);
            return path != null ? path.Count : -1;
        }
        
        /// <summary>
        /// NEW: Check if any pawn can reach any tile on their target side
        /// This replaces the pre-calculation approach
        /// UPDATED: Now ignores pawn occupancy in pathfinding calculations
        /// </summary>
        public static bool AllPawnsHaveValidPaths(GridSystem gridSystem, WallChessGameManager gameManager)
        {
            if (gameManager?.pawns == null || gameManager.pawns.Count == 0) return true;
            
            foreach (var pawn in gameManager.pawns)
            {
                if (!HasPathToTargetSide(gridSystem, pawn, gameManager.gridSize))
                {
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// NEW: Check if a specific pawn has a path to their target side
        /// UPDATED: Now ignores pawn occupancy in pathfinding calculations
        /// </summary>
        private static bool HasPathToTargetSide(GridSystem gridSystem, WallChessGameManager.PawnData pawn, int gridSize)
        {
            Vector2Int currentPos = pawn.position;
            List<Vector2Int> goalTiles = GetGoalTiles(pawn, gridSize);
            
            // Test if there's a path to ANY goal tile
            foreach (Vector2Int goalTile in goalTiles)
            {
                // Check if path exists to this goal tile
                // Since we now ignore pawn occupancy, we don't need to check if goal tile is occupied
                if (PathExists(gridSystem, currentPos, goalTile))
                {
                    return true; // Found at least one reachable tile on goal side
                }
            }
            
            return false; // No reachable tiles on goal side
        }
        
        /// <summary>
        /// NEW: Get all possible goal tiles for a pawn based on their starting position
        /// </summary>
        private static List<Vector2Int> GetGoalTiles(WallChessGameManager.PawnData pawn, int gridSize)
        {
            List<Vector2Int> goalTiles = new List<Vector2Int>();
            Vector2Int startPos = pawn.startPosition;
            
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
    }
}
