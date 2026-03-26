using System;
using UnityEngine;

public class HeroStats : MonoBehaviour
{
    public static HeroStats Instance { get; private set; }

    [Header("Starting Values")]
    [SerializeField] private int startingMoney = 0;
    [SerializeField] private int startingHealth = 100;
    [SerializeField] private int startingAmmo = 60;

    public int Money { get; private set; }
    public int Health { get; private set; }
    public int Ammo { get; private set; }

    public event Action OnStatsChanged;

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

    private void NotifyStatsChanged()
    {
        OnStatsChanged?.Invoke();
    }
}
