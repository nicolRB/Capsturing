using UnityEngine;

public class ObjectSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("Prefab instantiated at each interval.")]
    [SerializeField] private GameObject objectToSpawn;
    [Tooltip("Time in seconds between spawned objects.")]
    [SerializeField] private float spawnInterval = 2f;
    [Tooltip("World-space position associated with the spawn configuration.")]
    [SerializeField] private Vector3 spawnPosition = new Vector3(0, 0, 0);

    void Update()
    {
        // Use the interval value as the next scheduled spawn time.
        if (Time.time >= spawnInterval)
        {
            SpawnObject();
            spawnInterval += 2f;
        }
    }

    void SpawnObject()
    {
        if (objectToSpawn != null)
        {
            Instantiate(
                objectToSpawn, 
                spawnPosition != null ? spawnPosition : transform.position, 
                Quaternion.identity);
        }
        else
        {
            Debug.LogWarning("Spawner: no object assigned to spawn.");
        }
    }
}
