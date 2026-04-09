using UnityEngine;
using System.Collections.Generic;

public class TargetData
{
    public float spawnTime;
    public Vector2 position;
}

public class TargetMapPlayer : MonoBehaviour
{
    public GameObject targetPrefab;
    public RectTransform canvas;

    public List<TargetData> map;

    private float startTime;
    private int currentIndex = 0;

    void Start()
    {
        startTime = Time.time;
    }

    void Update()
    {
        float elapsed = Time.time - startTime;

        while (currentIndex < map.Count && elapsed >= map[currentIndex].spawnTime)
        {
            SpawnTarget(map[currentIndex]);
            currentIndex++;
        }
    }

    void SpawnTarget(TargetData data)
    {
        GameObject obj = Instantiate(targetPrefab, canvas);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchoredPosition = data.position;
    }
}