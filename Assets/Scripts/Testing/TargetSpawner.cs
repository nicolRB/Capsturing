using UnityEngine;

public class TargetSpawner : MonoBehaviour
{
    [Header("Target Definition")]
    public GameObject target;
    public int nextTargetIndex = 0;

    [Header("Target Properties")]
    public float targetSize = 1f;
    public float targetLifetime = 2f;
    public float targetPerfectWindow = 0.08f;
    public float targetHitWindow = 0.2f;
    public float targetActivationTime = 0.5f;
    public float targetFadeInDuration = 0.5f;
    public float targetMissDuration = 0.2f;

    [Header("Target Property Variance")]
    public float targetSizeVariance = 0.2f;
    public float targetLifetimeVariance = 0.5f;

    [Header("Spawn Area")]
    public Vector2 spawnPoint = new Vector2(0, 0); // Center of the spawn area
    public Vector2 spawnRange = new Vector2(100, 100); // Width and height of the spawn area

    [Header("Spawn Timing")]
    public float spawnInterval = 2f; // Time between spawns
    public float spawnIntervalVariance = 0.5f; // Random variance for spawn interval
    private float nextSpawnTime = 0f;

    // Update is called once per frame
    void Update()
    {
        if (Time.time >= nextSpawnTime)
        {
            SpawnObject();
            nextSpawnTime = Time.time + spawnInterval + Random.Range(-spawnIntervalVariance, spawnIntervalVariance);
        }
    }

    public void SpawnObject()
    {
        if (target != null)
        {
            GameObject obj = Instantiate(target, transform);

            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(
                spawnPoint.x + Random.Range(-spawnRange.x / 2, spawnRange.x / 2),
                spawnPoint.y + Random.Range(-spawnRange.y / 2, spawnRange.y / 2)
            );

            // 👇 Get the ClickableCircle script
            ClickableCircle circle = obj.GetComponent<ClickableCircle>();

            if (circle != null)
            {
                // Apply base properties
                circle.size = targetSize + Random.Range(-targetSizeVariance, targetSizeVariance);
                circle.lifetime = targetLifetime + Random.Range(-targetLifetimeVariance, targetLifetimeVariance);
                circle.perfectWindow = targetPerfectWindow;
                circle.hitWindow = targetHitWindow;
                circle.activationTime = targetActivationTime;
                circle.fadeInDuration = targetFadeInDuration;
                circle.missDuration = targetMissDuration;

                // Optional: assign index for sequencing
                circle.targetIndex = nextTargetIndex;
                nextTargetIndex++;
            }

            // Scale AFTER size is set (important)
            rect.localScale = Vector3.one * (circle != null ? circle.size : targetSize);
        }
    }
}
