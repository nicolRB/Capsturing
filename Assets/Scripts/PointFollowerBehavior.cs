using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.AI;

public class TargetFollowerBehavior : MonoBehaviour
{
    [Header("References")]
    public PlayerController player;
    public Transform playerPosition;
    public PointTargetScript pointer;
    private NavMeshAgent agent;
    private GameObject point;
    public PlayerInteractionScript playerInteraction;

    [Header("Follow Behavior")]
    public float followerOffset = 2f;
    private int followMode = 1; // 0 = atrás, 1 = lado, 2 = qualquer posição próxima
    public int followSide = 1; // 0 = esquerda, 1 = direita
    public bool following = true;
    public float followRange = 0.75f;
    public float stopRange = 2.5f;

    [Header("Teleport Settings")]
    public float timeToTeleport = 5f;
    private float teleportTimer = 0f;
    public float teleportDistance = 10f;
    private bool teleportTimerStarted = false;

    [Header("Update Settings")]
    public int updateFrequency = 15;
    private int frameCounter = 0;
    private Quaternion lastMovingRotation;

    void Start()
    {
        if (player == null) player = FindFirstObjectByType<PlayerController>();

        if (playerPosition == null && player != null) playerPosition = player.transform;

        if (pointer == null) pointer = FindFirstObjectByType<PointTargetScript>();

        agent = GetComponent<NavMeshAgent>();
    }

    // Update is called once per frame
    void Update()
    {
        frameCounter++;
        Vector3 targetPosition;
        Vector3 right = lastMovingRotation * Vector3.right;
        Vector3 forward = lastMovingRotation * Vector3.forward;

        if (!pointer.followPoint)
        {
            switch (followMode)
            {
                case 0: // Atrás
                    targetPosition = playerPosition.position - forward * followerOffset;
                    break;
                case 1: // Lado
                    targetPosition = playerPosition.position + right * followSide * followerOffset;
                    break;
                case 2: // Qualquer posição próxima
                    targetPosition = playerPosition.position;
                    break;
                default:
                    targetPosition = playerPosition.position;
                    break;
            }            
        }
        else
        {
            targetPosition = pointer.indicatedPosition;
        }

        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            followSide = -followSide;
        }
        
        bool interactableActive = playerInteraction != null && playerInteraction.interactableTarget != null;

        if (!interactableActive && Keyboard.current.fKey.wasPressedThisFrame 
            && (!pointer.followPoint && player.castState != PlayerController.CastState.Channeling 
            || player.castState != PlayerController.CastState.Casting))
        {
            followMode = (followMode + 1) % 3;
        }

        if (frameCounter >= updateFrequency)
        {
            frameCounter = 0; // resetar o contador de frames

            float targetDistance = CalculateTargetDistance(targetPosition);
            float playerDistance = CalculateTargetDistance(playerPosition.position);

            if (following && targetDistance > followRange)
            {
                agent.SetDestination(targetPosition);
            }

            if (followMode == 2) agent.stoppingDistance = stopRange;
            else agent.stoppingDistance = 0f;

            if (playerDistance > teleportDistance)
            {
                if (!teleportTimerStarted)
                {
                    teleportTimerStarted = true;
                    teleportTimer = Time.time;
                }

                float elapsed = Time.time - teleportTimer;

                if (elapsed >= timeToTeleport && playerDistance > teleportDistance)
                {
                    TeleportToTarget(playerPosition.position - forward * 5f);
                }
            } 
            else
            {
                teleportTimerStarted = false;
            }
            
            // atualiza a rotação de referência só quando o player se mover
            if (player.moving)
            {
                lastMovingRotation = player.transform.rotation;
            }
        }
    }

    private float CalculateTargetDistance(Vector3 targetPosition)
    {
        float targetDistance = Vector3.Distance(transform.position, targetPosition);

        return targetDistance;
    }

    private void TeleportToTarget(Vector3 targetPosition)
    {
        transform.position = targetPosition;
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
        }
    }
}
