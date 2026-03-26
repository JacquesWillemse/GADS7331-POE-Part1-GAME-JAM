using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Projectile : MonoBehaviour
{
    [SerializeField] private float lifetimeSeconds = 3f;
    [SerializeField] private float hitRadius = 0.2f;
    [SerializeField] private bool showDebugGizmo = true;
    [SerializeField] private Color missColor = Color.cyan;
    [SerializeField] private Color hitColor = Color.red;

    private int _damage;
    private float _speed;
    private bool _isInitialized;
    private Vector3 _lastCastStart;
    private Vector3 _lastCastEnd;
    private bool _lastCastHadHit;
    private Vector3 _travelDirection = Vector3.forward;

    public void Initialize(int damage, float speed, Vector3 direction)
    {
        _damage = damage;
        _speed = speed;
        _travelDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;
        transform.forward = _travelDirection;
        _isInitialized = true;
        Destroy(gameObject, lifetimeSeconds);
    }

    private void Update()
    {
        if (!_isInitialized)
        {
            return;
        }

        float moveDistance = _speed * Time.deltaTime;
        Vector3 start = transform.position;
        Vector3 direction = _travelDirection;
        _lastCastStart = start;
        _lastCastEnd = start + direction * moveDistance;
        _lastCastHadHit = false;

        // SphereCast prevents fast projectiles from tunneling through targets.
        if (Physics.SphereCast(start, hitRadius, direction, out RaycastHit hit, moveDistance))
        {
            _lastCastHadHit = true;
            _lastCastEnd = hit.point;
            if (TryHitEnemy(hit.collider))
            {
                return;
            }
        }

        transform.position = start + direction * moveDistance;
    }

    private void OnTriggerEnter(Collider other)
    {
        TryHitEnemy(other);
    }

    private bool TryHitEnemy(Collider other)
    {
        EnemyHealth enemy = other.GetComponent<EnemyHealth>();
        if (enemy == null)
        {
            enemy = other.GetComponentInParent<EnemyHealth>();
        }

        if (enemy == null)
        {
            return false;
        }

        enemy.TakeDamage(_damage);
        Destroy(gameObject);
        return true;
    }

    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmo)
        {
            return;
        }

        Gizmos.color = _lastCastHadHit ? hitColor : missColor;
        Gizmos.DrawLine(_lastCastStart, _lastCastEnd);
        Gizmos.DrawWireSphere(_lastCastEnd, hitRadius);
    }
}
