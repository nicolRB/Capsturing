using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    public bool lockCursor;
    public PauseManager pauseManager;

    private PlayerController playerController;
    private Transform pivot;

    private float xRotation = 0f;
    public float yRotation = 0f;
    public float FOV = 60f;
    public float FOVSetting = 60f;

    void Start()
    {
        playerController = GetComponentInParent<PlayerController>();

        // guarda referência ao pivot e desparenteia
        pivot = transform.parent;
        if (pivot.parent == playerController.transform)
        {
            pivot.SetParent(playerController.transform.parent);
        }
    }

    void Update()
    {
        if (playerController.castState != PlayerController.CastState.Channeling && !pauseManager.isPaused)
        {
            float mouseY = Mouse.current.delta.y.ReadValue()
                           * playerController.mouseSensitivity
                           * Time.deltaTime;

            float mouseX = Mouse.current.delta.x.ReadValue()
                           * playerController.mouseSensitivity
                           * Time.deltaTime;

            xRotation -= mouseY;
            yRotation += mouseX;
            xRotation = Mathf.Clamp(xRotation, -85f, 85f);

            // rotação total da câmera: pitch + yaw independentes
            pivot.rotation = Quaternion.Euler(xRotation, yRotation, 0f);
        }

        // pivot segue a posição do player sem herdar rotação
        pivot.position = playerController.transform.position;

        // cursor
        if (playerController.castState == PlayerController.CastState.Channeling || pauseManager.isPaused)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // Zoom (botão direito do mouse)
        if (Mouse.current.rightButton.isPressed || playerController.castState == PlayerController.CastState.Aiming)
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