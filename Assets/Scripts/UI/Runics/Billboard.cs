using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Transform cam;

    void Start()
    {
        cam = Camera.main.transform;
    }

    void LateUpdate()
    {
        // Copiar a rotação da câmera (em vez de LookAt) evita distorção
        // de perspectiva e mantém a barra sempre paralela à tela.
        transform.rotation = cam.rotation;
    }
}