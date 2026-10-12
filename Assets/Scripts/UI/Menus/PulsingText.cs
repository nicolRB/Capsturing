using UnityEngine;
using TMPro;

public class PulsingText : MonoBehaviour
{
    [Header("Text Settings")]
    [SerializeField] private Color startColor;
    [SerializeField] private Color endColor;
    [SerializeField] private Color currentColor;
    [SerializeField] private float pulseTime;
    [SerializeField] private bool customStartColor;
    private float pulseTimer;


    [Header("References")]
    [SerializeField] private TextMeshProUGUI text;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (text == null)
            text = GetComponent<TextMeshProUGUI>();

        if (text != null)
        {
            if (!customStartColor)
                startColor = text.color;

            currentColor = startColor;
            text.color = startColor;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (text == null || pulseTime <= 0f)
            return;

        pulseTimer += Time.deltaTime;
        float pulse = Mathf.PingPong(pulseTimer / pulseTime, 1f);
        currentColor = Color.Lerp(startColor, endColor, pulse);
        text.color = currentColor;
    }
}
