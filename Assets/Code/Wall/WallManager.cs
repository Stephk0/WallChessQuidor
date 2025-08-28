using UnityEngine;
using System.Collections.Generic;
using WallChess.Grid;

namespace WallChess
{
    /// <summary>
    /// Thin orchestrator MonoBehaviour that wires together subsystems.
    /// Now properly integrates with GridSystem alignment.
    /// </summary>
    public class WallManager : MonoBehaviour
    {
        [Header("Assets")]
        [SerializeField] private Material wallMaterial;
        [SerializeField] private GameObject wallPrefab;

        [Header("Placement Visuals")]
        [SerializeField] private Color validPreviewColor = new Color(0, 1, 0, 0.7f);
        [SerializeField] private Color invalidPreviewColor = new Color(1, 0, 0, 0.7f);
        [SerializeField] private Color placingPreviewColor = new Color(1, 1, 0, 0.5f);
        [SerializeField] private float placementPlaneZ = 0f;

        [Header("Snap & Lanes")]
        [SerializeField] private float gapSnapMargin = 0.25f;
        [SerializeField] private float laneSnapMargin = 0.3f;
        [SerializeField] private float unlockMultiplier = 1.5f;

        private WallChessGameManager gameManager;
        private GridSystem grid;
        private GridCoordinateConverter coordinateConverter;
        private WallState state;
        private GapDetector gaps;
        private WallValidator validator;
        private WallVisuals visuals;
        private WallPlacementController placement;

        public void Initialize(WallChessGameManager gm) // force recompile
        {
            gameManager = gm;
            grid = gameManager != null ? gameManager.GetGridSystem() : null;

            if (grid == null)
            {
                Debug.LogError("WallManager: GridSystem not found! WallManager requires GridSystem to be initialized first.");
                return;
            }

            // Get the coordinate converter from the grid system to respect alignment
            coordinateConverter = GetCoordinateConverterFromGrid();
            if (coordinateConverter == null)
            {
                Debug.LogError("WallManager: Could not get coordinate converter from GridSystem!");
                return;
            }

            float spacing = grid.GetTileSpacing();
            state = new WallState(
                () => gameManager?.gridSize ?? 8,
                spacing,
                coordinateConverter
            );

            gaps = new GapDetector(state, laneSnapMargin, unlockMultiplier, gapSnapMargin);
            validator = new WallValidator(state, gameManager);
            visuals = new WallVisuals(wallPrefab, wallMaterial, validPreviewColor, invalidPreviewColor, placingPreviewColor);

            placement = new WallPlacementController(gameManager, state, gaps, validator, visuals, placementPlaneZ);
        }

        /// <summary>
        /// Gets the coordinate converter from the grid system using reflection
        /// since GridCoordinateConverter is private in GridSystem
        /// </summary>
        private GridCoordinateConverter GetCoordinateConverterFromGrid()
        {
            var field = typeof(GridSystem).GetField("coordinateConverter", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (field != null)
            {
                return (GridCoordinateConverter)field.GetValue(grid);
            }

            // Fallback: create our own converter using grid settings
            var settings = grid.GetGridSettings();
            var alignment = grid.GetGridAlignment();
            return new GridCoordinateConverter(settings.TileSpacing, settings.gridSize, alignment);
        }

        void Update()
        {
            if (coordinateConverter == null) return;

            // debug hooks kept from original
            if (Input.GetKeyDown(KeyCode.Y)) placement.RunAutomaticWallTest();
            if (Input.GetKeyDown(KeyCode.T)) placement.TestWallBlocking();

            placement.Tick();
        }

        public bool TryPlaceWall(Vector3 worldPosition) => placement.TryPlaceWall(worldPosition);

        public void ClearAllWalls()
        {
            visuals.DestroyAll();
            state.ClearAll(grid, gameManager);
        }

        void OnDestroy()
        {
            visuals.CleanupPreview();
            ClearAllWalls();
        }

        // Public accessors for AI integration
        public GapDetector GetGapDetector() => gaps;
        public WallValidator GetWallValidator() => validator;
        public WallState GetWallState() => state;
        public WallVisuals GetWallVisuals() => visuals;
        public WallPlacementController GetPlacementController() => placement;
    }
}