using UnityEngine;

public class HeroAIMovement : MonoBehaviour
{
    [Header("Threat Detection")]
    [SerializeField] private string enemyTag = "Enemy";
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private int maxEnemiesToConsider = 30;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float minSpeedAtZeroHealth = 1.5f;
    [SerializeField] private float repathInterval = 0.25f;
    [SerializeField] private float arriveDistance = 0.2f;

    [Header("Pickup Priority")]
    [SerializeField] private float pickupDetectionRange = 30f;
    [SerializeField] private float safePickupEnemyDistance = 1.6f;
    [SerializeField] private int lowHealthThreshold = 50;
    [SerializeField] private int criticalHealthThreshold = 25;
    [SerializeField] private int lowAmmoThreshold = 20;
    [SerializeField] private int criticalAmmoThreshold = 8;
    [SerializeField] private int lowArmorThreshold = 20;
    [SerializeField] private int criticalArmorThreshold = 8;
    [SerializeField] private float pickupAvoidanceRadius = 3f;
    [SerializeField] private float pickupAvoidanceStrength = 1.5f;
    [SerializeField] private float pickupApproachStep = 2.5f;

    [Header("Safe Point Search")]
    [SerializeField] private float searchRadius = 4f;
    [SerializeField] private int candidateDirections = 16;
    [SerializeField] private float keepAwayWeight = 1f;
    [SerializeField] private float centerBiasWeight = 0.2f;
    [SerializeField] private float movementPenaltyWeight = 0.1f;

    [Header("Obstacle Avoidance")]
    [SerializeField] private LayerMask obstacleLayers = ~0;
    [SerializeField] private float obstacleCheckRadius = 0.35f;
    [SerializeField] private float pathBlockedPenalty = 100f;

    [Header("Fallback")]
    [SerializeField] private float fallbackStepDistance = 1.2f;
    [SerializeField] private int fallbackDirections = 8;
    [SerializeField] private float stuckTimeout = 0.4f;

    [Header("Idle Recenter")]
    [SerializeField] private Transform arenaCenter;
    [SerializeField] private float centerReturnDistance = 1.5f;
    [SerializeField] private bool drawDebug = false;

    private Vector3 _currentDestination;
    private float _nextRepathTime;
    private Vector3 _lastBestCandidate;
    private float _stuckTimer;
    private HeroStats _heroStats;
    private PickupItem _debugSelectedPickup;
    private string _debugPickupDecision = "None";
    private bool _debugPickupPathSafe;

    private void Start()
    {
        _heroStats = GetComponent<HeroStats>();
        if (_heroStats == null)
        {
            _heroStats = HeroStats.Instance;
        }

        _currentDestination = transform.position;
    }

    private void Update()
    {
        if (Time.time >= _nextRepathTime)
        {
            RecalculateDestination();
            _nextRepathTime = Time.time + repathInterval;
        }

        MoveTowardDestination();
    }

    private void RecalculateDestination()
    {
        Vector3 heroPos = transform.position;
        Transform[] threats = GetNearbyEnemies();
        PickupItem pickupTarget = ChoosePickupTarget(heroPos, threats);
        if (pickupTarget != null)
        {
            Vector3 pickupPos = pickupTarget.transform.position;
            pickupPos.y = heroPos.y;
            Vector3 approachPos = GetPickupApproachDestination(heroPos, pickupPos, threats);
            _currentDestination = approachPos;
            _lastBestCandidate = approachPos;
            _debugSelectedPickup = pickupTarget;
            return;
        }
        _debugSelectedPickup = null;
        _debugPickupDecision = "No pickup target";
        _debugPickupPathSafe = false;

        if (threats.Length == 0)
        {
            SetCenterDestinationIfNeeded(heroPos);
            return;
        }

        Vector3 bestCandidate = heroPos;
        float bestScore = ScoreCandidate(heroPos, threats, heroPos);

        for (int i = 0; i < candidateDirections; i++)
        {
            float angle = (360f / candidateDirections) * i;
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
            Vector3 candidate = heroPos + dir * searchRadius;
            candidate.y = heroPos.y;

            float score = ScoreCandidate(candidate, threats, heroPos);
            if (score > bestScore)
            {
                bestScore = score;
                bestCandidate = candidate;
            }
        }

        _currentDestination = bestCandidate;
        _lastBestCandidate = bestCandidate;
    }

    private float ScoreCandidate(Vector3 candidate, Transform[] threats, Vector3 heroPos)
    {
        if (IsCandidateBlocked(candidate))
        {
            return float.NegativeInfinity;
        }

        float minThreatDistance = float.MaxValue;
        Vector3 threatCenter = Vector3.zero;
        int count = 0;

        for (int i = 0; i < threats.Length; i++)
        {
            if (threats[i] == null)
            {
                continue;
            }

            Vector3 threatPos = threats[i].position;
            threatPos.y = candidate.y;
            float dist = Vector3.Distance(candidate, threatPos);
            if (dist < minThreatDistance)
            {
                minThreatDistance = dist;
            }

            threatCenter += threatPos;
            count++;
        }

        if (count == 0)
        {
            return 0f;
        }

        threatCenter /= count;
        float awayFromCenter = Vector3.Distance(candidate, threatCenter);
        float movementPenalty = Vector3.Distance(candidate, heroPos);
        float blockedPathPenalty = IsPathBlocked(heroPos, candidate) ? pathBlockedPenalty : 0f;

        return (minThreatDistance * keepAwayWeight)
            + (awayFromCenter * centerBiasWeight)
            - (movementPenalty * movementPenaltyWeight)
            - blockedPathPenalty;
    }

    private Transform[] GetNearbyEnemies()
    {
        GameObject[] allEnemies = GameObject.FindGameObjectsWithTag(enemyTag);
        if (allEnemies.Length == 0)
        {
            return new Transform[0];
        }

        Transform[] results = new Transform[Mathf.Min(maxEnemiesToConsider, allEnemies.Length)];
        int found = 0;
        float maxRangeSqr = detectionRange * detectionRange;

        for (int i = 0; i < allEnemies.Length; i++)
        {
            Vector3 delta = allEnemies[i].transform.position - transform.position;
            delta.y = 0f;

            if (delta.sqrMagnitude > maxRangeSqr)
            {
                continue;
            }

            results[found] = allEnemies[i].transform;
            found++;
            if (found >= results.Length)
            {
                break;
            }
        }

        if (found == results.Length)
        {
            return results;
        }

        Transform[] trimmed = new Transform[found];
        for (int i = 0; i < found; i++)
        {
            trimmed[i] = results[i];
        }

        return trimmed;
    }

    private void MoveTowardDestination()
    {
        Vector3 current = transform.position;
        Vector3 target = _currentDestination;
        target.y = current.y;

        Vector3 toTarget = target - current;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude <= arriveDistance * arriveDistance)
        {
            return;
        }

        Vector3 direction = toTarget.normalized;
        float currentSpeed = GetCurrentMoveSpeed();
        float moveDistance = currentSpeed * Time.deltaTime;
        Vector3 castOrigin = current + Vector3.up * 0.5f;

        // Stop before moving into an obstacle.
        if (Physics.SphereCast(castOrigin, obstacleCheckRadius, direction, out _, moveDistance, obstacleLayers, QueryTriggerInteraction.Ignore))
        {
            _stuckTimer += Time.deltaTime;
            if (_stuckTimer >= stuckTimeout)
            {
                _currentDestination = FindFallbackDestination(current, direction);
                _stuckTimer = 0f;
            }
            return;
        }

        transform.position += direction * moveDistance;
        transform.forward = direction;
        _stuckTimer = 0f;
    }

    private bool IsCandidateBlocked(Vector3 candidate)
    {
        Vector3 checkPos = candidate + Vector3.up * 0.5f;
        return Physics.CheckSphere(checkPos, obstacleCheckRadius, obstacleLayers, QueryTriggerInteraction.Ignore);
    }

    private bool IsPathBlocked(Vector3 from, Vector3 to)
    {
        Vector3 flatDelta = to - from;
        flatDelta.y = 0f;
        float distance = flatDelta.magnitude;
        if (distance <= 0.001f)
        {
            return false;
        }

        Vector3 direction = flatDelta / distance;
        Vector3 castOrigin = from + Vector3.up * 0.5f;
        return Physics.SphereCast(castOrigin, obstacleCheckRadius, direction, out _, distance, obstacleLayers, QueryTriggerInteraction.Ignore);
    }

    private void SetCenterDestinationIfNeeded(Vector3 heroPos)
    {
        if (arenaCenter == null)
        {
            _currentDestination = heroPos;
            return;
        }

        Vector3 centerPos = arenaCenter.position;
        centerPos.y = heroPos.y;
        float distToCenter = Vector3.Distance(heroPos, centerPos);
        if (distToCenter <= centerReturnDistance)
        {
            _currentDestination = heroPos;
            return;
        }

        _currentDestination = centerPos;
        _lastBestCandidate = centerPos;
    }

    private Vector3 FindFallbackDestination(Vector3 origin, Vector3 preferredDirection)
    {
        Vector3 best = origin;
        float bestScore = float.NegativeInfinity;

        for (int i = 0; i < fallbackDirections; i++)
        {
            float angle = (360f / fallbackDirections) * i;
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * preferredDirection.normalized;
            Vector3 candidate = origin + dir * fallbackStepDistance;
            candidate.y = origin.y;

            if (IsCandidateBlocked(candidate) || IsPathBlocked(origin, candidate))
            {
                continue;
            }

            float score = 0f;
            if (arenaCenter != null)
            {
                Vector3 centerPos = arenaCenter.position;
                centerPos.y = origin.y;
                score = -Vector3.Distance(candidate, centerPos);
            }

            if (score > bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        _lastBestCandidate = best;
        return best;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDebug)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, pickupDetectionRange);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(_lastBestCandidate, 0.3f);
        Gizmos.DrawLine(transform.position, _lastBestCandidate);

        if (_debugSelectedPickup != null)
        {
            Vector3 pickupPos = _debugSelectedPickup.transform.position;
            Gizmos.color = _debugPickupPathSafe ? Color.blue : new Color(1f, 0.5f, 0f);
            Gizmos.DrawWireSphere(pickupPos, 0.45f);
            Gizmos.DrawLine(transform.position, pickupPos);
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.5f, obstacleCheckRadius);

        if (arenaCenter != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(arenaCenter.position, 0.4f);
        }
    }

    private void OnGUI()
    {
        if (!drawDebug)
        {
            return;
        }

        GUI.color = Color.white;
        GUI.Label(new Rect(10f, 10f, 550f, 24f), $"Hero pickup decision: {_debugPickupDecision}");
    }

    private PickupItem ChoosePickupTarget(Vector3 heroPos, Transform[] threats)
    {
        PickupItem[] pickups = FindObjectsByType<PickupItem>(FindObjectsSortMode.None);
        if (pickups.Length == 0 || _heroStats == null)
        {
            _debugPickupDecision = "No pickups in scene";
            return null;
        }

        PickupItem best = null;
        float bestScore = float.NegativeInfinity;
        bool enemiesNearby = threats.Length > 0;

        for (int i = 0; i < pickups.Length; i++)
        {
            PickupItem pickup = pickups[i];
            if (pickup == null)
            {
                continue;
            }

            Vector3 pickupPos = pickup.transform.position;
            pickupPos.y = heroPos.y;
            float distToPickup = Vector3.Distance(heroPos, pickupPos);
            if (distToPickup > pickupDetectionRange)
            {
                continue;
            }

            float needScore = GetNeedScore(pickup.PickupType);
            bool criticalNeed = IsCriticalNeed(pickup.PickupType);
            bool safePath = !IsPathBlocked(heroPos, pickupPos) && !IsPathNearEnemy(heroPos, pickupPos, threats, safePickupEnemyDistance);

            if (enemiesNearby && !safePath && !criticalNeed)
            {
                continue;
            }

            float minEnemyDistAtPickup = GetMinDistanceToThreats(pickupPos, threats);
            float safetyScore = enemiesNearby ? minEnemyDistAtPickup : 5f;
            float pathSafetyBonus = safePath ? 10f : -15f;

            float score = (needScore * 2f) + safetyScore + pathSafetyBonus - (distToPickup * 0.35f);
            if (score > bestScore)
            {
                bestScore = score;
                best = pickup;
                _debugPickupPathSafe = safePath;
                _debugPickupDecision = criticalNeed
                    ? $"Critical {pickup.PickupType}"
                    : $"Best {pickup.PickupType}";
            }
        }

        if (best == null)
        {
            _debugPickupDecision = enemiesNearby
                ? "Threats too close to pickups"
                : "No valid pickup in range";
            _debugPickupPathSafe = false;
        }

        return best;
    }

    private float GetNeedScore(PickupType pickupType)
    {
        switch (pickupType)
        {
            case PickupType.Health:
                if (_heroStats.Health <= criticalHealthThreshold) return 100f;
                if (_heroStats.Health <= lowHealthThreshold) return 45f;
                return 6f;
            case PickupType.Ammo:
                if (_heroStats.Ammo <= criticalAmmoThreshold) return 95f;
                if (_heroStats.Ammo <= lowAmmoThreshold) return 40f;
                return 6f;
            case PickupType.Armor:
                if (_heroStats.Armor <= criticalArmorThreshold) return 80f;
                if (_heroStats.Armor <= lowArmorThreshold) return 35f;
                return 5f;
            case PickupType.DamageBuff:
                return _heroStats.BonusDamage >= 10 ? 4f : 24f;
            default:
                return 0f;
        }
    }

    private bool IsCriticalNeed(PickupType pickupType)
    {
        switch (pickupType)
        {
            case PickupType.Health:
                return _heroStats.Health <= criticalHealthThreshold;
            case PickupType.Ammo:
                return _heroStats.Ammo <= criticalAmmoThreshold;
            case PickupType.Armor:
                return _heroStats.Armor <= criticalArmorThreshold;
            default:
                return false;
        }
    }

    private float GetMinDistanceToThreats(Vector3 point, Transform[] threats)
    {
        if (threats == null || threats.Length == 0)
        {
            return 999f;
        }

        float minDist = float.MaxValue;
        for (int i = 0; i < threats.Length; i++)
        {
            if (threats[i] == null)
            {
                continue;
            }

            Vector3 threatPos = threats[i].position;
            threatPos.y = point.y;
            float dist = Vector3.Distance(point, threatPos);
            if (dist < minDist)
            {
                minDist = dist;
            }
        }

        return minDist;
    }

    private bool IsPathNearEnemy(Vector3 from, Vector3 to, Transform[] threats, float dangerDistance)
    {
        if (threats == null || threats.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < threats.Length; i++)
        {
            if (threats[i] == null)
            {
                continue;
            }

            Vector3 threatPos = threats[i].position;
            threatPos.y = from.y;
            float dist = DistancePointToSegment(threatPos, from, to);
            if (dist < dangerDistance)
            {
                return true;
            }
        }

        return false;
    }

    private float DistancePointToSegment(Vector3 point, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        float abLenSq = ab.sqrMagnitude;
        if (abLenSq <= 0.0001f)
        {
            return Vector3.Distance(point, a);
        }

        float t = Vector3.Dot(point - a, ab) / abLenSq;
        t = Mathf.Clamp01(t);
        Vector3 closest = a + ab * t;
        return Vector3.Distance(point, closest);
    }

    private Vector3 GetPickupApproachDestination(Vector3 heroPos, Vector3 pickupPos, Transform[] threats)
    {
        if (threats == null || threats.Length == 0)
        {
            return pickupPos;
        }

        Vector3 toPickup = pickupPos - heroPos;
        toPickup.y = 0f;
        if (toPickup.sqrMagnitude <= 0.001f)
        {
            return pickupPos;
        }

        Vector3 desiredDir = toPickup.normalized;
        Vector3 avoidForce = Vector3.zero;

        for (int i = 0; i < threats.Length; i++)
        {
            if (threats[i] == null)
            {
                continue;
            }

            Vector3 toEnemy = threats[i].position - heroPos;
            toEnemy.y = 0f;
            float dist = toEnemy.magnitude;
            if (dist <= 0.001f || dist > pickupAvoidanceRadius)
            {
                continue;
            }

            float weight = (pickupAvoidanceRadius - dist) / pickupAvoidanceRadius;
            avoidForce += (-toEnemy.normalized) * weight;
        }

        Vector3 steeredDir = desiredDir;
        if (avoidForce.sqrMagnitude > 0.0001f)
        {
            steeredDir = (desiredDir + avoidForce.normalized * pickupAvoidanceStrength).normalized;
        }

        float remainingDistance = toPickup.magnitude;
        float step = Mathf.Min(pickupApproachStep, remainingDistance);
        Vector3 candidate = heroPos + steeredDir * step;
        candidate.y = heroPos.y;

        // If the steered point is blocked, fall back to direct pickup destination.
        if (IsCandidateBlocked(candidate) || IsPathBlocked(heroPos, candidate))
        {
            return pickupPos;
        }

        return candidate;
    }

    private float GetCurrentMoveSpeed()
    {
        if (_heroStats == null)
        {
            return moveSpeed;
        }

        float t = Mathf.Clamp01(_heroStats.HealthPercent);
        return Mathf.Lerp(minSpeedAtZeroHealth, moveSpeed, t);
    }
}
