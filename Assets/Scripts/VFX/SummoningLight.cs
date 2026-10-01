using System;
using UnityEngine;

public class SummonLight : MonoBehaviour
{
    [Header("Flash Values")]
    [SerializeField] private float maxIntensity = 10;

    [Header("Timing")]
    [SerializeField] private float flashInDuration = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.2f;
    private float totalDuration;
    private float elapsed = 0;

    public float TotalDuration => totalDuration;
    public float FlashInDuration => flashInDuration;

    [Header("References")]
    [SerializeField] private Renderer lightRenderer;

    [Header("Shader Property Names")]
    [SerializeField] private string intensityProperty = "_Intensity";

    private MaterialPropertyBlock block;

    private int intensityPropId;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Initialize MaterialPropertyBlock and property ID for optimized GPU instancing/rendering
        block = new MaterialPropertyBlock();
        intensityPropId = Shader.PropertyToID(intensityProperty);
        
        elapsed = 0f;
        totalDuration = flashInDuration + fadeOutDuration;
    }

    // Update is called once per frame
    void Update()
    {
        elapsed += Time.deltaTime;
        FlashAnimation();
        if (elapsed >= totalDuration && totalDuration != 0)
            Destroy(gameObject);
    }

    public void Setup(float width, float height, float inDuration, float outDuration)
    {
        transform.localScale = new Vector3(width, height, width);
        flashInDuration = inDuration;
        fadeOutDuration = outDuration;
        totalDuration = inDuration + outDuration;
    }

    public void SetSize(float width, float height)
    {
        transform.localScale = new Vector3(width, height, width);
    }
    
    // Handles the intensity curve during the flash-in and fade-out phases
    void FlashAnimation()
    {
        float intensityValue = 0f;

        if (elapsed < flashInDuration)
        {
            // Flash-in phase: smoothly interpolates from 0 up to maxIntensity
            float t = Mathf.Clamp01(elapsed / flashInDuration);
            intensityValue = Mathf.Lerp(0f, maxIntensity, t);
        }
        else
        {
            // Fade-out phase: smoothly interpolates from maxIntensity down to 0
            float fadeElapsed = elapsed - flashInDuration;
            float t = Mathf.Clamp01(fadeElapsed / fadeOutDuration);
            intensityValue = Mathf.Lerp(maxIntensity, 0f, t);
        }

        // Apply the calculated intensity value safely using MaterialPropertyBlock
        ApplyIntensity(lightRenderer, intensityPropId, intensityValue);
    }

    // Helper method to update shader properties without creating material materialization leaks
    private void ApplyIntensity(Renderer target, int propertyId, float value)
    {
        if (target == null) return;

        target.GetPropertyBlock(block);
        block.SetFloat(propertyId, value);target.SetPropertyBlock(block);
    }
}
