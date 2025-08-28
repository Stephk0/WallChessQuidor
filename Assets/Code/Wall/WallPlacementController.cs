using UnityEngine;

namespace WallChess
{
    /// <summary>
    /// Handles input flow, preview, placement, and debug tests.
    /// </summary>
    public class WallPlacementController
    {
        private readonly WallChessGameManager gm;
        private readonly WallState s;
        private readonly GapDetector gaps;
        private readonly WallValidator validator;
        private readonly WallVisuals visuals;
        private readonly float planeZ;

        private bool isPlacing = false;

        public WallPlacementController(WallChessGameManager gm, WallState state, GapDetector gaps, WallValidator validator, WallVisuals visuals, float placementPlaneZ)
        {
            this.gm = gm; this.s = state; this.gaps = gaps; this.validator = validator; this.visuals = visuals; this.planeZ = placementPlaneZ;
        }

        public void Tick()
        {
            if (Input.GetMouseButtonDown(0) && gm.CanInitiateWallPlacement())
            {
                if (!IsClickingOnAvatar() && gm.TryStartWallPlacement())
                    isPlacing = true;
            }
            else if (Input.GetMouseButton(0) && isPlacing)
            {
                Vector3 mouse = GetMouseWorld();
                if (!IsWithinBounds(mouse)) { visuals.HidePreview(); return; }

                if (gaps.TryFind(mouse, gm, out var info))
                {
                    bool can = validator.CanPlace(info);
                    visuals.UpdatePreview(info.pos, info.scale, can);
                }
                else visuals.HidePreview();
            }
            else if (Input.GetMouseButtonUp(0) && isPlacing)
            {
                TryCommitAtMouse();
                isPlacing = false;
            }
            else if (Input.GetMouseButtonUp(0) && gm.GetCurrentState() == GameState.WallPlacement && !isPlacing)
            {
                gm.CompleteWallPlacement(false);
            }
        }

        public bool TryPlaceWall(Vector3 worldPosition)
        {
            if (!gm.CanInitiateWallPlacement()) return false;
            if (!gm.TryStartWallPlacement()) return false;
            if (gaps.TryFind(worldPosition, gm, out var info) && validator.CanPlace(info))
            {
                Commit(info);
                return true;
            }
            gm.CompleteWallPlacement(false);
            return false;
        }

        void TryCommitAtMouse()
        {
            bool success = false;
            Vector3 mouse = GetMouseWorld();
            if (IsWithinBounds(mouse) && gaps.TryFind(mouse, gm, out var info) && validator.CanPlace(info))
            {
                Commit(info);
                success = true;
            }
            visuals.CleanupPreview();
            gm.CompleteWallPlacement(success);
        }

        void Commit(GapDetector.WallInfo info)
        {
            visuals.CreateWall(info, s);
            if (info.orientation == WallState.Orientation.Horizontal)
            {
                s.SetOccupied(WallState.Orientation.Horizontal, info.x, info.y, true);
                s.SetOccupied(WallState.Orientation.Horizontal, info.x + 1, info.y, true);
            }
            else
            {
                s.SetOccupied(WallState.Orientation.Vertical, info.x, info.y, true);
                s.SetOccupied(WallState.Orientation.Vertical, info.x, info.y + 1, true);
            }
            // optionally sync with GridSystem if needed
           /*
            gm.GetGridSystem()?.SetGapOccupied(
                info.orientation == WallState.Orientation.Horizontal ? GridSystem.Orientation.Horizontal : GridSystem.Orientation.Vertical,
                info.x, info.y, true
            
            );
           */
        }

        bool IsClickingOnAvatar()
        {
            Vector3 mouse = GetMouseWorld();
            Vector3 p = gm.GetGridSystem().GridToWorldPosition(gm.playerPosition);
            if (Vector3.Distance(mouse, p) < gm.tileSize * 0.6f) return true;
            Vector3 o = gm.GetGridSystem().GridToWorldPosition(gm.opponentPosition);
            if (Vector3.Distance(mouse, o) < gm.tileSize * 0.6f) return true;
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
            float gridMargin = s.spacing * 0.75f;
            float min = -gridMargin;
            float max = (s.Cells - 1) * s.spacing + gridMargin;
            return p.x >= min && p.x <= max && p.y >= min && p.y <= max;
        }

        // --- Debug harness preserved from original ---
        public void RunAutomaticWallTest()
        {
            UnityEngine.Debug.Log("=== AUTOMATIC WALL BLOCKING TEST ===");
            s.ClearGaps();
            // horizontal at (4,7)
            s.SetOccupied(WallState.Orientation.Horizontal, 4, 7, true);
            s.SetOccupied(WallState.Orientation.Horizontal, 5, 7, true);
            bool b1 = IsMovementBlocked(new Vector2Int(4, 8), new Vector2Int(4, 7));
            bool b2 = IsMovementBlocked(new Vector2Int(5, 8), new Vector2Int(5, 7));
            bool b3 = IsMovementBlocked(new Vector2Int(6, 8), new Vector2Int(6, 7));
            UnityEngine.Debug.Log($"DOWN (4,8)->(4,7): {(b1 ? "BLOCKED" : "ALLOWED")} | (5,8)->(5,7): {(b2 ? "BLOCKED" : "ALLOWED")} | (6,8)->(6,7): {(b3 ? "BLOCKED" : "ALLOWED")}");

            s.ClearGaps();
            Vector2Int pawn = new Vector2Int(4,4);
            s.SetOccupied(WallState.Orientation.Vertical, 4, 4, true);
            s.SetOccupied(WallState.Orientation.Vertical, 4, 5, true);
            bool rb = IsMovementBlocked(pawn, pawn + Vector2Int.right);
            s.ClearGaps();
            s.SetOccupied(WallState.Orientation.Vertical, 3, 4, true);
            s.SetOccupied(WallState.Orientation.Vertical, 3, 5, true);
            bool lb = IsMovementBlocked(pawn, pawn + Vector2Int.left);
            UnityEngine.Debug.Log($"RIGHT blocked: {rb}, LEFT blocked: {lb}");
        }

        public void TestWallBlocking()
        {
            UnityEngine.Debug.Log("=== TESTING WALL BLOCKING LOGIC ===");
            Vector2Int pawn = new Vector2Int(4, 4);
            s.ClearGaps();
            int rightGapX = pawn.x, rightGapY = pawn.y;
            if (rightGapX >= 0 && rightGapX < s.VCols && rightGapY >= 0 && rightGapY + 1 < s.VRows)
            {
                s.SetOccupied(WallState.Orientation.Vertical, rightGapX, rightGapY, true);
                s.SetOccupied(WallState.Orientation.Vertical, rightGapX, rightGapY + 1, true);
                bool rightBlocked = IsMovementBlocked(pawn, pawn + Vector2Int.right);
                UnityEngine.Debug.Log($"Movement RIGHT: {(rightBlocked ? "BLOCKED" : "ALLOWED")} (should be BLOCKED)");
            }
            s.ClearGaps();
            int leftGapX = pawn.x - 1, leftGapY = pawn.y;
            if (leftGapX >= 0 && leftGapX < s.VCols && leftGapY >= 0 && leftGapY + 1 < s.VRows)
            {
                s.SetOccupied(WallState.Orientation.Vertical, leftGapX, leftGapY, true);
                s.SetOccupied(WallState.Orientation.Vertical, leftGapX, leftGapY + 1, true);
                bool leftBlocked = IsMovementBlocked(pawn, pawn + Vector2Int.left);
                UnityEngine.Debug.Log($"Movement LEFT: {(leftBlocked ? "BLOCKED" : "ALLOWED")} (should be BLOCKED)");
            }
        }

        public bool IsMovementBlocked(Vector2Int from, Vector2Int to)
        {
            var diff = to - from;
            if (diff.y == 1) return IsH(from.x, from.y);
            if (diff.y == -1) return IsH(from.x, to.y);
            if (diff.x == 1) return IsV(from.x, from.y);
            if (diff.x == -1) return IsV(to.x, from.y);
            return false;
        }

        bool IsH(int gapX, int gapY)
        {
            if (gapX < 0 || gapY < 0 || gapY >= s.HRows) return false;
            if (gapX + 1 < s.HCols &&
                s.IsOccupied(WallState.Orientation.Horizontal, gapX, gapY) &&
                s.IsOccupied(WallState.Orientation.Horizontal, gapX + 1, gapY)) return true;
            if (gapX - 1 >= 0 &&
                s.IsOccupied(WallState.Orientation.Horizontal, gapX - 1, gapY) &&
                s.IsOccupied(WallState.Orientation.Horizontal, gapX, gapY)) return true;
            return false;
        }

        bool IsV(int gapX, int gapY)
        {
            if (gapX < 0 || gapY < 0 || gapX >= s.VCols) return false;
            if (gapY + 1 < s.VRows &&
                s.IsOccupied(WallState.Orientation.Vertical, gapX, gapY) &&
                s.IsOccupied(WallState.Orientation.Vertical, gapX, gapY + 1)) return true;
            if (gapY - 1 >= 0 &&
                s.IsOccupied(WallState.Orientation.Vertical, gapX, gapY - 1) &&
                s.IsOccupied(WallState.Orientation.Vertical, gapX, gapY)) return true;
            return false;
        }
    }
}
