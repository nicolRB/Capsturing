using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 5f;
    public float runningSpeed = 10f;
    public float currentSpeed;

    [Header("Jump Settings")]
    public float jumpForce = 5f;
    public float coyoteTime = 0.2f;
    public float jumpBufferTime = 0.2f;

    private float coyoteTimer = 0f;
    private float jumpBufferTimer = 0f;

    [Header("Dash")]
    public float dashForce = 12f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;

    private float dashTimer = 0f;
    private float cooldownTimer = 0f;
    private bool isDashing = false;
    private Vector3 dashDirection;

    [Header("Dash Input")]
    public float tapThreshold = 0.2f;
    private float shiftPressedTime = 0f;
    private bool isHoldingShift = false;

    [Header("Mouse Look")]
    public float mouseSensitivity = 15f;

    private Rigidbody rb;
    private CapsuleCollider capsule;
    private Animator animator;

    public bool casting = true;
    public bool moving = false;

    [Header("References")]
    public GameObject castingUI;
    public CastingGameScript castingGameScript;
    public CameraController cameraController;
    public PauseManager pauseManager;

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
        currentSpeed = walkSpeed;
        if (castingUI != null)
        {
            castingUI.SetActive(casting);
        }
    }

    private void Update()
    {
        HandleMovementInput();
        HandleDashInput();
        HandleJumpInput();
        HandleRotation();
        UpdateCooldowns();
        UpdateAnimator();

        // flips casting state when E is pressed
        if (Keyboard.current.eKey.wasPressedThisFrame)        
        {
            casting = !casting;
            Debug.Log($"Casting state toggled: {casting}");
            if (castingUI != null)
            {
                foreach (Transform child in castingUI.transform)
                {
                    if (child.CompareTag("Target"))
                    {
                        Destroy(child.gameObject);
                    }
                }
                castingUI.SetActive(casting);
                castingGameScript.ResetCast();
            }
        }
    }

    private void FixedUpdate()
    {
        HandleJumpPhysics();
    }

    // ---------------- MOVEMENT ----------------
    private void HandleMovementInput()
    {
        Vector2 moveInput = Vector2.zero;

        if (!casting && (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)) moveInput.y += 1f;
        if (!casting && (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)) moveInput.y -= 1f;
        if (!casting && (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)) moveInput.x -= 1f;
        if (!casting && (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)) moveInput.x += 1f;

        moveInput = moveInput.normalized;
        Vector3 movement = transform.forward * moveInput.y + transform.right * moveInput.x;

        if (moveInput.magnitude > 0.1f)
            moving = true;
        else
            moving = false;

        if (!isDashing)
        {
            bool isTryingToRun = Keyboard.current.leftShiftKey.isPressed;
            bool isMovingForward = moveInput.y > 0f;

            float targetSpeed = (isTryingToRun && isMovingForward) ? runningSpeed : walkSpeed;
            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 5f);

            // Use Rigidbody for movement to avoid overriding physics
            rb.MovePosition(rb.position + movement * currentSpeed * Time.deltaTime);
        }
    }

    // ---------------- DASH ----------------
    private void HandleDashInput()
    {
        Vector2 moveInput = Vector2.zero;
        if (!casting && (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)) moveInput.y += 1f;
        if (!casting && (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)) moveInput.y -= 1f;
        if (!casting && (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)) moveInput.x -= 1f;
        if (!casting && (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)) moveInput.x += 1f;

        Vector3 movement = transform.forward * moveInput.y + transform.right * moveInput.x;

        if (!casting && Keyboard.current.leftShiftKey.wasPressedThisFrame)
        {
            shiftPressedTime = 0f;
            isHoldingShift = true;
        }

        if (!casting && isHoldingShift && Keyboard.current.leftShiftKey.isPressed)
        {
            shiftPressedTime += Time.deltaTime;
        }

        if (!casting && isHoldingShift && Keyboard.current.leftShiftKey.wasReleasedThisFrame)
        {
            if (shiftPressedTime <= tapThreshold && cooldownTimer <= 0f && !isDashing)
            {
                dashDirection = movement.sqrMagnitude > 0 ? movement.normalized : transform.forward;
                isDashing = true;
                dashTimer = dashDuration;
                cooldownTimer = dashCooldown;
            }
            isHoldingShift = false;
        }

        if (isDashing)
        {
            float dashProgress = dashTimer / dashDuration;
            rb.MovePosition(rb.position + dashDirection * dashForce * dashProgress * Time.fixedDeltaTime);
            dashTimer -= Time.deltaTime;

            if (dashTimer <= 0f)
                isDashing = false;
        }
    }

    // ---------------- JUMP ----------------
    private void HandleJumpInput()
    {
        if (!casting && Keyboard.current.spaceKey.wasPressedThisFrame)
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
        if (casting || isDashing) return;

        if (moving || Mouse.current.rightButton.isPressed)
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
            transform.Rotate(0f, mouseX, 0f);
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

        if (!casting && (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)) moveInput.y += 1f;
        if (!casting && (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)) moveInput.y -= 1f;
        if (!casting && (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)) moveInput.x -= 1f;
        if (!casting && (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)) moveInput.x += 1f;

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
}