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
    [SerializeField] private bool autoStart = true;

    private bool _isSpawning;

    private void Start()
    {
        if (autoStart)
        {
            StartSpawning();
        }
    }

    private void Update()
    {
        if (!_isSpawning)
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

        float interval = Random.Range(minSpawnInterval, maxSpawnInterval);
        _spawnTimer -= Time.deltaTime;
        if (_spawnTimer > 0f)
        {
            return;
        }

        SpawnOneEnemy();
        _spawnTimer = interval;
    }

    private float _spawnTimer;

    public void StartSpawning()
    {
        _isSpawning = true;
        _spawnTimer = 0f;
    }

    public void StopSpawning()
    {
        _isSpawning = false;
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
}
