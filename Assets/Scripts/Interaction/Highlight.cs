using System.Collections.Generic;
using UnityEngine;

public class Highlight : MonoBehaviour
{
    private static readonly HashSet<Highlight> activeHighlights = new HashSet<Highlight>();

    [Header("Highlight")]
    [SerializeField] private bool pointed = false;
    [SerializeField] private int originalLayer = -1;
    [SerializeField] private int highlightLayer = 3; // Layer number for the "Highlight" layer

    public int HighlightLayer => highlightLayer;
    public static int ActiveHighlightLayerMask
    {
        get
        {
            int layerMask = 0;
            foreach (Highlight highlight in activeHighlights)
            {
                if (highlight != null && highlight.highlightLayer >= 0 && highlight.highlightLayer < 32)
                    layerMask |= 1 << highlight.highlightLayer;
            }
            return layerMask;
        }
    }

    void OnEnable()
    {
        activeHighlights.Add(this);
    }

    void OnDisable()
    {
        activeHighlights.Remove(this);
    }

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
