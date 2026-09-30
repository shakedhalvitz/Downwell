using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI _scoreText;
    [SerializeField] private TextMeshProUGUI _livesText;
    [SerializeField] private TextMeshProUGUI _depthText;

    [Header("Tracking")]
    [SerializeField] private Transform _playerTransform;

    private void OnEnable()
    {
        if (GameManager.HasInstance)
        {
            GameManager.Instance.OnScoreChanged += UpdateScore;
            GameManager.Instance.OnLivesChanged += UpdateLives;
        }
    }

    private void OnDisable()
    {
        if (GameManager.HasInstance)
        {
            GameManager.Instance.OnScoreChanged -= UpdateScore;
            GameManager.Instance.OnLivesChanged -= UpdateLives;
        }
    }

    private void Start()
    {
        if (GameManager.HasInstance)
        {
            UpdateScore(GameManager.Instance.Score);
            UpdateLives(GameManager.Instance.Lives);
        }
    }

    private void Update()
    {
        if (_playerTransform != null && _depthText != null)
        {
            float depth = Mathf.Max(0, -_playerTransform.position.y);
            _depthText.text = $"{Mathf.FloorToInt(depth)}m";
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
            _livesText.text = $"Lives: {lives}";
        }
    }
}