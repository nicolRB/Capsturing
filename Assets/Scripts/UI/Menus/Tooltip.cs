using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class Tooltip : MonoBehaviour
{
    [SerializeField] float fadeDuration = 0.12f;
    CanvasGroup group;
    Coroutine fade;

    void Awake()
    {
        group = GetComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        group.interactable = false;
    }

    public void Show() => FadeTo(1f);
    public void Hide() => FadeTo(0f);

    void FadeTo(float target)
    {
        if (fade != null) StopCoroutine(fade);
        fade = StartCoroutine(FadeRoutine(target));
    }

    IEnumerator FadeRoutine(float target)
    {
        float start = group.alpha;
        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime; // funciona com o jogo pausado
            group.alpha = Mathf.Lerp(start, target, t / fadeDuration);
            yield return null;
        }
        group.alpha = target;
    }
}