using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerController : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private int maxHP = 100;
    [SerializeField] private int currentHP = 100;

    public int MaxHP => maxHP;
    public int CurrentHP => currentHP;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private float jogSpeed = 5f;
    [SerializeField] private float runningSpeed = 10f;
    [SerializeField] private float currentSpeed;
    [SerializeField] private Key runKey = Key.LeftShift;
    [SerializeField] private Key walkKey = Key.LeftAlt;

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
    [SerializeField] private float mouseSensitivity = 15f;

    public float MouseSensitivity => mouseSensitivity;

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

    public int MaxPartySize => maxPartySize;

    [Header("References")]
    [SerializeField] private GameObject playerCharacter;
    [SerializeField] private CameraController cameraController;
    [SerializeField] private CameraCollision cameraCollision;
    [SerializeField] private MenuManager menuManager;
    [SerializeField] private GameObject playerHUD;
    [SerializeField] private GameObject aimIndicator;
    [SerializeField] private bool useAimIndicator = true;

    public GameObject PlayerCharacter => playerCharacter;
    public CameraController CameraController => cameraController;
    public CameraCollision CameraCollision => cameraCollision;
    public MenuManager MenuManager => menuManager;
    public GameObject PlayerHUD => playerHUD;
    public GameObject AimIndicator => aimIndicator;
    public bool UseAimIndicator => useAimIndicator;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
        animator = GetComponentInChildren<Animator>();

        if (rb == null)
            Debug.LogError("Rigidbody missing on Player!");
        if (capsule == null)
            Debug.LogError("CapsuleCollider missing on Player!");
        if (animator == null)
            Debug.LogError("Animator missing on Player!");
    }

    private void Start()
    {
        currentSpeed = jogSpeed;
        if (cameraController == null) cameraController = FindFirstObjectByType<CameraController>();

        if (menuManager == null) menuManager = FindFirstObjectByType<MenuManager>();

        if (playerHUD == null) playerHUD = GameObject.Find("PlayerHUD");

        if (cameraCollision == null) cameraCollision = FindFirstObjectByType<CameraCollision>();
    }

    public void SetCastingState(CastState newState)
    {
        if (castingState == newState)
            return;

        castingState = newState;

        Debug.Log("Casting State changed to" + castingState);
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
            bool isTryingToRun = Keyboard.current[runKey].isPressed;
            bool isTryingToWalk = Keyboard.current[walkKey].isPressed;
            bool isMovingForward = moveInput.y > 0f;

            float targetSpeed = jogSpeed;
            if (isTryingToWalk)
                targetSpeed = walkSpeed;
            else if (isTryingToRun && isMovingForward)
                targetSpeed = runningSpeed;

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
            // player gira para o yaw da câmera
            Quaternion targetRotation = Quaternion.Euler(0f, cameraController.yRotation, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 15f);
        }
        else
        {
            // parado: mouse rotaciona player e câmera juntos
            float mouseX = Mouse.current.delta.x.ReadValue()
                        * mouseSensitivity * Time.deltaTime;

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
        float normalizedSpeed = currentSpeed / runningSpeed;
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
        currentHP = data.playerHP;
        Vector3 loadedPosition = new Vector3(data.playerPosition[0], data.playerPosition[1], data.playerPosition[2]);
        float loadedRotation = data.playerRotation;

        playerCharacter.transform.position = loadedPosition;
        playerCharacter.transform.rotation = Quaternion.Euler(0f, loadedRotation, 0f); // Reset rotation
    }

    public void PopulateSaveData(SaveDataContainer data)
    {
        data.playerHP = currentHP;
        data.playerPosition[0] = transform.position.x;
        data.playerPosition[1] = transform.position.y;
        data.playerPosition[2] = transform.position.z;
        data.playerRotation = transform.rotation.eulerAngles.y;
    }

    public void TakeDamage(int damage)
    {
        if (castingState == CastState.Channeling) return;

        currentHP -= damage;
        if (currentHP <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        // gameover logic
    }
}