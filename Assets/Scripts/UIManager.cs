using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("HUD")]
    [SerializeField] private TextMeshProUGUI _scoreText;
    [Tooltip("Sits next to the heart icon, shows e.g. \"x 3\"")]
    [SerializeField] private TextMeshProUGUI _livesText;
    [SerializeField] private TextMeshProUGUI _depthText;
    [Tooltip("Other HUD objects to hide while the start menu is open (e.g. the heart icon)")]
    [SerializeField] private GameObject[] _extraHudElements;

    [Header("Game Over")]
    [SerializeField] private GameObject _gameOverPanel;
    [SerializeField] private TextMeshProUGUI _gameOverText;

    private void Start()
    {
        // Hide both explicitly - the text isn't necessarily a child of the panel
        SetGameOverVisible(false);

        // Subscribed in Start (not OnEnable) and through Instance (not HasInstance):
        // the GameManager singleton is only registered the first time Instance is accessed,
        // so HasInstance can still be false during OnEnable.
        // GameManager fires its initial values one frame after Start, so nothing is missed.
        GameManager gm = GameManager.Instance;
        gm.OnScoreChanged += UpdateScore;
        gm.OnLivesChanged += UpdateLives;
        gm.OnDepthChanged += UpdateDepth;
        gm.OnGameOver += ShowGameOver;

        UpdateScore(gm.Score);
        UpdateLives(gm.Lives);
        UpdateDepth(Mathf.FloorToInt(gm.MaxDepth));
    }

    private void OnDestroy()
    {
        if (GameManager.HasInstance)
        {
            GameManager gm = GameManager.Instance;
            gm.OnScoreChanged -= UpdateScore;
            gm.OnLivesChanged -= UpdateLives;
            gm.OnDepthChanged -= UpdateDepth;
            gm.OnGameOver -= ShowGameOver;
        }
    }

    private void UpdateScore(int score)
    {
        if (_scoreText != null)
        {
            _scoreText.text = $"Score: {score}";
        }
    }

    private void UpdateLives(int lives)
    {
        if (_livesText != null)
        {
            _livesText.text = $"x {Mathf.Max(0, lives)}";
        }
    }

    private void UpdateDepth(int depth)
    {
        if (_depthText != null)
        {
            _depthText.text = $"{depth}m";
        }
    }

    private void ShowGameOver()
    {
        GameManager gm = GameManager.Instance;

        if (_gameOverText != null)
        {
            string restartKey = gm.RestartBindingDisplay;
            string restartLine = string.IsNullOrEmpty(restartKey) ? "" : $"\n\nPress {restartKey} to restart";

            _gameOverText.text =
                $"GAME OVER\n\n" +
                $"Depth: {Mathf.FloorToInt(gm.MaxDepth)}m\n" +
                $"Final Score: {gm.Score}" +
                restartLine;
        }

        SetGameOverVisible(true);
    }

    /// <summary>
    /// Shows/hides the in-game HUD (score, lives, depth). Used by the start menu.
    /// </summary>
    public void SetHudVisible(bool visible)
    {
        if (_scoreText != null) _scoreText.gameObject.SetActive(visible);
        if (_livesText != null) _livesText.gameObject.SetActive(visible);
        if (_depthText != null) _depthText.gameObject.SetActive(visible);

        if (_extraHudElements != null)
        {
            foreach (GameObject element in _extraHudElements)
            {
                if (element != null) element.SetActive(visible);
            }
        }
    }

    private void SetGameOverVisible(bool visible)
    {
        if (_gameOverPanel != null) _gameOverPanel.SetActive(visible);
        if (_gameOverText != null) _gameOverText.gameObject.SetActive(visible);
    }
}
