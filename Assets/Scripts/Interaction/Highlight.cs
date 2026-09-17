using UnityEngine;

public class Highlight : MonoBehaviour
{
    [Header("Highlight")]
    [SerializeField] private bool pointed = false;
    [SerializeField] private int originalLayer = -1;
    [SerializeField] private int highlightLayer = 3; // Layer number for the "Highlight" layer
    
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

    public void Toggle(bool state)
    {
        pointed = state;
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
