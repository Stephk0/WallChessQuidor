using UnityEngine;

namespace WallChess.Data
{
    /// <summary>
    /// Grid visual and layout configuration
    /// </summary>
    [CreateAssetMenu(fileName = "GridSettings", menuName = "Wall Chess/Grid Settings")]
    public class GridSettings : ScriptableObject
    {
        [Header("Grid Layout")]
        [Tooltip("Size of each tile in world units")]
        [Range(0.5f, 2.0f)]
        public float tileSize = 1f;
        
        [Tooltip("Gap between tiles in world units")]
        [Range(0.0f, 0.5f)]
        public float tileGap = 0.2f;

        [Header("Wall Dimensions")]
        [Tooltip("Thickness of walls in world units")]
        [Range(0.05f, 0.3f)]
        public float wallThickness = 0.15f;
        
        [Tooltip("Height of walls in world units")]
        [Range(0.5f, 2.0f)]
        public float wallHeight = 1f;
    }
}