using UnityEngine;
using TMPro;

public class PercentageResultUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI percentageText;

    [Header("Result")]
    private float percentage = 0f;
    private Color color = Color.white;

    [Header("Animation")]
    [Tooltip("Time in seconds for the result to fade out.")]
    [SerializeField] private float duration = 1f;
    [SerializeField] private float finalSize = 1.1f;

    private bool showResult = false;
    private float alpha = 0f;

    void Start()
    {
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (percentageText != null)
        {
            percentageText.color =
                new Color(
                    color.r,
                    color.g,
                    color.b,
                    alpha
                );
        }

        if (showResult)
        {
            alpha -= Time.deltaTime / duration;

            if (alpha <= 0f)
            {
                alpha = 0f;
                showResult = false;

                gameObject.SetActive(false);
            }
        }
    }

    public void ShowResult(
        float percentage,
        Color color)
    {
        this.percentage = percentage;
        this.color = color;

        showResult = true;
        alpha = 1f;

        percentageText.text =
            $"{percentage * 100f:F0}%";
    }
}