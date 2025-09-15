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
                
                // Try to get WallPrefabController for proper material assignment
                var prefabController = preview.GetComponent<WallPrefabController>();
                if (prefabController != null)
                {
                    if (prefabController.ValidateRenderers())
                    {
                        previewRenderer = prefabController.GetFirstRenderer();
                    }
                    else
                    {
                        Debug.LogWarning($"WallVisuals: WallPrefabController found but no valid renderers assigned on {prefab.name}");
                    }
                }
                else
                {
                    // Fallback: look for any renderer component
                    previewRenderer = preview.GetComponent<Renderer>();
                    if (previewRenderer == null)
                    {
                        previewRenderer = preview.GetComponentInChildren<Renderer>();
                    }
                    
                    if (previewRenderer == null)
                    {
                        Debug.LogError($"WallVisuals: Wall prefab {prefab.name} has no WallPrefabController and no Renderer components! Add WallPrefabController or ensure prefab has renderers.");
                        // Don't create mesh renderers automatically - this should be set up in the prefab
                        return;
                    }
                }
            }
            else
            {
                // Debug mode: Create single preview cube
                preview = GameObject.CreatePrimitive(PrimitiveType.Cube);
                preview.name = "WallPreview_Debug";
                previewRenderer = preview.GetComponent<Renderer>();
            }
            
            // Setup the preview material only for debug mode or when no prefab controller exists
            if (previewRenderer != null && (wallManager == null || wallManager.IsDebugMode() || preview.GetComponent<WallPrefabController>() == null))
            {
                // Only apply initial material setup for debug mode or simple prefabs without controller
                if (wallManager != null)
                {
                    Material placingMaterial = wallManager.GetValidPreviewMaterial();
                    if (placingMaterial != null)
                    {
                        previewRenderer.sharedMaterial = placingMaterial;
                    }
                    else if (mat != null)
                    {
                        previewRenderer.material = mat;
                        previewRenderer.material.color = placing;
                    }
                    else
                    {
                        // Create a new material with the placing color
                        Material defaultMaterial = new Material(Shader.Find("Standard"));
                        defaultMaterial.color = placing;
                        previewRenderer.material = defaultMaterial;
                    }
                }
            }
            
            // Remove collider to prevent interference
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
            if (preview == null || wallManager == null) return;
            
            // Check if preview has WallPrefabController for advanced material handling
            var prefabController = preview.GetComponent<WallPrefabController>();
            if (prefabController != null)
            {
                // Use WallPrefabController for material assignment
                Material targetMaterial = isValid ? wallManager.GetValidPreviewMaterial() : wallManager.GetInvalidPreviewMaterial();
                
                if (targetMaterial != null)
                {
                    prefabController.SetPreviewMaterial(targetMaterial);
                }
                else
                {
                    // Fallback to color-based preview when materials aren't assigned
                    Color targetColor = isValid ? ok : bad;
                    prefabController.SetPreviewColor(targetColor);
                }
                return;
            }
            
            // Fallback: Direct renderer material assignment for debug mode or simple prefabs
            if (previewRenderer == null) return;
            
            Material previewMaterial = isValid ? wallManager.GetValidPreviewMaterial() : wallManager.GetInvalidPreviewMaterial();
            
            if (previewMaterial != null)
            {
                previewRenderer.sharedMaterial = previewMaterial; // Use shared, not instanced
            }
            else
            {
                // Fallback: Create or reuse a material instance with appropriate color
                if (previewRenderer.material != null)
                {
                    Material material = previewRenderer.material;
                    Color targetColor = isValid ? ok : bad;
                    
                    // Only change color if it's different to avoid unnecessary updates
                    if (material.color != targetColor)
                    {
                        material.color = targetColor;
                    }
                }
                else
                {
                    Debug.LogWarning("WallVisuals: Preview renderer has no material assigned!");
                }
            }
        }

public void CleanupPreview()
        {
            if (preview != null)
            {
                // Restore original materials if using WallPrefabController
                var prefabController = preview.GetComponent<WallPrefabController>();
                if (prefabController != null && prefabController.IsInPreviewMode)
                {
                    prefabController.RestoreOriginalMaterials();
                }
                
                WallState.SafeDestroy(preview);
            }
            preview = null; 
            previewRenderer = null;
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

            // Check if the wall has WallPrefabController and restore original materials
            var prefabController = go.GetComponent<WallPrefabController>();
            if (prefabController != null)
            {
                // Ensure original materials are restored for placed walls
                if (prefabController.IsInPreviewMode)
                {
                    prefabController.RestoreOriginalMaterials();
                }
            }
            else
            {
                // Fallback: Direct renderer material assignment for simple prefabs
                var r = go.GetComponent<Renderer>();
                if (r != null)
                {
                    if (mat != null) r.material = mat;
                    else if (prefabToUse == null) r.material.color = Color.yellow; // Only set color if using primitive
                }
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