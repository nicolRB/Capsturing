using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; } // Singleton para facilitar chamar de qualquer lugar

    [Header("References")]
    public GameObject stringInputPrefab;
    public Transform menusCanvas; // Arraste o MenusCanvas para cá no Inspector

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Você pode chamar isso de qualquer outro script usando: InputManager.Instance.CallStringInput(...)
    public void CallStringInput(string prompt, string placeholder, float boxWidth, float boxHeight, Vector2 coordinates, System.Action<string> onResult)
    {
        // Instancia o prefab sendo filho do MenusCanvas
        GameObject inputScreen = Instantiate(stringInputPrefab, menusCanvas);
        
        // Pega o script que controla a janelinha
        StringInputPanel panel = inputScreen.GetComponent<StringInputPanel>();
        
        if (panel != null)
        {
            panel.Setup(prompt, placeholder, boxWidth, boxHeight, coordinates, onResult);
        }
    } 
}