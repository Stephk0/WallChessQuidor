using UnityEngine;

public class GridVisualizer : MonoBehaviour
{
    public bool showWallSlots = true;
    public Color wallSlotColor = new Color(0, 1, 0, 0.1f);

    void Start()
    {
        if (showWallSlots)
        {
            CreateWallSlotVisualization();
        }
    }

    void CreateWallSlotVisualization()
    {
        float spacing = 1.2f;
        float tileSize = 1.0f;
        
        // Create horizontal wall slot indicators
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                // Horizontal wall slots
                GameObject hSlot = GameObject.CreatePrimitive(PrimitiveType.Cube);
                hSlot.name = "HWallSlot_" + x + "_" + y;
                hSlot.transform.position = new Vector3(
                    x * spacing + spacing * 0.5f,
                    y * spacing + spacing * 0.5f,
                    0.05f
                );
                hSlot.transform.localScale = new Vector3(tileSize * 2.0f, 0.05f, 0.1f);
                
                Renderer hRenderer = hSlot.GetComponent<Renderer>();
                hRenderer.material.color = wallSlotColor;
                DestroyImmediate(hSlot.GetComponent<Collider>());
                
                // Vertical wall slots
                GameObject vSlot = GameObject.CreatePrimitive(PrimitiveType.Cube);
                vSlot.name = "VWallSlot_" + x + "_" + y;
                vSlot.transform.position = new Vector3(
                    x * spacing + spacing * 0.5f,
                    y * spacing + spacing * 0.5f,
                    0.05f
                );
                vSlot.transform.localScale = new Vector3(0.05f, tileSize * 2.0f, 0.1f);
                
                Renderer vRenderer = vSlot.GetComponent<Renderer>();
                vRenderer.material.color = wallSlotColor;
                DestroyImmediate(vSlot.GetComponent<Collider>());
            }
        }
    }
}