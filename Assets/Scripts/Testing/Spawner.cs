using UnityEngine;

public class Spawner : MonoBehaviour
{
    public GameObject objectToSpawn; // The object that will be spawned
    public float spawnInterval = 2f; // Time in seconds between spawns
    public Vector3 spawnPosition = new Vector3(0, 0, 0); // Coordinates for spawning the object

    // Update is called once per frame
    void Update()
    {
        // Spawn objects at regular intervals
        if (Time.time >= spawnInterval)
        {
            SpawnObject();
            spawnInterval += 2f; // Schedule the next spawn
        }
    }

    void SpawnObject()
    {
        if (objectToSpawn != null)
        {
            Instantiate(objectToSpawn, transform.position, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning("No object assigned to spawn!");
        }
    }
}
