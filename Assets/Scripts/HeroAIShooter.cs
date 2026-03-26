using UnityEngine;

public class HeroAIShooter : MonoBehaviour
{
    [Header("Targeting")]
    [SerializeField] private string enemyTag = "Enemy";
    [SerializeField] private float targetingRange = 20f;

    [Header("Firing")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private float shotsPerSecond = 2f;
    [SerializeField] private float projectileSpeed = 12f;
    [SerializeField] private int projectileDamage = 1;
    [SerializeField] private int ammoPerShot = 1;

    private float _nextShotTime;
    private HeroStats _heroStats;

    private void Awake()
    {
        _heroStats = GetComponent<HeroStats>();
        if (_heroStats == null)
        {
            _heroStats = HeroStats.Instance;
        }
    }

    private void Update()
    {
        Transform target = FindNearestEnemy();
        if (target == null)
        {
            return;
        }

        AimAt(target.position);

        if (Time.time >= _nextShotTime)
        {
            Fire(target);
            _nextShotTime = Time.time + (1f / shotsPerSecond);
        }
    }

    private Transform FindNearestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);
        if (enemies.Length == 0)
        {
            return null;
        }

        Transform nearest = null;
        float nearestDistance = float.MaxValue;

        for (int i = 0; i < enemies.Length; i++)
        {
            float distance = Vector3.Distance(transform.position, enemies[i].transform.position);
            if (distance > targetingRange || distance >= nearestDistance)
            {
                continue;
            }

            nearestDistance = distance;
            nearest = enemies[i].transform;
        }

        return nearest;
    }

    private void AimAt(Vector3 worldTarget)
    {
        Vector3 direction = (worldTarget - transform.position).normalized;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        transform.forward = direction;
    }

    private void Fire(Transform target)
    {
        if (projectilePrefab == null || firePoint == null || target == null)
        {
            return;
        }

        if (_heroStats != null && !_heroStats.TryUseAmmo(ammoPerShot))
        {
            return;
        }

        Vector3 direction = (target.position - firePoint.position);
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = transform.forward;
        }

        Quaternion rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        Projectile projectile = Instantiate(projectilePrefab, firePoint.position, rotation);
        int shotDamage = projectileDamage;
        if (_heroStats != null)
        {
            shotDamage = Mathf.Max(1, Mathf.RoundToInt(projectileDamage * _heroStats.CurrentDamageMultiplier));
        }

        projectile.Initialize(shotDamage, projectileSpeed, direction.normalized);
    }
}
