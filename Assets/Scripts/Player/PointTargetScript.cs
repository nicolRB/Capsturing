using UnityEngine;
using UnityEngine.InputSystem;

public class PointTargetScript : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController player;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private SpellcastingCoordinator spellcastingCoordinator;

    [Header("Point Indicator")]
    [SerializeField] private GameObject groundIndicator;
    private Vector3 indicatedPosition;
    private Renderer[] indicatorRenderers;

    public GameObject GroundIndicator => groundIndicator;
    public Vector3 IndicatedPosition => indicatedPosition;

    [Header("Emission")]
    [ColorUsage(true, true)]
    [SerializeField] private Color rayEmissionColor = new Color(0.075f, 0.47f, 0.75f);
    [SerializeField] private float sphereRadius = 0.5f;

    [Range(0f, 20f)]
    [SerializeField] private float rayEmissionIntensity = 3f;
    [SerializeField] private float sphereEmissionIntensity = 3f;

    [Header("Pointer Settings")]
    private Ray ray;
    [SerializeField] private float maxDistance = 20f;
    [SerializeField] private LayerMask hitLayers;
    [SerializeField] private float fadeDuration = 2f;
    [SerializeField] private float fadeStartValue = 1f;
    [SerializeField] private bool followPoint = false;
    private float timer = 0f;
    private bool selected = false;
    private bool selecting = false;
    private MaterialPropertyBlock emissionPropertyBlock;
    private static readonly int EmissionColorProperty = Shader.PropertyToID("_EmissionColor");

    private PlayerController.CastState stateOnMousePressed;

    public bool FollowPoint => followPoint;

    [Header("Creature Targeting")]
    private GameObject creatureTarget;
    private LayerMask creatureLayer;
    private Highlight highlightTarget;

    public GameObject CreatureTarget => creatureTarget;

    void Start()
    {
        if (player == null) player = FindFirstObjectByType<PlayerController>();
        if (playerCamera == null) playerCamera = FindFirstObjectByType<Camera>();
        if (spellcastingCoordinator == null) spellcastingCoordinator = FindFirstObjectByType<SpellcastingCoordinator>();

        if (player == null || playerCamera == null || spellcastingCoordinator == null || groundIndicator == null)
        {
            Debug.LogError(
                "PointTargetScript requires PlayerController, Camera, SpellcastingCoordinator, and ground indicator references.",
                this
            );
            enabled = false;
            return;
        }

        groundIndicator.SetActive(true);
        indicatorRenderers = groundIndicator.GetComponentsInChildren<Renderer>();
        groundIndicator.SetActive(false);
        emissionPropertyBlock = new MaterialPropertyBlock();
        int runicLayer = LayerMask.NameToLayer("Runic");
        int aimHighlightLayer = LayerMask.NameToLayer("AimHighlight");

        if (runicLayer == -1 || aimHighlightLayer == -1)
        {
            Debug.LogError(
                "PointTargetScript requires the 'Runic' and 'AimHighlight' layers to exist.",
                this
            );
            enabled = false;
            return;
        }

        creatureLayer = (1 << runicLayer) | (1 << aimHighlightLayer);
    }

    void Update()
    {
        if (Mouse.current == null || Keyboard.current == null)
            return;

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            stateOnMousePressed = player.CastingState;
        }

        Point();

        selecting = Mouse.current.leftButton.isPressed && stateOnMousePressed == PlayerController.CastState.Idle;

        if (selected && !selecting)
        {
            timer += Time.deltaTime;
            float t = fadeDuration > 0f ? timer / fadeDuration : 1f;
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

        if (Keyboard.current.fKey.wasPressedThisFrame && player.CastingState != PlayerController.CastState.Casting)
        {
            groundIndicator.tag = "Untagged";
            followPoint = false;
        }
    }

    public void ToggleFollowPoint(bool state)
    {
        followPoint = state;
    }

    void SetIndicatorIntensity(float alpha)
    {
        if (indicatorRenderers == null)
            return;

        Color emission = rayEmissionColor * (rayEmissionIntensity * alpha);

        foreach (Renderer renderer in indicatorRenderers)
        {
            if (renderer == null)
                continue;

            renderer.GetPropertyBlock(emissionPropertyBlock);
            emissionPropertyBlock.SetColor(EmissionColorProperty, emission);
            renderer.SetPropertyBlock(emissionPropertyBlock);
        }
    }

    public void ClearPoint()
    {
        groundIndicator.SetActive(false);
        indicatedPosition = Vector3.zero;
        selected = false;
        selecting = false;
        followPoint = false;
        timer = 0f;
        SetIndicatorIntensity(fadeStartValue);
    }

    void Point()
    {
        ray = new Ray(playerCamera.transform.position, Quaternion.Euler(playerCamera.transform.eulerAngles.x, 
        playerCamera.transform.eulerAngles.y, 0) * Vector3.forward);

        bool creaturePointed = PointCreature();
        
        bool inputActive = Mouse.current.leftButton.isPressed 
                            && player.MenuManager.CurrentMenu == null
                            && stateOnMousePressed == PlayerController.CastState.Idle;

        bool aimingSpell = player.CastingState == PlayerController.CastState.Aiming 
        && spellcastingCoordinator.CurrentSpellType == SpellBase.SpellType.Targeted;

        bool terrainHit = Physics.Raycast(ray, out RaycastHit hit, maxDistance) 
                        && (hitLayers.value & (1 << hit.collider.gameObject.layer)) != 0;

        if (inputActive && terrainHit && !creaturePointed)
        {
            groundIndicator.SetActive(true);
            groundIndicator.transform.position = hit.point + Vector3.up * 0.01f;
            groundIndicator.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
            indicatedPosition = groundIndicator.transform.position;
            selected = true;
            timer = 0f;
            SetIndicatorIntensity(fadeStartValue);
        }
        else if (aimingSpell && terrainHit && !creaturePointed)
        {
            groundIndicator.SetActive(true);
            groundIndicator.transform.position = hit.point + Vector3.up * 0.01f;
            groundIndicator.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
            indicatedPosition = groundIndicator.transform.position;
            SetIndicatorIntensity(fadeStartValue);
        }
        else if (aimingSpell && (creaturePointed || !terrainHit))
        {
            groundIndicator.SetActive(false);
        }
        else if (inputActive && (creaturePointed || !terrainHit))
        {
            groundIndicator.SetActive(false);
            selected = false;
            timer = 0f;
        }

        if (highlightTarget != null)
        {
            highlightTarget.Toggle(creaturePointed && (inputActive || aimingSpell));
        }

        if (terrainHit && groundIndicator.activeSelf && Mouse.current.leftButton.wasReleasedThisFrame 
            && player.MenuManager.CurrentMenu == null
            && stateOnMousePressed == PlayerController.CastState.Idle)
        {
            followPoint = true;
            indicatedPosition = groundIndicator.transform.position;
        }
    }

    bool PointCreature()
    {
        if (Physics.SphereCast(ray, sphereRadius, out RaycastHit hit, maxDistance, creatureLayer) && player.CastingState != PlayerController.CastState.Casting)
        {
            Highlight novoHighlight = hit.collider.gameObject.GetComponentInParent<Highlight>();

            if (novoHighlight != null)
            {
                if (highlightTarget != null && highlightTarget != novoHighlight)
                {
                    highlightTarget.Toggle(false);
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