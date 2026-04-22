using UnityEngine;
using TMPro;

public class FeedBackUI : MonoBehaviour
{
    public GameObject textPrefab;
    public Canvas canvas;

    public void Show(TargetScript.HitResult result, Vector2 position)
    {
        if (textPrefab == null)
        {
            Debug.LogError("FeedBackUI: textPrefab is not assigned in the Inspector.", this);
            return;
        }

        canvas = canvas ? canvas : FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("FeedBackUI: No Canvas found in the scene.", this);
            return;
        }

        GameObject obj = Instantiate(textPrefab, canvas.transform);
        if (obj == null)
        {
            Debug.LogError("FeedBackUI: Failed to instantiate textPrefab.", this);
            return;
        }

        TextMeshProUGUI text = obj.GetComponent<TextMeshProUGUI>();
        if (text == null)
        {
            Debug.LogError("FeedBackUI: textPrefab does not have a TextMeshProUGUI component.", this);
            Destroy(obj);
            return;
        }

        PopupText popup = obj.GetComponent<PopupText>();
        if (popup == null)
        {
            Debug.LogError("FeedBackUI: textPrefab does not have a PopupText component.", this);
            Destroy(obj);
            return;
        }

        // Setup text
        text.text = GetText(result);
        text.color = GetColor(result);
        
        Vector2 finalPos = position + Vector2.up * 30f;
        obj.transform.position = finalPos;

        // Set start position for animation and play
        popup.startPosition = finalPos;
        popup.Play();
    }

    public string GetText(TargetScript.HitResult result)
    {
        switch (result)
        {
            case TargetScript.HitResult.Perfect: return "PERFECT";
            case TargetScript.HitResult.Good: return "GOOD";
            case TargetScript.HitResult.Miss: return "MISS";
            default: return "";
        }
    }

    public Color GetColor(TargetScript.HitResult result)
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
