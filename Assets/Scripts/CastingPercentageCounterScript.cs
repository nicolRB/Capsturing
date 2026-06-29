using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CastingPercentageCounterScript : MonoBehaviour
{
    public CastingGameScript castingGame;
    public TextMeshProUGUI percentageText;

    public float percentage = 0;

    [SerializeField] public List<Color> colorShifts = new List<Color>
    {
        new Color(0.26f, 0.26f, 0.26f, 1),
        new Color(0.62f, 0, 0, 1),
        new Color(0.95f, 0.85f, 0, 1),
        new Color(0, 0.75f, 0, 1),
        new Color(0.2f, 0.45f, 1, 1)
    };

    public List<float> thresholds = new List<float> { 0.125f, 0.25f, 0.5f, 0.75f };

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (castingGame == null)
        {
            castingGame = GetComponentInParent<CastingGameScript>();
        }

        if ( percentageText == null)
        {
            percentageText = GetComponent<TextMeshProUGUI>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        switch (castingGame.castingMode)
        {
            case 1:
                int total1 = castingGame.hits + castingGame.misses;
                percentage = total1 > 0 ? (float)castingGame.hits / total1 : 0f;
                break;
            case 2:
                int mapCount = castingGame.targetMapPlayer?.map?.Count ?? 0;
                percentage = mapCount > 0 ? (float)castingGame.hits / mapCount : 0f;
                break;
            case 3:
                break;
        }

        float textPercentage = percentage * 100;

        percentageText.text = $"{textPercentage:F0}%";

        // Smoothly blend text color based on percentage and thresholds.
        float p = Mathf.Clamp01(percentage);

        Color blended = Color.white;
        int colorCount = colorShifts != null ? colorShifts.Count : 0;

        if (colorCount == 0)
        {
            blended = Color.white;
        }
        else if (colorCount == 1)
        {
            blended = colorShifts[0];
        }
        else if (thresholds != null && thresholds.Count == colorCount - 1)
        {
            // Use thresholds to determine segment
            if (p <= thresholds[0])
            {
                float t = thresholds[0] <= 0f ? 1f : Mathf.InverseLerp(0f, thresholds[0], p);
                blended = Color.Lerp(colorShifts[0], colorShifts[1], t);
            }
            else
            {
                bool matched = false;
                for (int i = 1; i < thresholds.Count; i++)
                {
                    if (p <= thresholds[i])
                    {
                        float start = thresholds[i - 1];
                        float end = thresholds[i];
                        float t = (end - start) <= 0f ? 1f : Mathf.InverseLerp(start, end, p);
                        blended = Color.Lerp(colorShifts[i], colorShifts[i + 1], t);
                        matched = true;
                        break;
                    }
                }

                if (!matched)
                {
                    // Above last threshold -> use last color
                    blended = colorShifts[colorCount - 1];
                }
            }
        }
        else
        {
            // Fallback: evenly distribute across color segments
            int segments = colorCount - 1;
            if (segments <= 0)
            {
                blended = colorShifts[0];
            }
            else
            {
                float scaled = p * segments;
                int idx = Mathf.Clamp(Mathf.FloorToInt(scaled), 0, segments - 1);
                float t = scaled - idx;
                blended = Color.Lerp(colorShifts[idx], colorShifts[idx + 1], t);
            }
        }

        if (percentageText != null)
            percentageText.color = blended;
    }
}
