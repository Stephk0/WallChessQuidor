using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace WallChess.View.UI
{
    /// <summary>
    /// Displays wall count for a player.
    /// </summary>
    public class WallCountView : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI countText;
        [SerializeField] private TextMeshProUGUI playerNameText;
        [SerializeField] private Image playerColorBar;
        [SerializeField] private Transform wallIconContainer;
        [SerializeField] private GameObject wallIconPrefab;

        [Header("Visual Settings")]
        [SerializeField] private Color[] playerColors = {
            new Color(0.2f, 0.6f, 0.9f),
            new Color(0.9f, 0.4f, 0.2f),
            new Color(0.3f, 0.8f, 0.3f),
            new Color(0.8f, 0.3f, 0.8f)
        };

        private int _playerIndex;
        private int _maxWalls;
        private GameObject[] _wallIcons;

        public void Initialize(int playerIndex, int startingWalls, string playerName)
        {
            _playerIndex = playerIndex;
            _maxWalls = startingWalls;

            // Set player name
            if (playerNameText != null)
            {
                playerNameText.text = playerName;
            }

            // Set color
            if (playerColorBar != null && playerIndex < playerColors.Length)
            {
                playerColorBar.color = playerColors[playerIndex];
            }

            // Create wall icons
            CreateWallIcons(startingWalls);

            // Set count text
            UpdateCount(startingWalls);
        }

        public void UpdateCount(int wallsRemaining)
        {
            // Update text
            if (countText != null)
            {
                countText.text = wallsRemaining.ToString();
            }

            // Update icons
            UpdateWallIcons(wallsRemaining);
        }

        private void CreateWallIcons(int count)
        {
            if (wallIconContainer == null || wallIconPrefab == null)
            {
                return;
            }

            // Clear existing
            if (_wallIcons != null)
            {
                foreach (var icon in _wallIcons)
                {
                    if (icon != null)
                        Destroy(icon);
                }
            }

            _wallIcons = new GameObject[count];

            for (int i = 0; i < count; i++)
            {
                var icon = Instantiate(wallIconPrefab, wallIconContainer);
                _wallIcons[i] = icon;
            }
        }

        private void UpdateWallIcons(int wallsRemaining)
        {
            if (_wallIcons == null) return;

            for (int i = 0; i < _wallIcons.Length; i++)
            {
                if (_wallIcons[i] != null)
                {
                    // Show icons for remaining walls, hide used ones
                    _wallIcons[i].SetActive(i < wallsRemaining);
                }
            }
        }
    }
}
