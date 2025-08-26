using UnityEngine;

namespace WallChess
{
    public class SimpleGridCreator : MonoBehaviour
    {
        [Header("Grid Settings")]
        public int gridSize = 9;
        public float tileSize = 1f;
        public float tileGap = 0.2f;

        void Start()
        {
            CreateGrid();
            SetupCamera();
        }

        void CreateGrid()
        {
            for (int x = 0; x < gridSize; x++)
            {
                for (int y = 0; y < gridSize; y++)
                {
                    Vector3 position = GridToWorldPosition(x, y);
                    
                    GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    tile.name = $"Tile_{x}_{y}";
                    tile.transform.position = position;
                    tile.transform.localScale = Vector3.one * tileSize;
                    
                    Renderer tileRenderer = tile.GetComponent<Renderer>();
                    Color tileColor = (x + y) % 2 == 0 ? Color.white : new Color(0.8f, 0.8f, 0.8f);
                    tileRenderer.material.color = tileColor;
                    
                    DestroyImmediate(tile.GetComponent<Collider>());
                }
            }
        }

        Vector3 GridToWorldPosition(int x, int y)
        {
            float spacing = tileSize + tileGap;
            return new Vector3(x * spacing, y * spacing, 0.1f);
        }

        void SetupCamera()
        {
            Camera.main.transform.position = new Vector3(4f * (tileSize + tileGap), 4f * (tileSize + tileGap), -10f);
            Camera.main.orthographicSize = 6f;
        }
    }
}