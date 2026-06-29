using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PopupText : MonoBehaviour
{
    public float duration = 0.6f;
    public float riseDistance = 50f;

    private TextMeshProUGUI text;
    public Vector2 startPosition;

    void Awake()
    {
        text = GetComponent<TextMeshProUGUI>();
    }

    public void Play()
    {
        if (duration > 0) StartCoroutine(Animate());
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
