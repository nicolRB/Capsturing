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

        // Cache the pivot and detach it when it is a child of the player.
        pivot = transform.parent;
        if (pivot.parent == player.transform)
        {
            pivot.SetParent(player.transform.parent);
        }
    }

    void Update()
    {
        // Cursor
        bool menuOpen = player.MenuManager.CurrentMenu != null;

        if (menuOpen || player.CastingState == PlayerController.CastState.Channeling)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (!menuOpen && player.CastingState != PlayerController.CastState.Channeling)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // Camera rotation.
        if (player.CastingState != PlayerController.CastState.Channeling && !player.MenuManager.IsPaused && !menuOpen)
        {
            float mouseY = Mouse.current.delta.y.ReadValue()
                           * player.MouseSensitivity
                           * Time.deltaTime;

            float mouseX = Mouse.current.delta.x.ReadValue()
                           * player.MouseSensitivity
                           * Time.deltaTime;

            xRotation -= mouseY;
            yRotation += mouseX;
            xRotation = Mathf.Clamp(xRotation, -85f, 85f);

            // Apply independent pitch and yaw rotation.
            pivot.rotation = Quaternion.Euler(xRotation, yRotation, 0f);
        }

        // Keep the pivot at the player's position without inheriting player rotation.
        pivot.position = player.transform.position;

        // Zoom while aiming or holding the right mouse button.
        if ((Mouse.current.rightButton.isPressed || player.CastingState == PlayerController.CastState.Aiming)
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