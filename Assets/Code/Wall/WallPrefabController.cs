using UnityEngine;

namespace WallChess
{
    /// <summary>
    /// Script to be attached to wall prefabs to handle mesh renderer material assignment
    /// for preview states (valid/invalid/placing). Allows designers to assign specific 
    /// mesh renderers that should receive preview materials.
    /// </summary>
    public class WallPrefabController : MonoBehaviour
    {
        [Header("Mesh Renderers for Preview")]
        [SerializeField] private Renderer[] meshRenderers;
        [Tooltip("Mesh renderers that will receive preview materials during wall placement")]
        
        [Header("Original Materials (Auto-captured)")]
        [SerializeField] private Material[] originalMaterials;
        [Tooltip("Original materials of the mesh renderers - captured automatically")]
        
        private bool isInPreviewMode = false;
        
        void Awake()
        {
            CaptureOriginalMaterials();
        }
        
        /// <summary>
        /// Captures the original materials from assigned mesh renderers
        /// </summary>
        private void CaptureOriginalMaterials()
        {
            if (meshRenderers == null || meshRenderers.Length == 0)
            {
                // Auto-find renderers if none assigned
                meshRenderers = GetComponentsInChildren<Renderer>();
            }
            
            if (meshRenderers != null && meshRenderers.Length > 0)
            {
                originalMaterials = new Material[meshRenderers.Length];
                for (int i = 0; i < meshRenderers.Length; i++)
                {
                    if (meshRenderers[i] != null)
                    {
                        originalMaterials[i] = meshRenderers[i].sharedMaterial;
                    }
                }
            }
        }
        
        /// <summary>
        /// Sets preview material on all assigned mesh renderers
        /// </summary>
        /// <param name="previewMaterial">Material to apply for preview</param>
        public void SetPreviewMaterial(Material previewMaterial)
        {
            if (meshRenderers == null || previewMaterial == null) return;
            
            for (int i = 0; i < meshRenderers.Length; i++)
            {
                if (meshRenderers[i] != null)
                {
                    meshRenderers[i].sharedMaterial = previewMaterial;
                }
            }
            
            isInPreviewMode = true;
        }
        
        /// <summary>
        /// Sets preview color on all assigned mesh renderers (fallback when no material assigned)
        /// </summary>
        /// <param name="previewColor">Color to apply for preview</param>
        public void SetPreviewColor(Color previewColor)
        {
            if (meshRenderers == null) return;
            
            for (int i = 0; i < meshRenderers.Length; i++)
            {
                if (meshRenderers[i] != null && meshRenderers[i].material != null)
                {
                    meshRenderers[i].material.color = previewColor;
                }
            }
            
            isInPreviewMode = true;
        }
        
        /// <summary>
        /// Restores original materials to all assigned mesh renderers
        /// </summary>
        public void RestoreOriginalMaterials()
        {
            if (meshRenderers == null || originalMaterials == null) return;
            
            for (int i = 0; i < meshRenderers.Length && i < originalMaterials.Length; i++)
            {
                if (meshRenderers[i] != null && originalMaterials[i] != null)
                {
                    meshRenderers[i].sharedMaterial = originalMaterials[i];
                }
            }
            
            isInPreviewMode = false;
        }
        
        /// <summary>
        /// Gets the first valid mesh renderer for fallback operations
        /// </summary>
        /// <returns>First valid mesh renderer or null</returns>
        public Renderer GetFirstRenderer()
        {
            if (meshRenderers != null)
            {
                for (int i = 0; i < meshRenderers.Length; i++)
                {
                    if (meshRenderers[i] != null)
                        return meshRenderers[i];
                }
            }
            return null;
        }
        
        /// <summary>
        /// Gets all assigned mesh renderers
        /// </summary>
        /// <returns>Array of mesh renderers</returns>
        public Renderer[] GetRenderers()
        {
            return meshRenderers;
        }
        
        /// <summary>
        /// Validates that mesh renderers are properly assigned
        /// </summary>
        /// <returns>True if at least one valid renderer is assigned</returns>
        public bool ValidateRenderers()
        {
            if (meshRenderers == null || meshRenderers.Length == 0)
            {
                Debug.LogWarning($"WallPrefabController on {gameObject.name}: No mesh renderers assigned!");
                return false;
            }
            
            int validRenderers = 0;
            for (int i = 0; i < meshRenderers.Length; i++)
            {
                if (meshRenderers[i] != null)
                {
                    validRenderers++;
                }
                else
                {
                    Debug.LogWarning($"WallPrefabController on {gameObject.name}: Null renderer at index {i}");
                }
            }
            
            if (validRenderers == 0)
            {
                Debug.LogError($"WallPrefabController on {gameObject.name}: All assigned renderers are null!");
                return false;
            }
            
            Debug.Log($"WallPrefabController on {gameObject.name}: {validRenderers} valid renderers assigned");
            return true;
        }
        
        /// <summary>
        /// Re-captures original materials (useful after runtime material changes)
        /// </summary>
        public void RecaptureOriginalMaterials()
        {
            if (!isInPreviewMode)
            {
                CaptureOriginalMaterials();
            }
            else
            {
                Debug.LogWarning($"WallPrefabController on {gameObject.name}: Cannot recapture materials while in preview mode. Restore originals first.");
            }
        }
        
        /// <summary>
        /// Gets whether this prefab is currently in preview mode
        /// </summary>
        public bool IsInPreviewMode => isInPreviewMode;
        
        /// <summary>
        /// Editor helper to auto-assign all child renderers
        /// </summary>
        [ContextMenu("Auto-Assign Child Renderers")]
        private void AutoAssignChildRenderers()
        {
            meshRenderers = GetComponentsInChildren<Renderer>();
            CaptureOriginalMaterials();
            Debug.Log($"Auto-assigned {meshRenderers.Length} renderers to {gameObject.name}");
        }
        
        /// <summary>
        /// Editor helper to validate current setup
        /// </summary>
        [ContextMenu("Validate Setup")]
        private void EditorValidateSetup()
        {
            ValidateRenderers();
        }
    }
}
