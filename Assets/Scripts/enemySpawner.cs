using UnityEngine;

public class enemySpawner : MonoBehaviour
{
    [SerializeField] GameObject[] enemyPrefabs;

    [SerializeField] float spawnRate;
    [SerializeField] float spawnDistance;
    [SerializeField] int maxEnemies;

    [SerializeField] float scaleInterval = 20.0f;
    [SerializeField] float spawnRateDecrease = 0.15f;
    [SerializeField] float minSpawnRate = 0.35f;
    [SerializeField] int maxEnemyIncrease = 5;

    float spawnTimer;
    float scaleTimer;

    float currentSpawnRate;
    int currentMaxEnemies;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentSpawnRate = spawnRate;                                        // stores starting spawn rate for scaling
        currentMaxEnemies = maxEnemies;                                      // stores starting enemy limit for scaling
    }

    // Update is called once per frame
    void Update()
    {
        if (gamemanager.instance != null && gamemanager.instance.isGameOver)
        {
            return;
        }

        spawnTimer += Time.deltaTime;
        scaleTimer += Time.deltaTime;

        if (scaleTimer >= scaleInterval)
        {
            scaleTimer = 0;
            scaleDifficulty();
        }

        if (spawnTimer >= currentSpawnRate && GameObject.FindGameObjectsWithTag("Enemy").Length < currentMaxEnemies)
        {
            spawnTimer = 0;
            spawnEnemy();
        }
    }

    void spawnEnemy()
    {
        if (enemyPrefabs.Length <= 0)
        {
            return;
        }

        if (gamemanager.instance == null || gamemanager.instance.player == null)
        {
            return;
        }

        int enemyIndex = Random.Range(0, enemyPrefabs.Length);               // randomly chooses basic, fast, or tank enemy

        Vector2 spawnDir = Random.insideUnitCircle.normalized;               // picks a random direction around the player
        Vector3 playerPos = gamemanager.instance.player.transform.position;
        Vector3 spawnPos = playerPos + new Vector3(spawnDir.x, 0, spawnDir.y) * spawnDistance;

        Instantiate(enemyPrefabs[enemyIndex], spawnPos, Quaternion.identity);
    }

    void scaleDifficulty()
    {
        currentSpawnRate -= spawnRateDecrease;
        currentSpawnRate = Mathf.Max(currentSpawnRate, minSpawnRate);        // prevents spawn rate from becoming too fast

        currentMaxEnemies += maxEnemyIncrease;

        Debug.Log("Enemy scaling increased. Spawn Rate: " + currentSpawnRate + " Max Enemies: " + currentMaxEnemies);
    }
}