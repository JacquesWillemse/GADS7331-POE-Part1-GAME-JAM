using System;
using UnityEngine;

public class HeroStats : MonoBehaviour
{
    public static HeroStats Instance { get; private set; }

    [Header("Starting Values")]
    [SerializeField] private int startingMoney = 0;
    [SerializeField] private int startingHealth = 100;
    [SerializeField] private int startingAmmo = 60;
    [SerializeField] private float defaultDamageMultiplier = 1f;

    public int Money { get; private set; }
    public int Health { get; private set; }
    public int Ammo { get; private set; }
    public float CurrentDamageMultiplier => _damageBuffActive ? _damageBuffMultiplier : defaultDamageMultiplier;
    public bool IsDamageBuffActive => _damageBuffActive;
    public int DamageBuffSecondsRemaining => Mathf.CeilToInt(_damageBuffRemainingSeconds);

    public event Action OnStatsChanged;
    private bool _damageBuffActive;
    private float _damageBuffMultiplier;
    private float _damageBuffRemainingSeconds;
    private int _lastBroadcastBuffSeconds = -1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Money = startingMoney;
        Health = startingHealth;
        Ammo = startingAmmo;
        _damageBuffMultiplier = defaultDamageMultiplier;
        NotifyStatsChanged();
    }

    private void Update()
    {
        if (!_damageBuffActive)
        {
            return;
        }

        _damageBuffRemainingSeconds = Mathf.Max(0f, _damageBuffRemainingSeconds - Time.deltaTime);
        int remainingSeconds = DamageBuffSecondsRemaining;
        if (remainingSeconds != _lastBroadcastBuffSeconds)
        {
            _lastBroadcastBuffSeconds = remainingSeconds;
            NotifyStatsChanged();
        }

        if (_damageBuffRemainingSeconds > 0f)
        {
            return;
        }

        _damageBuffActive = false;
        _damageBuffMultiplier = defaultDamageMultiplier;
        _lastBroadcastBuffSeconds = 0;
        NotifyStatsChanged();
    }

    public void AddMoney(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        Money += amount;
        NotifyStatsChanged();
    }

    public bool TrySpendMoney(int amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        if (Money < amount)
        {
            return false;
        }

        Money -= amount;
        NotifyStatsChanged();
        return true;
    }

    public bool TryUseAmmo(int amount)
    {
        if (amount <= 0)
        {
            return true;
        }

        if (Ammo < amount)
        {
            return false;
        }

        Ammo -= amount;
        NotifyStatsChanged();
        return true;
    }

    public void AddAmmo(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        Ammo += amount;
        NotifyStatsChanged();
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        Health = Mathf.Max(Health - amount, 0);
        NotifyStatsChanged();
    }

    public void ActivateDamageBuff(float durationSeconds, float multiplier)
    {
        if (durationSeconds <= 0f || multiplier <= 0f)
        {
            return;
        }

        _damageBuffActive = true;
        _damageBuffMultiplier = multiplier;
        _damageBuffRemainingSeconds = Mathf.Max(_damageBuffRemainingSeconds, durationSeconds);
        _lastBroadcastBuffSeconds = -1;
        NotifyStatsChanged();
    }

    private void NotifyStatsChanged()
    {
        OnStatsChanged?.Invoke();
    }
}
