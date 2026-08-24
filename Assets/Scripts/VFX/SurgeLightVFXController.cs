using System.Collections.Generic;
using UnityEngine;

public class CaptureBindVFXController : MonoBehaviour
{
    [Header("References")]
    public Renderer manaBallRenderer;
    public Renderer lightCenterRenderer;
    public Renderer manaChainRenderer;

    [Header("Timing (Inspector-tunable)")]
    public float sphereStart = 0f;
    public float sphereDuration = 0.3f;

    public float lightStart = 0.15f;
    public float lightDuration = 0.3f;

    public float chainStart = 0.25f;
    public float chainDuration = 0.5f;

    [Header("Shader Property Names")]
    public string sphereProgressProperty = "_Progress";
    public string lightProgressProperty = "_Progress";
    public string chainProgressProperty = "_Progress";

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

        // Normaliza o tempo de 0 a 1
        float t = Mathf.Clamp01((currentElapsed - startTime) / duration);

        // Mapeia o progresso de -0.01f até 1.0f
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