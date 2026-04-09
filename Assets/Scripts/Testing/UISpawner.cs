using UnityEngine;

public class UISpawner : MonoBehaviour
{
    public GameObject objectToSpawn;
    public float spawnInterval = 2f;
    public Vector2 spawnPosition = new Vector2(0, 0);

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
            rect.anchoredPosition = spawnPosition; // X, Y in UI space
        }
    }
}