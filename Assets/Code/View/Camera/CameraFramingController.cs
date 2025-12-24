using System;
using UnityEngine;
using WallChess.Core.Config;

namespace WallChess.View.Camera
{
    /// <summary>
    /// Result of camera framing calculation.
    /// </summary>
    public readonly struct CameraFramingResult
    {
        public readonly float Distance;
        public readonly float PitchDegrees;
        public readonly Vector3 Position;
        public readonly Quaternion Rotation;
        public readonly Bounds FrameBounds;

        public CameraFramingResult(float distance, float pitchDegrees, Vector3 position, Quaternion rotation, Bounds frameBounds)
        {
            Distance = distance;
            PitchDegrees = pitchDegrees;
            Position = position;
            Rotation = rotation;
            FrameBounds = frameBounds;
        }
    }

    /// <summary>
    /// Controls camera positioning to match the perspective trapezoid and frame the grid.
    /// Positions the camera on a "boomstick" from world origin, always looking at (0,0,0).
    /// </summary>
    [RequireComponent(typeof(UnityEngine.Camera))]
    public class CameraFramingController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private CameraFramingConfig framingConfig;
        [SerializeField] private BoardConfig boardConfig;

        [Header("Backdrop (Optional)")]
        [Tooltip("If assigned, this sprite will be sized to fill the visible ground area.")]
        [SerializeField] private SpriteRenderer backdropSprite;

        [Header("Runtime Debug")]
        [SerializeField] private float calculatedDistance;
        [SerializeField] private float calculatedPitch;
        [SerializeField] private Bounds currentFrameBounds;

        private UnityEngine.Camera _camera;
        private bool _isInitialized;

        // Runtime grid dimensions (override config values when set)
        private int _runtimeGridWidth;
        private int _runtimeGridHeight;
        private bool _useRuntimeDimensions;

        /// <summary>
        /// Event fired when framing is recalculated.
        /// </summary>
        public event Action<Bounds> OnFramingCalculated;

        /// <summary>
        /// The camera being controlled.
        /// </summary>
        public UnityEngine.Camera TargetCamera => _camera;

        /// <summary>
        /// Current frame bounds in world space.
        /// </summary>
        public Bounds CurrentFrameBounds => currentFrameBounds;

        /// <summary>
        /// Current framing configuration.
        /// </summary>
        public CameraFramingConfig FramingConfig => framingConfig;

        /// <summary>
        /// Current board configuration.
        /// </summary>
        public BoardConfig BoardConfig => boardConfig;

        #region Unity Lifecycle

        private void Awake()
        {
            _camera = GetComponent<UnityEngine.Camera>();
        }

        private void OnValidate()
        {
            if (Application.isPlaying && _isInitialized)
            {
                RecalculateFraming();
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initialize the camera framing controller with configuration.
        /// </summary>
        public void Initialize(BoardConfig board, CameraFramingConfig framing)
        {
            boardConfig = board;
            framingConfig = framing;

            if (_camera == null)
                _camera = GetComponent<UnityEngine.Camera>();

            _isInitialized = true;
            RecalculateFraming();
        }

        /// <summary>
        /// Initialize the camera framing controller with configuration and runtime grid dimensions.
        /// Use this overload when the actual game board dimensions differ from BoardConfig defaults.
        /// </summary>
        public void Initialize(BoardConfig board, CameraFramingConfig framing, int gridWidth, int gridHeight)
        {
            boardConfig = board;
            framingConfig = framing;
            
            // Store runtime dimensions
            _runtimeGridWidth = gridWidth;
            _runtimeGridHeight = gridHeight;
            _useRuntimeDimensions = true;

            if (_camera == null)
                _camera = GetComponent<UnityEngine.Camera>();

            _isInitialized = true;
            RecalculateFraming();
        }

        /// <summary>
        /// Recalculate and apply camera framing.
        /// </summary>
        public void RecalculateFraming()
        {
            if (framingConfig == null || boardConfig == null || _camera == null)
            {
                Debug.LogWarning("CameraFramingController: Missing configuration or camera reference.");
                return;
            }

            var result = CalculateCameraParameters();

            // Store debug values
            calculatedDistance = result.Distance;
            calculatedPitch = result.PitchDegrees;
            currentFrameBounds = result.FrameBounds;

            // Apply to camera
            ApplyToCamera(result);

            // Update backdrop if assigned
            UpdateBackdrop();

            // Notify listeners
            OnFramingCalculated?.Invoke(result.FrameBounds);
        }

        /// <summary>
        /// Calculate camera parameters without applying them.
        /// Useful for editor preview.
        /// </summary>
        public CameraFramingResult CalculateCameraParametersPreview(CameraFramingConfig config, BoardConfig board)
        {
            if (config == null || board == null)
                return default;

            var gridBounds = CalculateGridBounds(board);

            // Use the camera's actual FOV when available, otherwise use config as fallback
            float fov = _camera != null ? _camera.fieldOfView : config.fieldOfView;

            return CalculateCameraParametersInternal(
                config.Trapezoid,
                gridBounds,
                config.paddingPercentage,
                fov,
                _camera != null ? _camera.aspect : 16f / 9f,
                config.minCameraDistance,
                config.maxCameraDistance
            );
        }

        #endregion

        #region Private Methods

        private CameraFramingResult CalculateCameraParameters()
        {
            var gridBounds = CalculateGridBounds(boardConfig);

            // Use the camera's actual FOV, not config FOV
            float cameraFov = _camera.fieldOfView;

            return CalculateCameraParametersInternal(
                framingConfig.Trapezoid,
                gridBounds,
                framingConfig.paddingPercentage,
                cameraFov,
                _camera.aspect,
                framingConfig.minCameraDistance,
                framingConfig.maxCameraDistance
            );
        }

        private CameraFramingResult CalculateCameraParametersInternal(
            PerspectiveTrapezoid trapezoid,
            Bounds gridBounds,
            float paddingPercent,
            float fov,
            float aspectRatio,
            float minDistance,
            float maxDistance)
        {
            // Step 1: Calculate pitch from convergence ratio (defines perspective look)
            float pitchDeg = CameraFramingMath.CalculatePitchForConvergence(
                trapezoid.ConvergenceRatio,
                fov * Mathf.Deg2Rad
            );

            // Step 2: Calculate padded grid bounds (this is what we need to frame)
            float gridWidth = gridBounds.size.x;
            float gridDepth = gridBounds.size.z;
            float maxDim = Mathf.Max(gridWidth, gridDepth);
            float padding = maxDim * paddingPercent;
            
            float framedWidth = gridWidth + padding * 2f;
            float framedDepth = gridDepth + padding * 2f;

            // Step 3: Calculate distance to frame the padded grid at this pitch
            float distance = CameraFramingMath.CalculateDistanceToFrameGrid(
                framedWidth,
                framedDepth,
                pitchDeg,
                fov,
                aspectRatio
            );

            // Clamp distance
            distance = Mathf.Clamp(distance, minDistance, maxDistance);

            // Step 4: Calculate camera position with proper centering
            Vector3 position = CameraFramingMath.CalculateCameraPositionCentered(distance, pitchDeg, fov);
            
            // Step 4b: Apply half-grid-depth offset for steeper perspective appearance
            // Move camera FORWARD (toward grid) by half the grid's half-depth
            // This creates a steeper viewing angle and more pronounced perspective
            float gridHalfDepth = gridDepth * 0.5f;
            position.z += gridHalfDepth * 0.5f;
            
            // Step 4c: Adjust pitch to compensate - camera needs to look MORE steeply down
            // to keep the grid center in view after moving forward
            float pitchAdjustment = Mathf.Atan2(gridHalfDepth * 0.5f, position.y) * Mathf.Rad2Deg;
            float adjustedPitch = pitchDeg + pitchAdjustment;
            
            Quaternion rotation = CameraFramingMath.CalculateCameraRotation(adjustedPitch);

            // Step 5: Frame bounds is the padded grid area
            var frameBounds = new Bounds(
                Vector3.zero,
                new Vector3(framedWidth, 0.1f, framedDepth)
            );

            return new CameraFramingResult(distance, adjustedPitch, position, rotation, frameBounds);
        }

        private Bounds CalculateGridBounds(BoardConfig board)
        {
            float width, height;
            
            if (_useRuntimeDimensions && _runtimeGridWidth > 0 && _runtimeGridHeight > 0)
            {
                // Use runtime dimensions with board's tile sizing
                float tileSpacing = board.TileSpacing;
                float tileGap = board.tileGap;
                width = _runtimeGridWidth * tileSpacing - tileGap;
                height = _runtimeGridHeight * tileSpacing - tileGap;
            }
            else
            {
                // Fall back to static config dimensions
                width = board.BoardWorldWidth;
                height = board.BoardWorldHeight;
            }

            // Grid is centered at origin on XZ plane
            return new Bounds(
                Vector3.zero,
                new Vector3(width, 0.1f, height)
            );
        }

        private void ApplyToCamera(CameraFramingResult result)
        {
            _camera.transform.position = result.Position;
            _camera.transform.rotation = result.Rotation;
            // FOV is taken from the camera itself, not overridden from config
        }

        private void UpdateBackdrop()
        {
            if (backdropSprite == null || backdropSprite.sprite == null)
                return;

            // Get sprite dimensions in world units (before scaling)
            var sprite = backdropSprite.sprite;
            float spriteWidth = sprite.bounds.size.x;
            float spriteHeight = sprite.bounds.size.y;

            if (spriteWidth <= 0 || spriteHeight <= 0)
                return;

            // Use the same frame bounds we calculated for camera framing
            float targetWidth = currentFrameBounds.size.x;
            float targetDepth = currentFrameBounds.size.z;
            
            // Add backdrop margin on top
            float margin = framingConfig.backdropMargin;
            float maxDim = Mathf.Max(targetWidth, targetDepth);
            targetWidth += maxDim * margin * 2f;
            targetDepth += maxDim * margin * 2f;

            // Scale sprite to match frame bounds
            float scaleX = targetWidth / spriteWidth;
            float scaleZ = targetDepth / spriteHeight;
            float uniformScale = Mathf.Max(scaleX, scaleZ);

            // Apply scale and position at grid center
            backdropSprite.transform.localScale = new Vector3(uniformScale, uniformScale, 1f);
            backdropSprite.transform.position = new Vector3(
                currentFrameBounds.center.x, 
                framingConfig.backdropYOffset, 
                currentFrameBounds.center.z
            );
        }

        #endregion

        #region Editor Support

#if UNITY_EDITOR
        /// <summary>
        /// Apply current configuration to camera in edit mode.
        /// </summary>
        [ContextMenu("Apply Framing Now")]
        public void ApplyFramingInEditor()
        {
            if (_camera == null)
                _camera = GetComponent<UnityEngine.Camera>();

            if (framingConfig != null && boardConfig != null)
            {
                RecalculateFraming();
                Debug.Log($"Camera framing applied: pitch={calculatedPitch:F1}°, distance={calculatedDistance:F1}");
            }
            else
            {
                Debug.LogWarning("Cannot apply framing: missing config references");
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (framingConfig == null) return;

            // Draw trapezoid on ground plane
            var trapezoid = framingConfig.Trapezoid;
            var corners = trapezoid.GetCornersArray();

            Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.5f);

            // Draw edges
            for (int i = 0; i < 4; i++)
            {
                Gizmos.DrawLine(corners[i], corners[(i + 1) % 4]);
            }

            // Draw frame bounds if initialized
            if (currentFrameBounds.size.x > 0)
            {
                Gizmos.color = new Color(0.8f, 0.8f, 0.2f, 0.3f);
                Gizmos.DrawWireCube(currentFrameBounds.center, currentFrameBounds.size);
            }
        }
#endif

        #endregion
    }
}
