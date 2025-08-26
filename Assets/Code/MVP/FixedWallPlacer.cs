using UnityEngine;
using System.Collections.Generic;

public class FixedWallPlacer : MonoBehaviour
{
    private GameObject wallPreview;
    private bool isPlacing = false;
    private Vector3 startPos;
    public int wallsLeft = 9;
    
    private List<WallData> placedWalls = new List<WallData>();

    void Update()
    {
        if (Input.GetMouseButtonDown(1) && wallsLeft > 0)
        {
            StartPlacing();
        }
        else if (Input.GetMouseButton(1) && isPlacing)
        {
            UpdatePreview();
        }
        else if (Input.GetMouseButtonUp(1) && isPlacing)
        {
            PlaceWall();
        }
    }

    void StartPlacing()
    {
        Vector3 mousePos = GetMouseWorld();
        if (mousePos.y < 4.8f)
        {
            isPlacing = true;
            startPos = mousePos;
            CreatePreview();
        }
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
        
        WallData wallData = CalculateWallPlacement(current, horizontal);
        
        wallPreview.transform.position = wallData.position;
        wallPreview.transform.localScale = wallData.scale;
        
        var renderer = wallPreview.GetComponent<Renderer>();
        if (CanPlaceWall(wallData))
        {
            renderer.material.color = new Color(1, 1, 0, 0.5f);
        }
        else
        {
            renderer.material.color = new Color(1, 0, 0, 0.5f);
        }
    }

    void PlaceWall()
    {
        if (wallPreview != null)
        {
            Vector3 current = GetMouseWorld();
            Vector3 delta = current - startPos;
            bool horizontal = Mathf.Abs(delta.x) > Mathf.Abs(delta.y);
            
            WallData wallData = CalculateWallPlacement(current, horizontal);
            
            if (CanPlaceWall(wallData))
            {
                wallPreview.GetComponent<Renderer>().material.color = Color.yellow;
                wallPreview.name = "Wall";
                wallPreview.tag = "Wall";
                
                wallData.wallObject = wallPreview;
                placedWalls.Add(wallData);
                
                wallsLeft--;
                Debug.Log("Wall placed! Remaining: " + wallsLeft);
                wallPreview = null;
            }
            else
            {
                DestroyImmediate(wallPreview);
                wallPreview = null;
                Debug.Log("Cannot place wall here!");
            }
        }
        
        isPlacing = false;
    }

    WallData CalculateWallPlacement(Vector3 mousePos, bool horizontal)
    {
        float spacing = 1.2f;
        float tileSize = 1.0f;
        
        WallData wallData = new WallData();
        wallData.isHorizontal = horizontal;
        
        int gridX = Mathf.RoundToInt(mousePos.x / spacing);
        int gridY = Mathf.RoundToInt(mousePos.y / spacing);
        
        gridX = Mathf.Clamp(gridX, 0, 7);
        gridY = Mathf.Clamp(gridY, 0, 7);
        
        wallData.gridX = gridX;
        wallData.gridY = gridY;
        
        if (horizontal)
        {
            wallData.position = new Vector3(
                gridX * spacing + spacing * 0.5f,
                gridY * spacing + spacing * 0.5f,
                -0.1f
            );
            wallData.scale = new Vector3(tileSize * 2.0f, 0.15f, 0.2f);
        }
        else
        {
            wallData.position = new Vector3(
                gridX * spacing + spacing * 0.5f,
                gridY * spacing + spacing * 0.5f,
                -0.1f
            );
            wallData.scale = new Vector3(0.15f, tileSize * 2.0f, 0.2f);
        }
        
        return wallData;
    }

    bool CanPlaceWall(WallData newWall)
    {
        if (newWall.gridX < 0 || newWall.gridX > 7 || newWall.gridY < 0 || newWall.gridY > 7)
            return false;
            
        foreach (WallData existingWall in placedWalls)
        {
            if (existingWall.gridX == newWall.gridX && 
                existingWall.gridY == newWall.gridY && 
                existingWall.isHorizontal == newWall.isHorizontal)
            {
                return false;
            }
            
            if (existingWall.gridX == newWall.gridX && 
                existingWall.gridY == newWall.gridY && 
                existingWall.isHorizontal != newWall.isHorizontal)
            {
                return false;
            }
        }
        
        return true;
    }

    Vector3 GetMouseWorld()
    {
        Vector3 mouse = Input.mousePosition;
        mouse.z = 10f;
        return Camera.main.ScreenToWorldPoint(mouse);
    }
}