using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace WallChess
{
    public class GridSystemTester : MonoBehaviour
    {
        [Header("Testing")]
        [SerializeField] private GridSystem gridSystem;
        [SerializeField] private bool enableDebugLabels = true;
        
        void Start()
        {
            gridSystem = FindObjectOfType<GridSystem>();
            if (gridSystem != null)
            {
                Debug.Log("GridSystemTester found GridSystem");
                // Test label functionality
                TestLabelSystem();
            }
            else
            {
                Debug.LogWarning("GridSystemTester: No GridSystem found in scene");
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.T))
            {
                ToggleLabels();
            }
            
            if (Input.GetKeyDown(KeyCode.R))
            {
                TestLabelUpdates();
            }
        }

        private void TestLabelSystem()
        {
            if (gridSystem != null)
            {
                Debug.Log("Testing label system functionality:");
                
                // Try to toggle labels
                gridSystem.ToggleTileLabels(enableDebugLabels);
                
                // Try to update a few labels
                gridSystem.UpdateTileLabel(0, 0, "TEST_0,0");
                gridSystem.UpdateTileLabel(1, 1, "TEST_1,1");
                
                Debug.Log("Label test commands sent");
            }
        }
        
        private void ToggleLabels()
        {
            enableDebugLabels = !enableDebugLabels;
            if (gridSystem != null)
            {
                gridSystem.ToggleTileLabels(enableDebugLabels);
                Debug.Log($"Labels toggled: {enableDebugLabels}");
            }
        }
        
        private void TestLabelUpdates()
        {
            if (gridSystem != null)
            {
                // Update some random labels with test text
                for (int i = 0; i < 3; i++)
                {
                    int x = Random.Range(0, gridSystem.GetGridSize());
                    int y = Random.Range(0, gridSystem.GetGridSize());
                    gridSystem.UpdateTileLabel(x, y, $"RND_{x},{y}");
                }
                Debug.Log("Updated random tile labels");
            }
        }
    }
}
