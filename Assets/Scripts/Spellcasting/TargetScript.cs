using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public class TargetScript : MonoBehaviour, IPointerClickHandler
{
    [Header("Settings")]
    public float size = 1f;
    public float lifetime = 2f;
    public float perfectWindow = 0.1f;
    public float hitWindow = 0.2f;

    [Header("Activation")]
    public float activationTime = 0.5f;
    public float fadeInDuration = 0.5f;

    [Header("Miss Settings")]
    public float missDuration = 0.2f;

    [Header("References")]
    public int targetIndex;
    public Image targetRing;
    public Transform targetRingTransform;
    public Image timingRing;
    private ChannelingGameScript cast;
    private FeedBackUI feedBack;
    public Image rootImage;

    private const float InactiveTargetAlpha = 0.35f;
    private static readonly Color InactiveTargetTint = new Color(0.7f, 0.7f, 0.7f, 1f);

    private float spawnTime;
    private bool clicked = false;
    private bool missed = false;
    private float missStartTime;

    private Vector3 timingStartScale;
    private Vector3 timingEndScale;
    private Color targetBaseColor;

    private bool IsCurrentTarget => cast != null && cast.currentTargetIndex == targetIndex;

    public enum HitResult
    {
        Perfect,
        Good,
        Miss
    }

    void Start()
    {
        cast = FindFirstObjectByType<ChannelingGameScript>();
        feedBack = FindFirstObjectByType<FeedBackUI>();

        if (targetRing == null || timingRing == null)
        {
            Debug.LogError("TargetScript requires targetRing and timingRing references.", this);
            enabled = false;
            return;
        }

        spawnTime = Time.time;

        transform.localScale = Vector3.one * size;

        targetRingTransform = targetRing.transform;

        // Cache the target base color and start hidden
        targetBaseColor = targetRing.color;
        targetRingTransform.localScale = Vector3.one * 0.8f;
        Color targetColor = targetBaseColor;
        targetColor.a = 0f;
        targetRing.color = targetColor;

        // Timing ring setup
        timingStartScale = Vector3.one * 2.5f;
        // timingEndScale = Vector3.one;
        timingEndScale = timingRing.transform.localScale;

        timingRing.transform.localScale = timingStartScale;

        Color c = timingRing.color;
        c.a = 0f;
        timingRing.color = c;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (clicked || missed) return;

        clicked = true;

        if (cast == null || cast.currentTargetIndex != targetIndex)
            return;

        RectTransform rect = transform as RectTransform;

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rect,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint
        );

        // Primeiro garante que está dentro do retângulo do UI
        if (!RectTransformUtility.RectangleContainsScreenPoint(
            rect, eventData.position, eventData.pressEventCamera))
            return;

        // Depois filtra para círculo
        float radius = rect.rect.width * 0.5f;

        if (localPoint.magnitude > radius)
            return;

        float timeSinceSpawn = Time.time - spawnTime;
        float timeAfterActivation = timeSinceSpawn - activationTime;

        HitResult result;

        if (timeAfterActivation < 0f)
        {
            result = HitResult.Miss;
            StartCoroutine(MissEffect());
            if (cast != null) cast.RegisterMiss();
        }
        else
        {
            float diff = Mathf.Abs(timeAfterActivation - lifetime);

            if (diff <= perfectWindow)
            {
                result = HitResult.Perfect;
                StartCoroutine(HitEffect());
                if (cast != null) cast.RegisterHit(true);
            }
            else if (diff <= hitWindow)
            {
                result = HitResult.Good;
                StartCoroutine(HitEffect());
                if (cast != null) cast.RegisterHit(false);
            }
            else
            {
                result = HitResult.Miss;
                StartCoroutine(MissEffect());
                if (cast != null) cast.RegisterMiss();
            }
        }

        feedBack.Show(result, transform.position);
    }

    void Update()
    {
        float timeSinceSpawn = Time.time - spawnTime; // Total time since this circle was created
        float activationStartTime = activationTime - fadeInDuration; // When activation (fade-in) starts
        float timeAfterActivation = Mathf.Max(0f, timeSinceSpawn - activationTime); // Lifetime counting starts after activation
        float gameplayTime = timeAfterActivation; // Time since activation started

        // -------- ACTIVATION --------
        float tActivation = Mathf.Clamp01(
            (timeSinceSpawn - activationStartTime) / fadeInDuration
        );

        if (tActivation > 0f)
        {
            bool isCurrentTarget = IsCurrentTarget;

            // Only current target can receive clicks
            if (rootImage != null)
                rootImage.raycastTarget = isCurrentTarget;

            // Fade in target; inactive targets stay dimmer and more transparent
            float alpha = tActivation * (isCurrentTarget ? 1f : InactiveTargetAlpha);
            Color targetColor = targetBaseColor;
            if (!isCurrentTarget)
            {
                targetColor *= InactiveTargetTint;
                timingRing.color = targetColor; // Inherit the dimmed color for timing ring as well
            }
            targetColor.a = alpha;
            targetRing.color = targetColor;

            // Scale target
            float scaleT = Mathf.SmoothStep(0f, 1f, tActivation);
            targetRingTransform.localScale =
                Vector3.Lerp(Vector3.one * 0.8f, Vector3.one, scaleT);

            if (isCurrentTarget)
                transform.SetAsLastSibling();
        }

        float elapsed = timeAfterActivation;
        float timeToPerfect = lifetime;

        if (timeToPerfect <= 0f)
        {
            timeToPerfect = 0.001f;
        }

        // -------- ROTATION (starts at fade-in start, accelerates to full speed by fade-in end)
        float baseSpeed = 540f / timeToPerfect;
        float minSpeed = baseSpeed * 0.15f;

        float rotationAngle = 0f;

        float fadeElapsed = Mathf.Clamp(timeSinceSpawn - activationStartTime, 0f, fadeInDuration);
        float postFadeElapsed = Mathf.Max(0f, timeSinceSpawn - activationTime);

        // angle accumulated during fade-in with linear speed ramp
        float tFade = fadeInDuration > 0f ? fadeElapsed / fadeInDuration : 1f;
        float currentSpeedDuringFade = Mathf.Lerp(minSpeed, baseSpeed, tFade);
        float fadeAngle = minSpeed * fadeElapsed + (currentSpeedDuringFade - minSpeed) * fadeElapsed * 0.5f;

        rotationAngle = fadeAngle + baseSpeed * postFadeElapsed;

        targetRingTransform.rotation = Quaternion.Euler(0, 0, rotationAngle);

        // -------- TIMING RING --------
        float adjustedElapsed = timeAfterActivation;
        float adjustedLifetime = lifetime;

        // Normalized time for timing ring (0 to 1 during active phase)
        float t = adjustedLifetime > 0f ? adjustedElapsed / adjustedLifetime : 1f;

        if (tActivation > 0f) // Only show timing ring after activation starts
        {
            /*
            if (t <= 1f)
            {
                float curvedT = Mathf.SmoothStep(0f, 1f, t); // Ease out

                // Scale timing ring from large to normal
                // timingRing.transform.localScale =
                //   Vector3.Lerp(timingStartScale, timingEndScale, curvedT);
                

                // Linear scale down from start to end scale
                timingRing.transform.localScale =
                    Vector3.Lerp(timingStartScale, timingEndScale, t);

                // Fade in timing ring only after activation
                Color c = timingRing.color;
                c.a = curvedT * tActivation;
                timingRing.color = c;
            }
            else
            {
                // After perfect time, scale down and fade out timing ring
                float extraT = (elapsed - timeToPerfect) / hitWindow;
                extraT = Mathf.Clamp01(extraT);

                // Scale down timing ring from normal to zero
                // timingRing.transform.localScale =
                //    Vector3.Lerp(timingEndScale, Vector3.zero, extraT);
                
                // Linear scale down from end scale to zero
                timingRing.transform.localScale =
                    Vector3.Lerp(timingEndScale, Vector3.zero, extraT*0.5f);

                // Fade out timing ring
                Color c = timingRing.color;
                c.a = Mathf.Lerp(1f, 0f, extraT);
                timingRing.color = c;
            }
            */

            /* Timing ring scales down linearly from start to end scale over the lifetime
            from activation time to latter hit window, then fades out.
            The ring should be scaled to it's original size at the perfect hit time, 
            then continue scaling down until the end of the hit window,
            at which point it will fade out completely by the end of the hit window. 
            */

            if (t <= 1f)
            {
                // Linear scale down from start to end scale
                timingRing.transform.localScale =
                    Vector3.Lerp(timingStartScale, timingEndScale, t);

                // Fade in timing ring only after activation
                // Time where the hit window starts
                float hitWindowStart = lifetime - hitWindow;

                // Alpha progression:
                // 0 -> 1 from activation until hit window start
                float alphaT = Mathf.Clamp01(adjustedElapsed / hitWindowStart);

                Color c = timingRing.color;
                c.a = alphaT * tActivation;
                timingRing.color = c;
            }
            else
            {
                // After perfect time, scale down and fade out timing ring
                float extraT = (elapsed - timeToPerfect) / hitWindow;
                extraT = Mathf.Clamp01(extraT);

                // Linear scale down from end scale to zero
                timingRing.transform.localScale =
                    Vector3.Lerp(timingEndScale, Vector3.zero, extraT);

                // Fade out timing ring
                Color c = timingRing.color;
                c.a = Mathf.Lerp(1f, 0f, extraT);
                timingRing.color = c;
            }
        }

        // -------- MISS TRIGGER --------

        // Miss if we go past the hit window without clicking
        float missTime = lifetime + hitWindow;

        if (!clicked && !missed && gameplayTime > missTime)
        {
            Debug.Log("Miss!");
            missed = true;
            missStartTime = Time.time;
            StartCoroutine(MissEffect());
            if (cast != null) cast.RegisterMiss();
            feedBack.Show(TargetScript.HitResult.Miss, transform.position);
        }
    }

    IEnumerator HitEffect()
    {
        float duration = 0.18f;
        float t = 0f;
        timingRing.transform.localScale = Vector3.one;

        Vector3 originalScale = transform.localScale;
        Vector3 squishScale = originalScale * 0.9f;
        Vector3 popScale = originalScale * 1.25f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float normalized = t / duration;

            // Split animation into 2 phases
            if (normalized < 0.3f)
            {
                // Quick shrink
                float phase = normalized / 0.3f;
                transform.localScale = Vector3.Lerp(originalScale, squishScale, phase);
            }
            else
            {
                // Pop out
                float phase = (normalized - 0.3f) / 0.7f;
                transform.localScale = Vector3.Lerp(squishScale, popScale, phase);
            }

            // Fade out BOTH rings
            float fade = Mathf.SmoothStep(1f, 0f, normalized);

            Color targetColor = targetRing.color;
            targetColor.a = fade;
            targetRing.color = targetColor;

            Color timingColor = timingRing.color;
            timingColor.a = fade;
            timingRing.color = timingColor;

            yield return null;
        }

        Destroy(gameObject);
    }

    IEnumerator MissEffect()
    {
        // Animate the miss effect: scale down and fade out over missDuration
        while (true)
        {
            float tMiss = (Time.time - missStartTime) / missDuration;
            tMiss = Mathf.Clamp01(tMiss);

            // Scale down the circle
            transform.localScale = Vector3.Lerp(Vector3.one * size, Vector3.zero, tMiss);

            // Fade out target ring
            Color targetColor = targetRing.color;
            targetColor.a = Mathf.Lerp(1f, 0f, tMiss);
            targetRing.color = targetColor;

            // Fade out timing ring
            Color timingColor = timingRing.color;
            timingColor.a = Mathf.Lerp(timingColor.a, 0f, tMiss);
            timingRing.color = timingColor;

            if (tMiss >= 1f)
            {
                Destroy(gameObject);
                yield break;
            }

            yield return null;
        }
    }
}