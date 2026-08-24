using UnityEngine;
using UnityEngine.InputSystem;

public class PointTargetScript : MonoBehaviour
{
    [Header("References")]
    public PlayerController player;
    public Camera playerCamera;
    public SpellcastingScript spellcastingScript;

    [Header("Point Indicator")]
    public GameObject groundIndicator;
    public Vector3 indicatedPosition;
    private Renderer[] indicatorRenderers;

    [Header("Emission")]
    [ColorUsage(true, true)]
    public Color rayEmissionColor = new Color(0.075f, 0.47f, 0.75f);
    public Color sphereEmissionColor = new Color(0.055f, 0.37f, 0.55f);
    public float sphereRadius = 0.5f;

    [Range(0f, 20f)]
    public float rayEmissionIntensity = 3f;
    public float sphereEmissionIntensity = 3f;

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

    private PlayerController.CastState stateOnMousePressed;

    [Header("Creature Targeting")]
    public GameObject creatureTarget;
    public LayerMask creatureLayer;
    private GameObject previousTarget;
    private Highlight highlightTarget;


    void Start()
    {
        if (player == null) player = FindFirstObjectByType<PlayerController>();
        if (playerCamera == null) playerCamera = FindFirstObjectByType<Camera>();
        if (spellcastingScript == null) spellcastingScript = FindFirstObjectByType<SpellcastingScript>();

        groundIndicator.SetActive(true);
        indicatorRenderers = groundIndicator.GetComponentsInChildren<Renderer>();
        groundIndicator.SetActive(false);

        creatureLayer = 1 << LayerMask.NameToLayer("Runic") | 1 << LayerMask.NameToLayer("AimHighlight");
    }

    void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            stateOnMousePressed = player.castState;
        }

        Point();

        selecting = Mouse.current.leftButton.isPressed && stateOnMousePressed == PlayerController.CastState.Idle;

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
                SetIndicatorIntensity(fadeStartValue);
            }
        }
        else if (selecting)
        {
            timer = 0;
            SetIndicatorIntensity(fadeStartValue);
        }

        if (Keyboard.current.fKey.wasPressedThisFrame && player.castState != PlayerController.CastState.Casting)
        {
            groundIndicator.tag = "Untagged";
            followPoint = false;
        }
    }

    void SetIndicatorIntensity(float alpha)
    {
        Color emission = rayEmissionColor * (rayEmissionIntensity * alpha);

        foreach (Renderer renderer in indicatorRenderers)
        {
            renderer.material.SetColor("_EmissionColor", emission);
        }
    }

    void Point()
    {
        ray = new Ray(player.transform.position, Quaternion.Euler(playerCamera.transform.eulerAngles.x, 
        player.transform.eulerAngles.y, 0) * Vector3.forward);

        bool creaturePointed = PointCreature();
        
        bool inputActive = Mouse.current.leftButton.isPressed 
                            && player.pauseManager.isPaused == false 
                            && stateOnMousePressed == PlayerController.CastState.Idle;

        bool aimingSpell = player.castState == PlayerController.CastState.Aiming 
        && spellcastingScript.currentSpellType == SpellBase.SpellType.Targeted;

        bool terrainHit = Physics.Raycast(ray, out RaycastHit hit, maxDistance) 
                        && (hitLayers.value & (1 << hit.collider.gameObject.layer)) != 0;

        if (inputActive && terrainHit && !creaturePointed)
        {
            groundIndicator.SetActive(true);
            groundIndicator.transform.position = hit.point + Vector3.up * 0.01f;
            groundIndicator.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
            selected = true;
            timer = 0f;
            SetIndicatorIntensity(fadeStartValue);
        }
        else if (inputActive && (creaturePointed || !terrainHit))
        {
            groundIndicator.SetActive(false);
            selected = false;
            timer = 0f;
        }

        if (highlightTarget != null)
        {
            highlightTarget.pointed = creaturePointed && (inputActive || aimingSpell);
        }

        if (terrainHit && Mouse.current.leftButton.wasReleasedThisFrame 
            && player.pauseManager.isPaused == false 
            && stateOnMousePressed == PlayerController.CastState.Idle)
        {
            followPoint = true;
            indicatedPosition = groundIndicator.transform.position;
        }
    }

    bool PointCreature()
    {
        if (Physics.SphereCast(ray, sphereRadius, out RaycastHit hit, maxDistance, creatureLayer) && player.castState != PlayerController.CastState.Casting)
        {
            Highlight novoHighlight = hit.collider.gameObject.GetComponentInParent<Highlight>();

            if (novoHighlight != null)
            {
                if (highlightTarget != null && highlightTarget != novoHighlight)
                {
                    highlightTarget.pointed = false;
                }

                creatureTarget = hit.collider.gameObject;
                highlightTarget = novoHighlight;
                return true;
            }
        }

        creatureTarget = null;
        return false;
    }

    void OnDrawGizmos()
    {
        if (playerCamera == null || player == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(ray.origin + ray.direction * maxDistance, sphereRadius);
        Gizmos.DrawWireSphere(ray.origin, sphereRadius);
    }
}