using UnityEditor.UI;
using UnityEngine;
using UnityEngine.InputSystem;

public class PointTargetScript : MonoBehaviour
{
    [Header("References")]
    public PlayerController player;
    public Camera playerCamera;

    [Header("Point Indicator")]
    public GameObject groundIndicator;
    public Vector3 indicatedPosition;
    private Renderer[] indicatorRenderers;

    [Header("Emission")]
    [ColorUsage(true, true)]
    public Color emissionColor = new Color(0.075f, 0.47f, 0.75f);

    [Range(0f, 20f)]
    public float emissionIntensity = 3f;

    [Header("Pointer Settings")]
    Ray ray;
    public float maxDistance = 20f;
    public LayerMask hitLayers;
    public float fadeDuration = 2f;
    public float fadeStartValue = 1f;
    public bool followPoint = false;
    private float timer = 0f;
    private bool selected = false;
    private bool selecting = false;

    void Start()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerController>();
        }

        if (playerCamera == null)
        {
            playerCamera = FindFirstObjectByType<Camera>();
        }

        // Garante que o GameObject está ativo ANTES de ler as cores,
        // pra não capturar emissão zerada/padrão do material instanciado.
        groundIndicator.SetActive(true);
        indicatorRenderers = groundIndicator.GetComponentsInChildren<Renderer>();
        groundIndicator.SetActive(false);
    }

    void Update()
    {
        Point();

        selecting = Mouse.current.leftButton.isPressed && !player.casting;

        // Indicador de fade para desaparecer o círculo no chão após um tempo
        if (selected && !selecting)
        {
            timer += Time.deltaTime;
            float t = timer / fadeDuration;
            float alpha = Mathf.Pow(1f - t, 2f);
            SetIndicatorIntensity(alpha);

            if (timer >= fadeDuration)
            {
                selected = false;
                timer = 0f;
                groundIndicator.SetActive(false);
                SetIndicatorIntensity(fadeStartValue); // reseta para quando reaparecer
            }
        }
        else if (selecting)
        {
            timer = 0;
            SetIndicatorIntensity(fadeStartValue);
        }

        if (Keyboard.current.fKey.wasPressedThisFrame && !player.casting)
        {
            groundIndicator.tag = "Untagged";
            followPoint = false;
        }
    }

    // Controla só a emissão (HDR/bloom). Não toca no albedo/alpha do material,
    // que era o que estava escurecendo o indicador quando o script alterava a cor.
    void SetIndicatorIntensity(float alpha)
    {
        Color emission = emissionColor * (emissionIntensity * alpha);

        foreach (Renderer renderer in indicatorRenderers)
        {
            renderer.material.SetColor("_EmissionColor", emission);
        }
        Debug.Log(emission);
    }

    void Point()
    {
        // Cria um raio a partir do player em um ângulo vertical baseado no ângulo vertical da câmera
        // e ângulo horizontal baseado na direção do player
        ray = new Ray(player.transform.position, Quaternion.Euler(playerCamera.transform.eulerAngles.x, 
        player.transform.eulerAngles.y, 0) * Vector3.forward);

        Debug.DrawRay(ray.origin, ray.direction * maxDistance, Color.red);

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance) && Mouse.current.leftButton.isPressed 
        && player.pauseManager.isPaused == false && (hitLayers.value & (1 << hit.collider.gameObject.layer)) != 0 && !player.casting)
        {
            // Círculo no chão
            if (groundIndicator != null)
            {
                groundIndicator.SetActive(true);
                groundIndicator.transform.position = hit.point + Vector3.up * 0.01f;
                groundIndicator.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                selected = true;
            }
        }

        if (Physics.Raycast(ray, out RaycastHit hit2, maxDistance) && Mouse.current.leftButton.wasReleasedThisFrame 
        && player.pauseManager.isPaused == false && (hitLayers.value & (1 << hit.collider.gameObject.layer)) != 0 && !player.casting)
        {
            followPoint = true;
            indicatedPosition = groundIndicator.transform.position;
        }
    }
}