using UnityEngine;
using TMPro;

public class PopupText : MonoBehaviour
{
    [Header("Animation")]
    [Tooltip("Time in seconds before the popup is destroyed.")]
    [SerializeField] private float duration = 0.6f;
    [Tooltip("Vertical distance travelled by the popup.")]
    [SerializeField] private float riseDistance = 50f;

    private TextMeshProUGUI text;
    private Vector2 startPosition;

    void Awake()
    {
        text = GetComponent<TextMeshProUGUI>();
    }

    public void Play()
    {
        if (duration > 0) StartCoroutine(Animate());
    }

    public void Setup(Vector2 position, string message, float fontSize, Color color, float fadeDuration = 0.6f, float rise = 50)
    {
        startPosition = position;
        text.text = message;
        text.fontSize = fontSize;
        text.color = color;
        duration = fadeDuration;
        riseDistance = rise;
    }

    System.Collections.IEnumerator Animate()
    {
        float t = 0f;
        Color startColor = text.color;

        while (t < duration)
        {
            t += Time.deltaTime;
            float normalized = t / duration;

            // Move up
            transform.position = startPosition + Vector2.up * riseDistance * normalized;

            // Fade out
            Color c = startColor;
            c.a = Mathf.SmoothStep(1f, 0f, normalized);
            text.color = c;

            yield return null;
        }
        Destroy(gameObject);
    }
}
