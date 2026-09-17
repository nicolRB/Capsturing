using System.Collections.Generic;
using UnityEngine;

public class CaptureBindVFXController : MonoBehaviour
{
    [Header("ReferCaptureBindVFXControllerences")]
    [SerializeField] private Renderer manaBallRenderer;
    [SerializeField] private Renderer lightCenterRenderer;
    [SerializeField] private Renderer manaChainRenderer;

    [Header("Timing (Inspector-tunable)")]
    [SerializeField] private float sphereStart = 0f;
    [SerializeField] private float sphereDuration = 0.3f;

    [SerializeField] private float lightStart = 0.15f;
    [SerializeField] private float lightDuration = 0.3f;

    [SerializeField] private float chainStart = 0.25f;
    [SerializeField] private float chainDuration = 0.5f;

    [Header("Shader Property Names")]
    [SerializeField] private string sphereProgressProperty = "_Progress";
    [SerializeField] private string lightProgressProperty = "_Progress";
    [SerializeField] private string chainProgressProperty = "_Progress";

    private float elapsed = 0f;
    private MaterialPropertyBlock block;

    private int spherePropId;
    private int lightPropId;
    private int chainPropId;

    void Start()
    {
        block = new MaterialPropertyBlock();

        spherePropId = Shader.PropertyToID(sphereProgressProperty);
        lightPropId = Shader.PropertyToID(lightProgressProperty);
        chainPropId = Shader.PropertyToID(chainProgressProperty);

        elapsed = 0f;
    }

    void Update()
    {
        elapsed += Time.deltaTime;

        ApplyProgress(manaBallRenderer, spherePropId,
            ComputeProgress(elapsed, sphereStart, sphereDuration));

        ApplyProgress(lightCenterRenderer, lightPropId,
            ComputeProgress(elapsed, lightStart, lightDuration));

        ApplyProgress(manaChainRenderer, chainPropId,
            ComputeProgress(elapsed, chainStart, chainDuration));
    }

    private float ComputeProgress(float currentElapsed, float startTime, float duration)
    {
        if (duration <= 0f)
            return currentElapsed >= startTime ? 1f : -0.01f;

        // Normalizes the time from 0 to 1
        float t = Mathf.Clamp01((currentElapsed - startTime) / duration);

        // Maps the progress from -0.01f to 1.0f
        return Mathf.Lerp(-0.01f, 1f, t);
    }

    private void ApplyProgress(Renderer target, int propertyId, float value)
    {
        if (target == null) return;

        target.GetPropertyBlock(block);
        block.SetFloat(propertyId, value);
        target.SetPropertyBlock(block);
    }
}