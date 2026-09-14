using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    public bool lockCursor;

    private PlayerController player;
    private Transform pivot;

    private float xRotation = 0f;
    public float yRotation = 0f;
    public float FOV = 60f;
    public float FOVSetting = 60f;

    void Start()
    {
        player = GetComponentInParent<PlayerController>();

        // guarda referência ao pivot e desparenteia
        pivot = transform.parent;
        if (pivot.parent == player.transform)
        {
            pivot.SetParent(player.transform.parent);
        }
    }

    void Update()
    {
        // Cursor
        bool menuOpen = player.menuManager.currentMenu != null;

        if (menuOpen || player.castState == PlayerController.CastState.Channeling)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (!menuOpen && player.castState != PlayerController.CastState.Channeling)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // Rotação da câmera
        if (player.castState != PlayerController.CastState.Channeling && !player.menuManager.isPaused && !menuOpen)
        {
            float mouseY = Mouse.current.delta.y.ReadValue()
                           * player.mouseSensitivity
                           * Time.deltaTime;

            float mouseX = Mouse.current.delta.x.ReadValue()
                           * player.mouseSensitivity
                           * Time.deltaTime;

            xRotation -= mouseY;
            yRotation += mouseX;
            xRotation = Mathf.Clamp(xRotation, -85f, 85f);

            // rotação total da câmera: pitch + yaw independentes
            pivot.rotation = Quaternion.Euler(xRotation, yRotation, 0f);
        }

        // pivot segue a posição do player sem herdar rotação
        pivot.position = player.transform.position;

        // Zoom (botão direito do mouse)
        if ((Mouse.current.rightButton.isPressed || player.castState == PlayerController.CastState.Aiming)
        && !menuOpen)
        {
            FOV = Mathf.Lerp(FOV, FOVSetting/2.5f, Time.deltaTime * 5f);
        }
        else
        {
            FOV = Mathf.Lerp(FOV, FOVSetting, Time.deltaTime * 5f);
        }

        GetComponent<Camera>().fieldOfView = FOV;
    }
}