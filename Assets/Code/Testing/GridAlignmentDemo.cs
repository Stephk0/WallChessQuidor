using UnityEngine;
using WallChess;
using WallChess.Grid;

namespace WallChess.Testing
{
    /// <summary>
    /// **Test** **scr**ipt to **demo**nstrate **grid** **alig**nment **feat**ures
    /// **Show**cases the **new** **refact**ored **arch**itecture with **compos**ition
    /// </summary>
    public class GridAlignmentDemo : MonoBehaviour
    {
        [Header("Demo Settings")]
        [SerializeField] private GridSystem gridSystemPrefab;
        [SerializeField] private int gridSize = 9;
        [SerializeField] private float tileSize = 1f;
        [SerializeField] private float tileGap = 0.1f;
        
        [Header("Alignment Demo")]
        [SerializeField] private bool demoAlignment = true;
        [SerializeField] private float demoInterval = 2f;
        
        private GridSystem currentGrid;
        private GridAlignment[] alignmentOptions;
        private int currentAlignmentIndex = 0;
        private float timer = 0f;

        void Start()
        {
            InitializeAlignmentOptions();
            CreateInitialGrid();
        }

        void Update()
        {
            if (demoAlignment)
            {
                timer += Time.deltaTime;
                if (timer >= demoInterval)
                {
                    CycleToNextAlignment();
                    timer = 0f;
                }
            }
        }

        private void InitializeAlignmentOptions()
        {
            alignmentOptions = new GridAlignment[]
            {
                new GridAlignment(HorizontalAlignment.Left, VerticalAlignment.Bottom),
                new GridAlignment(HorizontalAlignment.Center, VerticalAlignment.Bottom),
                new GridAlignment(HorizontalAlignment.Right, VerticalAlignment.Bottom),
                new GridAlignment(HorizontalAlignment.Right, VerticalAlignment.Center),
                new GridAlignment(HorizontalAlignment.Right, VerticalAlignment.Top),
                new GridAlignment(HorizontalAlignment.Center, VerticalAlignment.Top),
                new GridAlignment(HorizontalAlignment.Left, VerticalAlignment.Top),
                new GridAlignment(HorizontalAlignment.Left, VerticalAlignment.Center),
                new GridAlignment(HorizontalAlignment.Center, VerticalAlignment.Center)
            };
        }

        private void CreateInitialGrid()
        {
            var gridSettings = new GridSystem.GridSettings
            {
                gridSize = this.gridSize,
                tileSize = this.tileSize,
                tileGap = this.tileGap,
                wallThickness = 0.1f,
                wallHeight = 0.5f
            };

            if (currentGrid != null)
            {
                DestroyImmediate(currentGrid.gameObject);
            }

            GameObject gridObj = new GameObject("GridSystem_Demo");
            gridObj.transform.position = Vector3.zero;
            
            currentGrid = gridObj.AddComponent<GridSystem>();
            currentGrid.Initialize(gridSettings);
            
            LogCurrentAlignment();
        }

        private void CycleToNextAlignment()
        {
            currentAlignmentIndex = (currentAlignmentIndex + 1) % alignmentOptions.Length;
            
            var gridSettings = currentGrid.GetGridSettings();
            var currentAlignment = alignmentOptions[currentAlignmentIndex];
            
            currentGrid.ReconfigureGrid(gridSettings, default, currentAlignment);
            
            LogCurrentAlignment();
        }

        private void LogCurrentAlignment()
        {
            var alignment = alignmentOptions[currentAlignmentIndex];
            Debug.Log($"**Grid** **Alig**nment: {alignment.horizontal} | {alignment.vertical}");
        }

        [ContextMenu("Cycle Alignment")]
        public void ManualCycleAlignment()
        {
            CycleToNextAlignment();
        }

        [ContextMenu("Toggle Demo")]
        public void ToggleDemo()
        {
            demoAlignment = !demoAlignment;
            Debug.Log($"**Alig**nment **Demo**: {(demoAlignment ? "**Start**ed" : "**Stop**ped")}");
        }

        void OnDrawGizmos()
        {
            // **Draw** **world** **orig**in
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one * 0.2f);
            
            // **Draw** **axis** **lines**
            Gizmos.color = Color.green;
            Gizmos.DrawLine(Vector3.left * 10f, Vector3.right * 10f);
            Gizmos.DrawLine(Vector3.down * 10f, Vector3.up * 10f);
        }
    }
}