using System;
using UnityEngine;

namespace WallChess.Core.Config
{
    /// <summary>
    /// Immutable struct representing a symmetric perspective trapezoid.
    /// Used to define the target perspective for camera matching.
    /// The trapezoid lies on the XZ plane with Y=0.
    /// </summary>
    [Serializable]
    public readonly struct PerspectiveTrapezoid : IEquatable<PerspectiveTrapezoid>
    {
        /// <summary>Width of the near (bottom) edge in world units.</summary>
        public readonly float NearWidth;

        /// <summary>Width of the far (top) edge in world units.</summary>
        public readonly float FarWidth;

        /// <summary>Distance from near to far edge (depth on Z axis).</summary>
        public readonly float Height;

        /// <summary>Offset of trapezoid center along Z axis. Positive = toward far, negative = toward near.</summary>
        public readonly float CenterOffsetZ;

        public PerspectiveTrapezoid(float nearWidth, float farWidth, float height, float centerOffsetZ = 0f)
        {
            NearWidth = Mathf.Max(0.1f, nearWidth);
            FarWidth = Mathf.Max(0.1f, farWidth);
            Height = Mathf.Max(0.1f, height);
            CenterOffsetZ = centerOffsetZ;
        }

        /// <summary>
        /// Ratio of far width to near width.
        /// Values less than 1 indicate perspective convergence (typical).
        /// Value of 1 indicates no convergence (orthographic-like).
        /// </summary>
        public float ConvergenceRatio => FarWidth / NearWidth;

        public float HalfNearWidth => NearWidth * 0.5f;
        public float HalfFarWidth => FarWidth * 0.5f;
        public float HalfHeight => Height * 0.5f;

        /// <summary>
        /// Gets the width at a given normalized depth (0=near, 1=far).
        /// </summary>
        public float GetWidthAtDepth(float normalizedDepth)
        {
            return Mathf.Lerp(NearWidth, FarWidth, normalizedDepth);
        }

        /// <summary>
        /// Gets the four corner positions on the XZ ground plane (Y=0).
        /// Near edge is at negative Z, far edge at positive Z, with optional center offset.
        /// </summary>
        /// <returns>Corners in order: BottomLeft, BottomRight, TopRight, TopLeft</returns>
        public (Vector3 bl, Vector3 br, Vector3 tr, Vector3 tl) GetCorners()
        {
            float halfNear = HalfNearWidth;
            float halfFar = HalfFarWidth;
            float halfH = HalfHeight;

            float nearZ = -halfH + CenterOffsetZ;
            float farZ = halfH + CenterOffsetZ;

            return (
                bl: new Vector3(-halfNear, 0, nearZ),
                br: new Vector3(halfNear, 0, nearZ),
                tr: new Vector3(halfFar, 0, farZ),
                tl: new Vector3(-halfFar, 0, farZ)
            );
        }

        /// <summary>
        /// Gets the four corners as an array suitable for Handles.DrawSolidRectangleWithOutline.
        /// Order: BottomLeft, BottomRight, TopRight, TopLeft
        /// </summary>
        public Vector3[] GetCornersArray()
        {
            var (bl, br, tr, tl) = GetCorners();
            return new Vector3[] { bl, br, tr, tl };
        }

        public bool Equals(PerspectiveTrapezoid other)
        {
            return Mathf.Approximately(NearWidth, other.NearWidth) &&
                   Mathf.Approximately(FarWidth, other.FarWidth) &&
                   Mathf.Approximately(Height, other.Height) &&
                   Mathf.Approximately(CenterOffsetZ, other.CenterOffsetZ);
        }

        public override bool Equals(object obj)
        {
            return obj is PerspectiveTrapezoid other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(NearWidth, FarWidth, Height, CenterOffsetZ);
        }

        public static bool operator ==(PerspectiveTrapezoid a, PerspectiveTrapezoid b) => a.Equals(b);
        public static bool operator !=(PerspectiveTrapezoid a, PerspectiveTrapezoid b) => !a.Equals(b);

        public override string ToString()
        {
            return $"Trapezoid(near:{NearWidth:F2}, far:{FarWidth:F2}, h:{Height:F2}, offset:{CenterOffsetZ:F2}, ratio:{ConvergenceRatio:F3})";
        }
    }

    /// <summary>
    /// Configuration for camera framing and perspective matching.
    /// Create via: Assets > Create > WallChess > Camera Framing Config
    /// </summary>
    [CreateAssetMenu(fileName = "CameraFramingConfig", menuName = "WallChess/Camera Framing Config")]
    public class CameraFramingConfig : ScriptableObject
    {
        [Header("Perspective Trapezoid")]
        [Tooltip("Width of the near (bottom) edge in world units. This is the wider edge at the front.")]
        [Min(0.1f)]
        public float nearWidth = 12f;

        [Tooltip("Width of the far (top) edge in world units. Smaller than near width creates perspective convergence.")]
        [Min(0.1f)]
        public float farWidth = 10f;

        [Tooltip("Height (depth) of the trapezoid from near to far edge in world units.")]
        [Min(0.1f)]
        public float trapezoidHeight = 14f;

        [Tooltip("Vertical offset of the trapezoid center. Positive moves toward far edge, negative toward near.")]
        public float centerOffsetZ = 0f;

        [Header("Grid Framing")]
        [Tooltip("Padding around grid as percentage of grid dimension (0.1 = 10%). Applied to all sides.")]
        [Range(0f, 0.5f)]
        public float paddingPercentage = 0.1f;

        [Header("Camera Settings")]
        [Tooltip("Camera field of view in degrees. Lower values create more orthographic look.")]
        [Range(1f, 60f)]
        public float fieldOfView = 10f;

        [Tooltip("Minimum distance from camera to ground plane.")]
        [Min(1f)]
        public float minCameraDistance = 10f;

        [Tooltip("Maximum distance from camera to ground plane.")]
        public float maxCameraDistance = 200f;

        [Header("Backdrop Settings")]
        [Tooltip("Additional margin beyond grid+padding for backdrop (percentage).")]
        [Range(0f, 0.5f)]
        public float backdropMargin = 0.05f;

        [Tooltip("Y offset for backdrop (negative = below grid plane).")]
        public float backdropYOffset = -0.01f;

        /// <summary>
        /// Gets the perspective trapezoid struct from current settings.
        /// </summary>
        public PerspectiveTrapezoid Trapezoid => new(nearWidth, farWidth, trapezoidHeight, centerOffsetZ);

        /// <summary>
        /// Calculates the camera pitch angle (X rotation) needed to match the trapezoid's convergence.
        /// </summary>
        /// <param name="verticalFovDeg">Camera's vertical FOV in degrees.</param>
        /// <returns>Pitch angle in degrees (0=horizontal, 90=straight down).</returns>
        public float CalculatePitchForConvergence(float verticalFovDeg)
        {
            return CameraFramingMath.CalculatePitchForConvergence(
                Trapezoid.ConvergenceRatio,
                verticalFovDeg * Mathf.Deg2Rad
            );
        }

        private void OnValidate()
        {
            nearWidth = Mathf.Max(0.1f, nearWidth);
            farWidth = Mathf.Max(0.1f, farWidth);
            trapezoidHeight = Mathf.Max(0.1f, trapezoidHeight);
            paddingPercentage = Mathf.Clamp01(paddingPercentage);
            minCameraDistance = Mathf.Max(1f, minCameraDistance);
            maxCameraDistance = Mathf.Max(minCameraDistance + 1f, maxCameraDistance);
        }
    }

    /// <summary>
    /// Static math utilities for camera framing calculations.
    /// </summary>
    public static class CameraFramingMath
    {
        /// <summary>
        /// Calculates the camera pitch angle needed to achieve a given convergence ratio.
        /// </summary>
        /// <param name="convergenceRatio">Target ratio farWidth/nearWidth (typically less than 1).</param>
        /// <param name="verticalFovRad">Camera's vertical FOV in radians.</param>
        /// <returns>Pitch angle in degrees (0=horizontal, 90=straight down).</returns>
        public static float CalculatePitchForConvergence(float convergenceRatio, float verticalFovRad)
        {
            // For a perspective camera looking at a ground plane:
            // When the camera is tilted down by angle θ from horizontal,
            // with vertical FOV φ, the visible frustum on the ground creates
            // a trapezoid where the 3D distance to each edge determines width.
            //
            // Near edge 3D distance: D_n = H / sin(θ - φ/2)
            // Far edge 3D distance:  D_f = H / sin(θ + φ/2)
            // Width is proportional to 3D distance, so:
            //   convergence = far/near = sin(θ - φ/2) / sin(θ + φ/2)
            //
            // Solving for θ:
            //   tan(θ) = tan(φ/2) × (c + 1) / (1 - c)
            //   θ = atan(tan(φ/2) × (c + 1) / (1 - c))
            // where c = convergence ratio (farWidth / nearWidth)

            if (Mathf.Approximately(convergenceRatio, 1f))
            {
                // No convergence = looking straight down
                return 90f;
            }

            float halfFov = verticalFovRad * 0.5f;

            // Avoid division by zero for extreme convergence ratios
            if (convergenceRatio <= 0.01f || convergenceRatio >= 0.99f)
            {
                return convergenceRatio < 0.5f ? 30f : 89f;
            }

            // Correct formula: factor = (c + 1) / (1 - c)
            float factor = (convergenceRatio + 1f) / (1f - convergenceRatio);
            float pitchRad = Mathf.Atan(factor * Mathf.Tan(halfFov));

            // Convert to degrees
            float pitchDeg = pitchRad * Mathf.Rad2Deg;

            // Clamp to reasonable range (must be looking down at ground)
            return Mathf.Clamp(pitchDeg, 15f, 89f);
        }

        /// <summary>
        /// Calculates the camera distance needed to frame given bounds with padding.
        /// </summary>
        /// <param name="gridBounds">World bounds of the grid.</param>
        /// <param name="paddingPercent">Padding as percentage of grid dimension (0-1).</param>
        /// <param name="pitchDeg">Camera pitch angle in degrees.</param>
        /// <param name="verticalFovDeg">Camera vertical FOV in degrees.</param>
        /// <param name="aspectRatio">Camera aspect ratio (width/height).</param>
        /// <returns>Distance from camera to origin.</returns>
        public static float CalculateFramingDistance(
            Bounds gridBounds,
            float paddingPercent,
            float pitchDeg,
            float verticalFovDeg,
            float aspectRatio)
        {
            // Add padding to bounds
            float maxDimension = Mathf.Max(gridBounds.size.x, gridBounds.size.z);
            float padding = maxDimension * paddingPercent;

            float framedWidth = gridBounds.size.x + padding * 2f;
            float framedDepth = gridBounds.size.z + padding * 2f;

            float verticalFovRad = verticalFovDeg * Mathf.Deg2Rad;
            float halfVertFov = verticalFovRad * 0.5f;
            float horizontalHalfFov = Mathf.Atan(Mathf.Tan(halfVertFov) * aspectRatio);
            
            float pitchRad = pitchDeg * Mathf.Deg2Rad;

            // For a tilted camera, the far edge (top of view) has less visible width
            // due to perspective. The grid must fit within this narrower far edge.
            //
            // At distance D with pitch θ:
            //   Camera height H = D × sin(θ)
            //   Far edge 3D distance D_f = H / sin(θ + φ/2)
            //   Far edge width W_f = 2 × D_f × tan(horizontal_half_fov)
            //
            // For grid to fit: framedWidth ≤ W_f
            //   D ≥ framedWidth × sin(θ + φ/2) / (2 × sin(θ) × tan(h_fov/2))

            float upperAngle = pitchRad + halfVertFov;
            float sinPitch = Mathf.Sin(pitchRad);
            float sinUpper = Mathf.Sin(Mathf.Min(upperAngle, Mathf.PI * 0.5f - 0.01f));
            
            // Distance needed to fit width at the far (narrower) edge
            float distForWidth = (framedWidth * 0.5f) * sinUpper / (sinPitch * Mathf.Tan(horizontalHalfFov));

            // Distance needed to fit depth (accounting for tilted view)
            float distForDepth = CalculateDistanceForTiltedDepth(framedDepth, pitchDeg, verticalFovDeg);

            return Mathf.Max(distForWidth, distForDepth);
        }

        /// <summary>
        /// Calculates the camera distance needed to show a trapezoid of given dimensions.
        /// </summary>
        /// <param name="trapezoidHeight">Height (depth) of the trapezoid on ground plane.</param>
        /// <param name="trapezoidFarWidth">Width at the far (narrower) edge of trapezoid.</param>
        /// <param name="pitchDeg">Camera pitch angle in degrees.</param>
        /// <param name="verticalFovDeg">Camera vertical FOV in degrees.</param>
        /// <param name="aspectRatio">Camera aspect ratio (width/height).</param>
        /// <returns>Distance from camera to origin.</returns>
        public static float CalculateDistanceToFrameGrid(
            float framedWidth,
            float framedDepth,
            float pitchDeg,
            float verticalFovDeg,
            float aspectRatio)
        {
            float verticalFovRad = verticalFovDeg * Mathf.Deg2Rad;
            float halfVertFov = verticalFovRad * 0.5f;
            float horizontalHalfFov = Mathf.Atan(Mathf.Tan(halfVertFov) * aspectRatio);
            float pitchRad = pitchDeg * Mathf.Deg2Rad;

            // For a tilted camera at pitch θ with vertical FOV φ:
            // The FAR edge of the view (looking more horizontal) sees a wider area on ground
            // The NEAR edge (looking more down) sees less ground distance
            //
            // At the far edge (angle θ + φ/2 from horizontal):
            //   3D distance to ground = H / sin(θ + φ/2)
            //   Visible width = 2 × (3D distance) × tan(horizontal_half_fov)
            //
            // Since far edge is narrower in perspective, we need to fit framedWidth there
            // Camera height H = D × sin(θ), so:
            //   D = framedWidth × sin(θ + φ/2) / (2 × sin(θ) × tan(h_fov/2))

            float upperAngle = pitchRad + halfVertFov;
            float sinPitch = Mathf.Sin(pitchRad);
            float sinUpper = Mathf.Sin(Mathf.Min(upperAngle, Mathf.PI * 0.5f - 0.01f));
            
            float distForWidth = (framedWidth * 0.5f) * sinUpper / 
                                 (sinPitch * Mathf.Tan(horizontalHalfFov));

            // Distance needed to fit depth (vertical span on ground)
            float distForDepth = CalculateDistanceForTiltedDepth(framedDepth, pitchDeg, verticalFovDeg);

            return Mathf.Max(distForWidth, distForDepth);
        }

        /// <summary>
        /// Calculates the distance needed to view a ground-plane depth at a tilted angle.
        /// </summary>
        private static float CalculateDistanceForTiltedDepth(
            float groundDepth,
            float pitchDeg,
            float verticalFovDeg)
        {
            float pitchRad = pitchDeg * Mathf.Deg2Rad;
            float halfFovRad = verticalFovDeg * Mathf.Deg2Rad * 0.5f;

            // When camera is tilted down by pitchDeg:
            // - Upper ray angle from horizontal = pitchDeg + halfFov
            // - Lower ray angle from horizontal = pitchDeg - halfFov
            //
            // Visible ground depth = H * (cot(lower) - cot(upper))
            // where H = D * sin(pitch) is camera height

            float upperAngle = pitchRad + halfFovRad;
            float lowerAngle = pitchRad - halfFovRad;

            // Clamp to avoid division issues near horizontal
            lowerAngle = Mathf.Max(lowerAngle, 0.1f);
            upperAngle = Mathf.Min(upperAngle, Mathf.PI * 0.5f - 0.01f);

            float cotUpper = 1f / Mathf.Tan(upperAngle);
            float cotLower = 1f / Mathf.Tan(lowerAngle);

            float depthFactor = cotLower - cotUpper;

            // Avoid division by zero
            if (depthFactor < 0.001f) depthFactor = 0.001f;

            float sinPitch = Mathf.Sin(pitchRad);

            // groundDepth = D * sinPitch * depthFactor
            // D = groundDepth / (sinPitch * depthFactor)
            return groundDepth / (sinPitch * depthFactor);
        }

        /// <summary>
        /// Calculates the camera position on a "boomstick" from the origin.
        /// </summary>
        /// <param name="distance">Distance from origin to camera.</param>
        /// <param name="pitchDeg">Pitch angle in degrees.</param>
        /// <returns>Camera world position.</returns>
        public static Vector3 CalculateCameraPosition(float distance, float pitchDeg)
        {
            // Simple boomstick position without FOV compensation
            // For centered framing, use CalculateCameraPositionCentered instead
            float pitchRad = pitchDeg * Mathf.Deg2Rad;
            return new Vector3(
                0f,
                distance * Mathf.Sin(pitchRad),
                -distance * Mathf.Cos(pitchRad)
            );
        }

        /// <summary>
        /// Calculates the camera position that centers the visible ground area on the origin.
        /// Accounts for the asymmetric frustum of a tilted perspective camera.
        /// </summary>
        /// <param name="distance">Distance from origin to camera.</param>
        /// <param name="pitchDeg">Pitch angle in degrees.</param>
        /// <param name="verticalFovDeg">Vertical FOV in degrees.</param>
        /// <returns>Camera world position with view centered on origin.</returns>
        public static Vector3 CalculateCameraPositionCentered(float distance, float pitchDeg, float verticalFovDeg)
        {
            float pitchRad = pitchDeg * Mathf.Deg2Rad;
            float halfFovRad = verticalFovDeg * Mathf.Deg2Rad * 0.5f;
            
            float cameraHeight = distance * Mathf.Sin(pitchRad);
            
            // Calculate where the upper and lower frustum edges hit the ground
            float upperAngle = pitchRad + halfFovRad;
            float lowerAngle = pitchRad - halfFovRad;
            
            // Clamp angles to valid range
            lowerAngle = Mathf.Max(lowerAngle, 0.1f);
            upperAngle = Mathf.Min(upperAngle, Mathf.PI * 0.5f - 0.01f);
            
            // Horizontal distance from camera to ground intersection
            // Lower ray (more horizontal) = farther from camera = far edge of view
            // Upper ray (more vertical) = closer to camera = near edge of view
            float distToFarEdge = cameraHeight / Mathf.Tan(lowerAngle);
            float distToNearEdge = cameraHeight / Mathf.Tan(upperAngle);
            
            // Center of visible ground area (offset from camera position in +Z direction)
            float viewCenterOffset = (distToFarEdge + distToNearEdge) * 0.5f;
            
            // Camera Z position needed so view center is at Z=0
            // Camera is at negative Z, looking toward +Z
            float cameraZ = -viewCenterOffset;
            
            return new Vector3(0f, cameraHeight, cameraZ);
        }

        /// <summary>
        /// Calculates the camera rotation to look at origin from a boomstick position.
        /// </summary>
        /// <param name="pitchDeg">Pitch angle in degrees.</param>
        /// <returns>Camera rotation.</returns>
        public static Quaternion CalculateCameraRotation(float pitchDeg)
        {
            return Quaternion.Euler(pitchDeg, 0f, 0f);
        }
    }
}
