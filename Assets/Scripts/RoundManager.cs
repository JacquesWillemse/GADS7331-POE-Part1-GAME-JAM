using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoundManager : MonoBehaviour
{
    [Header("Round Config")]
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private int totalRounds = 50;
    [SerializeField] private int startingEnemies = 2;
    [SerializeField] private int enemiesIncreasePerRound = 2;
    [SerializeField] private float nextRoundAutoStartSeconds = 30f;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI roundCounterText;
    [SerializeField] private TextMeshProUGUI nextEnemyType1Text;
    [SerializeField] private TextMeshProUGUI nextEnemyType2Text;
    [SerializeField] private TextMeshProUGUI nextEnemyType3Text;
    [SerializeField] private TextMeshProUGUI nextRoundCountdownText;
    [SerializeField] private Button startNextRoundButton;

    private bool _roundInProgress;
    private int _completedRounds;
    private int _activeRoundNumber;
    private float _betweenRoundCountdownRemaining;
    private int _lastCountdownDisplay = -1;

    private void Awake()
    {
        if (enemySpawner == null)
        {
            enemySpawner = FindFirstObjectByType<EnemySpawner>();
        }
    }

    private void Start()
    {
        UpdateRoundUI();
        UpdateNextEnemyCounters();
        ResetBetweenRoundTimer();
        UpdateCountdownUI();
        UpdateButtonState();
    }

    private void Update()
    {
        if (enemySpawner == null || _completedRounds >= totalRounds)
        {
            return;
        }

        if (_roundInProgress)
        {
            if (!enemySpawner.IsRoundComplete())
            {
                return;
            }

            _roundInProgress = false;
            _completedRounds = _activeRoundNumber;
            UpdateRoundUI();
            UpdateNextEnemyCounters();
            ResetBetweenRoundTimer();
            UpdateCountdownUI();
            UpdateButtonState();
            return;
        }

        _betweenRoundCountdownRemaining = Mathf.Max(0f, _betweenRoundCountdownRemaining - Time.deltaTime);
        UpdateCountdownUI();
        if (_betweenRoundCountdownRemaining <= 0f)
        {
            StartNextRound();
        }
    }

    public void StartNextRoundFromButton()
    {
        StartNextRound();
    }

    private void StartNextRound()
    {
        if (_roundInProgress || _completedRounds >= totalRounds || enemySpawner == null)
        {
            return;
        }

        int roundNumber = _completedRounds + 1;
        int enemyCount = GetEnemyCountForRound(roundNumber);

        bool started = enemySpawner.TryStartRound(enemyCount);
        if (!started)
        {
            Debug.LogWarning("Round could not start. Check EnemySpawner setup.");
            return;
        }

        _activeRoundNumber = roundNumber;
        _roundInProgress = true;
        _betweenRoundCountdownRemaining = 0f;
        _lastCountdownDisplay = -1;
        UpdateCountdownUI();
        UpdateButtonState();
    }

    private int GetEnemyCountForRound(int roundNumber)
    {
        return startingEnemies + (roundNumber - 1) * enemiesIncreasePerRound;
    }

    private void UpdateRoundUI()
    {
        if (roundCounterText != null)
        {
            roundCounterText.text = $"{_completedRounds}/{totalRounds}";
        }
    }

    private void UpdateNextEnemyCounters()
    {
        int nextRound = _completedRounds + 1;
        int nextType1Count = nextRound <= totalRounds ? GetEnemyCountForRound(nextRound) : 0;

        if (nextEnemyType1Text != null)
        {
            nextEnemyType1Text.text = nextType1Count.ToString();
        }

        if (nextEnemyType2Text != null)
        {
            nextEnemyType2Text.text = "0";
        }

        if (nextEnemyType3Text != null)
        {
            nextEnemyType3Text.text = "0";
        }
    }

    private void UpdateButtonState()
    {
        if (startNextRoundButton == null)
        {
            return;
        }

        startNextRoundButton.interactable = !_roundInProgress && _completedRounds < totalRounds;
    }

    private void ResetBetweenRoundTimer()
    {
        if (_completedRounds >= totalRounds)
        {
            _betweenRoundCountdownRemaining = 0f;
            return;
        }

        _betweenRoundCountdownRemaining = nextRoundAutoStartSeconds;
        _lastCountdownDisplay = -1;
    }

    private void UpdateCountdownUI()
    {
        if (nextRoundCountdownText == null)
        {
            return;
        }

        int seconds = _completedRounds >= totalRounds ? 0 : Mathf.CeilToInt(_betweenRoundCountdownRemaining);
        if (seconds == _lastCountdownDisplay)
        {
            return;
        }

        _lastCountdownDisplay = seconds;
        nextRoundCountdownText.text = $"({seconds})";
    }
}
