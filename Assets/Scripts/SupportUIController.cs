using TMPro;
using UnityEngine;

public class SupportUIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HeroStats heroStats;
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI ammoText;
    [SerializeField] private TextMeshProUGUI armorText;
    [SerializeField] private TextMeshProUGUI damageBuffInfoText;
    [SerializeField] private TextMeshProUGUI buyHealthButtonText;
    [SerializeField] private TextMeshProUGUI buyAmmoButtonText;
    [SerializeField] private TextMeshProUGUI buyDamageBuffButtonText;
    [SerializeField] private TextMeshProUGUI buyArmorButtonText;
    [SerializeField] private RoundManager roundManager;

    [Header("Pickup Prefabs")]
    [SerializeField] private GameObject healthPickupPrefab;
    [SerializeField] private GameObject ammoPickupPrefab;
    [SerializeField] private GameObject damageBuffPickupPrefab;
    [SerializeField] private GameObject armorPickupPrefab;

    [Header("Pickup Spawn Points")]
    [SerializeField] private Transform[] pickupSpawnPoints = new Transform[4];

    [Header("Buy Costs")]
    [SerializeField] private int healthCost = 20;
    [SerializeField] private int ammoCost = 15;
    [SerializeField] private int damageBuffCost = 30;
    [SerializeField] private int armorCost = 35;

    [Header("Pickup Values")]
    [SerializeField] private int healthPickupAmount = 25;
    [SerializeField] private int ammoPickupAmount = 20;
    [SerializeField] private int armorPickupAmount = 20;
    [SerializeField] private int damageBuffPickupAmount = 1;
    private bool _lastDiscountActive;

    private void Awake()
    {
        if (heroStats == null)
        {
            heroStats = FindFirstObjectByType<HeroStats>();
        }

        if (roundManager == null)
        {
            roundManager = FindFirstObjectByType<RoundManager>();
        }

        RefreshBuyButtonLabels();
    }

    private void Update()
    {
        bool discountActive = IsIntermissionDiscountActive();
        if (discountActive == _lastDiscountActive)
        {
            return;
        }

        _lastDiscountActive = discountActive;
        RefreshBuyButtonLabels();
    }

    private void OnEnable()
    {
        if (heroStats != null)
        {
            heroStats.OnStatsChanged += RefreshInfoUI;
        }

        RefreshInfoUI();
    }

    private void OnDisable()
    {
        if (heroStats != null)
        {
            heroStats.OnStatsChanged -= RefreshInfoUI;
        }
    }

    public void BuyHealth()
    {
        AttemptPurchaseAndSpawn("Health", healthCost, healthPickupPrefab, PickupType.Health, healthPickupAmount);
    }

    public void BuyAmmo()
    {
        AttemptPurchaseAndSpawn("Ammo", ammoCost, ammoPickupPrefab, PickupType.Ammo, ammoPickupAmount);
    }

    public void BuyDamageBuff()
    {
        AttemptPurchaseAndSpawn("Damage Buff", damageBuffCost, damageBuffPickupPrefab, PickupType.DamageBuff, damageBuffPickupAmount);
    }

    // Backward-compatible wrapper if existing button still calls old name.
    public void BuySpeed()
    {
        BuyDamageBuff();
    }

    public void BuyArmor()
    {
        AttemptPurchaseAndSpawn("Armor", armorCost, armorPickupPrefab, PickupType.Armor, armorPickupAmount);
    }

    private void AttemptPurchaseAndSpawn(string pickupName, int cost, GameObject pickupPrefab, PickupType pickupType, int amount)
    {
        if (heroStats == null)
        {
            Debug.LogWarning("No HeroStats assigned to SupportUIController.");
            return;
        }

        if (pickupPrefab == null)
        {
            Debug.LogWarning($"No pickup prefab assigned for {pickupName}.");
            return;
        }

        Transform spawnPoint = GetRandomSpawnPoint();
        if (spawnPoint == null)
        {
            Debug.LogWarning("No valid pickup spawn points assigned.");
            return;
        }

        int effectiveCost = GetEffectiveCost(cost);
        if (!heroStats.TrySpendMoney(effectiveCost))
        {
            Debug.LogWarning($"Insufficient funds for {pickupName}. Cost: {effectiveCost}, Money: {heroStats.Money}");
            return;
        }

        GameObject spawnedPickup = Instantiate(pickupPrefab, spawnPoint.position, spawnPoint.rotation);
        PickupItem pickupItem = spawnedPickup.GetComponent<PickupItem>();
        if (pickupItem == null)
        {
            pickupItem = spawnedPickup.AddComponent<PickupItem>();
        }

        pickupItem.Configure(pickupType, amount);
        Debug.Log($"Spawned {pickupName} pickup for {effectiveCost} at {spawnPoint.name}.");
    }

    private Transform GetRandomSpawnPoint()
    {
        int validCount = 0;
        for (int i = 0; i < pickupSpawnPoints.Length; i++)
        {
            if (pickupSpawnPoints[i] != null)
            {
                validCount++;
            }
        }

        if (validCount == 0)
        {
            return null;
        }

        int pick = Random.Range(0, validCount);
        for (int i = 0; i < pickupSpawnPoints.Length; i++)
        {
            if (pickupSpawnPoints[i] == null)
            {
                continue;
            }

            if (pick == 0)
            {
                return pickupSpawnPoints[i];
            }

            pick--;
        }

        return null;
    }

    private void RefreshInfoUI()
    {
        if (heroStats == null)
        {
            return;
        }

        if (moneyText != null)
        {
            moneyText.text = $"Money: {heroStats.Money}";
        }

        if (healthText != null)
        {
            healthText.text = $"Health: {heroStats.Health}/{heroStats.MaxHealth}";
        }

        if (ammoText != null)
        {
            ammoText.text = $"Ammo: {heroStats.Ammo}/{heroStats.MaxAmmo}";
        }

        if (armorText != null)
        {
            armorText.text = $"Armor: {heroStats.Armor}/{heroStats.MaxArmor}";
        }

        if (damageBuffInfoText != null)
        {
            damageBuffInfoText.text = $"Damage Buff: +{heroStats.BonusDamage}";
        }

    }

    private void RefreshBuyButtonLabels()
    {
        int healthDisplayCost = GetEffectiveCost(healthCost);
        int ammoDisplayCost = GetEffectiveCost(ammoCost);
        int damageDisplayCost = GetEffectiveCost(damageBuffCost);
        int armorDisplayCost = GetEffectiveCost(armorCost);

        if (buyHealthButtonText != null)
        {
            buyHealthButtonText.text = $"Buy Health ({healthDisplayCost})";
        }

        if (buyAmmoButtonText != null)
        {
            buyAmmoButtonText.text = $"Buy Ammo ({ammoDisplayCost})";
        }

        if (buyDamageBuffButtonText != null)
        {
            buyDamageBuffButtonText.text = $"Buy Damage ({damageDisplayCost})";
        }

        if (buyArmorButtonText != null)
        {
            buyArmorButtonText.text = $"Buy Armor ({armorDisplayCost})";
        }
    }

    private int GetEffectiveCost(int baseCost)
    {
        if (!IsIntermissionDiscountActive())
        {
            return baseCost;
        }

        return Mathf.Max(1, baseCost / 2);
    }

    private bool IsIntermissionDiscountActive()
    {
        return roundManager != null && roundManager.IsIntermission;
    }
}
