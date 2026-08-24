using UnityEngine;
using TMPro;

public class PercentageResultScript : MonoBehaviour
{
    [Header("References")]
    public TextMeshProUGUI percentageText;

    [Header("Result")]
    public float percentage = 0f;
    public Color color = Color.white;

    public float duration = 1f;
    public float finalSize = 1.1f;

    public bool showResult = false;
    public float alpha = 0f;

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