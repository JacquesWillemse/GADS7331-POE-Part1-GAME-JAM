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
    [SerializeField] private int budgetPerRoundStep = 5;
    [SerializeField] private int smallEnemyCost = 2;
    [SerializeField] private int mediumEnemyCost = 4;
    [SerializeField] private int largeEnemyCost = 10;
    [SerializeField] private int mediumUnlockRound = 21;
    [SerializeField] private int largeUnlockRound = 41;
    [SerializeField] private float nextRoundAutoStartSeconds = 30f;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI roundCounterText;
    [SerializeField] private TextMeshProUGUI nextEnemyType1Text;
    [SerializeField] private TextMeshProUGUI nextEnemyType2Text;
    [SerializeField] private TextMeshProUGUI nextEnemyType3Text;
    [SerializeField] private TextMeshProUGUI gauntletPromptText;
    [SerializeField] private TextMeshProUGUI nextRoundCountdownText;
    [SerializeField] private Button startNextRoundButton;
    [SerializeField] private TextMeshProUGUI startNextRoundButtonText;
    [SerializeField] private string preRoundStartText = "Start Gauntlet";
    [SerializeField] private string betweenRoundPromptText = "Start Next Round";

    private bool _roundInProgress;
    private int _completedRounds;
    private int _activeRoundNumber;
    private float _betweenRoundCountdownRemaining;
    private int _lastCountdownDisplay = -1;
    private int[] _nextRoundSpawnPlan;
    private int _nextRoundSpawnPlanNumber = -1;

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
        int[] enemyCounts = GetOrCreateSpawnPlanForRound(roundNumber);

        bool started = enemySpawner.TryStartRound(enemyCounts);
        if (!started)
        {
            Debug.LogWarning("Round could not start. Check EnemySpawner setup.");
            return;
        }

        _activeRoundNumber = roundNumber;
        _roundInProgress = true;
        _nextRoundSpawnPlan = null;
        _nextRoundSpawnPlanNumber = -1;
        _betweenRoundCountdownRemaining = 0f;
        _lastCountdownDisplay = -1;
        UpdateCountdownUI();
        UpdateButtonState();
    }

    private int[] GetOrCreateSpawnPlanForRound(int roundNumber)
    {
        if (_nextRoundSpawnPlan != null && _nextRoundSpawnPlanNumber == roundNumber)
        {
            return _nextRoundSpawnPlan;
        }

        int[] counts = BuildSpawnPlan(roundNumber);
        _nextRoundSpawnPlan = counts;
        _nextRoundSpawnPlanNumber = roundNumber;
        return counts;
    }

    private int[] BuildSpawnPlan(int roundNumber)
    {
        int budget = Mathf.Max(0, roundNumber * budgetPerRoundStep);
        int[] costs = new int[3] { Mathf.Max(1, smallEnemyCost), Mathf.Max(1, mediumEnemyCost), Mathf.Max(1, largeEnemyCost) };
        bool allowMedium = roundNumber >= mediumUnlockRound;
        bool allowLarge = roundNumber >= largeUnlockRound;

        int[] counts = new int[3];
        int[] allowedTypes = allowLarge ? new int[3] { 0, 1, 2 } : (allowMedium ? new int[2] { 0, 1 } : new int[1] { 0 });

        int safety = 0;
        while (budget >= costs[0] && safety < 2000)
        {
            safety++;
            int[] affordable = GetAffordableTypes(allowedTypes, costs, budget);
            if (affordable.Length == 0)
            {
                break;
            }

            int pick = affordable[Random.Range(0, affordable.Length)];
            counts[pick]++;
            budget -= costs[pick];
        }

        return counts;
    }

    private int[] GetAffordableTypes(int[] allowedTypes, int[] costs, int budget)
    {
        int count = 0;
        for (int i = 0; i < allowedTypes.Length; i++)
        {
            int type = allowedTypes[i];
            if (type >= 0 && type < costs.Length && costs[type] <= budget)
            {
                count++;
            }
        }

        int[] affordable = new int[count];
        int at = 0;
        for (int i = 0; i < allowedTypes.Length; i++)
        {
            int type = allowedTypes[i];
            if (type >= 0 && type < costs.Length && costs[type] <= budget)
            {
                affordable[at] = type;
                at++;
            }
        }

        return affordable;
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
        int nextType3Count = 0;
        if (nextRound <= totalRounds)
        {
            int[] counts = GetOrCreateSpawnPlanForRound(nextRound);
            nextType1Count = counts.Length > 0 ? counts[0] : 0;
            nextType2Count = counts.Length > 1 ? counts[1] : 0;
            nextType3Count = counts.Length > 2 ? counts[2] : 0;
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
            nextEnemyType3Text.text = nextType3Count.ToString();
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

        if (startNextRoundButtonText != null)
        {
            startNextRoundButtonText.text = _completedRounds == 0 ? preRoundStartText : betweenRoundPromptText;
        }
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
        if (_completedRounds == 0 && !_roundInProgress)
        {
            if (gauntletPromptText != null)
            {
                gauntletPromptText.text = preRoundStartText;
            }

            if (nextRoundCountdownText != null)
            {
                nextRoundCountdownText.text = string.Empty;
            }

            return;
        }

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
