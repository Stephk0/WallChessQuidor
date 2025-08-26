using UnityEngine;

public class WallPlacer : MonoBehaviour
{
    private GameObject wallPreview;
    private bool isPlacing = false;
    private Vector3 startPos;
    public int wallsLeft = 9;

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && wallsLeft > 0)
        {
            StartPlacing();
        }
        else if (Input.GetMouseButton(0) && isPlacing)
        {
            UpdatePreview();
        }
        else if (Input.GetMouseButtonUp(0) && isPlacing)
        {
            PlaceWall();
        }
    }

    void StartPlacing()
    {
        isPlacing = true;
        startPos = GetMouseWorld();
        CreatePreview();
    }

    void CreatePreview()
    {
        wallPreview = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wallPreview.name = "Preview";
        var renderer = wallPreview.GetComponent<Renderer>();
        renderer.material.color = new Color(1, 1, 0, 0.5f);
        DestroyImmediate(wallPreview.GetComponent<Collider>());
    }

    void UpdatePreview()
    {
        if (wallPreview == null) return;
        
        Vector3 current = GetMouseWorld();
        Vector3 delta = current - startPos;
        bool horizontal = Mathf.Abs(delta.x) > Mathf.Abs(delta.y);
        
        Vector3 pos = SnapToGrid(current, horizontal);
        wallPreview.transform.position = pos;
        
        if (horizontal)
            wallPreview.transform.localScale = new Vector3(2.4f, 0.1f, 0.2f);
        else
            wallPreview.transform.localScale = new Vector3(0.1f, 2.4f, 0.2f);
    }

    void PlaceWall()
    {
        if (wallPreview != null)
        {
            wallPreview.GetComponent<Renderer>().material.color = Color.yellow;
            wallPreview.name = "Wall";
            wallPreview.tag = "Wall";
            wallsLeft--;
            Debug.Log("Wall placed! Remaining: " + wallsLeft);
        }
        isPlacing = false;
        wallPreview = null;
    }

    Vector3 GetMouseWorld()
    {
        Vector3 mouse = Input.mousePosition;
        mouse.z = 10f;
        return Camera.main.ScreenToWorldPoint(mouse);
    }

    Vector3 SnapToGrid(Vector3 pos, bool horizontal)
    {
        float spacing = 1.2f;
        int x = Mathf.RoundToInt(pos.x / spacing);
        int y = Mathf.RoundToInt(pos.y / spacing);
        
        if (horizontal)
            return new Vector3(x * spacing + spacing, y * spacing + spacing * 0.5f, -0.1f);
        else
            return new Vector3(x * spacing + spacing * 0.5f, y * spacing + spacing, -0.1f);
    }
}