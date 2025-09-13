using UnityEngine;

public class ImprovedCollisionChecker : MonoBehaviour
{
    public static bool IsBlocked(Vector3 from, Vector3 to)
    {
        float spacing = 1.2f;
        
        Vector2Int fromGrid = new Vector2Int(
            Mathf.RoundToInt(from.x / spacing),
            Mathf.RoundToInt(from.y / spacing)
        );
        
        Vector2Int toGrid = new Vector2Int(
            Mathf.RoundToInt(to.x / spacing),
            Mathf.RoundToInt(to.y / spacing)
        );
        
        string gapKey = GetGapKey(fromGrid, toGrid);
        if (gapKey == null) return false;
        
        ImprovedWallPlacer wallPlacer = FindObjectOfType<ImprovedWallPlacer>();
        if (wallPlacer != null)
        {
            return wallPlacer.IsGapOccupied(gapKey);
        }
        
        return false;
    }

    static string GetGapKey(Vector2Int from, Vector2Int to)
    {
        int deltaX = to.x - from.x;
        int deltaY = to.y - from.y;
        
        if (deltaX == 1 && deltaY == 0)
        {
            // Moving right - check vertical gap
            return "V_" + from.x + "_" + from.y;
        }
        else if (deltaX == -1 && deltaY == 0)
        {
            // Moving left - check vertical gap
            return "V_" + (from.x - 1) + "_" + from.y;
        }
        else if (deltaX == 0 && deltaY == 1)
        {
            // Moving up - check horizontal gap
            return "H_" + from.x + "_" + from.y;
        }
        else if (deltaX == 0 && deltaY == -1)
        {
            // Moving down - check horizontal gap
            return "H_" + from.x + "_" + (from.y - 1);
        }
        
        return null; // Invalid movement
    }
}