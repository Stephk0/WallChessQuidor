using UnityEngine;

namespace WallChess.View
{
    /// <summary>
    /// Configuration for all visual elements and prefab references.
    /// </summary>
    [CreateAssetMenu(fileName = "ViewConfig", menuName = "WallChess/View Config")]
    public class ViewConfig : ScriptableObject
    {
        [Header("Board Prefabs")]
        public GameObject tilePrefab;
        public GameObject tileHighlightPrefab;
        public GameObject tileConfirmHighlightPrefab;

        [Header("Pawn Prefabs")]
        public GameObject[] pawnPrefabs; // Index by player

        [Header("Wall Prefabs")]
        public GameObject wallPrefab;
        public GameObject wallPreviewPrefab;

        [Header("Visual Settings")]
        public float pawnMoveSpeed = 8f;
        public float pawnJumpHeight = 0.3f;
        public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Highlight Colors")]
        public Color validMoveColor = new Color(0.2f, 0.8f, 0.2f, 0.6f);
        public Color confirmColor = new Color(0.9f, 0.9f, 0.2f, 0.8f);
        public Color invalidColor = new Color(0.8f, 0.2f, 0.2f, 0.6f);

        [Header("Wall Preview Colors")]
        public Color wallPreviewValid = new Color(0.2f, 0.6f, 0.9f, 0.6f);
        public Color wallPreviewInvalid = new Color(0.9f, 0.2f, 0.2f, 0.6f);

        [Header("Object Pooling")]
        public int highlightPoolSize = 20;
        public int wallPreviewPoolSize = 4;

        [Header("UI Settings")]
        public float turnTransitionDuration = 0.3f;
        public float gameOverDelay = 1.5f;

        /// <summary>
        /// Get pawn prefab for a player index.
        /// </summary>
        public GameObject GetPawnPrefab(int playerIndex)
        {
            if (pawnPrefabs == null || pawnPrefabs.Length == 0)
                return null;

            return pawnPrefabs[playerIndex % pawnPrefabs.Length];
        }
    }
}
