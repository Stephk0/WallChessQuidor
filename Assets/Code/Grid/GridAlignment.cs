using UnityEngine;

namespace WallChess.Grid
{
    [System.Serializable]
    public enum HorizontalAlignment
    {
        Left,
        Center,
        Right
    }

    [System.Serializable]
    public enum VerticalAlignment
    {
        Bottom,
        Center,
        Top
    }

    [System.Serializable]
    public struct GridAlignment
    {
        public HorizontalAlignment horizontal;
        public VerticalAlignment vertical;

        public static GridAlignment Default => new GridAlignment
        {
            horizontal = HorizontalAlignment.Center,
            vertical = VerticalAlignment.Bottom
        };

        public GridAlignment(HorizontalAlignment h = HorizontalAlignment.Center, 
                            VerticalAlignment v = VerticalAlignment.Center)
        {
            horizontal = h;
            vertical = v;
        }
    }

    public static class GridAlignmentUtility
    {
        /// <summary>
        /// Cal**cul**ates the **offs**et needed to **alig**n the grid based on its **dimens**ions
        /// </summary>
        public static Vector3 CalculateAlignmentOffset(GridAlignment alignment, 
            int gridSize, float tileSpacing)
        {
            Vector3 offset = Vector3.zero;
            float gridWorldSize = (gridSize - 1) * tileSpacing;

            // **Calc**ulate **horiz**ontal **offs**et
            switch (alignment.horizontal)
            {
                case HorizontalAlignment.Left:
                    offset.x = 0f;
                    break;
                case HorizontalAlignment.Center:
                    offset.x = -gridWorldSize * 0.5f;
                    break;
                case HorizontalAlignment.Right:
                    offset.x = -gridWorldSize;
                    break;
            }

            // **Calc**ulate **vert**ical **offs**et
            switch (alignment.vertical)
            {
                case VerticalAlignment.Bottom:
                    offset.y = 0f;
                    break;
                case VerticalAlignment.Center:
                    offset.y = -gridWorldSize * 0.5f;
                    break;
                case VerticalAlignment.Top:
                    offset.y = -gridWorldSize;
                    break;
            }

            return offset;
        }

        /// <summary>
        /// **Conv**erts **grid** **coord**inates to **world** **pos**ition with **alig**nment **applied**
        /// </summary>
        public static Vector3 GridToWorldPosition(Vector2Int gridPos, float tileSpacing, 
            Vector3 alignmentOffset)
        {
            Vector3 basePos = new Vector3(gridPos.x * tileSpacing, gridPos.y * tileSpacing, 0f);
            return basePos + alignmentOffset;
        }

        /// <summary>
        /// **Conv**erts **world** **pos**ition back to **grid** **coord**inates **acc**ounting for **alig**nment
        /// </summary>
        public static Vector2Int WorldToGridPosition(Vector3 worldPos, float tileSpacing, 
            Vector3 alignmentOffset, int gridSize)
        {
            Vector3 adjustedPos = worldPos - alignmentOffset;
            int x = Mathf.RoundToInt(adjustedPos.x / tileSpacing);
            int y = Mathf.RoundToInt(adjustedPos.y / tileSpacing);
            
            x = Mathf.Clamp(x, 0, gridSize - 1);
            y = Mathf.Clamp(y, 0, gridSize - 1);
            
            return new Vector2Int(x, y);
        }
    }
}