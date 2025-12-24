using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace WallChess.View.UI
{
    /// <summary>
    /// Displays whose turn it is and provides visual feedback.
    /// </summary>
    public class TurnIndicatorView : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI playerNameText;
        [SerializeField] private TextMeshProUGUI turnLabelText;
        [SerializeField] private Image playerColorIndicator;
        [SerializeField] private GameObject aiThinkingIndicator;

        [Header("Player Colors")]
        [SerializeField] private Color[] playerColors = {
            new Color(0.2f, 0.6f, 0.9f), // Blue
            new Color(0.9f, 0.4f, 0.2f), // Orange
            new Color(0.3f, 0.8f, 0.3f), // Green
            new Color(0.8f, 0.3f, 0.8f)  // Purple
        };

        [Header("Animation")]
        [SerializeField] private float transitionDuration = 0.3f;
        [SerializeField] private CanvasGroup canvasGroup;

        private int _playerCount;
        private int _currentPlayer = -1;

        public void Initialize(int playerCount)
        {
            _playerCount = playerCount;

            if (aiThinkingIndicator != null)
            {
                aiThinkingIndicator.SetActive(false);
            }
        }

        public void SetCurrentPlayer(int playerIndex, string playerName, bool isAI)
        {
            _currentPlayer = playerIndex;

            // Update name
            if (playerNameText != null)
            {
                playerNameText.text = playerName;
            }

            // Update turn label
            if (turnLabelText != null)
            {
                turnLabelText.text = isAI ? "AI Thinking..." : "Your Turn";
            }

            // Update color indicator
            if (playerColorIndicator != null)
            {
                Color color = GetPlayerColor(playerIndex);
                playerColorIndicator.color = color;
            }

            // Show/hide AI indicator
            if (aiThinkingIndicator != null)
            {
                aiThinkingIndicator.SetActive(isAI);
            }

            // Optional: animate transition
            if (canvasGroup != null)
            {
                StartCoroutine(AnimateTransition());
            }
        }

        private Color GetPlayerColor(int playerIndex)
        {
            if (playerColors == null || playerColors.Length == 0)
            {
                return Color.white;
            }

            return playerColors[playerIndex % playerColors.Length];
        }

        private System.Collections.IEnumerator AnimateTransition()
        {
            // Fade out
            float elapsed = 0f;
            while (elapsed < transitionDuration * 0.5f)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = 1f - (elapsed / (transitionDuration * 0.5f));
                yield return null;
            }

            // Fade in
            elapsed = 0f;
            while (elapsed < transitionDuration * 0.5f)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = elapsed / (transitionDuration * 0.5f);
                yield return null;
            }

            canvasGroup.alpha = 1f;
        }
    }
}
