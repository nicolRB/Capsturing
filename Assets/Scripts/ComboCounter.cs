using UnityEngine;
using TMPro;
using System.Collections;

public class ComboCounter : MonoBehaviour
{
    public TextMeshProUGUI comboText;
    public RectTransform rect;

    public int combo = 0;

    public void UpdateComboText()
    {
        comboText.text = combo > 1 ? $"COMBO: {combo}" : "";
        if(combo > 1)
        {
            comboText.rectTransform.localScale = Vector3.one; // reset scale to default
            comboText.CrossFadeAlpha(1, 0f, false); // ensure text is fully visible immediately
        }
    }

    public void IncrementCombo()
    {
        combo++;
        UpdateComboText();
        Animate();
    }

    public void ResetCombo()
    {
        combo = 0;

        
        StartCoroutine(ScaleDown()); //smoothly shrink the size of the text to zero over 0.3 seconds
        comboText.CrossFadeAlpha(0, 0.2f, false); // fade out over 0.3 seconds
        Invoke(nameof(UpdateComboText), 0.2f); // clear text after fade-out
    }

    public void Animate()
    {
        StopAllCoroutines();
        StartCoroutine(Wiggle());
    }

    IEnumerator Wiggle()
    {
        Vector2 original = rect.anchoredPosition;

        float time = 0f;
        float duration = 0.3f;

        while (time < duration)
        {
            float offset = Mathf.Sin(time * 40f) * 10f;
            rect.anchoredPosition = original + new Vector2(0, offset);

            time += Time.deltaTime;
            yield return null;
        }

        rect.anchoredPosition = original;
    }

    IEnumerator ScaleDown()
    {
        float time = 0f;
        float duration = 0.2f;
        Vector3 originalScale = comboText.rectTransform.localScale;

        while (time < duration)
        {
            float scale = Mathf.Lerp(1f, 0f, time / duration);
            comboText.rectTransform.localScale = originalScale * scale;

            time += Time.deltaTime;
            yield return null;
        }

        comboText.rectTransform.localScale = Vector3.zero; // Ensure it's fully scaled down at the end
    }
}
