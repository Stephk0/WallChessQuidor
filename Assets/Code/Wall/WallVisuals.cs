using UnityEngine;
using System.Collections.Generic;

namespace WallChess
{
    /// <summary>
    /// Handles preview & wall object creation.
    /// Enhanced with rotation support and proper prefab preview.
    /// </summary>
    public class WallVisuals
    {
        private readonly GameObject prefab;
        private readonly Material mat;
        private readonly Color ok, bad, placing;
        private readonly List<GameObject> spawned = new List<GameObject>();
        private GameObject preview;
        private Renderer previewRenderer;
        private WallManager wallManager; // Reference to get debug mode and prefab info

        public WallVisuals(GameObject prefab, Material material, Color valid, Color invalid, Color placing)
        {
            this.prefab = prefab; mat = material; ok = valid; bad = invalid; this.placing = placing;
        }

        // Set wall manager reference for advanced preview features
        public void SetWallManager(WallManager manager)
        {
            wallManager = manager;
        }

        public void EnsurePreview()
        {
            if (preview != null) return;
            
            // Create preview using appropriate method based on mode
            if (wallManager != null && !wallManager.IsDebugMode() && prefab != null)
            {
                // Use actual prefab for preview in production mode
                preview = GameObject.Instantiate(prefab);
                preview.name = "WallPreview_Prefab";
                
                previewRenderer = preview.GetComponent<Renderer>();
                if (previewRenderer != null)
                {
                    if (mat != null)
                    {
                        previewRenderer.material = mat;
                    }
                    previewRenderer.material.color = placing;
                }
            }
            else
            {
                // Debug mode: Create single preview cube (segments will be created on placement)
                preview = GameObject.CreatePrimitive(PrimitiveType.Cube);
                preview.name = "WallPreview_Debug";
                
                previewRenderer = preview.GetComponent<Renderer>();
                if (previewRenderer != null)
                {
                    if (mat != null)
                    {
                        previewRenderer.material = mat;
                    }
                    previewRenderer.material.color = placing;
                }
            }
            
            var col = preview.GetComponent<Collider>();
            if (col) WallState.SafeDestroy(col);
        }

        // Enhanced preview method with rotation support
        public void UpdatePreview(Vector3 pos, Vector3 scale, Quaternion rotation, bool canPlace)
        {
            EnsurePreview();
            preview.SetActive(true);
            
            // Use safe slide animation helper
            ApplySafeSlideAnimation(pos);
            
                        preview.transform.rotation = rotation;
            preview.transform.localScale = scale;
            
            // Use material-based preview with fallback to colors
            UpdatePreviewMaterial(canPlace);
                }

        public void HidePreview() 
        { 
            if (preview) preview.SetActive(false); 
        }

        private void UpdatePreviewMaterial(bool isValid)
        {
            if (previewRenderer == null || wallManager == null) return;
            
            Material targetMaterial = isValid ? wallManager.GetValidPreviewMaterial() : wallManager.GetInvalidPreviewMaterial();
            
            if (targetMaterial != null)
            {
                previewRenderer.sharedMaterial = targetMaterial; // Use shared, not instanced
            }
            else
            {
                // Fallback to color-based preview if materials aren't assigned
                if (previewRenderer.material != null)
                {
                    previewRenderer.material.color = isValid ? ok : bad;
                }
            }
        }

        public void CleanupPreview()
        {
            if (preview) WallState.SafeDestroy(preview);
            preview = null; previewRenderer = null;
        }
        
        public void ShowInvalidPlacementFeedback(Vector3 position)
        {
            // Create a temporary red flash at the invalid position
            GameObject feedbackObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            feedbackObj.name = "InvalidPlacementFeedback";
            feedbackObj.transform.position = position;
            feedbackObj.transform.localScale = Vector3.one * 0.5f;
            
            // Make it red and semi-transparent
            Renderer renderer = feedbackObj.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material tempMat = new Material(renderer.material);
                tempMat.color = new Color(1f, 0f, 0f, 0.5f);
                renderer.material = tempMat;
            }
            
            // Remove collider so it doesn't interfere
            Collider collider = feedbackObj.GetComponent<Collider>();
            if (collider != null)
            {
                GameObject.Destroy(collider);
            }
            
            // Auto-destroy after a short time
            GameObject.Destroy(feedbackObj, 0.5f);
        }

        public GameObject CreateWall(GapDetector.WallInfo w, WallState state)
        {
            var go = prefab != null ? GameObject.Instantiate(prefab) : GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = $"Wall_{w.orientation}_{w.x}_{w.y}";
            go.transform.position = w.pos;
            go.transform.localScale = w.scale;
            go.tag = "Wall";

            var r = go.GetComponent<Renderer>();
            if (r != null)
            {
                if (mat != null) r.material = mat;
                else r.material.color = Color.yellow;
            }
            spawned.Add(go);
            state.AddManaged(go);
            return go;
        }
        
        // New overload for unified system that doesn't require WallState
        public GameObject CreateWall(Vector3 position, Vector3 scale)
        {
            var go = prefab != null ? GameObject.Instantiate(prefab) : GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Wall_Unified";
            go.transform.position = position;
            go.transform.localScale = scale;
            go.tag = "Wall";

            var r = go.GetComponent<Renderer>();
            if (r != null)
            {
                if (mat != null) r.material = mat;
                else r.material.color = Color.yellow;
            }
            spawned.Add(go);
            return go;
        }

        // Enhanced overload for new rotation-based prefab system with translation animation
        public GameObject CreateWall(Vector3 position, Vector3 scale, Quaternion rotation, GameObject wallPrefab = null)
        {
            if (wallManager != null && wallManager.IsDebugMode())
            {
                // Debug mode: Create 3 separate boxes for wall segments and intersection
                return CreateDebugWallSegments(position, scale, rotation);
            }
            
            // Production mode: Use prefab with rotation
            GameObject prefabToUse = wallPrefab ?? prefab;
            var go = prefabToUse != null ? GameObject.Instantiate(prefabToUse) : GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Wall_Prefab";
            go.transform.rotation = rotation;
            go.transform.localScale = scale;
            go.tag = "Wall";

            var r = go.GetComponent<Renderer>();
            if (r != null)
            {
                if (mat != null) r.material = mat;
                else if (prefabToUse == null) r.material.color = Color.yellow; // Only set color if using primitive
            }
            
            // Determine orientation from rotation and apply smooth translation animation
            GridSystem.Orientation orientation = GetOrientationFromRotation(rotation);
            
            // Apply smooth translation animation if enabled
            if (wallManager != null && wallManager.IsTranslationOnPlaceLerpEnabled())
            {
                ApplySmoothTranslationToWall(go, position, orientation);
            }
            else
            {
                // Set position immediately if animation is disabled
                go.transform.position = position;
            }
            
            spawned.Add(go);
            return go;
        }
        
        
        
        /// <summary>
        /// Determines wall orientation from rotation quaternion
        /// </summary>
        private GridSystem.Orientation GetOrientationFromRotation(Quaternion rotation)
        {
            // Get the Z component of the rotation to determine orientation
            // Horizontal walls typically have 0° rotation, vertical walls have 90° rotation
            Vector3 eulerAngles = rotation.eulerAngles;
            float zRotation = eulerAngles.z;
            
            // Normalize to 0-360 range and check if it's closer to 0° (horizontal) or 90° (vertical)
            if (zRotation > 180f) zRotation -= 360f; // Convert to -180 to 180 range
            
            // If rotation is closer to 90° or -90°, it's vertical, otherwise horizontal
            return (Mathf.Abs(zRotation) > 45f) ? GridSystem.Orientation.Vertical : GridSystem.Orientation.Horizontal;
        }
        /// <summary>
        /// Applies smooth translation to an existing wall GameObject using WallManager's lerp system
        /// </summary>
        public void ApplySmoothTranslationToWall(GameObject wallObject, Vector3 finalPosition, GridSystem.Orientation orientation)
        {
            if (wallManager != null && wallObject != null)
            {
                wallManager.ApplySmoothTranslationOnPlace(wallObject, finalPosition);
            }
        }
        /// <summary>
        /// Applies smooth rotation to an existing wall GameObject using WallManager's lerp system
        /// </summary>
        public void ApplySmoothRotationToWall(GameObject wallObject, GridSystem.Orientation newOrientation)
        {
            if (wallManager != null && wallObject != null)
            {
                wallManager.ApplySmoothRotation(wallObject, newOrientation);
            }
        }

/// <summary>
        /// Helper method to safely apply slide animation with better first-time handling
        /// </summary>
        private void ApplySafeSlideAnimation(Vector3 newPosition)
        {
            if (preview == null || wallManager == null) return;
            
            Vector3 currentPosition = preview.transform.position;
            
            // Check if this is the first position set (current position is approximately zero/default)
            bool isFirstPosition = Vector3.Distance(currentPosition, Vector3.zero) < 0.1f;
            
            // Check if position has changed significantly
            bool positionChanged = Vector3.Distance(currentPosition, newPosition) > 0.01f;
            
            if (wallManager.IsSlideTranslationLerpEnabled() && !wallManager.IsDebugMode() && positionChanged && !isFirstPosition)
            {
                // Use slide translation animation for smooth movement
                Debug.Log($"Applying slide animation from {currentPosition} to {newPosition}");
                wallManager.ApplySlideTranslation(preview, newPosition);
            }
            else
            {
                // Set position immediately for first position, or if animation is disabled
                if (isFirstPosition)
                {
                    Debug.Log($"Setting initial preview position: {newPosition}");
                }
                preview.transform.position = newPosition;
            }
        }

        
        /// <summary>
        /// Enhanced preview method with smooth rotation transition
        /// </summary>
/// <summary>
        /// Enhanced preview method with smooth rotation transition and slide animation
        /// </summary>
/// <summary>
        /// Enhanced preview method with smooth rotation transition and slide animation
        /// </summary>
        public void UpdatePreviewWithSmoothRotation(Vector3 pos, Vector3 scale, GridSystem.Orientation orientation, bool canPlace)
        {
            Debug.Log($"UpdatePreviewWithSmoothRotation called for {orientation} orientation. Lerp enabled: {wallManager?.IsRotationLerpEnabled()}, Debug mode: {wallManager?.IsDebugMode()}");
            
            EnsurePreview();
            preview.SetActive(true);
            
            // Use safe slide animation helper
            ApplySafeSlideAnimation(pos);
            
            preview.transform.localScale = scale;
            
            // Apply smooth rotation if wall manager supports it
            if (wallManager != null && wallManager.IsRotationLerpEnabled() && !wallManager.IsDebugMode())
            {
                wallManager.ApplySmoothRotation(preview, orientation);
            }
            else
            {
                // Fallback to immediate rotation
                Quaternion targetRotation = wallManager?.GetWallRotation(orientation) ?? Quaternion.identity;
                preview.transform.rotation = targetRotation;
            }
            
                        // Use material-based preview with fallback to colors
            UpdatePreviewMaterial(canPlace);
        }
        
        /// <summary>
        /// Creates 3 separate debug boxes: 2 wall segments + 1 intersection
        /// Uses the same scaling logic as the working single preview box
        /// </summary>
        private GameObject CreateDebugWallSegments(Vector3 centerPosition, Vector3 wallScale, Quaternion rotation)
        {
            // Create parent object to hold all segments
            GameObject wallParent = new GameObject("DebugWall_Segments");
            wallParent.transform.position = centerPosition;
            wallParent.tag = "Wall";
            
            // Get grid settings for positioning calculations
            var gridSettings = wallManager.GetGridSystem().GetGridSettings();
            float tileSize = gridSettings.tileSize;
            float tileGap = gridSettings.tileGap;
            float spacing = tileSize + tileGap;
            
            // Determine orientation from the wallScale (same logic as GetWallScale)
            bool isHorizontal = wallScale.x > wallScale.y;
            
            Vector3[] segmentPositions = new Vector3[3];
            Vector3[] segmentScales = new Vector3[3];
            
            if (isHorizontal)
            {
                // Horizontal wall: divide the working scale into 3 parts
                float gapWidth = wallScale.y; // Use the working thickness
                float gapLength = (wallScale.x - gapWidth) / 2f; // Each gap gets half the length minus intersection
                
                // Scales: 2 gap segments + 1 square intersection
                Vector3 gapScale = new Vector3(gapLength, gapWidth, wallScale.z);
                Vector3 intersectionScale = new Vector3(gapWidth, gapWidth, wallScale.z);
                
                // Positions: spread along X axis
                float offset = gapLength / 2f + gapWidth / 2f;
                segmentPositions[0] = new Vector3(-offset, 0, 0); // Left gap
                segmentPositions[1] = new Vector3(0, 0, 0);       // Center intersection
                segmentPositions[2] = new Vector3(offset, 0, 0);  // Right gap
                
                segmentScales[0] = gapScale;
                segmentScales[1] = intersectionScale;
                segmentScales[2] = gapScale;
            }
            else
            {
                // Vertical wall: divide the working scale into 3 parts
                float gapWidth = wallScale.x; // Use the working thickness
                float gapLength = (wallScale.y - gapWidth) / 2f; // Each gap gets half the length minus intersection
                
                // Scales: 2 gap segments + 1 square intersection
                Vector3 gapScale = new Vector3(gapWidth, gapLength, wallScale.z);
                Vector3 intersectionScale = new Vector3(gapWidth, gapWidth, wallScale.z);
                
                // Positions: spread along Y axis
                float offset = gapLength / 2f + gapWidth / 2f;
                segmentPositions[0] = new Vector3(0, -offset, 0); // Bottom gap
                segmentPositions[1] = new Vector3(0, 0, 0);       // Center intersection
                segmentPositions[2] = new Vector3(0, offset, 0);  // Top gap
                
                segmentScales[0] = gapScale;
                segmentScales[1] = intersectionScale;
                segmentScales[2] = gapScale;
            }
            
            // Create the 3 boxes with proper positioning and scaling
            for (int i = 0; i < 3; i++)
            {
                GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Cube);
                segment.transform.SetParent(wallParent.transform);
                segment.transform.localPosition = segmentPositions[i];
                segment.transform.localScale = segmentScales[i];
                
                if (i == 1)
                {
                    segment.name = "Intersection";
                }
                else
                {
                    segment.name = $"GapSegment_{i}";
                }
                
                // Apply material and color
                var renderer = segment.GetComponent<Renderer>();
                if (renderer != null)
                {
                    if (mat != null)
                    {
                        renderer.material = mat;
                    }
                    else
                    {
                        // Different colors for segments vs intersection
                        renderer.material.color = i == 1 ? Color.red : Color.yellow;
                    }
                }
                
                // Remove colliders to avoid interference
                var col = segment.GetComponent<Collider>();
                if (col) WallState.SafeDestroy(col);
            }
            
            spawned.Add(wallParent);
            return wallParent;
        }

        public void DestroyAll()
        {
            for (int i = spawned.Count - 1; i >= 0; i--)
                if (spawned[i] != null) WallState.SafeDestroy(spawned[i]);
            spawned.Clear();
        }
    



}
}