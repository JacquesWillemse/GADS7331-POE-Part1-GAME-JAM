using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HeroStats : MonoBehaviour
{
    public static HeroStats Instance { get; private set; }

    [Header("Starting Values")]
    [SerializeField] private int startingMoney = 0;
    [SerializeField] private int startingHealth = 100;
    [SerializeField] private int startingAmmo = 60;
    [SerializeField] private int startingArmor = 0;
    [SerializeField] private int startingBonusDamage = 0;
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int maxArmor = 100;
    [SerializeField] private int maxAmmo = 150;
    [SerializeField] private float restartDelaySeconds = 1f;

    public int Money { get; private set; }
    public int Health { get; private set; }
    public int Ammo { get; private set; }
    public int Armor { get; private set; }
    public int BonusDamage { get; private set; }
    public int MaxHealth => maxHealth;
    public int MaxArmor => maxArmor;
    public int MaxAmmo => maxAmmo;
    public float HealthPercent => maxHealth > 0 ? (float)Health / maxHealth : 0f;

    public event Action OnStatsChanged;
    private bool _isDead;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Money = startingMoney;
        Health = Mathf.Clamp(startingHealth, 0, maxHealth);
        Ammo = Mathf.Clamp(startingAmmo, 0, maxAmmo);
        Armor = Mathf.Clamp(startingArmor, 0, maxArmor);
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
        Ammo = Mathf.Clamp(Ammo, 0, maxAmmo);
        NotifyStatsChanged();
        return true;
    }

    public void AddAmmo(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        Ammo = Mathf.Clamp(Ammo + amount, 0, maxAmmo);
        NotifyStatsChanged();
    }

    public void TakeDamage(int amount)
    {
        if (amount <= 0)
        {
            return;
        }
        if (_isDead)
        {
            return;
        }

        int remainingDamage = amount;
        if (Armor > 0)
        {
            int absorbed = Mathf.Min(Armor, remainingDamage);
            Armor = Mathf.Clamp(Armor - absorbed, 0, maxArmor);
            remainingDamage -= absorbed;
        }

        if (remainingDamage > 0)
        {
            Health = Mathf.Clamp(Health - remainingDamage, 0, maxHealth);
        }

        NotifyStatsChanged();

        if (Health <= 0)
        {
            HandleHeroDeath();
        }
    }

    public void AddHealth(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        Health = Mathf.Clamp(Health + amount, 0, maxHealth);
        NotifyStatsChanged();
    }

    public void AddArmor(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        Armor = Mathf.Clamp(Armor + amount, 0, maxArmor);
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

    private void HandleHeroDeath()
    {
        if (_isDead)
        {
            return;
        }

        _isDead = true;
        Invoke(nameof(ReloadCurrentScene), Mathf.Max(0f, restartDelaySeconds));
    }

    private void ReloadCurrentScene()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }
}
