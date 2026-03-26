using System;
using UnityEngine;

public class HeroStats : MonoBehaviour
{
    public static HeroStats Instance { get; private set; }

    [Header("Starting Values")]
    [SerializeField] private int startingMoney = 0;
    [SerializeField] private int startingHealth = 100;
    [SerializeField] private int startingAmmo = 60;
    [SerializeField] private int startingArmor = 0;
    [SerializeField] private int startingBonusDamage = 0;

    public int Money { get; private set; }
    public int Health { get; private set; }
    public int Ammo { get; private set; }
    public int Armor { get; private set; }
    public int BonusDamage { get; private set; }

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
        Armor = startingArmor;
        BonusDamage = startingBonusDamage;
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

        int remainingDamage = amount;
        if (Armor > 0)
        {
            int absorbed = Mathf.Min(Armor, remainingDamage);
            Armor -= absorbed;
            remainingDamage -= absorbed;
        }

        if (remainingDamage > 0)
        {
            Health = Mathf.Max(Health - remainingDamage, 0);
        }

        NotifyStatsChanged();
    }

    public void AddHealth(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        Health += amount;
        NotifyStatsChanged();
    }

    public void AddArmor(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        Armor += amount;
        NotifyStatsChanged();
    }

    public void AddBonusDamage(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        BonusDamage += amount;
        NotifyStatsChanged();
    }

    public void ApplyPickup(PickupItem pickup)
    {
        if (pickup == null)
        {
            return;
        }

        switch (pickup.PickupType)
        {
            case PickupType.Health:
                AddHealth(pickup.Amount);
                break;
            case PickupType.Ammo:
                AddAmmo(pickup.Amount);
                break;
            case PickupType.Armor:
                AddArmor(pickup.Amount);
                break;
            case PickupType.DamageBuff:
                AddBonusDamage(pickup.Amount);
                break;
        }
    }

    private void NotifyStatsChanged()
    {
        OnStatsChanged?.Invoke();
    }
}
