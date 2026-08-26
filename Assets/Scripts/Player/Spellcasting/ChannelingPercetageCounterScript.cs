using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ChannelingPercentageCounterScript : MonoBehaviour
{
    public float percentage = 0f;

    [Header("References")]
    public ChannelingGameScript channelingGame;
    public TextMeshProUGUI percentageText;

    [Header("Color Settings")]
    [SerializeField]
    public List<Color> colorShifts = new List<Color>
    {
        new Color(0.26f, 0.26f, 0.26f, 1),
        new Color(0.62f, 0f, 0f, 1),
        new Color(0.95f, 0.85f, 0f, 1),
        new Color(0f, 0.75f, 0f, 1),
        new Color(0.2f, 0.45f, 1f, 1)
    };

    public List<float> thresholds = new List<float>
    {
        0.125f,
        0.25f,
        0.5f,
        0.75f
    };

    [Header("Weight Settings")]
    public float perfectWeight = 1f;
    public float goodWeight = 0.5f;
    public float missWeight = 0f;

    [Header("Category Multipliers")]
    public float perfectMultiplier = 1f;
    public float goodMultiplier = 1f;
    public float missMultiplier = 1f;

    [Header("Category Modifiers")]
    public float perfectModifier = 0f;
    public float goodModifier = 0f;
    public float missModifier = 0f;

    [Header("Global Multiplier")]
    [Tooltip("Aplicado por cima do score inteiro.")]
    public float globalMultiplier = 1f;

    void Start()
    {
        if (channelingGame == null)
            channelingGame =
                GetComponentInParent<ChannelingGameScript>();

        if (percentageText == null)
            percentageText =
                GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        // Atualização normal enquanto a canalização acontece.
        RecalculatePercentage();
    }

    // ============================================================
    // CÁLCULO NORMAL
    // ============================================================

    public void RecalculatePercentage()
    {
        if (channelingGame == null)
            return;

        int mapCount =
            channelingGame.targetMapPlayer?.map?.Count ?? 0;

        int goods =
            channelingGame.hits -
            channelingGame.perfects;

        RecalculatePercentage(
            channelingGame.perfects,
            goods,
            channelingGame.misses,
            mapCount
        );
    }

    // ============================================================
    // CÁLCULO DO RESULTADO FINAL
    // ============================================================

    public void RecalculatePercentage(
        ChannelingGameScript.ChannelingResult result)
    {
        RecalculatePercentage(
            result.perfects,
            result.goods,
            result.misses,
            result.total
        );
    }

    // ============================================================
    // MÉTODO CENTRAL DE CÁLCULO
    // ============================================================

    private void RecalculatePercentage(
        int perfects,
        int goods,
        int misses,
        int total)
    {
        percentage = ChannelingGameScript.ComputeScore(
            perfects,
            goods,
            misses,
            total,

            perfectWeight,
            goodWeight,
            missWeight,

            perfectMultiplier,
            goodMultiplier,
            missMultiplier,

            perfectModifier,
            goodModifier,
            missModifier,

            globalMultiplier
        );

        UpdateVisuals();
    }

    // ============================================================
    // ATUALIZA TEXTO E COR
    // ============================================================

    private void UpdateVisuals()
    {
        if (percentageText == null)
            return;

        float textPercentage =
            percentage * 100f;

        percentageText.text =
            $"{textPercentage:F0}%";

        percentageText.color =
            CalculatePercentageColor(percentage);
    }

    // ============================================================
    // CÁLCULO DA COR
    // ============================================================

    private Color CalculatePercentageColor(float p)
    {
        Color blended = Color.white;

        int colorCount =
            colorShifts != null
                ? colorShifts.Count
                : 0;

        if (colorCount == 0)
        {
            return Color.white;
        }

        if (colorCount == 1)
        {
            return colorShifts[0];
        }

        if (thresholds != null &&
            thresholds.Count == colorCount - 1)
        {
            if (p <= thresholds[0])
            {
                float t =
                    thresholds[0] <= 0f
                        ? 1f
                        : Mathf.InverseLerp(
                            0f,
                            thresholds[0],
                            p
                        );

                blended = Color.Lerp(
                    colorShifts[0],
                    colorShifts[1],
                    t
                );
            }
            else
            {
                bool matched = false;

                for (int i = 1; i < thresholds.Count; i++)
                {
                    if (p <= thresholds[i])
                    {
                        float start =
                            thresholds[i - 1];

                        float end =
                            thresholds[i];

                        float t =
                            (end - start) <= 0f
                                ? 1f
                                : Mathf.InverseLerp(
                                    start,
                                    end,
                                    p
                                );

                        blended = Color.Lerp(
                            colorShifts[i],
                            colorShifts[i + 1],
                            t
                        );

                        matched = true;
                        break;
                    }
                }

                if (!matched)
                {
                    blended =
                        colorShifts[colorCount - 1];
                }
            }
        }
        else
        {
            int segments =
                colorCount - 1;

            if (segments <= 0)
            {
                blended =
                    colorShifts[0];
            }
            else
            {
                float scaled =
                    p * segments;

                int idx =
                    Mathf.Clamp(
                        Mathf.FloorToInt(scaled),
                        0,
                        segments - 1
                    );

                float t =
                    scaled - idx;

                blended = Color.Lerp(
                    colorShifts[idx],
                    colorShifts[idx + 1],
                    t
                );
            }
        }

        return blended;
    }

    // ============================================================
    // CONFIGURAÇÃO DOS VALORES
    // ============================================================

    public void SetValues(
        float perfectWgt,
        float goodWgt,
        float missWgt,

        float perfectMult,
        float goodMult,
        float missMult,

        float perfectMod,
        float goodMod,
        float missMod,

        float globalMult)
    {
        perfectWeight = perfectWgt;
        goodWeight = goodWgt;
        missWeight = missWgt;

        perfectMultiplier = perfectMult;
        goodMultiplier = goodMult;
        missMultiplier = missMult;

        perfectModifier = perfectMod;
        goodModifier = goodMod;
        missModifier = missMod;

        globalMultiplier = globalMult;
    }
}