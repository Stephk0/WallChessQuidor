using UnityEngine;

public class WallGapVisualizer : MonoBehaviour
{
    public bool showGapGrid = true;
    public Color gapColor = new Color(0, 1, 1, 0.1f);

    void Start()
    {
        if (showGapGrid)
        {
            CreateGapVisualization();
        }
    }

    void CreateGapVisualization()
    {
        float spacing = 1.2f;
        float thickness = 0.05f;
        float length = 2.4f;
        
        // Create horizontal gap indicators
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                // Horizontal gaps - centered between tiles horizontally
                GameObject hGap = GameObject.CreatePrimitive(PrimitiveType.Cube);
                hGap.name = "HGap_" + x + "_" + y;
                hGap.transform.position = new Vector3(
                    x * spacing + spacing * 0.5f,    // Fixed: center between tiles horizontally
                    y * spacing + spacing * 0.5f,    // Between rows vertically
                    0.05f
                );
                hGap.transform.localScale = new Vector3(length, thickness, 0.1f);
                
                Renderer hRenderer = hGap.GetComponent<Renderer>();
                hRenderer.material = new Material(Shader.Find("Sprites/Default"));
                hRenderer.material.color = gapColor;
                DestroyImmediate(hGap.GetComponent<Collider>());
                
                // Vertical gaps - centered between tiles vertically
                GameObject vGap = GameObject.CreatePrimitive(PrimitiveType.Cube);
                vGap.name = "VGap_" + x + "_" + y;
                vGap.transform.position = new Vector3(
                    x * spacing + spacing * 0.5f,    // Between columns horizontally
                    y * spacing + spacing * 0.5f,    // Fixed: center between tiles vertically
                    0.05f
                );
                vGap.transform.localScale = new Vector3(thickness, length, 0.1f);
                
                Renderer vRenderer = vGap.GetComponent<Renderer>();
                vRenderer.material = new Material(Shader.Find("Sprites/Default"));
                vRenderer.material.color = gapColor;
                DestroyImmediate(vGap.GetComponent<Collider>());
            }
        }
        
        Debug.Log("Gap visualization created - cyan lines show where walls can be placed");
    }
}