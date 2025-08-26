using UnityEngine;
using System.Collections.Generic;

namespace WallChess
{
    public class GridManager : MonoBehaviour
    {
        private WallChessGameManager gameManager;
        private GameObject[,] gridTiles;
        private bool[,] tileOccupied; // Track which tiles are occupied

        public void Initialize(WallChessGameManager gm)
        {
            gameManager = gm;
            CreateGrid();
        }

        void CreateGrid()
        {
            gridTiles = new GameObject[gameManager.gridSize, gameManager.gridSize];
            tileOccupied = new bool[gameManager.gridSize, gameManager.gridSize];

            for (int x = 0; x < gameManager.gridSize; x++)
            {
                for (int y = 0; y < gameManager.gridSize; y++)
                {
                    Vector3 position = GridToWorldPosition(new Vector2Int(x, y));
                    
                    // Create tile visual (simple quad)
                    GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Quad);
                    tile.name = $"Tile_{x}_{y}";
                    tile.transform.position = position;
                    tile.transform.localScale = Vector3.one * gameManager.tileSize;
                    
                    // Set up tile appearance
                    Renderer tileRenderer = tile.GetComponent<Renderer>();
                    tileRenderer.material = new Material(Shader.Find("Sprites/Default"));
                    tileRenderer.material.color = (x + y) % 2 == 0 ? Color.white : Color.gray;
                    
                    // Remove collider since we don't need physics
                    DestroyImmediate(tile.GetComponent<Collider>());
                    
                    gridTiles[x, y] = tile;
                }
            }

            // Mark initial player positions as occupied
            tileOccupied[gameManager.playerPosition.x, gameManager.playerPosition.y] = true;
            tileOccupied[gameManager.opponentPosition.x, gameManager.opponentPosition.y] = true;
        }

        public Vector3 GridToWorldPosition(Vector2Int gridPos)
        {
            float spacing = gameManager.tileSize + gameManager.tileGap;
            return new Vector3(
                gridPos.x * spacing,
                gridPos.y * spacing,
                0
            );
        }

        public Vector2Int WorldToGridPosition(Vector3 worldPos)
        {
            float spacing = gameManager.tileSize + gameManager.tileGap;
            int x = Mathf.RoundToInt(worldPos.x / spacing);
            int y = Mathf.RoundToInt(worldPos.y / spacing);
            
            // Clamp to grid bounds
            x = Mathf.Clamp(x, 0, gameManager.gridSize - 1);
            y = Mathf.Clamp(y, 0, gameManager.gridSize - 1);
            
            return new Vector2Int(x, y);
        }

        public bool IsValidGridPosition(Vector2Int gridPos)
        {
            return gridPos.x >= 0 && gridPos.x < gameManager.gridSize &&
                   gridPos.y >= 0 && gridPos.y < gameManager.gridSize;
        }

        public bool IsTileOccupied(Vector2Int gridPos)
        {
            if (!IsValidGridPosition(gridPos)) return true;
            return tileOccupied[gridPos.x, gridPos.y];
        }

        public void SetTileOccupied(Vector2Int gridPos, bool occupied)
        {
            if (IsValidGridPosition(gridPos))
                tileOccupied[gridPos.x, gridPos.y] = occupied;
        }

        public List<Vector2Int> GetValidMoves(Vector2Int currentPos)
        {
            List<Vector2Int> validMoves = new List<Vector2Int>();
            
            // Check four directions: up, down, left, right
            Vector2Int[] directions = {
                Vector2Int.up,
                Vector2Int.down,
                Vector2Int.left,
                Vector2Int.right
            };

            foreach (Vector2Int direction in directions)
            {
                Vector2Int newPos = currentPos + direction;
                
                if (IsValidGridPosition(newPos) && !IsTileOccupied(newPos))
                {
                    // TODO: Check if path is blocked by walls
                    validMoves.Add(newPos);
                }
            }

            return validMoves;
        }

        public GameObject GetTile(Vector2Int gridPos)
        {
            if (IsValidGridPosition(gridPos))
                return gridTiles[gridPos.x, gridPos.y];
            return null;
        }

        public Vector3 GetTileCenter(Vector2Int gridPos)
        {
            if (IsValidGridPosition(gridPos))
                return gridTiles[gridPos.x, gridPos.y].transform.position;
            return Vector3.zero;
        }
    }
}