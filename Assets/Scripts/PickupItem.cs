using UnityEngine;

public enum PickupType
{
    Health,
    Ammo,
    DamageBuff,
    Armor
}

public class PickupItem : MonoBehaviour
{
    [SerializeField] private PickupType pickupType = PickupType.Health;
    [SerializeField] private int amount = 20;
    [SerializeField] private float buffDurationSeconds = 30f;
    [SerializeField] private float buffMultiplier = 2f;

    public PickupType PickupType => pickupType;
    public int Amount => amount;
    public float BuffDurationSeconds => buffDurationSeconds;
    public float BuffMultiplier => buffMultiplier;

    public void Configure(PickupType type, int pickupAmount, float durationSeconds = 30f, float multiplier = 2f)
    {
        pickupType = type;
        amount = pickupAmount;
        buffDurationSeconds = durationSeconds;
        buffMultiplier = multiplier;
    }

    private void OnTriggerEnter(Collider other)
    {
        HeroStats heroStats = other.GetComponent<HeroStats>();
        if (heroStats == null)
        {
            heroStats = other.GetComponentInParent<HeroStats>();
        }

        if (heroStats == null)
        {
            return;
        }

        heroStats.ApplyPickup(this);
        Destroy(gameObject);
    }
}
