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

    [Header("Input Actions")]
    [SerializeField] private InputActionReference _restartAction;

    // Set by RestartGame so the reloaded scene skips the start menu
    private static bool _skipMenuOnNextLoad;

    private GameState _state = GameState.Menu;
    private int _score;
    private int _lives;
    private float _maxDepth;
    private bool _playerAlive;

    public GameState State => _state;
    public bool IsPlaying => _state == GameState.Playing;
    public int Score => _score;
    public int Lives => _lives;
    public float MaxDepth => _maxDepth;
    public bool PlayerAlive => _playerAlive;

    // Human-readable key for the restart action (e.g. "Space"), used by the Game Over screen
    public string RestartBindingDisplay => _restartAction != null ? _restartAction.action.GetBindingDisplayString() : "";

    public event Action<GameState> OnStateChanged;
    public event Action<int> OnScoreChanged;
    public event Action<int> OnLivesChanged;
    public event Action<int> OnDepthChanged;
    public event Action OnGameStarted;
    public event Action OnGameOver;
    public event Action OnPlayerDied;
    public event Action OnPlayerRespawned;

    private void OnEnable()
    {
        if (_restartAction != null)
        {
            _restartAction.action.Enable();
            _restartAction.action.performed += OnRestartPerformed;
        }
    }

    private void OnDisable()
    {
        if (_restartAction != null)
        {
            _restartAction.action.Disable();
            _restartAction.action.performed -= OnRestartPerformed;
        }
    }

    // Resets the static flag on every Play in the editor, even with domain reload turned off
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        _skipMenuOnNextLoad = false;
    }

    private void Start()
    {
        // Other scripts read State in their own Start and also listen to OnStateChanged,
        // so it doesn't matter whether they run before or after this.
        if (_skipMenuOnNextLoad)
        {
            _skipMenuOnNextLoad = false;
            StartGame();
        }
        else
        {
            SetState(GameState.Menu);
        }
    }

    private void SetState(GameState newState)
    {
        _state = newState;
        OnStateChanged?.Invoke(_state);
    }

    /// <summary>
    /// Called by the START button (hooked up by UIManager) or directly after a restart.
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

    private void OnRestartPerformed(InputAction.CallbackContext context)
    {
        if (_state == GameState.GameOver)
        {
            RestartGame();
        }
    }

    public void RestartGame()
    {
        // Reloading the active scene cleanly resets all states, objects, and pools
        _skipMenuOnNextLoad = true;
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