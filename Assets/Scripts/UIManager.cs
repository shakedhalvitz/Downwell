using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("Screens (exactly one is shown at a time)")]
    [Tooltip("Parent of the start menu (title, START button, player animation)")]
    [SerializeField] private GameObject _menuGroup;
    [Tooltip("Parent of the in-game HUD (score, lives, heart icon, depth, mobile controls)")]
    [SerializeField] private GameObject _hudGroup;
    [Tooltip("Parent of the Game Over panel and text")]
    [SerializeField] private GameObject _gameOverGroup;

    [Header("Buttons")]
    [Tooltip("Optional - if empty, the first Button inside the menu group is used")]
    [SerializeField] private Button _startButton;

    [Header("HUD")]
    [SerializeField] private TextMeshProUGUI _scoreText;
    [Tooltip("Sits next to the heart icon, shows e.g. \"x 3\"")]
    [SerializeField] private TextMeshProUGUI _livesText;
    [SerializeField] private TextMeshProUGUI _depthText;

    [Header("Game Over")]
    [SerializeField] private TextMeshProUGUI _gameOverText;
    [Tooltip("The Game Over text auto-sizes to fill its box, up to this font size")]
    [SerializeField] private float _gameOverMaxFontSize = 110f;

    private void Start()
    {
        // Subscribed in Start (not OnEnable) and through Instance (not HasInstance):
        // the GameManager singleton is only registered the first time Instance is accessed,
        // so HasInstance can still be false during OnEnable.
        GameManager gm = GameManager.Instance;
        gm.OnStateChanged += ShowScreen;
        gm.OnScoreChanged += UpdateScore;
        gm.OnLivesChanged += UpdateLives;
        gm.OnDepthChanged += UpdateDepth;
        gm.OnGameOver += FillGameOverText;

        HookUpButtons(gm);

        // Apply the current state right away - GameManager.Start may have already run
        ShowScreen(gm.State);
        UpdateScore(gm.Score);
        UpdateLives(gm.Lives);
        UpdateDepth(Mathf.FloorToInt(gm.MaxDepth));
    }

    private void OnDestroy()
    {
        if (GameManager.HasInstance)
        {
            GameManager gm = GameManager.Instance;
            gm.OnStateChanged -= ShowScreen;
            gm.OnScoreChanged -= UpdateScore;
            gm.OnLivesChanged -= UpdateLives;
            gm.OnDepthChanged -= UpdateDepth;
            gm.OnGameOver -= FillGameOverText;
        }
    }

    private void HookUpButtons(GameManager gm)
    {
        // START button - no need to set up OnClick in the Inspector
        if (_startButton == null && _menuGroup != null)
        {
            _startButton = _menuGroup.GetComponentInChildren<Button>(true);
        }
        if (_startButton != null)
        {
            _startButton.onClick.AddListener(gm.StartGame);
        }

        // Leaving the Game Over screen (any key / click / tap) is handled by GameManager
    }

    /// <summary>
    /// Shows exactly one screen group for the given game state and hides the others.
    /// </summary>
    private void ShowScreen(GameState state)
    {
        if (_menuGroup != null) _menuGroup.SetActive(state == GameState.Menu);
        if (_hudGroup != null) _hudGroup.SetActive(state == GameState.Playing);
        if (_gameOverGroup != null) _gameOverGroup.SetActive(state == GameState.GameOver);
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

    private void FillGameOverText()
    {
        if (_gameOverText == null) return;

        GameManager gm = GameManager.Instance;

        // Grow the text as large as its box allows
        _gameOverText.enableAutoSizing = true;
        _gameOverText.fontSizeMin = 36f;
        _gameOverText.fontSizeMax = _gameOverMaxFontSize;

        _gameOverText.text =
            $"GAME OVER\n\n" +
            $"Depth: {Mathf.FloorToInt(gm.MaxDepth)}m\n" +
            $"Final Score: {gm.Score}";
    }
}
