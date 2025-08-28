using UnityEngine;
using System.Collections.Generic;

namespace WallChess
{
    /// <summary>
    /// Thin orchestrator MonoBehaviour that wires together subsystems.
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
        private WallState state;
        private GapDetector gaps;
        private WallValidator validator;
        private WallVisuals visuals;
        private WallPlacementController placement;

        void Awake()
        {
            gameManager = FindObjectOfType<WallChessGameManager>();
            grid = gameManager != null ? gameManager.GetGridSystem() : null;

            float spacing = (gameManager?.tileSize ?? 1f) + (gameManager?.tileGap ?? 0.2f);
            state = new WallState(
                () => gameManager?.gridSize ?? 8,
                spacing
            );

            gaps = new GapDetector(state, laneSnapMargin, unlockMultiplier, gapSnapMargin);
            validator = new WallValidator(state, gameManager);
            visuals = new WallVisuals(wallPrefab, wallMaterial, validPreviewColor, invalidPreviewColor, placingPreviewColor);

            placement = new WallPlacementController(gameManager, state, gaps, validator, visuals, placementPlaneZ);
        }

        void Update()
        {
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