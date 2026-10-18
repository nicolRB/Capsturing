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
    [SerializeField] private GameObject creatureTarget;
    private LayerMask creatureLayer;
    private Highlight highlightTarget;
    private Runic hoveredRunic;

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

        if (Mouse.current.leftButton.wasPressedThisFrame && 
            stateOnMousePressed != PlayerController.CastState.Casting)
            stateOnMousePressed = player.CastingState;

        if (Mouse.current.leftButton.wasReleasedThisFrame)
            stateOnMousePressed = PlayerController.CastState.Idle;

        selecting = Mouse.current.leftButton.isPressed
            && player.MenuManager.CurrentMenu == null 
            && stateOnMousePressed == PlayerController.CastState.Idle;

        Point();


        // Se foi selecionado, começa a contar o tempo do fade independentemente de o selecting atualizar depois
        if (selected)
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
            player.SetCommandedTarget(null);
        }
    }
    void Point()
    {
        if (player == null)
            return;

        if (player.CastingState == PlayerController.CastState.Casting ||
            player.CastingState == PlayerController.CastState.Channeling)
        {
            ClearPoint();
            return;
        }

        ray = new Ray(playerCamera.transform.position, Quaternion.Euler(playerCamera.transform.eulerAngles.x, 
        playerCamera.transform.eulerAngles.y, 0) * Vector3.forward);

        if (Mouse.current.leftButton.wasPressedThisFrame && stateOnMousePressed == PlayerController.CastState.Idle)
        {
            selected = false;
            timer = 0f;
        }

        bool isAimingSpell = player.CastingState == PlayerController.CastState.Aiming;
        bool isPointTargetedSpell = isAimingSpell
            && spellcastingCoordinator.CurrentSpellType == SpellBase.SpellType.PointTargeted;
        bool isCreatureTargetedSpell = isAimingSpell
            && spellcastingCoordinator.CurrentSpellType == SpellBase.SpellType.Targeted;

        // Point-targeted spells select terrain, while targeted spells select creatures.
        bool canTargetCreature = !isPointTargetedSpell && (selecting || isCreatureTargetedSpell);
        bool creaturePointed = canTargetCreature ? PointCreature() : ClearCreatureTarget();

        bool terrainHit = Physics.Raycast(ray, out RaycastHit hit, maxDistance) 
                        && (hitLayers.value & (1 << hit.collider.gameObject.layer)) != 0;

        // Keeps visible while the button is pressed, when in fade-out after releasing or when a point-targeted spells is being aimed
        bool canShowGroundIndicator = player.CastingState != PlayerController.CastState.Channeling &&
            player.CastingState != PlayerController.CastState.Casting &&
            (selecting || selected || isPointTargetedSpell);

        // If the mouse button is released on this frame, activate selected immediately
        if (terrainHit && groundIndicator.activeSelf && Mouse.current.leftButton.wasReleasedThisFrame 
            && player.MenuManager.CurrentMenu == null && stateOnMousePressed == PlayerController.CastState.Idle
            && player.PointCommand != RunicPointCommand.Ignore)
        {
            followPoint = true;
            indicatedPosition = groundIndicator.transform.position;
            selected = true; // Ativa o fade imediatamente aqui
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame && player.CastingState == PlayerController.CastState.Idle)
        {
            if (hoveredRunic != null)
            {
                player.SetCommandedTarget(hoveredRunic);
            }
        }

        if (canShowGroundIndicator && terrainHit && !creaturePointed)
        {
            groundIndicator.SetActive(true);
            
            // Se não estiver selecionado (ou seja, arrastando o mouse), atualiza a posição livremente
            if (!selected)
            {
                groundIndicator.transform.position = hit.point + Vector3.up * 0.01f;
                groundIndicator.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                indicatedPosition = groundIndicator.transform.position;
            }

            if (selecting || isPointTargetedSpell)
            {
                timer = 0f;
                SetIndicatorIntensity(fadeStartValue);
            }
        }
        else if (!canShowGroundIndicator || creaturePointed || !terrainHit)
        {
            if (!selected) // Só desativa imediatamente se não estiver no processo de fade
            {
                groundIndicator.SetActive(false);
            }
        }

        if (highlightTarget != null)
        {
            highlightTarget.Toggle(creaturePointed && (selecting || isCreatureTargetedSpell));
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

        if (emissionPropertyBlock == null)
            emissionPropertyBlock = new MaterialPropertyBlock();

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


    bool PointCreature()
    {
        if (Physics.SphereCast(ray, sphereRadius, out RaycastHit hit, maxDistance, creatureLayer))
        {
            Runic targetRunic = hit.collider.gameObject.GetComponentInParent<Runic>();
            Highlight newHighlight = hit.collider.gameObject.GetComponentInParent<Highlight>();

            if (newHighlight != null && targetRunic != null && targetRunic.TameState == RunicState.Wild)
            {
                if (highlightTarget != null && highlightTarget != newHighlight)
                {
                    highlightTarget.Toggle(false);
                }

                creatureTarget = hit.collider.gameObject;
                highlightTarget = newHighlight;
                hoveredRunic = targetRunic;
                return true;
            }
        }

        creatureTarget = null;
        hoveredRunic = null;
        return false;
    }

    bool ClearCreatureTarget()
    {
        if (highlightTarget != null)
            highlightTarget.Toggle(false);

        highlightTarget = null;
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

    public void SetStateOnMousePressed(PlayerController.CastState castState)
    {
        stateOnMousePressed = castState;
    }
}