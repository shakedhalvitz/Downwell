using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameManager : Singleton<GameManager>
{
    [Header("Game Settings")]
    [SerializeField] private int _startingLives = 3;
    [SerializeField] private float _respawnDelay = 2f;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference _restartAction;

    private int _score;
    private int _lives;
    private float _maxDepth;
    private bool _gameOver;
    private bool _playerAlive;

    public int Score => _score;
    public int Lives => _lives;
    public float MaxDepth => _maxDepth;
    public bool GameOver => _gameOver;
    public bool PlayerAlive => _playerAlive;

    // Human-readable key for the restart action (e.g. "Space"), used by the Game Over screen
    public string RestartBindingDisplay => _restartAction != null ? _restartAction.action.GetBindingDisplayString() : "";

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

    private void Start()
    {
        // Wait one frame to ensure all other scripts have subscribed to the events before starting
        StartCoroutine(StartGameNextFrame());
    }

    private IEnumerator StartGameNextFrame()
    {
        yield return null;
        StartGame();
    }

    public void StartGame()
    {
        _gameOver = false;
        _playerAlive = true;
        _score = 0;
        _maxDepth = 0f;
        _lives = _startingLives;

        OnScoreChanged?.Invoke(_score);
        OnLivesChanged?.Invoke(_lives);
        OnDepthChanged?.Invoke(0);
        OnGameStarted?.Invoke();
    }

    private void OnRestartPerformed(InputAction.CallbackContext context)
    {
        if (_gameOver)
        {
            RestartGame();
        }
    }

    public void RestartGame()
    {
        // Reloading the active scene cleanly resets all states, objects, and pools
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void AddScore(int pointsToAdd)
    {
        if (_gameOver || !_playerAlive) return;

        _score += pointsToAdd;
        OnScoreChanged?.Invoke(_score);
    }

    public void AddLife()
    {
        if (_gameOver || !_playerAlive) return;

        _lives++;
        OnLivesChanged?.Invoke(_lives);
    }

    public void UpdateDepth(float playerY)
    {
        if (_gameOver || !_playerAlive) return;

        float currentDepth = Mathf.Max(0, -playerY);
        if (currentDepth > _maxDepth)
        {
            _maxDepth = currentDepth;
            OnDepthChanged?.Invoke(Mathf.FloorToInt(_maxDepth));
        }
    }

    public void OnPlayerHit()
    {
        if (_gameOver || !_playerAlive) return;

        _playerAlive = false;
        _lives--;

        OnLivesChanged?.Invoke(_lives);
        OnPlayerDied?.Invoke();

        if (_lives <= 0)
        {
            _gameOver = true;

            // Add the max depth achieved to the final score when the game is over
            _score += Mathf.FloorToInt(_maxDepth);
            OnScoreChanged?.Invoke(_score);

            OnGameOver?.Invoke();
        }
        else
        {
            Invoke(nameof(RespawnPlayer), _respawnDelay);
        }
    }

    private void RespawnPlayer()
    {
        if (_gameOver) return;

        _playerAlive = true;
        OnPlayerRespawned?.Invoke();
    }
}