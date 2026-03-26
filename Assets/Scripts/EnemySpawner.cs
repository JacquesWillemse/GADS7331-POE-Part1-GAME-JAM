using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private GameObject[] enemyPrefabs;
    [SerializeField] private Transform heroTarget;
    [SerializeField] private Transform[] spawnPoints = new Transform[4];

    [Header("Spawn Timing")]
    [SerializeField] private float minSpawnInterval = 0.5f;
    [SerializeField] private float maxSpawnInterval = 1.5f;
    [SerializeField] private int maxAliveEnemies = 50;

    [Header("Clump Spawning")]
    [SerializeField] private bool enableClumpSpawns = true;
    [SerializeField, Range(0f, 1f)] private float clumpSpawnChance = 0.25f;
    [SerializeField] private int clumpMinSize = 3;
    [SerializeField] private int clumpMaxSize = 5;
    [SerializeField] private float clumpRadius = 2f;

    [Header("Grounding")]
    [SerializeField] private LayerMask groundLayers = ~0;
    [SerializeField] private float groundRayStartHeight = 25f;
    [SerializeField] private float groundRayDistance = 200f;
    [SerializeField] private float spawnVerticalOffset = 0f;
    [SerializeField] private float[] spawnVerticalOffsetByType = new float[3];

    private bool _isSpawningRound;
    private int _remainingToSpawn;
    private int[] _remainingByType;
    private float _spawnTimer;

    private void Update()
    {
        if (!_isSpawningRound)
        {
            return;
        }

        if (!HasValidEnemyPrefab() || !HasValidSpawnPoint())
        {
            return;
        }

        if (maxAliveEnemies > 0 && CountAliveEnemies() >= maxAliveEnemies)
        {
            return;
        }

        _spawnTimer -= Time.deltaTime;
        if (_spawnTimer > 0f)
        {
            return;
        }

        int spawnedCount = SpawnTick();
        if (spawnedCount <= 0)
        {
            _spawnTimer = 0.1f;
            return;
        }

        _remainingToSpawn -= spawnedCount;
        _spawnTimer = Random.Range(minSpawnInterval, maxSpawnInterval);

        if (_remainingToSpawn <= 0)
        {
            _isSpawningRound = false;
        }
    }

    private int SpawnTick()
    {
        bool canClump = enableClumpSpawns
            && _remainingToSpawn >= clumpMinSize
            && Random.value <= clumpSpawnChance;

        if (canClump)
        {
            return SpawnClump();
        }

        return SpawnSingle();
    }

    private int SpawnSingle()
    {
        Transform spawnPoint = GetRandomSpawnPoint();
        if (spawnPoint == null)
        {
            return 0;
        }

        return SpawnEnemyAt(spawnPoint.position, spawnPoint.rotation) ? 1 : 0;
    }

    private int SpawnClump()
    {
        Transform spawnPoint = GetFurthestSpawnPointFromHero();
        if (spawnPoint == null)
        {
            return SpawnSingle();
        }

        int desiredSize = Random.Range(clumpMinSize, clumpMaxSize + 1);
        desiredSize = Mathf.Min(desiredSize, _remainingToSpawn);

        if (maxAliveEnemies > 0)
        {
            int room = Mathf.Max(0, maxAliveEnemies - CountAliveEnemies());
            desiredSize = Mathf.Min(desiredSize, room);
        }

        if (desiredSize <= 0)
        {
            return 0;
        }

        int spawned = 0;
        for (int i = 0; i < desiredSize; i++)
        {
            Vector2 offset2D = Random.insideUnitCircle * clumpRadius;
            Vector3 spawnPos = spawnPoint.position + new Vector3(offset2D.x, 0f, offset2D.y);
            if (SpawnEnemyAt(spawnPos, spawnPoint.rotation))
            {
                spawned++;
            }
        }

        return spawned;
    }

    private bool SpawnEnemyAt(Vector3 position, Quaternion rotation)
    {
        int typeIndex;
        GameObject prefab = GetRandomEnemyPrefabForCurrentRound(out typeIndex);
        if (prefab == null)
        {
            return false;
        }

        GameObject enemy = Instantiate(prefab, position, rotation);
        GroundSpawnedEnemy(enemy, position, typeIndex);

        EnemyMover mover = enemy.GetComponent<EnemyMover>();
        if (mover != null && heroTarget != null)
        {
            mover.SetTarget(heroTarget);
        }

        return true;
    }

    private void GroundSpawnedEnemy(GameObject enemy, Vector3 requestedPosition, int typeIndex)
    {
        if (enemy == null)
        {
            return;
        }

        Vector3 rayStart = requestedPosition + Vector3.up * groundRayStartHeight;
        if (!Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, groundRayDistance, groundLayers, QueryTriggerInteraction.Ignore))
        {
            return;
        }

        // First place near ground, then align the actual lowest bound to ground height.
        float typeOffset = GetTypeVerticalOffset(typeIndex);
        float totalOffset = spawnVerticalOffset + typeOffset;
        Vector3 groundedPos = hit.point + Vector3.up * totalOffset;
        enemy.transform.position = groundedPos;

        float lowestY = GetLowestWorldY(enemy);
        if (!float.IsNaN(lowestY))
        {
            float desiredMinY = hit.point.y + totalOffset;
            float correction = desiredMinY - lowestY;
            enemy.transform.position += Vector3.up * correction;
        }
    }

    private float GetTypeVerticalOffset(int typeIndex)
    {
        if (spawnVerticalOffsetByType == null || typeIndex < 0 || typeIndex >= spawnVerticalOffsetByType.Length)
        {
            return 0f;
        }

        return spawnVerticalOffsetByType[typeIndex];
    }

    private float GetLowestWorldY(GameObject enemy)
    {
        Collider[] colliders = enemy.GetComponentsInChildren<Collider>(true);
        if (colliders != null && colliders.Length > 0)
        {
            float minY = float.MaxValue;
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] == null || !colliders[i].enabled)
                {
                    continue;
                }

                float colliderMinY = colliders[i].bounds.min.y;
                if (colliderMinY < minY)
                {
                    minY = colliderMinY;
                }
            }

            if (minY != float.MaxValue)
            {
                return minY;
            }
        }

        // Fallback to renderer bounds if colliders are missing/misaligned.
        Renderer[] renderers = enemy.GetComponentsInChildren<Renderer>(true);
        if (renderers != null && renderers.Length > 0)
        {
            float minY = float.MaxValue;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null || !renderers[i].enabled)
                {
                    continue;
                }

                float rendererMinY = renderers[i].bounds.min.y;
                if (rendererMinY < minY)
                {
                    minY = rendererMinY;
                }
            }

            if (minY != float.MaxValue)
            {
                return minY;
            }
        }

        return float.NaN;
    }

    private Transform GetRandomSpawnPoint()
    {
        int validCount = 0;
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] != null)
            {
                validCount++;
            }
        }

        if (validCount == 0)
        {
            return null;
        }

        int pick = Random.Range(0, validCount);
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] == null)
            {
                continue;
            }

            if (pick == 0)
            {
                return spawnPoints[i];
            }

            pick--;
        }

        return null;
    }

    private Transform GetFurthestSpawnPointFromHero()
    {
        if (heroTarget == null)
        {
            return GetRandomSpawnPoint();
        }

        Transform furthest = null;
        float furthestSqrDist = float.MinValue;
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            Transform point = spawnPoints[i];
            if (point == null)
            {
                continue;
            }

            Vector3 delta = point.position - heroTarget.position;
            delta.y = 0f;
            float sqrDist = delta.sqrMagnitude;
            if (sqrDist > furthestSqrDist)
            {
                furthestSqrDist = sqrDist;
                furthest = point;
            }
        }

        return furthest;
    }

    private bool HasValidSpawnPoint()
    {
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] != null)
            {
                return true;
            }
        }

        return false;
    }

    private int CountAliveEnemies()
    {
        return GameObject.FindGameObjectsWithTag("Enemy").Length;
    }

    public bool TryStartRound(int enemyCount)
    {
        int[] singleTypeCounts = new int[1];
        singleTypeCounts[0] = enemyCount;
        return TryStartRound(singleTypeCounts);
    }

    public bool TryStartRound(int[] enemyCountsByType)
    {
        if (enemyCountsByType == null || enemyCountsByType.Length == 0 || !HasValidEnemyPrefab() || !HasValidSpawnPoint())
        {
            return false;
        }

        _remainingByType = new int[Mathf.Max(enemyCountsByType.Length, 1)];
        _remainingToSpawn = 0;
        for (int i = 0; i < enemyCountsByType.Length; i++)
        {
            _remainingByType[i] = Mathf.Max(0, enemyCountsByType[i]);
            _remainingToSpawn += _remainingByType[i];
        }

        if (_remainingToSpawn <= 0)
        {
            return false;
        }

        _spawnTimer = 0f;
        _isSpawningRound = true;
        return true;
    }

    public bool IsRoundComplete()
    {
        return !_isSpawningRound && _remainingToSpawn <= 0 && CountAliveEnemies() == 0;
    }

    private bool HasValidEnemyPrefab()
    {
        if (enemyPrefabs != null)
        {
            for (int i = 0; i < enemyPrefabs.Length; i++)
            {
                if (enemyPrefabs[i] != null)
                {
                    return true;
                }
            }
        }

        return enemyPrefab != null;
    }

    private GameObject GetRandomEnemyPrefab()
    {
        int validCount = 0;
        if (enemyPrefabs != null)
        {
            for (int i = 0; i < enemyPrefabs.Length; i++)
            {
                if (enemyPrefabs[i] != null)
                {
                    validCount++;
                }
            }
        }

        if (validCount == 0)
        {
            return enemyPrefab;
        }

        int pick = Random.Range(0, validCount);
        for (int i = 0; i < enemyPrefabs.Length; i++)
        {
            if (enemyPrefabs[i] == null)
            {
                continue;
            }

            if (pick == 0)
            {
                return enemyPrefabs[i];
            }

            pick--;
        }

        return enemyPrefab;
    }

    private GameObject GetEnemyPrefabByTypeIndex(int typeIndex)
    {
        if (typeIndex == 0)
        {
            if (enemyPrefabs != null && enemyPrefabs.Length > 0 && enemyPrefabs[0] != null)
            {
                return enemyPrefabs[0];
            }

            return enemyPrefab;
        }

        if (enemyPrefabs != null && typeIndex >= 0 && typeIndex < enemyPrefabs.Length)
        {
            return enemyPrefabs[typeIndex];
        }

        return null;
    }

    private GameObject GetRandomEnemyPrefabForCurrentRound(out int selectedTypeIndex)
    {
        selectedTypeIndex = -1;
        if (_remainingByType == null || _remainingByType.Length == 0)
        {
            return GetRandomEnemyPrefab();
        }

        int eligibleTypes = 0;
        for (int i = 0; i < _remainingByType.Length; i++)
        {
            if (_remainingByType[i] > 0 && GetEnemyPrefabByTypeIndex(i) != null)
            {
                eligibleTypes++;
            }
        }

        if (eligibleTypes == 0)
        {
            return GetRandomEnemyPrefab();
        }

        int pick = Random.Range(0, eligibleTypes);
        for (int i = 0; i < _remainingByType.Length; i++)
        {
            if (_remainingByType[i] <= 0 || GetEnemyPrefabByTypeIndex(i) == null)
            {
                continue;
            }

            if (pick == 0)
            {
                _remainingByType[i]--;
                selectedTypeIndex = i;
                return GetEnemyPrefabByTypeIndex(i);
            }

            pick--;
        }

        return GetRandomEnemyPrefab();
    }
}
