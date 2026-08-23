using UnityEngine;

public class SpawnEnemy : MonoBehaviour
{
    public GameObject enemyPrefab;
    public GameObject originalEnemy;
    public bool spawnOnStart = false;
    public bool deletePrevious = false;
    public Transform spawnPoint;
    public EnemyTargetHolder targetHolder;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (spawnOnStart)
        {
            Spawn();
        }
    }

    public void Spawn()
    {
        Debug.Log("SpawnEnemy: Spawning enemy at " + spawnPoint.position);
        if (enemyPrefab != null)
        {
            if (deletePrevious && originalEnemy != null) Destroy(originalEnemy);

            GameObject newEnemy = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);

            if (targetHolder != null) targetHolder.SetTarget(newEnemy.GetComponent<EnemyScript>());

            if (deletePrevious) originalEnemy = newEnemy;
        }
        else
        {
            Debug.LogWarning("SpawnEnemy: enemyPrefab is not assigned.");
        }
    }
}
