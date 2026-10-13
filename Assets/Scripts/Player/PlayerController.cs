using System;
using UnityEngine;
using UnityEngine.InputSystem;

public enum RunicFightingCommand { Pacific, Escort, Aggressive }
public enum RunicPointCommand { Stand, Scout, Patrol, Ignore }

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerController : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private int maxHP = 100;
    [SerializeField] private int currentHP = 100;
    [SerializeField] private int defense = 5;
    [SerializeField] private int knockbackResistance = 0;

    public int MaxHP => maxHP;
    public int CurrentHP => currentHP;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 1.25f;
    [SerializeField] private float jogSpeed = 5f;
    [SerializeField] private float sprintSpeed = 10f;
    [SerializeField] private float currentSpeed;
    [SerializeField] private Key runKey = Key.LeftShift;
    [SerializeField] private Key walkKey = Key.LeftAlt;
    private bool isTryingToWalk;
    private bool isTryingToSprint;

    [SerializeField, Range(0f, 1f)] private float knockbackUpwardBias = 0.75f;

    public float WalkSpeed => walkSpeed;
    public float JogSpeed => jogSpeed;
    public float SprintSpeed => sprintSpeed;
    public bool IsTryingToWalk => isTryingToWalk;
    public bool IsTryingToSprint => isTryingToSprint;

    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float coyoteTime = 0.2f;
    [SerializeField] private float jumpBufferTime = 0.2f;

    private float coyoteTimer = 0f;
    private float jumpBufferTimer = 0f;

    [Header("Dash")]
    [SerializeField] private float dashForce = 6f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;

    private float dashTimer = 0f;
    private float cooldownTimer = 0f;
    private bool isDashing = false;
    private Vector3 dashDirection;

    [Header("Dash Input")]
    [SerializeField] private float tapThreshold = 0.2f;
    private float shiftPressedTime = 0f;
    private bool isHoldingShift = false;

    [Header("Mouse Look")]
    [SerializeField] private float cameraSensitivity;

    public float CameraSensitivity => cameraSensitivity;

    private Rigidbody rb;
    private CapsuleCollider capsule;
    private Animator animator;

    [Header("State Flags")]
    [SerializeField] private bool moving = false;

    public bool Moving => moving;

    // Idle - no active action; Channeling - playing the spell minigame;
    // Aiming - the spell is charged and ready to launch;
    // Casting - lançando feitiço
    public enum CastState { Idle, Channeling, Aiming, Casting } 
    private CastState castingState = CastState.Idle;

    public CastState CastingState => castingState;

    [Header("Runics")]
    [SerializeField] private int maxPartySize = 3;
    [SerializeField] private RunicFightingCommand fightingCommand = RunicFightingCommand.Escort;
    [SerializeField] private RunicPointCommand pointCommand = RunicPointCommand.Stand;
    [SerializeField] private Runic commandedTarget;

    public int MaxPartySize => maxPartySize;

    [Header("References")]
    [SerializeField] private GameObject playerCharacter;
    [SerializeField] private CameraController cameraController;
    [SerializeField] private CameraCollision cameraCollision;
    [SerializeField] private MenuManager menuManager;
    [SerializeField] private GameObject playerHUD;
    [SerializeField] private CanvasGroup hudCanvasGroup;
    [SerializeField] private GameObject aimIndicator;
    [SerializeField] private SpellcastingCoordinator spellcastingCoordinator;
    [SerializeField] private RunicCommands runicCommand;

    private bool useAimIndicator = true;

    public GameObject PlayerCharacter => playerCharacter;
    public CameraController CameraController => cameraController;
    public CameraCollision CameraCollision => cameraCollision;
    public MenuManager MenuManager => menuManager;
    public GameObject PlayerHUD => playerHUD;
    public GameObject AimIndicator => aimIndicator;
    public bool UseAimIndicator => useAimIndicator;
    public RunicFightingCommand FightingCommand => fightingCommand;
    public RunicPointCommand PointCommand => pointCommand;
    public Runic CommandedTarget => commandedTarget;
    public RunicCommands RunicCommand => runicCommand;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
        animator = GetComponentInChildren<Animator>();
        runicCommand = GetComponent<RunicCommands>();

        if (rb == null)
            Debug.LogError("Rigidbody missing on Player!");
        if (capsule == null)
            Debug.LogError("CapsuleCollider missing on Player!");
        if (animator == null)
            Debug.LogError("Animator missing on Player!");

        SettingsManager.OnSettingsChanged += Apply;
        Apply(SettingsManager.Current);
    }

    void OnDisable() => SettingsManager.OnSettingsChanged -= Apply;

    private void Start()
    {
        currentSpeed = jogSpeed;
        if (cameraController == null) 
            cameraController = FindFirstObjectByType<CameraController>();

        if (menuManager == null) 
            menuManager = FindFirstObjectByType<MenuManager>();

        if (playerHUD == null) 
            playerHUD = GameObject.Find("PlayerHUD");

        if (cameraCollision == null) 
            cameraCollision = FindFirstObjectByType<CameraCollision>();

        if (playerCharacter == null) 
            playerCharacter = gameObject;

        if (spellcastingCoordinator == null) 
            spellcastingCoordinator = FindFirstObjectByType<SpellcastingCoordinator>();
    }

    public void SetCastingState(CastState newState)
    {
        if (castingState == newState)
            return;

        castingState = newState;

        Debug.Log("Casting State changed to " + castingState);
    }

    public void ToggleAimIndicator(bool state)
    {
        useAimIndicator = state;
    }

    private void Update()
    {
        HandleMovementInput();
        HandleDashInput();
        HandleJumpInput();
        HandleRotation();
        UpdateCooldowns();
        UpdateAnimator();
        if (currentHP <= 0) Die();
        if (castingState == CastState.Aiming && useAimIndicator) 
        {
            aimIndicator.SetActive(true);
            AimIndicatorAnimation();
        }
        else if (aimIndicator.activeSelf) aimIndicator.SetActive(false);
    }

    private void FixedUpdate()
    {
        HandleJumpPhysics();
    }

    // ---------------- MOVEMENT ----------------
    private void HandleMovementInput()
    {
        Vector2 moveInput = Vector2.zero;

        if (castingState != CastState.Channeling && (Keyboard.current.wKey.isPressed 
        || Keyboard.current.upArrowKey.isPressed)) 
            moveInput.y += 1f;
        if (castingState != CastState.Channeling && (Keyboard.current.sKey.isPressed 
        || Keyboard.current.downArrowKey.isPressed)) 
            moveInput.y -= 1f;
        if (castingState != CastState.Channeling && (Keyboard.current.aKey.isPressed 
        || Keyboard.current.leftArrowKey.isPressed)) 
            moveInput.x -= 1f;
        if (castingState != CastState.Channeling && (Keyboard.current.dKey.isPressed 
        || Keyboard.current.rightArrowKey.isPressed)) 
            moveInput.x += 1f;

        moveInput = moveInput.normalized;
        Vector3 movement = transform.forward * moveInput.y + transform.right * moveInput.x;

        if (moveInput.magnitude > 0.1f)
            moving = true;
        else
            moving = false;

        if (!isDashing)
        {
            isTryingToWalk = Keyboard.current[walkKey].isPressed;
            isTryingToSprint = Keyboard.current[runKey].isPressed;
            bool isMovingForward = moveInput.y > 0f;

            float targetSpeed = jogSpeed;
            if (isTryingToWalk)
                targetSpeed = walkSpeed;
            else if (isTryingToSprint && isMovingForward)
                targetSpeed = sprintSpeed;

            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 5f);

            // Use Rigidbody for movement to avoid overriding physics
            rb.MovePosition(rb.position + movement * currentSpeed * Time.deltaTime);
        }
    }

    // ---------------- DASH ----------------
    private void HandleDashInput()
    {
        if (!IsGrounded()) 
        {
            isDashing = false;
            return;
        }
        Vector2 moveInput = Vector2.zero;
        if (castingState != CastState.Channeling && (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)) moveInput.y += 1f;
        if (castingState != CastState.Channeling && (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)) moveInput.y -= 1f;
        if (castingState != CastState.Channeling && (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)) moveInput.x -= 1f;
        if (castingState != CastState.Channeling && (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)) moveInput.x += 1f;

        Vector3 movement = transform.forward * moveInput.y + transform.right * moveInput.x;

        if (castingState != CastState.Channeling && Keyboard.current[runKey].wasPressedThisFrame)
        {
            shiftPressedTime = 0f;
            isHoldingShift = true;
        }

        if (castingState != CastState.Channeling && isHoldingShift && Keyboard.current[runKey].isPressed)
        {
            shiftPressedTime += Time.deltaTime;
        }

        if (castingState != CastState.Channeling && isHoldingShift && Keyboard.current[runKey].wasReleasedThisFrame)
        {
            if (shiftPressedTime <= tapThreshold && cooldownTimer <= 0f && !isDashing &&
                (Keyboard.current.wKey.isPressed || Keyboard.current.sKey.isPressed || 
                Keyboard.current.aKey.isPressed || Keyboard.current.dKey.isPressed || 
                Keyboard.current.upArrowKey.isPressed || Keyboard.current.downArrowKey.isPressed || 
                Keyboard.current.leftArrowKey.isPressed || Keyboard.current.rightArrowKey.isPressed) 
            )
            {
                dashDirection = movement.sqrMagnitude > 0 ? movement.normalized : transform.forward;
                isDashing = true;
                dashTimer = dashDuration;
                cooldownTimer = dashCooldown;

                Vector3 velocity = rb.linearVelocity;
                velocity.x = 0f;
                velocity.z = 0f;
                rb.linearVelocity = velocity;
            }
            isHoldingShift = false;
        }

        if (isDashing)
        {
            float dashProgress = dashTimer / dashDuration;
            rb.MovePosition(rb.position + dashDirection * dashForce * dashProgress * Time.fixedDeltaTime);
            dashTimer -= Time.deltaTime;

            if (dashTimer <= 0f)
            {
                isDashing = false;
                Vector3 velocity = rb.linearVelocity;
                velocity.x = 0f;
                velocity.z = 0f;
                rb.linearVelocity = velocity;
            }
        }
    }

    // ---------------- JUMP ----------------
    private void HandleJumpInput()
    {
        if (castingState != CastState.Channeling && Keyboard.current.spaceKey.wasPressedThisFrame)
            jumpBufferTimer = jumpBufferTime;
        else
            jumpBufferTimer -= Time.deltaTime;

        if (IsGrounded())
            coyoteTimer = coyoteTime;
        else
            coyoteTimer -= Time.deltaTime;
    }

    private void HandleJumpPhysics()
    {
        if (jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            Vector3 lv = rb.linearVelocity;
            lv.y = 0f;
            rb.linearVelocity = lv;

            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);

            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
        }
    }

    // ---------------- ROTATION ----------------
    private void HandleRotation()
    {
        if (castingState == CastState.Channeling || isDashing || menuManager.CurrentMenu != null) return;

        if (moving || Mouse.current.rightButton.isPressed || Mouse.current.leftButton.isPressed 
        || Keyboard.current[runKey].isPressed || castingState == CastState.Aiming)
        {
            // player rotates with the camera
            Quaternion targetRotation = Quaternion.Euler(0f, cameraController.yRotation, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 15f);
        }
        else
        {
            // rotates only camera
            float mouseX = Mouse.current.delta.x.ReadValue()
                        * cameraSensitivity * Time.deltaTime;

            cameraController.yRotation += mouseX;
        }
    }

    // ---------------- COOLDOWNS ----------------
    private void UpdateCooldowns()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;
    }

    // ---------------- GROUND CHECK ----------------
    public bool IsGrounded()
    {
        if (capsule == null) return false;

        Vector3 origin = rb.position + Vector3.up * 0.05f; // offset from the center of the capsule
        float rayLength = capsule.bounds.extents.y + 0.07f; // slightly longer than the distance from the center to the bottom of the capsule

        return Physics.Raycast(origin, Vector3.down, rayLength); // Debug ray for visualization
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;

        // -------- INPUT --------
        Vector2 moveInput = Vector2.zero;

        if (castingState != CastState.Channeling && (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)) moveInput.y += 1f;
        if (castingState != CastState.Channeling && (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)) moveInput.y -= 1f;
        if (castingState != CastState.Channeling && (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)) moveInput.x -= 1f;
        if (castingState != CastState.Channeling && (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)) moveInput.x += 1f;

        float inputAmount = moveInput.magnitude;

        // -------- GROUNDED --------
        bool grounded = IsGrounded();

        // -------- SPEED PARAMETER (for Blend Tree) --------
        float normalizedSpeed = currentSpeed / sprintSpeed;
        animator.SetFloat("Speed", normalizedSpeed);

        // -------- WALKING BOOL (optional) --------
        animator.SetBool("Walking", grounded && inputAmount > 0.1f);

        // -------- ANIMATION PLAYBACK SPEED --------
        animator.speed = Mathf.Lerp(0.8f, 1.5f, normalizedSpeed*2f); // slightly faster than actual speed for better feel
    }

    private void AimIndicatorAnimation()
    {
        if (aimIndicator == null) return;

        float pulse = Mathf.Sin(Time.time * 5f) * 0.5f + 0.5f; // oscillates between 0 and 1
        float scale = Mathf.Lerp(0.9f, 1.1f, pulse);
        aimIndicator.transform.localScale = new Vector3(scale, scale, scale);
    }

    public void LoadPlayerData(SaveDataContainer data)
    {
        maxHP = data.playerMaxHP;
        currentHP = data.playerHP;
        Vector3 loadedPosition = new Vector3(data.playerPosition[0], data.playerPosition[1], data.playerPosition[2]);
        float loadedRotation = data.playerRotation;

        playerCharacter.transform.position = loadedPosition;
        playerCharacter.transform.rotation = Quaternion.Euler(0f, loadedRotation, 0f); // Reset rotation

        spellcastingCoordinator.SetSpellCooldowns(data.spellCooldownList);
    }

    public void PopulateSaveData(SaveDataContainer data)
    {
        data.playerMaxHP = maxHP;
        data.playerHP = currentHP;
        data.playerPosition[0] = transform.position.x;
        data.playerPosition[1] = transform.position.y;
        data.playerPosition[2] = transform.position.z;
        data.playerRotation = transform.rotation.eulerAngles.y;
        if (spellcastingCoordinator != null)
            data.spellCooldownList = spellcastingCoordinator.SpellCooldowns;
    }

    // ---------------- Combat ---------------- 

    public void TakeDamage(int damage)
    {
        if (castingState == CastState.Channeling) return;

        Debug.Log("Player took " + damage + " damage.");

        if (damage > 0)
            currentHP -= damage;

        if (currentHP <= 0)
            Die();
    }

    public void ApplyKnockback(Transform knockbackOrigin, float knockback)
    {
        if (knockback == 0 || knockbackOrigin == null)
            return;
        
        Vector3 away = transform.position - knockbackOrigin.position;
        away.y = 0f;

        if (away.sqrMagnitude < 0.001f)
        {
            away = knockbackOrigin.transform.forward;
            away.y = 0f;
        }

        if (away.sqrMagnitude < 0.001f)
            away = Vector3.forward;

        away.Normalize();

        Vector3 direction = (away + Vector3.up * Mathf.Max(0f, knockbackUpwardBias)).normalized;
        float resistance = Mathf.Clamp01(knockbackResistance / 100f);
        Runic runicAttacker = knockbackOrigin.GetComponent<Runic>();
        float weightFactor;
        if (runicAttacker == null)
            runicAttacker = knockbackOrigin.GetComponentInParent<Runic>();

        if (runicAttacker != null)
            weightFactor = runicAttacker.Weight / Mathf.Max(runicAttacker.Weight + rb.mass, 0.01f);
        else 
            weightFactor = 1;
        
        float impulse = knockback * weightFactor * (1f - resistance);
        
        Debug.Log(impulse + " impulse applied to player.");

        if (impulse >= 0)
            rb.AddForce(impulse*direction, ForceMode.Impulse);
    }

    public void Heal(int healing)
    {
        if (healing > 0)
        {
            currentHP += healing;
            currentHP = Math.Min(currentHP, maxHP);
        }
    }

    public void SetHP(int amount)
    {
        currentHP = amount;
        currentHP = Math.Min(currentHP, maxHP);

        if (amount < 0)
            Die();
    }

    public void SetMaxHP(int amount)
    {
        maxHP = amount > 0 ? amount : 1;
    }

    public void Die()
    {
        if (currentHP > 0)
            currentHP = 0;

        if (currentHP < 0)
            Debug.Log("Overkill Damage Amout = " + (-currentHP));

        // gameover logic...
    }

    public void SetCommandedTarget(Runic target)
    {
        commandedTarget = target;

        if (target == null)
            Debug.Log("Alvo do Rúnico limpo.");
    }

    // ---------------- Settings ---------------- 

    void Apply(GameSettings s)
    {
        cameraSensitivity = s.cameraSensitivity;
        
        // Escala o HUD inteiro (o painel inferior e seus filhos)
        playerHUD.transform.localScale = new Vector3(s.hudScale, s.hudScale, s.hudScale);
        
        // Controla a opacidade do HUD inteiro de uma só vez (incluindo ícones e textos filhos)
        if (hudCanvasGroup == null)
            hudCanvasGroup = playerHUD.GetComponent<CanvasGroup>();
            
        if (hudCanvasGroup != null)
        {
            hudCanvasGroup.alpha = s.hudOpacity / 100f; 
        }
    }
}