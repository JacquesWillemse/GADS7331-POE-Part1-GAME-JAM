using UnityEngine;

public class EnemyMover : MonoBehaviour
{
    [SerializeField] private Transform heroTarget;
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float stopDistance = 0.1f;
    [SerializeField] private int contactDamage = 10;

    private void Update()
    {
        if (heroTarget == null)
        {
            return;
        }

        Vector3 toTarget = heroTarget.position - transform.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude <= stopDistance * stopDistance)
        {
            return;
        }

        Vector3 direction = toTarget.normalized;
        transform.position += direction * (moveSpeed * Time.deltaTime);
        transform.forward = direction;
    }

    public void SetTarget(Transform target)
    {
        heroTarget = target;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryDamageHeroAndDie(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryDamageHeroAndDie(collision.collider);
    }

    private void TryDamageHeroAndDie(Collider other)
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

        heroStats.TakeDamage(contactDamage);
        Destroy(gameObject);
    }
}
