using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform heroTarget;
    [SerializeField] private Transform[] spawnPoints = new Transform[4];

    [Header("Spawn Timing")]
    [SerializeField] private float minSpawnInterval = 0.5f;
    [SerializeField] private float maxSpawnInterval = 1.5f;
    [SerializeField] private int maxAliveEnemies = 50;

    private bool _isSpawningRound;
    private int _remainingToSpawn;
    private float _spawnTimer;

    private void Update()
    {
        if (!_isSpawningRound)
        {
            return;
        }

        if (enemyPrefab == null || !HasValidSpawnPoint())
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

        SpawnOneEnemy();
        _remainingToSpawn--;
        _spawnTimer = Random.Range(minSpawnInterval, maxSpawnInterval);

        if (_remainingToSpawn <= 0)
        {
            _isSpawningRound = false;
        }
    }

    private void SpawnOneEnemy()
    {
        Transform spawnPoint = GetRandomSpawnPoint();
        if (spawnPoint == null)
        {
            return;
        }

        GameObject enemy = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);

        EnemyMover mover = enemy.GetComponent<EnemyMover>();
        if (mover != null && heroTarget != null)
        {
            mover.SetTarget(heroTarget);
        }
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
        if (enemyCount <= 0 || enemyPrefab == null || !HasValidSpawnPoint())
        {
            return false;
        }

        _remainingToSpawn = enemyCount;
        _spawnTimer = 0f;
        _isSpawningRound = true;
        return true;
    }

    public bool IsRoundComplete()
    {
        return !_isSpawningRound && _remainingToSpawn <= 0 && CountAliveEnemies() == 0;
    }
}
