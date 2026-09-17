using UnityEngine;
using TMPro;

public class FeedbackUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject textPrefab;
    [SerializeField] private Canvas canvas;

    public void Show(TargetScript.HitResult result, Vector2 position)
    {
        if (textPrefab == null)
        {
            Debug.LogError("FeedbackUI: textPrefab is not assigned in the Inspector.", this);
            return;
        }

        canvas = canvas ? canvas : FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("FeedbackUI: No Canvas found in the scene.", this);
            return;
        }

        GameObject obj = Instantiate(textPrefab, canvas.transform);
        if (obj == null)
        {
            Debug.LogError("FeedbackUI: Failed to instantiate textPrefab.", this);
            return;
        }

        TextMeshProUGUI text = obj.GetComponent<TextMeshProUGUI>();
        if (text == null)
        {
            Debug.LogError("FeedbackUI: textPrefab does not have a TextMeshProUGUI component.", this);
            Destroy(obj);
            return;
        }

        PopupText popup = obj.GetComponent<PopupText>();
        if (popup == null)
        {
            Debug.LogError("FeedbackUI: textPrefab does not have a PopupText component.", this);
            Destroy(obj);
            return;
        }

        // Configure and play the popup.
        text.text = GetText(result);
        text.color = GetColor(result);
        
        Vector2 finalPos = position + Vector2.up * 30f;
        obj.transform.position = finalPos;

        popup.SetStartPosition(finalPos);
        popup.Play();
    }

    private string GetText(TargetScript.HitResult result)
    {
        switch (result)
        {
            case TargetScript.HitResult.Perfect: return "PERFECT";
            case TargetScript.HitResult.Good: return "GOOD";
            case TargetScript.HitResult.Miss: return "MISS";
            default: return "";
        }
    }

    private Color GetColor(TargetScript.HitResult result)
    {
        switch (result)
        {
            case TargetScript.HitResult.Perfect: return Color.green;
            case TargetScript.HitResult.Good: return Color.blue;
            case TargetScript.HitResult.Miss: return Color.lightGray;
            default: return Color.white;
        }
    }

}
