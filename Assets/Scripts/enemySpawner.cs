using UnityEngine;

public class enemySpawner : MonoBehaviour
{
    [SerializeField] GameObject[] enemyPrefabs;

    [SerializeField] float spawnRate;
    [SerializeField] float spawnDistance;
    [SerializeField] int maxEnemies;

    float spawnTimer;

    // Update is called once per frame
    void Update()
    {
        if (gamemanager.instance != null && gamemanager.instance.isGameOver)
        {
            return;
        }

        spawnTimer += Time.deltaTime;

        if (spawnTimer >= spawnRate && GameObject.FindGameObjectsWithTag("Enemy").Length < maxEnemies)
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
}