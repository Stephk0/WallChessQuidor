using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace WallChess.Grid
{
    /// <summary>
    /// Utility script to configure tile prefabs and materials in the GridSystem.
    /// 
    /// NEW SYSTEM WORKFLOW:
    /// 1. Materials are applied dynamically during tile creation based on checkerboard pattern
    /// 2. Multiple tile prefabs can be used for visual variety (randomly selected)
    /// 3. Light/Dark materials determine the checkerboard appearance
    /// 4. Prefab variants (PB_GB_Tile_Light/Dark) are optional but can still be created for compatibility
    /// 
    /// RECOMMENDED SETUP:
    /// - Set lightTileMaterial and darkTileMaterial
    /// - Set baseTilePrefab (or multiple prefabs for variety)
    /// - Use "Simple Setup" for basic configuration
    /// - Use "Create Prefab Variants" only if you need pre-made light/dark variants
    /// </summary>
    public class TileMaterialAssigner : MonoBehaviour
    {
        [Header("Material Assignment")]
        [SerializeField] private Material lightTileMaterial;
        [SerializeField] private Material darkTileMaterial;
        
        [Header("Prefab Configuration")]
        [SerializeField] private GameObject baseTilePrefab;
        [SerializeField] private GameObject lightTilePrefab;
        [SerializeField] private GameObject darkTilePrefab;
        
        [Header("Grid System")]
        [SerializeField] private GridSystem targetGridSystem;
        
        [Header("Auto-Find Materials")]
        [SerializeField] private bool autoFindMaterials = true;
        
        [Header("Auto-Find Prefabs")]
        [SerializeField] private bool autoFindPrefabs = true;

        private void Awake()
        {
            if (autoFindMaterials)
                AutoFindMaterials();
                
            if (autoFindPrefabs)
                AutoFindPrefabs();
        }

        /// <summary>
        /// Automatically finds the materials in the Art/Materials folder
        /// </summary>
        public void AutoFindMaterials()
        {
#if UNITY_EDITOR
            if (lightTileMaterial == null)
            {
                string[] guids = AssetDatabase.FindAssets("M_GB_Tile_Light t:Material");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    lightTileMaterial = AssetDatabase.LoadAssetAtPath<Material>(path);
                }
            }
            
            if (darkTileMaterial == null)
            {
                string[] guids = AssetDatabase.FindAssets("M_GB_Tile_Dark t:Material");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    darkTileMaterial = AssetDatabase.LoadAssetAtPath<Material>(path);
                }
            }
            
            if (lightTileMaterial != null) Debug.Log($"Found light tile material: {lightTileMaterial.name}");
            if (darkTileMaterial != null) Debug.Log($"Found dark tile material: {darkTileMaterial.name}");
#endif
        }

        /// <summary>
        /// Automatically finds the tile prefabs
        /// </summary>
        public void AutoFindPrefabs()
        {
#if UNITY_EDITOR
            if (baseTilePrefab == null)
            {
                string[] guids = AssetDatabase.FindAssets("PB_GB_Tile_Base t:GameObject");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    baseTilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                }
            }
            
            if (lightTilePrefab == null)
            {
                string[] guids = AssetDatabase.FindAssets("PB_GB_Tile_Light t:GameObject");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    lightTilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                }
            }
            
            if (darkTilePrefab == null)
            {
                string[] guids = AssetDatabase.FindAssets("PB_GB_Tile_Dark t:GameObject");
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    darkTilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                }
            }
            
            if (baseTilePrefab != null) Debug.Log($"Found base tile prefab: {baseTilePrefab.name}");
            if (lightTilePrefab != null) Debug.Log($"Found light tile prefab: {lightTilePrefab.name}");
            if (darkTilePrefab != null) Debug.Log($"Found dark tile prefab: {darkTilePrefab.name}");
#endif
        }

        /// <summary>
        /// Assigns materials to the prefabs and configures them in GridSystem.
        /// Note: With the new system, material assignment to prefabs is optional since
        /// GridSystem can apply materials dynamically during tile creation.
        /// </summary>
        [ContextMenu("Assign Materials and Configure Grid")]
        public void AssignMaterialsAndConfigureGrid()
        {
            if (!ValidateComponents())
                return;
                
            // Optional: Assign materials to prefab variants if they exist
            if (lightTilePrefab != null)
                AssignMaterialToPrefab(lightTilePrefab, lightTileMaterial);
                
            if (darkTilePrefab != null)
                AssignMaterialToPrefab(darkTilePrefab, darkTileMaterial);
            
            ConfigureGridSystem();
            
            Debug.Log("Successfully configured GridSystem with tile prefabs and materials!");
        }

        /// <summary>
        /// Assigns a material to a prefab's renderer
        /// </summary>
        private void AssignMaterialToPrefab(GameObject prefab, Material material)
        {
            if (prefab == null || material == null) return;

#if UNITY_EDITOR
            // Get the prefab path and make sure we're modifying the prefab asset
            string prefabPath = AssetDatabase.GetAssetPath(prefab);
            GameObject prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            
            if (prefabAsset == null)
            {
                Debug.LogError($"Could not load prefab asset at path: {prefabPath}");
                return;
            }

            // Open prefab for editing
            using (var editingScope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
            {
                GameObject editingPrefab = editingScope.prefabContentsRoot;
                Renderer[] renderers = editingPrefab.GetComponentsInChildren<Renderer>();
                
                if (renderers != null && renderers.Length > 0)
                {
                    // Apply material to all renderers in the prefab hierarchy
                    foreach (Renderer renderer in renderers)
                    {
                        if (renderer != null)
                        {
                            // Use sharedMaterial to avoid creating material instances
                            renderer.sharedMaterial = material;
                        }
                    }
                    Debug.Log($"Assigned material {material.name} to {renderers.Length} renderer(s) in prefab {prefab.name}");
                }
                else
                {
                    Debug.LogWarning($"No Renderer components found in prefab {prefab.name} hierarchy");
                }
            }
            
            // Mark the prefab as dirty to save changes
            EditorUtility.SetDirty(prefabAsset);
            AssetDatabase.SaveAssets();
#endif
        }

        /// <summary>
        /// Configures the GridSystem with the tile prefabs and materials
        /// </summary>
        private void ConfigureGridSystem()
        {
            if (targetGridSystem == null)
            {
                targetGridSystem = FindObjectOfType<GridSystem>();
                if (targetGridSystem == null)
                {
                    Debug.LogError("No GridSystem found in scene!");
                    return;
                }
            }

            // Set up the tile prefabs array (prioritize base prefab, but include variants if available)
            GameObject[] tilePrefabsArray = GetTilePrefabsArray();

            // Configure the GridSystem with new API
            targetGridSystem.SetTilePrefabs(tilePrefabsArray);
            
            if (lightTileMaterial != null)
                targetGridSystem.SetLightTileMaterial(lightTileMaterial);
                
            if (darkTileMaterial != null)
                targetGridSystem.SetDarkTileMaterial(darkTileMaterial);

            Debug.Log($"Configured GridSystem with {tilePrefabsArray.Length} tile prefabs, " +
                     $"light material: {(lightTileMaterial ? lightTileMaterial.name : "none")}, " +
                     $"dark material: {(darkTileMaterial ? darkTileMaterial.name : "none")}");
        }
        
        /// <summary>
        /// Gets the tile prefabs array, prioritizing variety
        /// </summary>
        private GameObject[] GetTilePrefabsArray()
        {
            // Create a list to collect available prefabs
            var prefabList = new System.Collections.Generic.List<GameObject>();
            
            // Add base prefab if available
            if (baseTilePrefab != null)
                prefabList.Add(baseTilePrefab);
                
            // Add light prefab if available and different from base
            if (lightTilePrefab != null && lightTilePrefab != baseTilePrefab)
                prefabList.Add(lightTilePrefab);
                
            // Add dark prefab if available and different from base and light
            if (darkTilePrefab != null && darkTilePrefab != baseTilePrefab && darkTilePrefab != lightTilePrefab)
                prefabList.Add(darkTilePrefab);
                
            // If no prefabs found, create array with base prefab only
            if (prefabList.Count == 0 && baseTilePrefab != null)
                prefabList.Add(baseTilePrefab);
                
            return prefabList.ToArray();
        }

        /// <summary>
        /// Validates that all required components are assigned.
        /// With the new system, only materials and at least one prefab are required.
        /// </summary>
        private bool ValidateComponents()
        {
            bool isValid = true;

            if (lightTileMaterial == null)
            {
                Debug.LogError("Light tile material is required!");
                isValid = false;
            }

            if (darkTileMaterial == null)
            {
                Debug.LogError("Dark tile material is required!");
                isValid = false;
            }

            // At least one tile prefab is required
            bool hasPrefab = baseTilePrefab != null || lightTilePrefab != null || darkTilePrefab != null;
            if (!hasPrefab)
            {
                Debug.LogError("At least one tile prefab (base, light, or dark) must be assigned!");
                isValid = false;
            }
            
            // Warn if only variants exist but no base prefab
            if (baseTilePrefab == null && (lightTilePrefab != null || darkTilePrefab != null))
            {
                Debug.LogWarning("Base tile prefab is recommended for maximum variety in the new system.");
            }

            return isValid;
        }
        
        /// <summary>
        /// Performs complete setup: finds components, assigns materials, configures grid
        /// </summary>
        [ContextMenu("Complete Setup (Legacy)")]
        public void CompleteSetup()
        {
            AutoFindMaterials();
            AutoFindPrefabs();
            AssignMaterialsAndConfigureGrid();
        }

        /// <summary>
        /// Creates tile prefab variants with proper materials if they don't exist
        /// </summary>
        [ContextMenu("Create Tile Prefab Variants")]
        public void CreateTilePrefabVariants()
        {
#if UNITY_EDITOR
            if (baseTilePrefab == null)
            {
                Debug.LogError("Base tile prefab is required to create variants!");
                return;
            }

            if (lightTileMaterial == null || darkTileMaterial == null)
            {
                Debug.LogError("Both light and dark materials are required!");
                return;
            }

            string prefabFolder = "Assets/Prefabs/MVP/";
            
            // Create light tile variant
            if (lightTilePrefab == null)
            {
                GameObject lightVariant = PrefabUtility.InstantiatePrefab(baseTilePrefab) as GameObject;
                Renderer[] renderers = lightVariant.GetComponentsInChildren<Renderer>();
                
                if (renderers != null && renderers.Length > 0)
                {
                    foreach (Renderer renderer in renderers)
                    {
                        if (renderer != null)
                            renderer.sharedMaterial = lightTileMaterial;
                    }
                }
                
                lightVariant.name = "PB_GB_Tile_Light";
                string lightPath = prefabFolder + "PB_GB_Tile_Light.prefab";
                lightTilePrefab = PrefabUtility.SaveAsPrefabAsset(lightVariant, lightPath);
                DestroyImmediate(lightVariant);
                
                Debug.Log($"Created light tile prefab at {lightPath}");
            }
            
            // Create dark tile variant
            if (darkTilePrefab == null)
            {
                GameObject darkVariant = PrefabUtility.InstantiatePrefab(baseTilePrefab) as GameObject;
                Renderer[] renderers = darkVariant.GetComponentsInChildren<Renderer>();
                
                if (renderers != null && renderers.Length > 0)
                {
                    foreach (Renderer renderer in renderers)
                    {
                        if (renderer != null)
                            renderer.sharedMaterial = darkTileMaterial;
                    }
                }
                
                darkVariant.name = "PB_GB_Tile_Dark";
                string darkPath = prefabFolder + "PB_GB_Tile_Dark.prefab";
                darkTilePrefab = PrefabUtility.SaveAsPrefabAsset(darkVariant, darkPath);
                DestroyImmediate(darkVariant);
                
                Debug.Log($"Created dark tile prefab at {darkPath}");
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
#endif
        }

        /// <summary>
        /// Simple setup for the new system: just configure materials and base prefab
        /// </summary>
        [ContextMenu("Simple Setup (Recommended)")]
        public void SimpleSetup()
        {
            if (!ValidateSimpleSetup())
                return;
                
            ConfigureGridSystem();
            
            Debug.Log("Successfully configured GridSystem with simple setup! " +
                     "Materials will be applied dynamically during tile creation.");
        }
        
        /// <summary>
        /// Validates components needed for simple setup
        /// </summary>
        private bool ValidateSimpleSetup()
        {
            bool isValid = true;

            if (lightTileMaterial == null)
            {
                Debug.LogError("Light tile material is required for simple setup!");
                isValid = false;
            }

            if (darkTileMaterial == null)
            {
                Debug.LogError("Dark tile material is required for simple setup!");
                isValid = false;
            }

            if (baseTilePrefab == null)
            {
                Debug.LogError("Base tile prefab is required for simple setup!");
                isValid = false;
            }

            return isValid;
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(TileMaterialAssigner))]
    public class TileMaterialAssignerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            TileMaterialAssigner assigner = (TileMaterialAssigner)target;
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Recommended Workflow", EditorStyles.boldLabel);
            
            EditorGUILayout.HelpBox(
                "NEW SYSTEM: Materials are applied dynamically during tile creation. " +
                "Just set your base prefab and materials, then use Simple Setup!", 
                MessageType.Info);
                
            if (GUILayout.Button("Simple Setup (Recommended)", GUILayout.Height(30)))
            {
                assigner.SimpleSetup();
            }
            
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Advanced Actions", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Auto-Find Materials"))
            {
                assigner.AutoFindMaterials();
            }
            
            if (GUILayout.Button("Auto-Find Prefabs"))
            {
                assigner.AutoFindPrefabs();
            }
            
            if (GUILayout.Button("Create Tile Prefab Variants"))
            {
                assigner.CreateTilePrefabVariants();
            }
            
            if (GUILayout.Button("Assign Materials & Configure Grid"))
            {
                assigner.AssignMaterialsAndConfigureGrid();
            }
            
            EditorGUILayout.Space(5);
            
            if (GUILayout.Button("Complete Setup (Legacy)"))
            {
                assigner.CompleteSetup();
            }
        }
    }
#endif
}
