using UnityEngine;

public class EnemyMover : MonoBehaviour
{
    [SerializeField] private Transform heroTarget;
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float stopDistance = 0.1f;

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
}
