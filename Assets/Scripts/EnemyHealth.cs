using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private int moneyReward = 5;

    private int _currentHealth;

    private void Awake()
    {
        _currentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        _currentHealth -= amount;
        if (_currentHealth > 0)
        {
            return;
        }

        if (HeroStats.Instance != null)
        {
            HeroStats.Instance.AddMoney(moneyReward);
        }

        FloatingMoneyPopup.Spawn(transform.position, moneyReward);

        Destroy(gameObject);
    }
}
