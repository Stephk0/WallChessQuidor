using UnityEngine;
using System.Collections.Generic;

namespace WallChess.Grid
{
    public class GridTileManager
    {
        private readonly GridCoordinateConverter _coordinateConverter;
        private readonly Transform _parentTransform;
        
        private GameObject[,] _gridTiles;
        private bool[,] _tileOccupied;
        
        private readonly Material _tileMaterial;
        private readonly Color _lightTileColor;
        private readonly Color _darkTileColor;
        private readonly float _tileSize;

        public System.Action<Vector2Int, bool> OnTileOccupancyChanged;

        public GridTileManager(GridCoordinateConverter coordinateConverter, 
            Transform parentTransform, float tileSize, Material tileMaterial = null, 
            Color lightColor = default, Color darkColor = default)
        {
            _coordinateConverter = coordinateConverter;
            _parentTransform = parentTransform;
            _tileSize = tileSize;
            _tileMaterial = tileMaterial;
            _lightTileColor = lightColor == default ? Color.white : lightColor;
            _darkTileColor = darkColor == default ? Color.gray : darkColor;
            
            InitializeArrays();
        }

        private void InitializeArrays()
        {
            int gridSize = _coordinateConverter.GetGridSize();
            _gridTiles = new GameObject[gridSize, gridSize];
            _tileOccupied = new bool[gridSize, gridSize];
            
            for (int x = 0; x < gridSize; x++)
                for (int y = 0; y < gridSize; y++)
                    _tileOccupied[x, y] = false;
        }

        public void CreateAllTiles()
        {
            int gridSize = _coordinateConverter.GetGridSize();
            
            for (int x = 0; x < gridSize; x++)
            {
                for (int y = 0; y < gridSize; y++)
                {
                    CreateTileAt(new Vector2Int(x, y));
                }
            }
        }

        private void CreateTileAt(Vector2Int gridPos)
        {
            Vector3 worldPosition = _coordinateConverter.GridToWorldPosition(gridPos);
            
            GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Quad);
            tile.name = $"Tile_{gridPos.x}_{gridPos.y}";
            tile.transform.position = worldPosition;
            tile.transform.localScale = Vector3.one * _tileSize;
            tile.transform.parent = _parentTransform;
            
            Renderer tileRenderer = tile.GetComponent<Renderer>();
            
            if (_tileMaterial != null)
                tileRenderer.material = _tileMaterial;
            else
                tileRenderer.material = new Material(Shader.Find("Sprites/Default"));
            
            tileRenderer.material.color = (gridPos.x + gridPos.y) % 2 == 0 ? 
                _lightTileColor : _darkTileColor;
            
            if (tile.GetComponent<Collider>() != null)
                Object.DestroyImmediate(tile.GetComponent<Collider>());
            
            _gridTiles[gridPos.x, gridPos.y] = tile;
        }

        public bool IsTileOccupied(Vector2Int gridPos)
        {
            if (!_coordinateConverter.IsValidGridPosition(gridPos)) return true;
            return _tileOccupied[gridPos.x, gridPos.y];
        }

        public void SetTileOccupied(Vector2Int gridPos, bool occupied)
        {
            if (!_coordinateConverter.IsValidGridPosition(gridPos)) return;
            
            _tileOccupied[gridPos.x, gridPos.y] = occupied;
            OnTileOccupancyChanged?.Invoke(gridPos, occupied);
        }

        public GameObject GetTile(Vector2Int gridPos)
        {
            if (!_coordinateConverter.IsValidGridPosition(gridPos)) return null;
            return _gridTiles[gridPos.x, gridPos.y];
        }

        public Vector3 GetTileCenter(Vector2Int gridPos)
        {
            if (!_coordinateConverter.IsValidGridPosition(gridPos)) return Vector3.zero;
            return _gridTiles[gridPos.x, gridPos.y].transform.position;
        }

        public List<Vector2Int> GetValidAdjacentPositions(Vector2Int currentPos)
        {
            List<Vector2Int> validPositions = new List<Vector2Int>();
            
            Vector2Int[] directions = {
                Vector2Int.up, Vector2Int.down,
                Vector2Int.left, Vector2Int.right
            };

            foreach (Vector2Int direction in directions)
            {
                Vector2Int newPos = currentPos + direction;
                
                if (_coordinateConverter.IsValidGridPosition(newPos) && !IsTileOccupied(newPos))
                {
                    validPositions.Add(newPos);
                }
            }

            return validPositions;
        }

        public void ClearOccupancy()
        {
            int gridSize = _coordinateConverter.GetGridSize();
            for (int x = 0; x < gridSize; x++)
                for (int y = 0; y < gridSize; y++)
                    _tileOccupied[x, y] = false;
        }

        public void DestroyAllTiles()
        {
            if (_gridTiles != null)
            {
                int sizeX = _gridTiles.GetLength(0);
                int sizeY = _gridTiles.GetLength(1);
                
                for (int x = 0; x < sizeX; x++)
                {
                    for (int y = 0; y < sizeY; y++)
                    {
                        if (_gridTiles[x, y] != null)
                            Object.DestroyImmediate(_gridTiles[x, y]);
                    }
                }
            }
        }
    }
}