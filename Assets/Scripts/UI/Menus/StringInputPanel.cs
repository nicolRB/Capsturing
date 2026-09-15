using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class StringInputPanel : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private RectTransform boxRectTransform;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private TMP_InputField inputField;
    
    private System.Action<string> onAcceptCallback;

    public void Setup(string prompt, string placeholder, float boxWidth, float boxHeight, Vector2 coordinates, System.Action<string> onResult)
    {
        promptText.text = prompt;

        TMP_Text placeholderText = (TMP_Text)inputField.placeholder;
        if (placeholderText != null) placeholderText.text = placeholder;

        boxRectTransform.sizeDelta = new Vector2(boxWidth, boxHeight);
        boxRectTransform.anchoredPosition = coordinates;

        onAcceptCallback = onResult;
    }

    // Called by the AcceptButton OnClick event in the Inspector.
    public void AcceptString()
    {
        if (onAcceptCallback != null)
        {
            onAcceptCallback.Invoke(inputField.text);
        }
        
        // This script is on the root panel, so destroy the panel itself.
        Destroy(gameObject); 
    }
}