using UnityEngine;
using WallChess.Core.Data;

namespace WallChess.Core.Config
{
    /// <summary>
    /// Board dimensions and visual settings.
    /// Create via: Assets > Create > WallChess > Board Config
    /// </summary>
    [CreateAssetMenu(fileName = "BoardConfig", menuName = "WallChess/Board Config")]
    public class BoardConfig : ScriptableObject
    {
        [Header("Grid Dimensions")]
        [Range(5, 15)]
        [Tooltip("Number of tiles on the X axis (horizontal). Standard Quoridor is 9")]
        public int gridWidth = 9;

        [Range(5, 15)]
        [Tooltip("Number of tiles on the Y axis (vertical). Standard Quoridor is 9")]
        public int gridHeight = 9;

        [Header("Tile Settings")]
        [Tooltip("Size of each tile in world units")]
        public float tileSize = 1f;

        [Tooltip("Gap between tiles where walls are placed")]
        public float tileGap = 0.15f;

        [Header("Wall Settings")]
        [Tooltip("Height of walls")]
        public float wallHeight = 0.5f;

        [Tooltip("Thickness of walls (fills the gap)")]
        public float wallThickness = 0.14f;

        [Header("Pawn Settings")]
        [Tooltip("Height offset for pawn placement above tiles")]
        public float pawnHeightOffset = 0.1f;

        [Header("Camera Settings")]
        [Tooltip("Camera framing configuration for perspective matching and grid framing.")]
        public CameraFramingConfig cameraFramingConfig;

        [Tooltip("Angle of camera view in degrees (legacy, prefer using CameraFramingConfig)")]
        [Range(30f, 60f)]
        public float cameraAngle = 45f;

        [Tooltip("Distance multiplier for camera (legacy, prefer using CameraFramingConfig)")]
        [Range(0.8f, 2f)]
        public float cameraDistanceMultiplier = 1.2f;

        // Computed properties
        public float TileSpacing => tileSize + tileGap;
        public float BoardWorldWidth => gridWidth * TileSpacing - tileGap;
        public float BoardWorldHeight => gridHeight * TileSpacing - tileGap;
        public float BoardHalfWidth => BoardWorldWidth * 0.5f;
        public float BoardHalfHeight => BoardWorldHeight * 0.5f;

        /// <summary>
        /// Whether the board is square (width equals height).
        /// </summary>
        public bool IsSquare => gridWidth == gridHeight;

        /// <summary>
        /// For backwards compatibility. Returns gridWidth for square boards, throws for non-square.
        /// Prefer using gridWidth/gridHeight directly.
        /// </summary>
        public int GridSize
        {
            get
            {
                if (!IsSquare)
                    Debug.LogWarning($"BoardConfig.GridSize called on non-square board ({gridWidth}x{gridHeight}). Use gridWidth/gridHeight instead.");
                return gridWidth;
            }
        }

        /// <summary>
        /// Converts grid position to world position (center of tile).
        /// </summary>
        public Vector3 GridToWorld(BoardPosition gridPos)
        {
            float x = (gridPos.X - (gridWidth - 1) * 0.5f) * TileSpacing;
            float z = (gridPos.Y - (gridHeight - 1) * 0.5f) * TileSpacing;
            return new Vector3(x, 0, z);
        }

        /// <summary>
        /// Converts world position to nearest grid position.
        /// </summary>
        public BoardPosition WorldToGrid(Vector3 worldPos)
        {
            int x = Mathf.RoundToInt(worldPos.x / TileSpacing + (gridWidth - 1) * 0.5f);
            int y = Mathf.RoundToInt(worldPos.z / TileSpacing + (gridHeight - 1) * 0.5f);
            return new BoardPosition(
                Mathf.Clamp(x, 0, gridWidth - 1),
                Mathf.Clamp(y, 0, gridHeight - 1)
            );
        }

        /// <summary>
        /// Gets wall world position for a wall placement.
        /// </summary>
        public Vector3 GetWallWorldPosition(BoardPosition gapPos, WallOrientation orientation)
        {
            Vector3 pos = GridToWorld(gapPos);

            if (orientation == WallOrientation.Horizontal)
            {
                // Wall sits between rows, offset north
                pos.z += TileSpacing * 0.5f;
                pos.x += TileSpacing * 0.5f; // Center over two cells
            }
            else
            {
                // Wall sits between columns, offset east
                pos.x += TileSpacing * 0.5f;
                pos.z += TileSpacing * 0.5f;
            }

            pos.y = wallHeight * 0.5f;
            return pos;
        }

        /// <summary>
        /// Gets wall scale based on orientation.
        /// </summary>
        public Vector3 GetWallScale(WallOrientation orientation)
        {
            float length = tileSize * 2 + tileGap;
            if (orientation == WallOrientation.Horizontal)
                return new Vector3(length, wallHeight, wallThickness);
            else
                return new Vector3(wallThickness, wallHeight, length);
        }

        /// <summary>
        /// Gets wall rotation based on orientation.
        /// </summary>
        public Quaternion GetWallRotation(WallOrientation orientation)
        {
            return orientation == WallOrientation.Vertical
                ? Quaternion.Euler(0, 90, 0)
                : Quaternion.identity;
        }

        private void OnValidate()
        {
            gridWidth = Mathf.Clamp(gridWidth, 5, 15);
            gridHeight = Mathf.Clamp(gridHeight, 5, 15);
            if (tileSize < 0.1f) tileSize = 0.1f;
            if (tileGap < 0.01f) tileGap = 0.01f;
        }
    }
}
