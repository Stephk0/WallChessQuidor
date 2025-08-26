using UnityEngine;
using System.Collections.Generic;

namespace WallChess
{
    public class Wall
    {
        public Vector2Int gridPosition;
        public bool isHorizontal;
        public GameObject wallObject;
        
        public Wall(Vector2Int pos, bool horizontal, GameObject obj)
        {
            gridPosition = pos;
            isHorizontal = horizontal;
            wallObject = obj;
        }
        
        public bool BlocksMovement(Vector2Int from, Vector2Int to)
        {
            if (isHorizontal)
            {
                if (from.x == to.x && Mathf.Abs(from.y - to.y) == 1)
                {
                    int wallY = Mathf.Min(from.y, to.y);
                    return gridPosition.x == from.x && gridPosition.y == wallY;
                }
            }
            else
            {
                if (from.y == to.y && Mathf.Abs(from.x - to.x) == 1)
                {
                    int wallX = Mathf.Min(from.x, to.x);
                    return gridPosition.y == from.y && gridPosition.x == wallX;
                }
            }
            return false;
        }
    }
}