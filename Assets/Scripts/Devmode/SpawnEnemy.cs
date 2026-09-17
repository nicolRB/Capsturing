using UnityEngine;

public class SpawnEnemy : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("Prefab used to create a new enemy.")]
    [SerializeField] private GameObject enemyPrefab;
    [Tooltip("Previously spawned enemy that may be removed before spawning.")]
    [SerializeField] private GameObject originalEnemy;
    [Tooltip("Spawn an enemy automatically when the scene starts.")]
    [SerializeField] private bool spawnOnStart = false;
    [Tooltip("Remove the previous enemy before spawning a new one.")]
    [SerializeField] private bool deletePrevious = false;
    [Tooltip("Transform that defines the enemy's spawn position and rotation.")]
    [SerializeField] private Transform spawnPoint;
    [Tooltip("Optional holder updated with the newly spawned enemy.")]
    [SerializeField] private EnemyTargetHolder targetHolder;

    void Start()
    {
        if (spawnOnStart)
        {
            Spawn();
        }
    }

    public void Spawn()
    {
        Debug.Log("SpawnEnemy: spawning enemy at " + spawnPoint.position);
        if (enemyPrefab != null)
        {
            if (deletePrevious && originalEnemy != null) Destroy(originalEnemy);

            GameObject newEnemy = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);

            if (targetHolder != null) targetHolder.SetTarget(newEnemy.GetComponent<Runic>());

            if (deletePrevious) originalEnemy = newEnemy;
        }
        else
        {
            Debug.LogWarning("SpawnEnemy: enemyPrefab is not assigned.");
        }
    }
}
