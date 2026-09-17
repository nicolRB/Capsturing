using UnityEngine;

public class UIObjectSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("Prefab instantiated at each interval.")]
    [SerializeField] private GameObject objectToSpawn;
    [Tooltip("Time in seconds between spawned objects.")]
    [SerializeField] private float spawnInterval = 2f;
    [Tooltip("Anchored position used by spawned UI objects.")]
    [SerializeField] private Vector2 spawnPosition = new Vector2(0, 0);

    // Time at which the next object may be spawned.
    private float nextSpawnTime = 0f;

    void Update()
    {
        if (Time.time >= nextSpawnTime)
        {
            SpawnObject();
            nextSpawnTime = Time.time + spawnInterval;
        }
    }

    void SpawnObject()
    {
        if (objectToSpawn != null)
        {
            GameObject obj = Instantiate(objectToSpawn, transform);

            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchoredPosition = spawnPosition;
        }
    }
}