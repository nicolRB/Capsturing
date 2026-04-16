using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    private PlayerController playerController;

    private float xRotation = 0f;

    void Start()
    {
        playerController = GetComponentInParent<PlayerController>();

        // Cursor.lockState = CursorLockMode.Locked;
        // Cursor.visible = false;
    }

    void Update()
    {
        if (!playerController.casting)
        {
            float mouseY = Mouse.current.delta.y.ReadValue()
                           * playerController.mouseSensitivity
                           * Time.deltaTime;

            // acumula rotação vertical
            xRotation -= mouseY;

            // limita ângulo (evita virar de cabeça pra baixo)
            xRotation = Mathf.Clamp(xRotation, -85f, 85f);

            // aplica no PAI (pivot), não na câmera
            transform.parent.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
    }
}