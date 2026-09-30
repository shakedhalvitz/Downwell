using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : Singleton<GameManager>
{
    [Header("Game Settings")]
    [SerializeField] private int _startingLives = 3;
    [SerializeField] private float _respawnDelay = 2f;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference _restartAction;

    private int _score;
    private int _lives;
    private bool _gameOver;
    private bool _playerAlive;

    public int Score => _score;
    public int Lives => _lives;
    public bool GameOver => _gameOver;
    public bool PlayerAlive => _playerAlive;

    public event Action<int> OnScoreChanged;
    public event Action<int> OnLivesChanged;
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
        // UI subscribes in their own Start, and script execution order
        // between them is not guaranteed - so hold one frame before kicking the round off.
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
        _lives = _startingLives;

        OnScoreChanged?.Invoke(_score);
        OnLivesChanged?.Invoke(_lives);
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
        CancelInvoke(nameof(RespawnPlayer));

        // TODO: Clear Object Pools here later (Bullets, Enemies)

        StartGame();
    }

    public void AddScore(int pointsToAdd)
    {
        if (_gameOver || !_playerAlive) return;

        _score += pointsToAdd;
        OnScoreChanged?.Invoke(_score);
    }

    public void OnPlayerHit()
    {
        if (_gameOver || !_playerAlive) return;

        _playerAlive = false;
        _lives--;

        OnLivesChanged?.Invoke(_lives);
        OnPlayerDied?.Invoke();

        // In the future, we will tell the PlayerController to play death animation here

        if (_lives <= 0)
        {
            _gameOver = true;
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
        // In the future, we will tell the PlayerController to reset position here
    }
}