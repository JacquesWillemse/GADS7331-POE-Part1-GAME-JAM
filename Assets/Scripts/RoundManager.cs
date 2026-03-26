using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoundManager : MonoBehaviour
{
    public bool IsRoundInProgress => _roundInProgress;
    public bool IsIntermission => !_roundInProgress && _completedRounds > 0 && _completedRounds < totalRounds;

    [Header("Round Config")]
    [SerializeField] private EnemySpawner enemySpawner;
    [SerializeField] private int totalRounds = 50;
    [SerializeField] private int type1StartingEnemies = 2;
    [SerializeField] private int type1IncreasePerRound = 2;
    [SerializeField] private int type2StartingEnemies = 0;
    [SerializeField] private int type2IncreaseEveryNRounds = 5;
    [SerializeField] private int type2IncreaseAmount = 1;
    [SerializeField] private float nextRoundAutoStartSeconds = 30f;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI roundCounterText;
    [SerializeField] private TextMeshProUGUI nextEnemyType1Text;
    [SerializeField] private TextMeshProUGUI nextEnemyType2Text;
    [SerializeField] private TextMeshProUGUI nextEnemyType3Text;
    [SerializeField] private TextMeshProUGUI gauntletPromptText;
    [SerializeField] private TextMeshProUGUI nextRoundCountdownText;
    [SerializeField] private Button startNextRoundButton;
    [SerializeField] private string preRoundStartText = "Start Gauntlet";
    [SerializeField] private string betweenRoundPromptText = "Next Round";

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

        if (_completedRounds == 0)
        {
            UpdateCountdownUI();
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
        int[] enemyCounts = GetEnemyCountsForRound(roundNumber);

        bool started = enemySpawner.TryStartRound(enemyCounts);
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

    private int[] GetEnemyCountsForRound(int roundNumber)
    {
        int type1Count = type1StartingEnemies + (roundNumber - 1) * type1IncreasePerRound;
        int type2Count = type2StartingEnemies;
        if (type2IncreaseEveryNRounds > 0)
        {
            type2Count += ((roundNumber - 1) / type2IncreaseEveryNRounds) * type2IncreaseAmount;
        }

        int[] counts = new int[2];
        counts[0] = Mathf.Max(0, type1Count);
        counts[1] = Mathf.Max(0, type2Count);
        return counts;
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
        int nextType1Count = 0;
        int nextType2Count = 0;
        if (nextRound <= totalRounds)
        {
            int[] counts = GetEnemyCountsForRound(nextRound);
            nextType1Count = counts.Length > 0 ? counts[0] : 0;
            nextType2Count = counts.Length > 1 ? counts[1] : 0;
        }

        if (nextEnemyType1Text != null)
        {
            nextEnemyType1Text.text = nextType1Count.ToString();
        }

        if (nextEnemyType2Text != null)
        {
            nextEnemyType2Text.text = nextType2Count.ToString();
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

        bool showButton = !_roundInProgress && _completedRounds < totalRounds;
        startNextRoundButton.gameObject.SetActive(showButton);
        startNextRoundButton.interactable = showButton;
    }

    private void ResetBetweenRoundTimer()
    {
        if (_completedRounds == 0)
        {
            _betweenRoundCountdownRemaining = -1f;
            _lastCountdownDisplay = -1;
            return;
        }

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
        if (_roundInProgress)
        {
            if (gauntletPromptText != null)
            {
                gauntletPromptText.text = string.Empty;
            }

            if (nextRoundCountdownText != null)
            {
                nextRoundCountdownText.text = "(0)";
            }
            return;
        }

        if (gauntletPromptText != null)
        {
            gauntletPromptText.text = _completedRounds == 0 ? preRoundStartText : betweenRoundPromptText;
        }

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
