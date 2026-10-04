using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public enum GameState { Menu, Playing, GameOver }

public class GameManager : Singleton<GameManager>
{
    [Header("Game Settings")]
    [SerializeField] private int _startingLives = 3;
    [SerializeField] private float _respawnDelay = 2f;

    [Header("Audio")]
    [Tooltip("Played when the player loses a life but still has lives left")]
    [SerializeField] private AudioClip _playerDeathSound;
    [Tooltip("Played instead of the death sound when the last life is lost")]
    [SerializeField] private AudioClip _gameOverSound;

    [Header("Game Over")]
    [Tooltip("Ignore input for this long after Game Over, so a press at the moment of death doesn't skip the screen")]
    [SerializeField] private float _gameOverInputDelay = 1f;

    private GameState _state = GameState.Menu;
    private int _score;
    private int _lives;
    private float _maxDepth;
    private bool _playerAlive;
    private float _gameOverTime;

    public GameState State => _state;
    public bool IsPlaying => _state == GameState.Playing;
    public int Score => _score;
    public int Lives => _lives;
    public float MaxDepth => _maxDepth;
    public bool PlayerAlive => _playerAlive;

    public event Action<GameState> OnStateChanged;
    public event Action<int> OnScoreChanged;
    public event Action<int> OnLivesChanged;
    public event Action<int> OnDepthChanged;
    public event Action OnGameStarted;
    public event Action OnGameOver;
    public event Action OnPlayerDied;
    public event Action OnPlayerRespawned;

    private void Start()
    {
        // Other scripts read State in their own Start and also listen to OnStateChanged,
        // so it doesn't matter whether they run before or after this.
        SetState(GameState.Menu);
    }

    private void Update()
    {
        // On the Game Over screen: any key, mouse click or screen tap goes back to the main menu
        if (_state == GameState.GameOver &&
            Time.unscaledTime - _gameOverTime >= _gameOverInputDelay &&
            AnyPressThisFrame())
        {
            ReturnToMainMenu();
        }
    }

    private static bool AnyPressThisFrame()
    {
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) return true;
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame) return true;
        return false;
    }

    private void SetState(GameState newState)
    {
        _state = newState;
        OnStateChanged?.Invoke(_state);
    }

    /// <summary>
    /// Called by the START button (hooked up by UIManager).
    /// </summary>
    public void StartGame()
    {
        if (_state == GameState.Playing) return;

        _playerAlive = true;
        _score = 0;
        _maxDepth = 0f;
        _lives = _startingLives;

        OnScoreChanged?.Invoke(_score);
        OnLivesChanged?.Invoke(_lives);
        OnDepthChanged?.Invoke(0);

        SetState(GameState.Playing);
        OnGameStarted?.Invoke();
    }

    public void ReturnToMainMenu()
    {
        // Reloading the active scene cleanly resets all states, objects, and pools - and starts at the menu
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void AddScore(int pointsToAdd)
    {
        if (!IsPlaying || !_playerAlive) return;

        _score += pointsToAdd;
        OnScoreChanged?.Invoke(_score);
    }

    public void AddLife()
    {
        if (!IsPlaying || !_playerAlive) return;

        _lives++;
        OnLivesChanged?.Invoke(_lives);
    }

    public void UpdateDepth(float playerY)
    {
        if (!IsPlaying || !_playerAlive) return;

        float currentDepth = Mathf.Max(0, -playerY);
        if (currentDepth > _maxDepth)
        {
            _maxDepth = currentDepth;
            OnDepthChanged?.Invoke(Mathf.FloorToInt(_maxDepth));
        }
    }

    public void OnPlayerHit()
    {
        if (!IsPlaying || !_playerAlive) return;

        _playerAlive = false;
        _lives--;

        OnLivesChanged?.Invoke(_lives);
        OnPlayerDied?.Invoke();

        if (_lives <= 0)
        {
            EndGame();
        }
        else
        {
            AudioManager.Instance.PlaySfx(_playerDeathSound);
            Invoke(nameof(RespawnPlayer), _respawnDelay);
        }
    }

    /// <summary>
    /// Ends the run immediately regardless of lives left (e.g. pushed off the top of the screen).
    /// </summary>
    public void KillPlayerInstantly()
    {
        if (!IsPlaying) return;

        CancelInvoke(nameof(RespawnPlayer));
        _playerAlive = false;
        _lives = 0;

        OnLivesChanged?.Invoke(_lives);
        OnPlayerDied?.Invoke();

        EndGame();
    }

    private void EndGame()
    {
        // Add the max depth achieved to the final score when the game is over
        _score += Mathf.FloorToInt(_maxDepth);
        OnScoreChanged?.Invoke(_score);

        AudioManager.Instance.PlaySfx(_gameOverSound);

        _gameOverTime = Time.unscaledTime;
        SetState(GameState.GameOver);
        OnGameOver?.Invoke();
    }

    private void RespawnPlayer()
    {
        if (!IsPlaying) return;

        _playerAlive = true;
        OnPlayerRespawned?.Invoke();
    }
}