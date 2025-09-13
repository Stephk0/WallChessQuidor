using UnityEngine;

public class WallCollisionChecker : MonoBehaviour
{
    public static bool IsBlocked(Vector3 from, Vector3 to)
    {
        GameObject[] walls = GameObject.FindGameObjectsWithTag("Wall");
        
        foreach (GameObject wall in walls)
        {
            if (WallBlocksPath(wall, from, to))
                return true;
        }
        
        return false;
    }

    static bool WallBlocksPath(GameObject wall, Vector3 from, Vector3 to)
    {
        Vector3 wallPos = wall.transform.position;
        Vector3 wallScale = wall.transform.localScale;
        float spacing = 1.2f;
        
        Vector2Int fromGrid = new Vector2Int(
            Mathf.RoundToInt(from.x / spacing),
            Mathf.RoundToInt(from.y / spacing)
        );
        
        Vector2Int toGrid = new Vector2Int(
            Mathf.RoundToInt(to.x / spacing),
            Mathf.RoundToInt(to.y / spacing)
        );
        
        // Determine wall grid position
        int wallGridX = Mathf.RoundToInt((wallPos.x - spacing * 0.5f) / spacing);
        int wallGridY = Mathf.RoundToInt((wallPos.y - spacing * 0.5f) / spacing);
        
        bool wallIsHorizontal = wallScale.x > wallScale.y;
        
        if (wallIsHorizontal)
        {
            // Horizontal wall blocks vertical movement
            if (fromGrid.x == toGrid.x && Mathf.Abs(fromGrid.y - toGrid.y) == 1)
            {
                // Check if wall is at the boundary being crossed
                int boundaryY = Mathf.Min(fromGrid.y, toGrid.y);
                return wallGridX == fromGrid.x && wallGridY == boundaryY;
            }
        }
        else
        {
            // Vertical wall blocks horizontal movement
            if (fromGrid.y == toGrid.y && Mathf.Abs(fromGrid.x - toGrid.x) == 1)
            {
                // Check if wall is at the boundary being crossed
                int boundaryX = Mathf.Min(fromGrid.x, toGrid.x);
                return wallGridY == fromGrid.y && wallGridX == boundaryX;
            }
        }
        
        return false;
    }
}