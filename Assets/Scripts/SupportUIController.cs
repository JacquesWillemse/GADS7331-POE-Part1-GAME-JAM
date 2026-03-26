using TMPro;
using UnityEngine;

public class SupportUIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HeroStats heroStats;
    [SerializeField] private TextMeshProUGUI moneyText;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI ammoText;
    [SerializeField] private TextMeshProUGUI damageBuffInfoText;

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

    private void Awake()
    {
        if (heroStats == null)
        {
            heroStats = FindFirstObjectByType<HeroStats>();
        }
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
        AttemptPurchaseAndSpawn("Health", healthCost, healthPickupPrefab);
    }

    public void BuyAmmo()
    {
        AttemptPurchaseAndSpawn("Ammo", ammoCost, ammoPickupPrefab);
    }

    public void BuyDamageBuff()
    {
        AttemptPurchaseAndSpawn("Damage Buff", damageBuffCost, damageBuffPickupPrefab);
    }

    // Backward-compatible wrapper if existing button still calls old name.
    public void BuySpeed()
    {
        BuyDamageBuff();
    }

    public void BuyArmor()
    {
        AttemptPurchaseAndSpawn("Armor", armorCost, armorPickupPrefab);
    }

    private void AttemptPurchaseAndSpawn(string pickupName, int cost, GameObject pickupPrefab)
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

        if (!heroStats.TrySpendMoney(cost))
        {
            Debug.LogWarning($"Insufficient funds for {pickupName}. Cost: {cost}, Money: {heroStats.Money}");
            return;
        }

        Instantiate(pickupPrefab, spawnPoint.position, spawnPoint.rotation);
        Debug.Log($"Spawned {pickupName} pickup for {cost} at {spawnPoint.name}.");
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
            healthText.text = $"Health: {heroStats.Health}";
        }

        if (ammoText != null)
        {
            ammoText.text = $"Ammo: {heroStats.Ammo}";
        }

        if (damageBuffInfoText != null)
        {
            string state = heroStats.IsDamageBuffActive ? "ON" : "OFF";
            damageBuffInfoText.text = $"Damage Buff: {state}";
        }

    }
}
