using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple start menu: freezes the game and hides the HUD and player until START GAME is pressed.
/// Put this on the menu panel. The button inside the panel is found and hooked up automatically.
/// </summary>
public class StartMenu : MonoBehaviour
{
    [Tooltip("Optional - if empty, the first Button inside this panel is used")]
    [SerializeField] private Button _startButton;

    // Survives scene reloads, so restarting after Game Over goes straight into the game
    private static bool _hasStartedOnce;

    private UIManager _uiManager;
    private SpriteRenderer _playerSprite;

    // Resets the flag on every Play in the editor, even with domain reload turned off
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        _hasStartedOnce = false;
    }

    private void Awake()
    {
        if (_startButton == null)
        {
            _startButton = GetComponentInChildren<Button>(true);
        }

        if (_startButton != null)
        {
            _startButton.onClick.AddListener(StartGame);
        }

        _uiManager = FindAnyObjectByType<UIManager>();

        PlayerController player = FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            _playerSprite = player.GetComponent<SpriteRenderer>();
        }
    }

    private void Start()
    {
        if (_hasStartedOnce)
        {
            SetMenuVisible(false);
            return;
        }

        Time.timeScale = 0f;
        SetMenuVisible(true);
    }

    /// <summary>
    /// Called by the START GAME button (hooked up automatically in Awake).
    /// </summary>
    public void StartGame()
    {
        _hasStartedOnce = true;
        Time.timeScale = 1f;
        SetMenuVisible(false);
    }

    /// <summary>
    /// Menu visible = panel shown, HUD and player hidden. And the other way around.
    /// </summary>
    private void SetMenuVisible(bool menuVisible)
    {
        if (_uiManager != null) _uiManager.SetHudVisible(!menuVisible);
        if (_playerSprite != null) _playerSprite.enabled = !menuVisible;

        gameObject.SetActive(menuVisible);
    }
}
