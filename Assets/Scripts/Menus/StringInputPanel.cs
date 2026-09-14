using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class StringInputPanel : MonoBehaviour
{
    [Header("UI References")]
    public RectTransform boxRectTransform; // O RectTransform do filho "InputBox"
    public TMP_Text promptText;
    public TMP_InputField inputField;
    
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

    // Vinculado ao OnClick do AcceptButton no Inspector
    public void AcceptString()
    {
        if (onAcceptCallback != null)
        {
            onAcceptCallback.Invoke(inputField.text);
        }
        
        // Como este script está no "StringInput" (raiz), destruímos o próprio gameObject
        Destroy(gameObject); 
    }
}