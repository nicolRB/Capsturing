using UnityEngine;

public class PlayerAnimationCoordinator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerController player;

    private Vector3 lastPosition;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        if (player == null)
            player = GetComponentInChildren<PlayerController>();

        lastPosition = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if (animator == null || player == null) return;

        animator.SetBool("IsGrounded", player.IsGrounded());

        // Doesn't alter the value on game paused
        if (Time.deltaTime <= 0f) return;

        Vector3 movement = transform.position - lastPosition;
        Vector3 localVelocity = transform.InverseTransformDirection(movement / Time.deltaTime);
        lastPosition = transform.position;

        animator.SetFloat("VelocityX", localVelocity.x, 0.15f, Time.deltaTime);
        animator.SetFloat("VelocityY", localVelocity.z, 0.25f, Time.deltaTime);
    }

    public void JumpAnimation()
    {
        if (animator != null)
            animator.SetTrigger("Jump");
    }
}
