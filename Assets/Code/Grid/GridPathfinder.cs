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
        /// </summary>
        public static bool PathExists(GridSystem gridSystem, Vector2Int fromTile, Vector2Int toTile)
        {
            var path = FindPath(gridSystem, fromTile, toTile);
            return path != null && path.Count > 0;
        }
        
        /// <summary>
        /// Find the shortest path between two tile positions
        /// Returns null if no path exists
        /// </summary>
        public static List<Vector2Int> FindPath(GridSystem gridSystem, Vector2Int fromTile, Vector2Int toTile)
        {
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
                    
                    // Check if neighbor is valid tile position
                    if (!gridSystem.IsValidUnifiedPosition(neighborPos))
                        continue;
                    
                    GridSystem.GridCell neighborCell = gridSystem.GetCell(neighborPos);
                    if (!neighborCell.IsTile)
                        continue;
                    
                    // Check if tile is occupied (except for the destination)
                    if (neighborCell.isOccupied && neighborPos != endUnified)
                        continue;
                    
                    // Check if gap is blocked by wall
                    GridSystem.GridCell gapCell = gridSystem.GetCell(gapPos);
                    if (gapCell.isOccupied)
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
    }
}