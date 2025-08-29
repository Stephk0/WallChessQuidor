using UnityEngine;
using WallChess.Grid;

namespace WallChess
{
    /// <summary>
    /// Computes nearest valid gap using intersection + direction approach.
    /// Finds nearest intersection point and uses mouse direction to determine wall orientation.
    /// </summary>
    public class GapDetector
    {
        private readonly WallState s;
        private readonly GridCoordinateConverter coordinateConverter;
        private readonly float laneSnap;
        private readonly float unlockMul;
        private readonly float margin;
        private WallState.Orientation? lockOrient = null;

        public GapDetector(WallState state, float laneSnapMargin, float unlockMultiplier, float tieMargin)
        {
            s = state;
            this.coordinateConverter = state.GetCoordinateConverter();
            laneSnap = laneSnapMargin;
            unlockMul = unlockMultiplier;
            margin = tieMargin;
        }

        public struct WallInfo
        {
            public WallState.Orientation orientation;
            public int x, y;
            public Vector3 pos, scale;
        }

        public bool TryFind(Vector3 world, WallChessGameManager gm, out WallInfo result)
        {
            return TryFindByNearestIntersection(world, gm, out result);
        }
        
        /// <summary>
        /// NEW APPROACH: Find nearest intersection and use mouse direction to determine orientation
        /// FIXED: Proper lane locking and cursor-aligned positioning
        /// </summary>
        private bool TryFindByNearestIntersection(Vector3 world, WallChessGameManager gm, out WallInfo result)
        {
            result = default;
            Vector3 alignmentOffset = coordinateConverter?.GetAlignmentOffset() ?? Vector3.zero;
            Vector3 adjustedWorld = world - alignmentOffset;
            
            // Find nearest intersection point on the grid
            // Intersections are at (n + 0.5) * spacing positions
            float intersectionX = Mathf.Round(adjustedWorld.x / s.spacing - 0.5f) + 0.5f;
            float intersectionY = Mathf.Round(adjustedWorld.y / s.spacing - 0.5f) + 0.5f;
            
            Vector3 nearestIntersection = new Vector3(
                intersectionX * s.spacing,
                intersectionY * s.spacing,
                0f
            ) + alignmentOffset;
            
            // Calculate direction from intersection to mouse
            Vector3 directionToMouse = world - nearestIntersection;
            float distanceToIntersection = directionToMouse.magnitude;
            
            // Check if we're close enough to an intersection to trigger wall placement
            float maxDetectionDistance = s.spacing * 0.7f; // Generous detection radius
            
            if (distanceToIntersection > maxDetectionDistance)
            {
                Debug.Log($"TOO FAR from intersection ({distanceToIntersection:F3} > {maxDetectionDistance:F3})");
                return false;
            }
            
            // FIXED LANE LOCKING: Check if we're in a lane for the current locked orientation
            WallState.Orientation orientation;
            
            if (lockOrient.HasValue)
            {
                // Calculate distance to the locked orientation's lane
                float laneDistance = GetDistanceToOrientationLane(world, lockOrient.Value);
                float unlockThreshold = laneSnap * unlockMul;
                
                if (laneDistance <= unlockThreshold)
                {
                    // Stay locked - we're still in the lane
                    orientation = lockOrient.Value;
                    Debug.Log($"LANE LOCKED: {orientation} (distance={laneDistance:F3} <= threshold={unlockThreshold:F3})");
                }
                else
                {
                    // Exit lane - unlock and determine new orientation
                    lockOrient = null;
                    orientation = DetermineOrientationFromDirection(directionToMouse);
                    Debug.Log($"LANE UNLOCKED: {orientation} (exited lane: {laneDistance:F3} > {unlockThreshold:F3})");
                }
            }
            else
            {
                // No lock - determine orientation from direction
                orientation = DetermineOrientationFromDirection(directionToMouse);
                Debug.Log($"NEW ORIENTATION: {orientation}");
            }
            
            // FIXED POSITIONING: Use mouse position to find the actual gap being targeted
            int wallX, wallY;
            if (!FindWallCoordinatesNearMouse(world, orientation, out wallX, out wallY))
            {
                Debug.Log($"Could not find {orientation} wall coordinates near mouse");
                return false;
            }
            
            // Apply boundary constraints
            Vector2Int constrained = ApplyBoundaryConstraints(orientation, wallX, wallY);
            wallX = constrained.x;
            wallY = constrained.y;
            
            // Validate the wall can be placed
            if (!CanPlaceWallAt(orientation, wallX, wallY))
            {
                Debug.Log($"Cannot place {orientation} wall at ({wallX},{wallY})");
                return false;
            }
            
            // Create wall info
            result = MakeInfo(orientation, wallX, wallY, gm);
            
            // FIXED LOCKING: Set lock based on distance to the orientation's lane
            float currentLaneDistance = GetDistanceToOrientationLane(world, orientation);
            if (currentLaneDistance <= laneSnap)
            {
                lockOrient = orientation;
                Debug.Log($"LOCKING to {orientation} (lane distance: {currentLaneDistance:F3})");
            }
            
            Debug.Log($"✓ SUCCESS: {orientation} wall at ({wallX},{wallY})");
            return true;
        }

        /// <summary>
        /// Determine wall orientation based on mouse direction from intersection
        /// CORRECTED: Direction along gap determines wall orientation (not perpendicular blocking)
        /// </summary>
        private WallState.Orientation DetermineOrientationFromDirection(Vector3 direction)
        {
            // If moving more horizontally → place horizontal wall (drag along horizontal gap)
            // If moving more vertically → place vertical wall (drag along vertical gap)
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            {
                Debug.Log($"→ HORIZONTAL wall (|{direction.x:F3}| > |{direction.y:F3}|)");
                return WallState.Orientation.Horizontal;
            }
            else
            {
                Debug.Log($"→ VERTICAL wall (|{direction.y:F3}| >= |{direction.x:F3}|)");
                return WallState.Orientation.Vertical;
            }
        }
        
        /// <summary>
        /// Calculate distance to the appropriate lane for the given orientation
        /// </summary>
        private float GetDistanceToOrientationLane(Vector3 world, WallState.Orientation orientation)
        {
            if (orientation == WallState.Orientation.Horizontal)
            {
                // Distance to nearest horizontal lane (where horizontal walls can be placed)
                Vector3 alignmentOffset = coordinateConverter?.GetAlignmentOffset() ?? Vector3.zero;
                float adjustedY = world.y - alignmentOffset.y;
                
                // Horizontal lanes are at (n + 0.5) * spacing positions
                float laneIndex = adjustedY / s.spacing - 0.5f;
                float nearestLaneIndex = Mathf.Round(laneIndex);
                float nearestLaneY = (nearestLaneIndex + 0.5f) * s.spacing + alignmentOffset.y;
                
                return Mathf.Abs(world.y - nearestLaneY);
            }
            else
            {
                // Distance to nearest vertical lane (where vertical walls can be placed)
                Vector3 alignmentOffset = coordinateConverter?.GetAlignmentOffset() ?? Vector3.zero;
                float adjustedX = world.x - alignmentOffset.x;
                
                // Vertical lanes are at (n + 0.5) * spacing positions
                float laneIndex = adjustedX / s.spacing - 0.5f;
                float nearestLaneIndex = Mathf.Round(laneIndex);
                float nearestLaneX = (nearestLaneIndex + 0.5f) * s.spacing + alignmentOffset.x;
                
                return Mathf.Abs(world.x - nearestLaneX);
            }
        }
        
        /// <summary>
        /// Find wall coordinates that place the wall closest to the mouse cursor
        /// </summary>
        private bool FindWallCoordinatesNearMouse(Vector3 world, WallState.Orientation orientation, out int wallX, out int wallY)
        {
            Vector3 alignmentOffset = coordinateConverter?.GetAlignmentOffset() ?? Vector3.zero;
            Vector3 adjustedWorld = world - alignmentOffset;
            
            if (orientation == WallState.Orientation.Horizontal)
            {
                // For horizontal walls, find the gap that the mouse is closest to
                // Horizontal walls are placed at row boundaries (between y and y+1)
                float gapY = adjustedWorld.y / s.spacing - 0.5f;
                wallY = Mathf.RoundToInt(gapY);
                
                // For horizontal walls, find the x position that centers the wall under the mouse
                // Wall spans 2 columns, so we want the leftmost x where wall center aligns with mouse
                float wallCenterX = adjustedWorld.x / s.spacing;
                wallX = Mathf.RoundToInt(wallCenterX - 0.5f); // Offset to get left edge of 2-tile span
                
                Debug.Log($"H-Wall positioning: mouse({adjustedWorld.x:F2},{adjustedWorld.y:F2}) → wall({wallX},{wallY})");
                return wallY >= 0 && wallY < s.Cells - 1;
            }
            else
            {
                // For vertical walls, find the gap that the mouse is closest to
                // Vertical walls are placed at column boundaries (between x and x+1)
                float gapX = adjustedWorld.x / s.spacing - 0.5f;
                wallX = Mathf.RoundToInt(gapX);
                
                // For vertical walls, find the y position that centers the wall under the mouse
                // Wall spans 2 rows, so we want the bottommost y where wall center aligns with mouse
                float wallCenterY = adjustedWorld.y / s.spacing;
                wallY = Mathf.RoundToInt(wallCenterY - 0.5f); // Offset to get bottom edge of 2-tile span
                
                Debug.Log($"V-Wall positioning: mouse({adjustedWorld.x:F2},{adjustedWorld.y:F2}) → wall({wallX},{wallY})");
                return wallX >= 0 && wallX < s.Cells - 1;
            }
        }
        /// </summary>
        private bool IntersectionToWallCoordinates(float intersectionX, float intersectionY, WallState.Orientation orientation, out int wallX, out int wallY)
        {
            if (orientation == WallState.Orientation.Horizontal)
            {
                // Horizontal wall: spans columns at this row boundary
                wallX = Mathf.FloorToInt(intersectionX);
                wallY = Mathf.FloorToInt(intersectionY);
                
                Debug.Log($"H-Wall: intersection({intersectionX:F1},{intersectionY:F1}) → ({wallX},{wallY})");
                return wallY >= 0 && wallY < s.Cells - 1;
            }
            else
            {
                // Vertical wall: spans rows at this column boundary  
                wallX = Mathf.FloorToInt(intersectionX);
                wallY = Mathf.FloorToInt(intersectionY);
                
                Debug.Log($"V-Wall: intersection({intersectionX:F1},{intersectionY:F1}) → ({wallX},{wallY})");
                return wallX >= 0 && wallX < s.Cells - 1;
            }
        }
        
        /// <summary>
        /// Check if a wall can be placed at the given coordinates
        /// </summary>
        private bool CanPlaceWallAt(WallState.Orientation orientation, int x, int y)
        {
            // Check bounds
            if (orientation == WallState.Orientation.Horizontal)
            {
                if (x < 0 || x + 1 >= s.Cells || y < 0 || y >= s.Cells - 1)
                {
                    Debug.Log($"H-Wall bounds fail: x+1={x+1} >= {s.Cells} OR y={y} >= {s.Cells-1}");
                    return false;
                }
            }
            else
            {
                if (x < 0 || x >= s.Cells - 1 || y < 0 || y + 1 >= s.Cells)
                {
                    Debug.Log($"V-Wall bounds fail: x={x} >= {s.Cells-1} OR y+1={y+1} >= {s.Cells}");
                    return false;
                }
            }
            
            // Check if gap is already occupied
            bool occupied = s.IsOccupied(orientation, x, y);
            if (occupied)
            {
                Debug.Log($"Gap occupied: {orientation}({x},{y})");
                return false;
            }
            
            return true;
        }

        /// <summary>
        /// Apply boundary constraints to ensure walls are always 2 units long
        /// </summary>
        private Vector2Int ApplyBoundaryConstraints(WallState.Orientation orientation, int x, int y)
        {
            if (orientation == WallState.Orientation.Horizontal)
            {
                // Horizontal walls need 2 columns, so max x position is Cells-2
                int maxX = s.Cells - 2;
                x = Mathf.Clamp(x, 0, maxX);
                
                // For top boundary, offset down by one
                int maxY = s.Cells - 2;
                y = Mathf.Clamp(y, 0, maxY);
            }
            else // Vertical
            {
                // For right boundary, offset left by one
                int maxX = s.Cells - 2;
                x = Mathf.Clamp(x, 0, maxX);
                
                // Vertical walls need 2 rows, so max y position is Cells-2
                int maxY = s.Cells - 2;
                y = Mathf.Clamp(y, 0, maxY);
            }
            
            return new Vector2Int(x, y);
        }

        WallInfo MakeInfo(WallState.Orientation o, int x, int y, WallChessGameManager gm)
        {
            // Use the corrected gap center calculation
            Vector3 c = s.GapCenter(o, x, y);
            
            // Calculate proper wall scale - wall spans exactly 2 tiles plus the gap between them
            Vector3 scale;
            
            if (o == WallState.Orientation.Horizontal)
            {
                // Horizontal wall: 2 tiles wide + 1 gap between tiles, thickness in Y direction
                float wallLength = (gm.tileSize * 2f) + gm.tileGap;
                scale = new Vector3(wallLength, gm.wallThickness, gm.wallHeight);
            }
            else
            {
                // Vertical wall: 2 tiles tall + 1 gap between tiles, thickness in X direction  
                float wallLength = (gm.tileSize * 2f) + gm.tileGap;
                scale = new Vector3(gm.wallThickness, wallLength, gm.wallHeight);
            }
            
            return new WallInfo { 
                orientation = o, 
                x = x, 
                y = y, 
                pos = new Vector3(c.x, c.y, -0.1f), // Slightly in front for visibility
                scale = scale 
            };
        }
    }
}
