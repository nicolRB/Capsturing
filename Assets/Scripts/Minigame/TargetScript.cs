using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public class TargetScript : MonoBehaviour, IPointerClickHandler
{
    [Header("Settings")]
    [SerializeField] private float size = 1f;
    [SerializeField] private float lifetime = 2f;
    [SerializeField] private float perfectWindow = 0.1f;
    [SerializeField] private float hitWindow = 0.2f;

    [Header("Activation")]
    [SerializeField] private float activationTime = 0.5f;
    [SerializeField] private float fadeInDuration = 0.5f;

    [Header("Miss Settings")]
    [SerializeField] private float missDuration = 0.2f;

    [Header("References")]
    [SerializeField] private int targetIndex;
    [SerializeField] private Image targetRing;
    [SerializeField] private Transform targetRingTransform;
    [SerializeField] private Image timingRing;
    [SerializeField] private Image rootImage;
    private ChannelingGameCoordinator cast;
    private FeedbackUI feedback;

    private const float InactiveTargetAlpha = 0.35f;
    private static readonly Color InactiveTargetTint = new Color(0.7f, 0.7f, 0.7f, 1f);

    private float spawnTime;
    private bool clicked = false;
    private bool missed = false;
    private float missStartTime;

    private Vector3 timingStartScale;
    private Vector3 timingEndScale;
    private Color targetBaseColor;

    private bool IsCurrentTarget => cast != null && cast.CurrentTargetIndex == targetIndex;

    public enum HitResult
    {
        Perfect,
        Good,
        Miss
    }

    void Start()
    {
        cast = FindFirstObjectByType<ChannelingGameCoordinator>();
        feedback = FindFirstObjectByType<FeedbackUI>();

        if (targetRing == null || timingRing == null)
        {
            Debug.LogError("TargetScript requires targetRing and timingRing references.", this);
            enabled = false;
            return;
        }

        spawnTime = Time.time;

        transform.localScale = Vector3.one * size;

        targetRingTransform = targetRing.transform;

        // Cache the base color and start hidden.
        targetBaseColor = targetRing.color;
        targetRingTransform.localScale = Vector3.one * 0.8f;
        Color targetColor = targetBaseColor;
        targetColor.a = 0f;
        targetRing.color = targetColor;

        // Configure the timing ring.
        timingStartScale = Vector3.one * 2.5f;
        // timingEndScale = Vector3.one;
        timingEndScale = timingRing.transform.localScale;

        timingRing.transform.localScale = timingStartScale;

        Color c = timingRing.color;
        c.a = 0f;
        timingRing.color = c;
    }

    public void Setup(float size, float lifetime, float perfectWindow, float hitWindow, int targetIndex, ChannelingGameCoordinator cast)
    {
        this.size = size;
        this.lifetime = lifetime;
        this.perfectWindow = perfectWindow > 0 ? perfectWindow : 0.01f;
        this.hitWindow = hitWindow > 0 ? hitWindow : 0.02f;
        this.targetIndex = targetIndex;
        this.cast = cast;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (clicked || missed) return;

        clicked = true;

        if (cast == null || cast.CurrentTargetIndex != targetIndex)
            return;

        RectTransform rect = transform as RectTransform;

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rect,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint
        );

        // First ensure the click is inside the UI rectangle.
        if (!RectTransformUtility.RectangleContainsScreenPoint(
            rect, eventData.position, eventData.pressEventCamera))
            return;

        // Then filter the click against the circular target shape.
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

        if (feedback != null)
            feedback.Show(result, transform.position);
    }

    void Update()
    {
        float timeSinceSpawn = Time.time - spawnTime;
        float activationStartTime = activationTime - fadeInDuration;
        float timeAfterActivation = Mathf.Max(0f, timeSinceSpawn - activationTime);
        float gameplayTime = timeAfterActivation;

        // -------- ACTIVATION --------
        float tActivation = Mathf.Clamp01(
            (timeSinceSpawn - activationStartTime) / fadeInDuration
        );

        if (tActivation > 0f)
        {
            bool isCurrentTarget = IsCurrentTarget;

            // Only the current target can receive clicks.
            if (rootImage != null)
                rootImage.raycastTarget = isCurrentTarget;

            // Fade in the target; inactive targets remain dimmer.
            float alpha = tActivation * (isCurrentTarget ? 1f : InactiveTargetAlpha);
            Color targetColor = targetBaseColor;
            if (!isCurrentTarget)
            {
                targetColor *= InactiveTargetTint;
                timingRing.color = targetColor;
            }
            targetColor.a = alpha;
            targetRing.color = targetColor;

            // Scale the target.
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

        // -------- ROTATION (ramps up during fade-in) --------
        float baseSpeed = 540f / timeToPerfect;
        float minSpeed = baseSpeed * 0.15f;

        float rotationAngle = 0f;

        float fadeElapsed = Mathf.Clamp(timeSinceSpawn - activationStartTime, 0f, fadeInDuration);
        float postFadeElapsed = Mathf.Max(0f, timeSinceSpawn - activationTime);

        // Accumulate the angle during fade-in with a linear speed ramp.
        float tFade = fadeInDuration > 0f ? fadeElapsed / fadeInDuration : 1f;
        float currentSpeedDuringFade = Mathf.Lerp(minSpeed, baseSpeed, tFade);
        float fadeAngle = minSpeed * fadeElapsed + (currentSpeedDuringFade - minSpeed) * fadeElapsed * 0.5f;

        rotationAngle = fadeAngle + baseSpeed * postFadeElapsed;

        targetRingTransform.rotation = Quaternion.Euler(0, 0, rotationAngle);

        // -------- TIMING RING --------
        float adjustedElapsed = timeAfterActivation;
        float adjustedLifetime = lifetime;

        // Normalized timing-ring progress during the active phase.
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
                

                // Scale down from the initial size to the final size.
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

                // Fade in the timing ring after activation.
                float hitWindowStart = lifetime - hitWindow;

                // Alpha progresses from activation until the hit window starts.
                float alphaT = Mathf.Clamp01(adjustedElapsed / hitWindowStart);

                Color c = timingRing.color;
                c.a = alphaT * tActivation;
                timingRing.color = c;
            }
            else
            {
                // After the perfect time, scale down and fade out the timing ring.
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

        // Register a miss when the hit window passes without a click.
        float missTime = lifetime + hitWindow;

        if (!clicked && !missed && gameplayTime > missTime)
        {
            Debug.Log("Miss!");
            missed = true;
            missStartTime = Time.time;
            StartCoroutine(MissEffect());
            if (cast != null) cast.RegisterMiss();

            if (feedback != null)
                feedback.Show(TargetScript.HitResult.Miss, transform.position);
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