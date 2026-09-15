using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; } // Global access point for input panels.

    [Header("References")]
    [SerializeField]
    private GameObject stringInputPrefab;
    [SerializeField]
    private Transform menusCanvas;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Opens a string input panel and invokes the callback when the user accepts it.
    public void CallStringInput(string prompt, string placeholder, float boxWidth, float boxHeight, Vector2 coordinates, System.Action<string> onResult)
    {
        // Instantiate the prefab as a child of the menus canvas.
        GameObject inputScreen = Instantiate(stringInputPrefab, menusCanvas);
        
        // Get the component that controls the input panel.
        StringInputPanel panel = inputScreen.GetComponent<StringInputPanel>();
        
        if (panel != null)
        {
            panel.Setup(prompt, placeholder, boxWidth, boxHeight, coordinates, onResult);
        }
    } 
}