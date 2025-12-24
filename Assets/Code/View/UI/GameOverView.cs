using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace WallChess.View.UI
{
    /// <summary>
    /// Displays game over screen with winner information.
    /// </summary>
    public class GameOverView : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI winnerText;
        [SerializeField] private TextMeshProUGUI resultText;
        [SerializeField] private Image winnerColorPanel;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button mainMenuButton;

        [Header("Visual Settings")]
        [SerializeField] private Color[] playerColors = {
            new Color(0.2f, 0.6f, 0.9f),
            new Color(0.9f, 0.4f, 0.2f),
            new Color(0.3f, 0.8f, 0.3f),
            new Color(0.8f, 0.3f, 0.8f)
        };

        [Header("Animation")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float fadeInDuration = 0.5f;
        [SerializeField] private RectTransform contentPanel;
        [SerializeField] private float scaleAnimDuration = 0.3f;

        private void Awake()
        {
            // Setup button listeners
            if (restartButton != null)
            {
                restartButton.onClick.AddListener(OnRestartClicked);
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.AddListener(OnMainMenuClicked);
            }
        }

        public void Show(int winnerIndex, string winnerName)
        {
            gameObject.SetActive(true);

            // Set winner info
            if (winnerText != null)
            {
                winnerText.text = winnerName;
            }

            if (resultText != null)
            {
                resultText.text = "WINS!";
            }

            // Set winner color
            if (winnerColorPanel != null && winnerIndex < playerColors.Length)
            {
                winnerColorPanel.color = playerColors[winnerIndex];
            }

            // Animate
            StartCoroutine(AnimateIn());
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private System.Collections.IEnumerator AnimateIn()
        {
            // Initial state
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }

            if (contentPanel != null)
            {
                contentPanel.localScale = Vector3.one * 0.8f;
            }

            // Fade in
            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeInDuration;

                if (canvasGroup != null)
                {
                    canvasGroup.alpha = t;
                }

                if (contentPanel != null)
                {
                    float scale = Mathf.Lerp(0.8f, 1f, EaseOutBack(t));
                    contentPanel.localScale = Vector3.one * scale;
                }

                yield return null;
            }

            // Ensure final state
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            if (contentPanel != null)
            {
                contentPanel.localScale = Vector3.one;
            }
        }

        private float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }

        private void OnRestartClicked()
        {
            Core.GameEvents.RequestRestart();
        }

        private void OnMainMenuClicked()
        {
            Core.GameEvents.RequestMainMenu();
        }
    }
}
