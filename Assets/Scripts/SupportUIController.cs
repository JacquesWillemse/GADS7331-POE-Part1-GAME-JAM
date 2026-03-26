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

    [Header("Buy Costs")]
    [SerializeField] private int healthCost = 20;
    [SerializeField] private int ammoCost = 15;
    [SerializeField] private int damageBuffCost = 30;
    [SerializeField] private int armorCost = 35;

    [Header("Damage Buff")]
    [SerializeField] private float damageBuffDurationSeconds = 30f;
    [SerializeField] private float damageBuffMultiplier = 2f;

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
        AttemptPurchase("Health", healthCost);
    }

    public void BuyAmmo()
    {
        AttemptPurchase("Ammo", ammoCost);
    }

    public void BuyDamageBuff()
    {
        if (heroStats == null)
        {
            Debug.LogWarning("No HeroStats assigned to SupportUIController.");
            return;
        }

        if (!heroStats.TrySpendMoney(damageBuffCost))
        {
            Debug.LogWarning($"Insufficient funds for Damage Buff. Cost: {damageBuffCost}, Money: {heroStats.Money}");
            return;
        }

        heroStats.ActivateDamageBuff(damageBuffDurationSeconds, damageBuffMultiplier);
        Debug.Log($"Purchased Damage Buff for {damageBuffCost}. Active for {damageBuffDurationSeconds:0}s.");
    }

    // Backward-compatible wrapper if existing button still calls old name.
    public void BuySpeed()
    {
        BuyDamageBuff();
    }

    public void BuyArmor()
    {
        AttemptPurchase("Armor", armorCost);
    }

    private void AttemptPurchase(string purchaseName, int cost)
    {
        if (heroStats == null)
        {
            Debug.LogWarning("No HeroStats assigned to SupportUIController.");
            return;
        }

        if (!heroStats.TrySpendMoney(cost))
        {
            Debug.LogWarning($"Insufficient funds for {purchaseName}. Cost: {cost}, Money: {heroStats.Money}");
            return;
        }

        Debug.Log($"Purchased {purchaseName} for {cost}.");
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
