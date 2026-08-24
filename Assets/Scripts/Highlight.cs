using UnityEngine;

public class Highlight : MonoBehaviour
{
    public bool pointed = false;
    public int originalLayer = -1;
    public int highlightLayer = 3; // Layer number for the "Highlight" layer
    
    void Start()
    {
        originalLayer = gameObject.layer;
    }

    void Update()
    {
        int targetLayer = pointed ? highlightLayer : originalLayer;

        if (gameObject.layer != targetLayer)
        {
            SetLayerRecursively(gameObject, targetLayer);
        }
    }

    void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
}
