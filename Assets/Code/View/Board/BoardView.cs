using System.Collections.Generic;
using UnityEngine;
using WallChess.Core.Data;
using WallChess.Core.Config;

namespace WallChess.View.Board
{
    /// <summary>
    /// Manages the visual board representation including tiles and highlights.
    /// </summary>
    public class BoardView : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private BoardConfig boardConfig;

        [Header("Containers")]
        [SerializeField] private Transform tileContainer;
        [SerializeField] private Transform highlightContainer;
        [SerializeField] private Transform wallPreviewContainer;

        // Runtime
        private ViewConfig _viewConfig;
        private int _boardWidth;
        private int _boardHeight;
        private TileView[,] _tiles;
        private readonly List<TileHighlightView> _activeHighlights = new();
        private readonly Queue<TileHighlightView> _highlightPool = new();
        private TileHighlightView _confirmHighlight;
        private GameObject _wallPreview;

        #region Initialization

        public void Initialize(int boardWidth, int boardHeight, ViewConfig viewConfig)
        {
            _boardWidth = boardWidth;
            _boardHeight = boardHeight;
            _viewConfig = viewConfig;

            CreateTiles();
            InitializeHighlightPool();
        }

        /// <summary>
        /// Backwards compatibility for square boards.
        /// </summary>
        public void Initialize(int boardSize, ViewConfig viewConfig) => Initialize(boardSize, boardSize, viewConfig);

        private void CreateTiles()
        {
            // Clean up existing tiles
            if (_tiles != null)
            {
                foreach (var tile in _tiles)
                {
                    if (tile != null)
                        Destroy(tile.gameObject);
                }
            }

            _tiles = new TileView[_boardWidth, _boardHeight];

            if (_viewConfig.tilePrefab == null)
            {
                Debug.LogWarning("No tile prefab assigned");
                return;
            }

            // Rotation to orient tiles with Y-up (flat on XZ plane)
            var tileRotation = Quaternion.Euler(-90f, 0f, 0f);

            for (int y = 0; y < _boardHeight; y++)
            {
                for (int x = 0; x < _boardWidth; x++)
                {
                    var position = new BoardPosition(x, y);
                    var worldPos = GetWorldPosition(position);

                    var tileObj = Instantiate(_viewConfig.tilePrefab, worldPos, tileRotation, tileContainer);
                    tileObj.name = $"Tile_{x}_{y}";

                    var tileView = tileObj.GetComponent<TileView>();
                    if (tileView == null)
                    {
                        tileView = tileObj.AddComponent<TileView>();
                    }

                    tileView.Initialize(position, IsLightTile(x, y));
                    _tiles[x, y] = tileView;
                }
            }
        }

        private void InitializeHighlightPool()
        {
            // Clean up existing pool
            foreach (var highlight in _highlightPool)
            {
                if (highlight != null)
                    Destroy(highlight.gameObject);
            }
            _highlightPool.Clear();

            if (_viewConfig.tileHighlightPrefab == null) return;

            for (int i = 0; i < _viewConfig.highlightPoolSize; i++)
            {
                var highlight = CreateHighlight();
                highlight.gameObject.SetActive(false);
                _highlightPool.Enqueue(highlight);
            }
        }

        private TileHighlightView CreateHighlight()
        {
            var obj = Instantiate(_viewConfig.tileHighlightPrefab, highlightContainer);
            var view = obj.GetComponent<TileHighlightView>();
            if (view == null)
            {
                view = obj.AddComponent<TileHighlightView>();
            }
            return view;
        }

        #endregion

        #region Coordinate Conversion

        public Vector3 GetWorldPosition(BoardPosition boardPos)
        {
            if (boardConfig != null)
            {
                return boardConfig.GridToWorld(boardPos);
            }

            // Fallback: center the board
            float offsetX = (_boardWidth - 1) * 0.5f;
            float offsetY = (_boardHeight - 1) * 0.5f;
            return new Vector3(boardPos.X - offsetX, 0, boardPos.Y - offsetY);
        }

        public BoardPosition? GetBoardPosition(Vector3 worldPos)
        {
            if (boardConfig != null)
            {
                var gridPos = boardConfig.WorldToGrid(worldPos);
                if (gridPos.X >= 0 && gridPos.X < _boardWidth && gridPos.Y >= 0 && gridPos.Y < _boardHeight)
                    return gridPos;
                return null;
            }

            // Fallback conversion
            float offsetX = (_boardWidth - 1) * 0.5f;
            float offsetY = (_boardHeight - 1) * 0.5f;
            int x = Mathf.RoundToInt(worldPos.x + offsetX);
            int y = Mathf.RoundToInt(worldPos.z + offsetY);

            if (x >= 0 && x < _boardWidth && y >= 0 && y < _boardHeight)
            {
                return new BoardPosition(x, y);
            }
            return null;
        }

        private bool IsLightTile(int x, int y)
        {
            return (x + y) % 2 == 0;
        }

        #endregion

        #region Highlights

        public void ShowMoveHighlights(List<BoardPosition> positions, Color color)
        {
            ClearHighlights();

            foreach (var pos in positions)
            {
                var highlight = GetHighlightFromPool();
                highlight.Show(GetWorldPosition(pos), color);
                _activeHighlights.Add(highlight);
            }
        }

        public void ClearHighlights()
        {
            foreach (var highlight in _activeHighlights)
            {
                highlight.Hide();
                _highlightPool.Enqueue(highlight);
            }
            _activeHighlights.Clear();
        }

        public void ShowConfirmHighlight(BoardPosition position, Color color)
        {
            if (_confirmHighlight == null)
            {
                if (_viewConfig.tileConfirmHighlightPrefab != null)
                {
                    var obj = Instantiate(_viewConfig.tileConfirmHighlightPrefab, highlightContainer);
                    _confirmHighlight = obj.GetComponent<TileHighlightView>();
                    if (_confirmHighlight == null)
                    {
                        _confirmHighlight = obj.AddComponent<TileHighlightView>();
                    }
                }
                else
                {
                    _confirmHighlight = CreateHighlight();
                }
            }

            _confirmHighlight.Show(GetWorldPosition(position), color);
        }

        public void ClearConfirmHighlight()
        {
            if (_confirmHighlight != null)
            {
                _confirmHighlight.Hide();
            }
        }

        private TileHighlightView GetHighlightFromPool()
        {
            if (_highlightPool.Count > 0)
            {
                return _highlightPool.Dequeue();
            }

            // Pool exhausted, create new
            return CreateHighlight();
        }

        #endregion

        #region Wall Preview

        public void ShowWallPreview(WallPlacement wall, Color color)
        {
            if (_wallPreview == null && _viewConfig.wallPreviewPrefab != null)
            {
                _wallPreview = Instantiate(_viewConfig.wallPreviewPrefab, wallPreviewContainer);
            }

            if (_wallPreview == null) return;

            // Position wall preview
            var worldPos = GetWallWorldPosition(wall);
            var rotation = wall.Orientation == WallOrientation.Horizontal
                ? Quaternion.identity
                : Quaternion.Euler(0, 90, 0);

            _wallPreview.transform.SetPositionAndRotation(worldPos, rotation);
            _wallPreview.SetActive(true);

            // Set color
            var renderer = _wallPreview.GetComponentInChildren<Renderer>();
            if (renderer != null)
            {
                var propBlock = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(propBlock);
                propBlock.SetColor("_Color", color);
                propBlock.SetColor("_BaseColor", color);
                renderer.SetPropertyBlock(propBlock);
            }
        }

        public void HideWallPreview()
        {
            if (_wallPreview != null)
            {
                _wallPreview.SetActive(false);
            }
        }

        public Vector3 GetWallWorldPosition(WallPlacement wall)
        {
            if (boardConfig != null)
            {
                return boardConfig.GetWallWorldPosition(wall.Position, wall.Orientation);
            }

            // Fallback: wall sits between tiles
            float offsetX = (_boardWidth - 1) * 0.5f;
            float offsetY = (_boardHeight - 1) * 0.5f;
            float wallOffset = 0.5f;

            float x = wall.Position.X - offsetX + wallOffset;
            float z = wall.Position.Y - offsetY + wallOffset;

            return new Vector3(x, 0, z);
        }

        #endregion

        #region Public Accessors

        public int BoardWidth => _boardWidth;
        public int BoardHeight => _boardHeight;
        public int BoardSize => _boardWidth; // Backwards compat
        public BoardConfig Config => boardConfig;

        public TileView GetTile(BoardPosition position)
        {
            if (position.X >= 0 && position.X < _boardWidth &&
                position.Y >= 0 && position.Y < _boardHeight)
            {
                return _tiles[position.X, position.Y];
            }
            return null;
        }

        #endregion
    }
}
