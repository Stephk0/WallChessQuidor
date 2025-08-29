using UnityEngine;
using System.Collections.Generic;

namespace WallChess
{
    /// <summary>
    /// Unified wall placement controller that works directly with GridSystem.
    /// </summary>
    public class WallPlacementController
    {
        private readonly WallManager wallManager;
        private readonly WallChessGameManager gameManager;
        private readonly GridSystem gridSystem;
        private readonly WallValidator validator;
        private readonly WallVisuals visuals;
        private readonly float planeZ;

        private bool isPlacing = false;
        
        // Lane locking state (restored from ImprovedWallPlacer)
        private GridSystem.Orientation? orientationLock = null;

        // Gap detection settings (from WallManager inspector)
        private float gapSnapMargin => wallManager != null ? wallManager.GetGapSnapMargin() : 0.25f;
        private float laneSnapMargin => wallManager != null ? wallManager.GetLaneSnapMargin() : 0.3f;
        private float unlockMultiplier => wallManager != null ? wallManager.GetUnlockMultiplier() : 1.5f;

        // Gap offsets aligned with grid
        private const float horizontalGapOffsetX = 0.5f;
        private const float horizontalGapOffsetY = 0.0f;   // horizontal gaps are between rows
        private const float verticalGapOffsetX = 0.0f;     // vertical gaps are between columns
        private const float verticalGapOffsetY = 0.5f;

        public WallPlacementController(WallManager manager, WallChessGameManager gm, GridSystem grid, WallValidator validator, WallVisuals visuals, float placementPlaneZ)
        {
            this.wallManager = manager;
            this.gameManager = gm;
            this.gridSystem = grid;
            this.validator = validator;
            this.visuals = visuals;
            this.planeZ = placementPlaneZ;
        }

        public void Tick()
        {
            if (Input.GetMouseButtonDown(0) && gameManager.CanInitiateWallPlacement())
            {
                if (!IsClickingOnAvatar() && gameManager.TryStartWallPlacement())
                    isPlacing = true;
            }
            else if (Input.GetMouseButton(0) && isPlacing)
            {
                Vector3 mouse = GetMouseWorld();
                if (!IsWithinBounds(mouse)) { visuals.HidePreview(); return; }

                var wallInfo = FindNearestWallGap(mouse);
                if (wallInfo.HasValue)
                {
                    bool canPlace = ValidateWallPlacement(wallInfo.Value);
                    Vector3 scale = wallManager.GetWallScale(wallInfo.Value.orientation);
                    Quaternion rotation = wallManager.GetWallRotation(wallInfo.Value.orientation);
                    visuals.UpdatePreview(wallInfo.Value.worldPosition, scale, rotation, canPlace);
                }
                else visuals.HidePreview();
            }
            else if (Input.GetMouseButtonUp(0) && isPlacing)
            {
                TryCommitAtMouse();
                orientationLock = null; // Reset orientation lock after placement attempt
                isPlacing = false;
            }
            else if (Input.GetMouseButtonUp(0) && gameManager.GetCurrentState() == GameState.WallPlacement && !isPlacing)
            {
                gameManager.CompleteWallPlacement(false);
            }
        }

        public bool TryPlaceWall(Vector3 worldPosition)
        {
            if (!gameManager.CanInitiateWallPlacement()) return false;
            if (!gameManager.TryStartWallPlacement()) return false;
            
            var wallInfo = FindNearestWallGap(worldPosition);
            if (wallInfo.HasValue && ValidateWallPlacement(wallInfo.Value))
            {
                Commit(wallInfo.Value);
                orientationLock = null; // Reset after placement
                Debug.Log($"TryPlaceWall: Wall committed successfully - turn ending handled by event system");
                return true;
            }
            
            // Only call CompleteWallPlacement for failed attempts
            gameManager.CompleteWallPlacement(false);
            return false;
        }

        /// <summary>
        /// FIXED: Comprehensive validation that prevents duplicate placements
        /// </summary>
        private bool ValidateWallPlacement(UnifiedWallInfo wallInfo)
        {
            // First check: Basic game rules (player has walls, game allows wall placement)
            if (!gameManager.CanPlaceWalls() || !gameManager.CurrentPlayerHasWalls())
                return false;

            // Second check: Grid occupancy (prevents duplicate placements)
            if (!gridSystem.CanPlaceWall(wallInfo.orientation, wallInfo.x, wallInfo.y))
                return false;

            // Third check: Advanced rules (path blocking, intersections)
            if (!validator.CanPlace(wallInfo.orientation, wallInfo.x, wallInfo.y))
                return false;

            return true;
        }

        void TryCommitAtMouse()
        {
            bool success = false;
            Vector3 mouse = GetMouseWorld();
            var wallInfo = FindNearestWallGap(mouse);
            
            if (IsWithinBounds(mouse) && wallInfo.HasValue && ValidateWallPlacement(wallInfo.Value))
            {
                Commit(wallInfo.Value);
                success = true;
                Debug.Log($"Wall placement committed successfully at {wallInfo.Value.orientation} ({wallInfo.Value.x},{wallInfo.Value.y})");
            }
            else if (wallInfo.HasValue)
            {
                Debug.LogWarning($"Cannot place wall at {wallInfo.Value.orientation} ({wallInfo.Value.x},{wallInfo.Value.y}) - validation failed");
            }
            
            visuals.CleanupPreview();
            
            // FIXED: Don't call CompleteWallPlacement here - it's handled by OnWallPlaced event
            // This prevents double calls to EndTurn() which was causing activePlayerIndex issues
            if (!success)
            {
                // Only handle failed placements here - successful ones are handled by event system
                gameManager.CompleteWallPlacement(false);
            }
        }

        // Wall info structure for unified system
        public struct UnifiedWallInfo
        {
            public GridSystem.Orientation orientation;
            public int x, y;
            public Vector3 worldPosition;
            
            public UnifiedWallInfo(GridSystem.Orientation ori, int x, int y, Vector3 worldPos)
            {
                orientation = ori;
                this.x = x;
                this.y = y;
                worldPosition = worldPos;
            }
        }
        
        /// <summary>
        /// FIXED: Only creates wall visual and decrements count if placement succeeds
        /// Now uses rotation-based prefab system
        /// </summary>
        void Commit(UnifiedWallInfo info)
        {
            Debug.Log($"Commit: Attempting to place wall {info.orientation} at ({info.x},{info.y}) for activePlayer {gameManager.GetActivePawnIndex()}");
            
            // Final validation before commitment
            if (!ValidateWallPlacement(info))
            {
                Debug.LogWarning($"Wall placement validation failed at commit for {info.orientation} ({info.x},{info.y})");
                return;
            }

            // Get rotation and scale from WallManager
            Quaternion rotation = wallManager.GetWallRotation(info.orientation);
            Vector3 scale = wallManager.GetWallScale(info.orientation);
            GameObject prefabToUse = wallManager.GetRandomWallPrefab();
            
            // Create wall visual with rotation and appropriate prefab
            GameObject wallObj = visuals.CreateWall(info.worldPosition, scale, rotation, prefabToUse);
            
            // Attempt to place wall in grid system (single source of truth)
            Debug.Log($"Commit: About to call wallManager.PlaceWall() - this should trigger OnWallPlaced event");
            bool placed = wallManager.PlaceWall(info.orientation, info.x, info.y, info.worldPosition, scale);
            
            if (placed)
            {
                wallManager.AddManagedWall(wallObj);
                Debug.Log($"Commit: Wall successfully placed at {info.orientation} {info.x},{info.y} with rotation {rotation.eulerAngles}");
                // OnWallPlaced event will be triggered automatically, which handles wall count decrementation and turn ending
            }
            else
            {
                // Failed to place in grid - destroy visual and don't decrement wall count
                WallState.SafeDestroy(wallObj);
                Debug.LogError($"CRITICAL: Wall validation passed but GridSystem.PlaceWall failed at {info.orientation} {info.x},{info.y}. This indicates a validation bug!");
            }
        }
        
        // Legacy method for compatibility
        void Commit(GapDetector.WallInfo info)
        {
            GridSystem.Orientation orientation = info.orientation == WallState.Orientation.Horizontal ? 
                GridSystem.Orientation.Horizontal : GridSystem.Orientation.Vertical;
            Vector3 worldPos = wallManager.GetWallWorldPosition(orientation, info.x, info.y);
            Commit(new UnifiedWallInfo(orientation, info.x, info.y, worldPos));
        }

        bool IsClickingOnAvatar()
        {
            Vector3 mouse = GetMouseWorld();
            Vector3 p = gridSystem.GridToWorldPosition(gameManager.playerPosition);
            if (Vector3.Distance(mouse, p) < gameManager.tileSize * 0.6f) return true;
            Vector3 o = gridSystem.GridToWorldPosition(gameManager.opponentPosition);
            if (Vector3.Distance(mouse, o) < gameManager.tileSize * 0.6f) return true;
            return false;
        }

        Vector3 GetMouseWorld()
        {
            var cam = Camera.main;
            if (!cam) return Vector3.zero;
            var ray = cam.ScreenPointToRay(Input.mousePosition);
            var plane = new Plane(Vector3.forward, new Vector3(0, 0, planeZ));
            return plane.Raycast(ray, out float enter) ? ray.GetPoint(enter)
                 : cam.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, Mathf.Abs(cam.transform.position.z - planeZ)));
        }

        bool IsWithinBounds(Vector3 p)
        {
            return gridSystem?.IsWithinGridBounds(p) ?? false;
        }
        
        /// <summary>
        /// RESTORED: Lane-based gap detection with orientation locking from ImprovedWallPlacer
        /// </summary>
        public UnifiedWallInfo? FindNearestWallGap(Vector3 worldPos)
        {
            var settings = gridSystem.GetGridSettings();
            float spacing = settings.TileSpacing;
            
            // Lane distances (stripe logic) - RESTORED from ImprovedWallPlacer
            float yLane = NearestHorizontalLaneY(worldPos.y, spacing);
            float xLane = NearestVerticalLaneX(worldPos.x, spacing);
            float dY = Mathf.Abs(worldPos.y - yLane);   // distance to horizontal lane (controls Horizontal orientation)
            float dX = Mathf.Abs(worldPos.x - xLane);   // distance to vertical lane (controls Vertical orientation)

            bool inHStripe = dY <= laneSnapMargin;
            bool inVStripe = dX <= laneSnapMargin;

            // RESTORED: Hysteresis - if we're locked to an orientation, keep it until we leave a wider stripe
            if (orientationLock.HasValue)
            {
                if (orientationLock.Value == GridSystem.Orientation.Horizontal)
                {
                    if (dY <= laneSnapMargin * unlockMultiplier)
                    {
                        inHStripe = true; inVStripe = false; // Stay locked to horizontal
                        Debug.Log($"Maintaining HORIZONTAL lock (dY={dY:F3} <= {laneSnapMargin * unlockMultiplier:F3})");
                    }
                    else
                    {
                        orientationLock = null; // left the stripe -> unlock
                        Debug.Log($"UNLOCKING from horizontal (dY={dY:F3} > {laneSnapMargin * unlockMultiplier:F3})");
                    }
                }
                else // Vertical lock
                {
                    if (dX <= laneSnapMargin * unlockMultiplier)
                    {
                        inVStripe = true; inHStripe = false; // Stay locked to vertical
                        Debug.Log($"Maintaining VERTICAL lock (dX={dX:F3} <= {laneSnapMargin * unlockMultiplier:F3})");
                    }
                    else
                    {
                        orientationLock = null; // left the stripe -> unlock
                        Debug.Log($"UNLOCKING from vertical (dX={dX:F3} > {laneSnapMargin * unlockMultiplier:F3})");
                    }
                }
            }
            else
            {
                // If neither stripe is hit, allow both so we can still find a nearest center anywhere on the board
                if (!inHStripe && !inVStripe) { inHStripe = true; inVStripe = true; }
            }

            // RESTORED: Candidate computation using direct index math (from ImprovedWallPlacer)
            int hx0, hy0, hx1, hy1; // horizontal candidates (floor & round)
            int vx0, vy0, vx1, vy1; // vertical candidates (floor & round)
            
            MapToGapIndices(worldPos, GridSystem.Orientation.Horizontal, out hx0, out hy0, floor: true, spacing);
            MapToGapIndices(worldPos, GridSystem.Orientation.Horizontal, out hx1, out hy1, floor: false, spacing);
            MapToGapIndices(worldPos, GridSystem.Orientation.Vertical, out vx0, out vy0, floor: true, spacing);
            MapToGapIndices(worldPos, GridSystem.Orientation.Vertical, out vx1, out vy1, floor: false, spacing);

            bool hValid0 = ClampToGapRange(ref hx0, ref hy0, GridSystem.Orientation.Horizontal);
            bool hValid1 = ClampToGapRange(ref hx1, ref hy1, GridSystem.Orientation.Horizontal);
            bool vValid0 = ClampToGapRange(ref vx0, ref vy0, GridSystem.Orientation.Vertical);
            bool vValid1 = ClampToGapRange(ref vx1, ref vy1, GridSystem.Orientation.Vertical);

            float bestDistSq = float.PositiveInfinity;
            bool found = false;
            UnifiedWallInfo result = default;

            // Gate evaluation by lane stripes
            if (inHStripe)
            {
                EvaluateCandidate(GridSystem.Orientation.Horizontal, hx0, hy0, hValid0, worldPos, spacing, ref bestDistSq, ref found, ref result);
                EvaluateCandidate(GridSystem.Orientation.Horizontal, hx1, hy1, hValid1, worldPos, spacing, ref bestDistSq, ref found, ref result);
            }
            if (inVStripe)
            {
                EvaluateCandidate(GridSystem.Orientation.Vertical, vx0, vy0, vValid0, worldPos, spacing, ref bestDistSq, ref found, ref result);
                EvaluateCandidate(GridSystem.Orientation.Vertical, vx1, vy1, vValid1, worldPos, spacing, ref bestDistSq, ref found, ref result);
            }

            // RESTORED: Lock orientation if we picked within the narrow stripe (prevents flicker)
            if (found)
            {
                if (result.orientation == GridSystem.Orientation.Horizontal && dY <= laneSnapMargin)
                {
                    orientationLock = GridSystem.Orientation.Horizontal;
                    Debug.Log($"LOCKING to HORIZONTAL orientation (dY={dY:F3} <= {laneSnapMargin:F3})");
                }
                else if (result.orientation == GridSystem.Orientation.Vertical && dX <= laneSnapMargin)
                {
                    orientationLock = GridSystem.Orientation.Vertical;
                    Debug.Log($"LOCKING to VERTICAL orientation (dX={dX:F3} <= {laneSnapMargin:F3})");
                }
                // If we selected while outside both narrow stripes (far away), don't lock
            }

            return found ? result : (UnifiedWallInfo?)null;
        }
        
        /// <summary>
        /// Calculate nearest horizontal lane line (world Y) for horizontal walls (between rows)
        /// </summary>
        private float NearestHorizontalLaneY(float worldY, float spacing)
        {
            // Find the nearest row gap by converting to grid space and back
            // This ensures alignment is properly respected
            Vector3 testPoint = new Vector3(0, worldY, 0);
            Vector2Int nearestTile = gridSystem.WorldToGridPosition(testPoint);
            
            // For horizontal walls, we want the gap between row y and y+1
            // Test both possible gap positions and choose the nearest
            Vector3 gap1 = GetHorizontalGapWorldPos(0, nearestTile.y - 1);
            Vector3 gap2 = GetHorizontalGapWorldPos(0, nearestTile.y);
            
            float dist1 = Mathf.Abs(gap1.y - worldY);
            float dist2 = Mathf.Abs(gap2.y - worldY);
            
            return dist1 < dist2 ? gap1.y : gap2.y;
        }

        /// <summary>
        /// Calculate nearest vertical lane line (world X) for vertical walls (between columns)
        /// </summary>
        private float NearestVerticalLaneX(float worldX, float spacing)
        {
            // Find the nearest column gap by converting to grid space and back
            // This ensures alignment is properly respected
            Vector3 testPoint = new Vector3(worldX, 0, 0);
            Vector2Int nearestTile = gridSystem.WorldToGridPosition(testPoint);
            
            // For vertical walls, we want the gap between column x and x+1
            // Test both possible gap positions and choose the nearest
            Vector3 gap1 = GetVerticalGapWorldPos(nearestTile.x - 1, 0);
            Vector3 gap2 = GetVerticalGapWorldPos(nearestTile.x, 0);
            
            float dist1 = Mathf.Abs(gap1.x - worldX);
            float dist2 = Mathf.Abs(gap2.x - worldX);
            
            return dist1 < dist2 ? gap1.x : gap2.x;
        }
        
        /// <summary>
        /// Get world position of horizontal gap (between rows)
        /// </summary>
        private Vector3 GetHorizontalGapWorldPos(int x, int y)
        {
            Vector2Int tile1 = new Vector2Int(x, y);
            Vector2Int tile2 = new Vector2Int(x, y + 1);
            Vector3 pos1 = gridSystem.GridToWorldPosition(tile1);
            Vector3 pos2 = gridSystem.GridToWorldPosition(tile2);
            return new Vector3(pos1.x, (pos1.y + pos2.y) / 2f, 0f);
        }
        
        /// <summary>
        /// Get world position of vertical gap (between columns)
        /// </summary>
        private Vector3 GetVerticalGapWorldPos(int x, int y)
        {
            Vector2Int tile1 = new Vector2Int(x, y);
            Vector2Int tile2 = new Vector2Int(x + 1, y);
            Vector3 pos1 = gridSystem.GridToWorldPosition(tile1);
            Vector3 pos2 = gridSystem.GridToWorldPosition(tile2);
            return new Vector3((pos1.x + pos2.x) / 2f, pos1.y, 0f);
        }

        /// <summary>
        /// Map world position to gap indices with floor/round options
        /// </summary>
        private void MapToGapIndices(Vector3 p, GridSystem.Orientation o, out int gx, out int gy, bool floor, float spacing)
        {
            // Convert world position to nearest tile coordinates using grid system
            Vector2Int nearestTile = gridSystem.WorldToGridPosition(p);
            
            if (o == GridSystem.Orientation.Horizontal)
            {
                // For horizontal walls, find the nearest gap between rows
                // The wall coordinate represents the lower row of the gap
                if (floor)
                {
                    // Use the tile row or the row below
                    Vector3 gap1 = GetHorizontalGapWorldPos(nearestTile.x, nearestTile.y - 1);
                    Vector3 gap2 = GetHorizontalGapWorldPos(nearestTile.x, nearestTile.y);
                    
                    if (Mathf.Abs(gap1.y - p.y) < Mathf.Abs(gap2.y - p.y))
                    {
                        gx = nearestTile.x;
                        gy = nearestTile.y - 1;
                    }
                    else
                    {
                        gx = nearestTile.x;
                        gy = nearestTile.y;
                    }
                }
                else
                {
                    // Round to nearest gap
                    gx = nearestTile.x;
                    gy = nearestTile.y;
                }
            }
            else
            {
                // For vertical walls, find the nearest gap between columns
                // The wall coordinate represents the left column of the gap
                if (floor)
                {
                    // Use the tile column or the column to the left
                    Vector3 gap1 = GetVerticalGapWorldPos(nearestTile.x - 1, nearestTile.y);
                    Vector3 gap2 = GetVerticalGapWorldPos(nearestTile.x, nearestTile.y);
                    
                    if (Mathf.Abs(gap1.x - p.x) < Mathf.Abs(gap2.x - p.x))
                    {
                        gx = nearestTile.x - 1;
                        gy = nearestTile.y;
                    }
                    else
                    {
                        gx = nearestTile.x;
                        gy = nearestTile.y;
                    }
                }
                else
                {
                    // Round to nearest gap
                    gx = nearestTile.x;
                    gy = nearestTile.y;
                }
            }
        }

        /// <summary>
        /// Clamp gap coordinates to valid ranges and check bounds
        /// </summary>
        private bool ClampToGapRange(ref int x, ref int y, GridSystem.Orientation o)
        {
            int gridSize = gridSystem.GetGridSize();
            
            if (o == GridSystem.Orientation.Horizontal)
            {
                // For horizontal walls: x can be [0..gridSize-2], y can be [0..gridSize-2]
                // Wall spans 2 tiles horizontally (x to x+1)
                x = Mathf.Clamp(x, 0, gridSize - 2);
                y = Mathf.Clamp(y, 0, gridSize - 2);
                return x + 1 < gridSize && y >= 0 && y < gridSize - 1;
            }
            else
            {
                // For vertical walls: x can be [0..gridSize-2], y can be [0..gridSize-2]
                // Wall spans 2 tiles vertically (y to y+1)
                x = Mathf.Clamp(x, 0, gridSize - 2);
                y = Mathf.Clamp(y, 0, gridSize - 2);
                return x >= 0 && x < gridSize - 1 && y + 1 < gridSize;
            }
        }

        /// <summary>
        /// Evaluate a wall placement candidate
        /// </summary>
        private void EvaluateCandidate(GridSystem.Orientation o, int x, int y, bool valid, Vector3 mousePos, float spacing,
                                       ref float bestDistSq, ref bool found, ref UnifiedWallInfo best)
        {
            if (!valid) return;

            Vector3 center = GapCenter(o, x, y, spacing);
            float d2 = (mousePos - center).sqrMagnitude;

            // Apply margin: if within margin of current best, prefer to KEEP same orientation (reduces flicker)
            if (found && Mathf.Abs(d2 - bestDistSq) < gapSnapMargin * gapSnapMargin)
            {
                // Don't switch orientation if both candidates are nearly tied
                if (best.orientation == GridSystem.Orientation.Vertical && o == GridSystem.Orientation.Horizontal)
                    return; // keep vertical
            }

            if (d2 < bestDistSq)
            {
                bestDistSq = d2;
                best = new UnifiedWallInfo(o, x, y, center);
                found = true;
            }
        }

        /// <summary>
        /// Calculate the world position center of a wall gap using grid alignment
        /// </summary>
        private Vector3 GapCenter(GridSystem.Orientation o, int x, int y, float spacing)
        {
            // Use the WallManager's method which properly handles alignment
            return wallManager.GetWallWorldPosition(o, x, y);
        }

        /// <summary>
        /// Apply boundary constraints to ensure walls stay within grid bounds
        /// </summary>
        private Vector2Int ApplyBoundaryConstraints(GridSystem.Orientation orientation, int x, int y)
        {
            int gridSize = gridSystem.GetGridSize();
            
            if (orientation == GridSystem.Orientation.Horizontal)
            {
                // Horizontal walls span 2 tiles horizontally, so max x is gridSize-2
                int maxX = gridSize - 2;
                x = Mathf.Clamp(x, 0, maxX);
                
                // Horizontal walls separate rows, so max y is gridSize-2
                int maxY = gridSize - 2;
                y = Mathf.Clamp(y, 0, maxY);
            }
            else // Vertical
            {
                // Vertical walls separate columns, so max x is gridSize-2
                int maxX = gridSize - 2;
                x = Mathf.Clamp(x, 0, maxX);
                
                // Vertical walls span 2 tiles vertically, so max y is gridSize-2
                int maxY = gridSize - 2;
                y = Mathf.Clamp(y, 0, maxY);
            }
            
            return new Vector2Int(x, y);
        }

        // --- Debug harness using unified GridSystem ---
        public void RunAutomaticWallTest()
        {
            Debug.Log("=== UNIFIED WALL PLACEMENT TEST ===");
            
            // Test placing a horizontal wall
            Vector3 testPos = wallManager.GetWallWorldPosition(GridSystem.Orientation.Horizontal, 4, 7);
            Debug.Log($"Horizontal wall at (4,7) world position: {testPos}");
            
            if (TryPlaceWall(testPos))
            {
                Debug.Log("Successfully placed horizontal wall at (4,7)");
            }
            else
            {
                Debug.Log("Failed to place horizontal wall at (4,7)");
            }
            
            // Test wall positioning accuracy
            TestWallPositioning();
        }

        private void TestWallPositioning()
        {
            Debug.Log("=== WALL POSITIONING ACCURACY TEST ===");
            
            // Test horizontal wall positioning
            Vector3 horizontalPos = wallManager.GetWallWorldPosition(GridSystem.Orientation.Horizontal, 2, 3);
            Vector3 tile1 = gridSystem.GridToWorldPosition(new Vector2Int(2, 3));     // Bottom-left tile
            Vector3 tile2 = gridSystem.GridToWorldPosition(new Vector2Int(3, 4));     // Top-right tile
            Vector3 expectedPos = (tile1 + tile2) / 2f;
            
            Debug.Log($"Horizontal wall (2,3): Actual={horizontalPos}, Expected={expectedPos}, Diff={Vector3.Distance(horizontalPos, expectedPos)}");
            
            // Test vertical wall positioning
            Vector3 verticalPos = wallManager.GetWallWorldPosition(GridSystem.Orientation.Vertical, 2, 3);
            Vector3 expectedVertical = (tile1 + tile2) / 2f; // Same calculation for intersection point
            
            Debug.Log($"Vertical wall (2,3): Actual={verticalPos}, Expected={expectedVertical}, Diff={Vector3.Distance(verticalPos, expectedVertical)}");
            
            // Test wall scaling
            Vector3 horizontalScale = wallManager.GetWallScale(GridSystem.Orientation.Horizontal);
            Vector3 verticalScale = wallManager.GetWallScale(GridSystem.Orientation.Vertical);
            
            var settings = gridSystem.GetGridSettings();
            float expectedLength = (settings.tileSize * 2f) + settings.tileGap;
            
            Debug.Log($"Horizontal scale: {horizontalScale} (expected length: {expectedLength})");
            Debug.Log($"Vertical scale: {verticalScale} (expected length: {expectedLength})");
        }

        public void TestWallBlocking()
        {
            Debug.Log("=== UNIFIED WALL BLOCKING TEST ===");
            
            Vector2Int testPawn = new Vector2Int(4, 4);
            
            // Test placing a vertical wall that should block rightward movement
            Vector3 verticalWallPos = wallManager.GetWallWorldPosition(GridSystem.Orientation.Vertical, 4, 4);
            if (TryPlaceWall(verticalWallPos))
            {
                Debug.Log("Placed vertical wall at (4,4)");
                
                // Test if rightward movement is blocked
                List<Vector2Int> validMoves = gridSystem.GetValidMoves(testPawn);
                bool canMoveRight = validMoves.Contains(testPawn + Vector2Int.right);
                Debug.Log($"Movement RIGHT from (4,4): {(canMoveRight ? "ALLOWED" : "BLOCKED")} (should be BLOCKED)");
            }
            
            // Clear and test pathfinding
            Vector2Int player = gameManager.playerPosition;
            Vector2Int opponent = gameManager.opponentPosition;
            
            bool playerCanReachGoal = gridSystem.PathExists(player, new Vector2Int(player.x, gridSystem.GetGridSize() - 1));
            bool opponentCanReachGoal = gridSystem.PathExists(opponent, new Vector2Int(opponent.x, 0));
            
            Debug.Log($"Player can reach goal: {playerCanReachGoal}");
            Debug.Log($"Opponent can reach goal: {opponentCanReachGoal}");
        }
        
        // Removed old movement blocking methods - now using GridSystem.GetValidMoves() for unified approach
    }
}
