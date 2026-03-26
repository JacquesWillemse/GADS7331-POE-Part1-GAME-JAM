using UnityEngine;

public class HeroAIMovement : MonoBehaviour
{
    [Header("Threat Detection")]
    [SerializeField] private string enemyTag = "Enemy";
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private int maxEnemiesToConsider = 30;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float repathInterval = 0.25f;
    [SerializeField] private float arriveDistance = 0.2f;

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

    private void Start()
    {
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
        float moveDistance = moveSpeed * Time.deltaTime;
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

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(_lastBestCandidate, 0.3f);
        Gizmos.DrawLine(transform.position, _lastBestCandidate);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.5f, obstacleCheckRadius);

        if (arenaCenter != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(arenaCenter.position, 0.4f);
        }
    }
}
