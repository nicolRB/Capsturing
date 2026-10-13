using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.AI;
using System.Collections.Generic;

public class RunicMovement : MonoBehaviour
{
    private Runic runic;
    private RunicCombat runicCombat;
    private Quaternion lastMovingRotation;
    private Vector3 faceTargetPosition;
    private bool hasFaceTarget;

    [Header("Follow Behavior")]
    [SerializeField] private float followerOffset = 2f;
    [Tooltip("Follow position mode: 0 = behind, 1 = side, 2 = ahead, 3 = any nearby position.")]
    [SerializeField] private int followMode = 1;
    [SerializeField] private int followSide = 1;
    [SerializeField] private float followRange = 0.75f;
    [SerializeField] private float unboundFollowDistance = 2.5f;
    [Tooltip("Distance kept from the player in follow mode 3")]
    [SerializeField] private float maxProximity = 0.7f;
    [SerializeField] private float stopRange = 0.5f;
    [SerializeField] private float leadDistance = 4.5f;
    
    [Header("Update Settings")]
    [SerializeField] private float updateInterval = 0.2f; // Updates five times per second
    [SerializeField] private float faceTurnSpeed = 540f;
    private float updateTimer = 0f;

    [Header("Teleport Settings")]
    [SerializeField] private float timeToTeleport = 5f;
    private float teleportTimer = 0f;
    [SerializeField] private float teleportDistance = 10f;
    private bool teleportTimerStarted = false;

    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private float chaseSideOffset = 2f;
    private Transform chaseTarget;
    private int chaseSide;
    private bool leapInProgress;
    private bool leapEndRequested;
    private bool leapHasAscended;
    private bool leapRequiresAscent;
    private bool wasKinematicBeforeLeap;
    private bool agentWasEnabledBeforeLeap;
    private float leapStartedAt;
    private readonly HashSet<Collider> leapGroundContacts = new HashSet<Collider>();

    private float speed = 5f;
    private float walkSpeed = 2.5f;
    private float sprintSpeed = 9f;

    public bool Leaping => leapInProgress;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        runic = GetComponent<Runic>();
        runicCombat = GetComponent<RunicCombat>();
        agent = GetComponent<NavMeshAgent>();

        speed = runic.Speed;
        walkSpeed = runic.WalkSpeed;
        sprintSpeed = runic.SprintSpeed;

        if (runic.Player != null)
            lastMovingRotation = runic.Player.transform.rotation;
    }

    void Update()
    {
        UpdateFacing();

        if (Keyboard.current.qKey.wasPressedThisFrame)
            followSide = -followSide;
        
        bool interactableActive = runic.PlayerInteraction != null && runic.PlayerInteraction.InteractableTarget != null;

        bool isNotCastingOrChanneling = runic.Player.CastingState != PlayerController.CastState.Channeling 
                                && runic.Player.CastingState != PlayerController.CastState.Casting;

        if (!interactableActive && Keyboard.current.fKey.wasPressedThisFrame 
            && !runic.Pointer.FollowPoint && isNotCastingOrChanneling)
        {
            followMode = (followMode + 1) % 4;
        }

        updateTimer += Time.deltaTime;

        if (runic.TameState == RunicState.Tamed) 
        {
            // Only does logic in regular intervals
            if (updateTimer >= updateInterval)
            {
                updateTimer = 0f;
                if (runic.ActState == BehaviorState.Following || runic.ActState == BehaviorState.CommandedMove)
                    TameMovement();
            }
        }
    }

    void FixedUpdate()
    {
        if (leapInProgress && GetComponent<Rigidbody>().linearVelocity.y > 0.1f)
            leapHasAscended = true;
    }

    void OnCollisionEnter(Collision collision)
    {
        TrackLeapGroundContact(collision);
    }

    void OnCollisionStay(Collision collision)
    {
        TrackLeapGroundContact(collision);
    }

    void OnCollisionExit(Collision collision)
    {
        leapGroundContacts.Remove(collision.collider);
    }

    private void TrackLeapGroundContact(Collision collision)
    {
        if (!leapInProgress || (leapRequiresAscent && !leapHasAscended))
            return;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null || rb.linearVelocity.y > 0.1f)
            return;

        for (int i = 0; i < collision.contactCount; i++)
        {
            if (collision.GetContact(i).normal.y > 0.5f)
            {
                leapGroundContacts.Add(collision.collider);
                TryCompleteLeap();
                return;
            }
        }
    }

    public void TameMovement()
    {
        agent.updateRotation = true;
        hasFaceTarget = false;
        Vector3 targetPosition;
        Vector3 right = lastMovingRotation * Vector3.right;
        Vector3 forward = lastMovingRotation * Vector3.forward;

        if (runic.ActState == BehaviorState.CommandedMove)
        {
            SetMovementSpeed(speed);
            HandleCommandedMovement(runic.Pointer.GroundIndicator.transform.position);
            return;
        }

        if (runic.Player.IsTryingToWalk)
        {
            SetMovementSpeed(Mathf.Min(walkSpeed, runic.Player.WalkSpeed));
        }
        else if (runic.Player.IsTryingToSprint)
        {
            SetMovementSpeed(Mathf.Min(sprintSpeed, runic.Player.SprintSpeed));
        }
        else 
            SetMovementSpeed(Mathf.Min(speed, runic.Player.JogSpeed));

        if (!runic.Pointer.FollowPoint)
        {
            switch (followMode)
            {
                case 0: // Behind
                    targetPosition = runic.Player.transform.position - forward * followerOffset;
                    break;
                case 1: // To the side
                    targetPosition =runic.Player.transform.position + right * followSide * followerOffset;
                    break;
                case 2: // Ahead
                    targetPosition =runic.Player.transform.position + (forward * leadDistance) + (right * followSide * followerOffset);
                    break;
                case 3: // Nearby in any direction
                    Vector3 toRunic = transform.position - runic.Player.transform.position;
                    toRunic.y = 0; // remains on the horizontal plane
                    if (toRunic.sqrMagnitude < 0.01f)
                    {
                        toRunic = -runic.Player.transform.forward; // avoids dividing by zero
                    }
                    targetPosition = runic.Player.transform.position + toRunic.normalized * unboundFollowDistance;
                    break;
                default:
                    targetPosition =runic.Player.transform.position;
                    break;
            }            
        }
        else 
        targetPosition = runic.Pointer.IndicatedPosition;

        updateTimer = 0; // resets the frame counter

        float targetDistance = CalculateTargetDistance(targetPosition);
        float playerDistance = CalculateTargetDistance(runic.Player.transform.position);

        if (runic.ActState == BehaviorState.Following && targetDistance > followRange)
            TrySetDestination(targetPosition);

        if (followMode == 3 && runic.ActState == BehaviorState.Following && playerDistance < followRange) agent.stoppingDistance = maxProximity;
        else agent.stoppingDistance = stopRange;

        if (playerDistance > teleportDistance && runic.ActState == BehaviorState.Following)
        {
            if (!teleportTimerStarted)
            {
                teleportTimerStarted = true;
                teleportTimer = Time.time;
            }

            float elapsed = Time.time - teleportTimer;

            if (elapsed >= timeToTeleport && playerDistance > teleportDistance)
            {
                TeleportToTarget(runic.Player.transform.position - forward * 5f);
            }
        } 
        else
            teleportTimerStarted = false;
        
        // updates the reference rotation only when the player is moving
        if (runic.Player.Moving)
            lastMovingRotation = runic.Player.transform.rotation;
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

    private bool TrySetDestination(Vector3 targetPosition, float sampleRadius = 1.5f)
    {
        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
            return false;

        float searchRadius = Mathf.Max(sampleRadius, 0.01f);
        if (!NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, searchRadius, agent.areaMask))
        {
            agent.ResetPath();
            return false;
        }

        if (agent.SetDestination(hit.position))
            return true;

        agent.ResetPath();
        return false;
    }

    public void MoveToPosition(Vector3 targetPosition)
    {
        TrySetDestination(targetPosition);
    }

    public void Leap(Vector3 targetPosition, float force)
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (!BeginPhysicsMotion(rb, true))
            return;

        // Calculate the horizontal direction to the target.
        Vector3 direction = (targetPosition - transform.position);
        direction.y = 0;
        direction.Normalize();

        // Combine forward movement and vertical lift into the jump velocity.
        Vector3 jumpVelocity = (direction * force) + (Vector3.up * (force * 0.8f));

        // Apply velocity using the Unity 6 API.
        rb.linearVelocity = jumpVelocity;
    }

    public void ApplyKnockback(Vector3 impulse)
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null || impulse.sqrMagnitude <= 0f)
            return;

        if (!leapInProgress)
        {
            if (!BeginPhysicsMotion(rb, false))
                return;

            leapEndRequested = true;
            leapHasAscended = true;
        }

        if (impulse.y > 0f && rb.linearVelocity.y < 0f)
        {
            Vector3 velocity = rb.linearVelocity;
            velocity.y = 0f;
            rb.linearVelocity = velocity;
        }

        rb.AddForce(impulse, ForceMode.Impulse);
    }

    private bool BeginPhysicsMotion(Rigidbody rb, bool requiresAscent)
    {
        if (rb == null || leapInProgress)
            return false;

        wasKinematicBeforeLeap = rb.isKinematic;
        agentWasEnabledBeforeLeap = agent != null && agent.enabled;

        if (agentWasEnabledBeforeLeap)
        {
            agent.ResetPath();
            agent.enabled = false;
        }

        leapInProgress = true;
        leapEndRequested = false;
        leapHasAscended = false;
        leapRequiresAscent = requiresAscent;
        leapStartedAt = Time.time;
        leapGroundContacts.Clear();
        rb.isKinematic = false;
        return true;
    }

    // Re-enable the NavMeshAgent after landing.
    public void EndLeap()
    {
        if (!leapInProgress)
            return;

        leapEndRequested = true;
        TryCompleteLeap();
    }

    private void TryCompleteLeap()
    {
        if (!leapInProgress || !leapEndRequested || (leapRequiresAscent && !leapHasAscended)
            || leapGroundContacts.Count == 0 || Time.time - leapStartedAt < 0.08f)
            return;

        NavMeshHit landingHit = default;
        bool hasNavMeshLanding = agent != null && agentWasEnabledBeforeLeap
            && NavMesh.SamplePosition(transform.position, out landingHit, Mathf.Max(agent.height, 0.5f), agent.areaMask);

        if (agentWasEnabledBeforeLeap && !hasNavMeshLanding)
            return;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = wasKinematicBeforeLeap;
        }

        if (agent != null && agentWasEnabledBeforeLeap)
        {
            agent.enabled = true;
            agent.Warp(landingHit.position);
        }

        leapInProgress = false;
        leapEndRequested = false;
        leapHasAscended = false;
        leapRequiresAscent = false;
        leapGroundContacts.Clear();
    }

    public void HandleCombatMovement(Transform targetTransform, CombatStyle style)
    {
        SetMovementSpeed(sprintSpeed);

        agent.updateRotation = false;
        if (targetTransform == null) return;

        Vector3 currentPos = transform.position;
        Vector3 targetPos = targetTransform.position;
        Vector3 directionToTarget = targetPos - currentPos;
            directionToTarget.y = 0;
        float dist = directionToTarget.magnitude;

        Vector3 destination = currentPos;

        switch (style)
        {
            case CombatStyle.FaceOn:
                // Maintain a short distance (e.g., between 1.5 and 2.5 units).
                float idealFaceOnDist = 3.5f;
                if (dist < idealFaceOnDist - 0.5f)
                {
                    // Backs off a little if too close.
                    destination = targetPos - (directionToTarget.normalized * idealFaceOnDist);
                }
                else if (dist > idealFaceOnDist + 0.75f)
                {
                    // Moves closer if it's outside the ideal distance.
                    destination = targetPos;
                }
                else
                {
                    SetMovementSpeed(walkSpeed);

                    Vector3 awayFromTarget = -directionToTarget;
                    if (awayFromTarget.sqrMagnitude < 0.01f)
                        awayFromTarget = transform.forward;

                    float currentAngle = Mathf.Atan2(awayFromTarget.x, awayFromTarget.z) * Mathf.Rad2Deg;
                    float angleOffset = Random.Range(-30f, 30f);
                    float newAngle = currentAngle + angleOffset;

                    float randomFaceOnDist = Random.Range(idealFaceOnDist - 0.5f, 
                        idealFaceOnDist + 0.5f);
                    
                    destination = targetPos + Quaternion.Euler(0f, newAngle, 0f) 
                        * Vector3.forward * randomFaceOnDist;
                }

                runicCombat.SetMovementTimer(0.7f);

                break;

            case CombatStyle.Skittish:
                // Maintain a greater distance (e.g., between 4 and 7 units) and perform small, random bursts.
                float minSkittishTargetDist = 4f;
                float maxSkittishTargetDist = 7f;
                float minSkitterDistance = 2.5f;
                float MaxSkitterDistance = 4f;

                float randomSkittishDist = Random.Range(minSkittishTargetDist, maxSkittishTargetDist);

                if (dist < minSkittishTargetDist)
                {
                    destination = targetPos - (directionToTarget.normalized * randomSkittishDist);
                }
                else if (dist > maxSkittishTargetDist)
                {
                    destination = targetPos;
                }
                else
                {
                    SetMovementSpeed(sprintSpeed);

                    if (agent != null && agent.enabled && agent.isOnNavMesh)
                    {
                        NavMeshPath path = new NavMeshPath();
                        for (int attempt = 0; attempt < 12; attempt++)
                        {
                            float randomAngle = Random.Range(0f, 360f);
                            float randSkitterDist = Random.Range(minSkitterDistance, MaxSkitterDistance);
                            Vector3 offset = Quaternion.Euler(0f, randomAngle, 0f)
                                * Vector3.forward * randSkitterDist;
                            Vector3 candidate = currentPos + offset;

                            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2f, agent.areaMask))
                                continue;

                            float sampledDistance = Vector3.Distance(hit.position, targetPos);
                            if (sampledDistance < minSkittishTargetDist || sampledDistance > maxSkittishTargetDist)
                                continue;

                            if (agent.CalculatePath(hit.position, path) && path.status == NavMeshPathStatus.PathComplete)
                            {
                                destination = hit.position;
                                break;
                            }
                        }
                    }
                }
                
                runicCombat.SetMovementTimer(1.25f);

                break;
        }

        TrySetDestination(destination, 2f);
    }

    public void FaceTarget(Vector3 targetPosition)
    {
        Vector3 lookDirection = targetPosition - transform.position;
        lookDirection.y = 0f;
        if (lookDirection.sqrMagnitude < 0.001f)
            return;

        faceTargetPosition = targetPosition;
        hasFaceTarget = true;
        if (agent != null)
            agent.updateRotation = false;
    }

    private void UpdateFacing()
    {
        if (!hasFaceTarget)
            return;

        Vector3 lookDirection = faceTargetPosition - transform.position;
        lookDirection.y = 0f;
        if (lookDirection.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            faceTurnSpeed * Time.deltaTime);

        if (Quaternion.Angle(transform.rotation, targetRotation) < 0.1f)
            hasFaceTarget = false;
    }

    public void HandleCommandedMovement(Vector3 PointedPos)
    {
        hasFaceTarget = false;
        if (agent != null)
            agent.updateRotation = true;

        switch (runic.CommandedMovementMode)
        {
            case CommandedMoveMode.Stand:
                TrySetDestination(PointedPos);
                break;
            
            case CommandedMoveMode.Scout:
                break;

            case CommandedMoveMode.Patrol:
                break;

            default:
                break;
        }        
    }

    public void Wander(float maxWanderDistance = 10)
    {
        hasFaceTarget = false;
        if (agent != null)
            agent.updateRotation = true;

        SetMovementSpeed(walkSpeed);

        float randomAngle = Random.Range(0, 360);
        float randomDistance = Random.Range(1f, maxWanderDistance);

        Vector3 offset = Quaternion.Euler(0f, randomAngle, 0f)
            * Vector3.forward * randomDistance;
        Vector3 wanderPosition = transform.position + offset;

        TrySetDestination(wanderPosition, maxWanderDistance);
    }

    public void Patrol(Vector3 territoryPos, float territoryRadius, float territoryThreatRadius = 0)
    {
        hasFaceTarget = false;
        if (agent != null)
            agent.updateRotation = true;

        SetMovementSpeed(walkSpeed);

        float randomAngle = Random.Range(0f, 360f);
        float randomDistance = Random.Range(0f, territoryRadius + territoryThreatRadius);

        Vector3 offset = Quaternion.Euler(0f, randomAngle, 0f)
            * Vector3.forward * randomDistance;
        Vector3 patrolPosition = territoryPos + offset;

        TrySetDestination(patrolPosition, territoryRadius);
    }

    public void Flee(Vector3 threatPosition, float fleeDistance = 15f)
    {
        hasFaceTarget = false;
        if (agent != null)
            agent.updateRotation = true;

        SetMovementSpeed(sprintSpeed);
        
        Vector3 awayDir = transform.position - threatPosition;
        awayDir.y = 0f;
        awayDir.Normalize();

        if (awayDir.sqrMagnitude < 0.001f)
        {
            awayDir = transform.forward;
            awayDir.y = 0f;

            if (awayDir.sqrMagnitude < 0.001f)
                awayDir = Vector3.forward;
            else
                awayDir.Normalize();
        }

        // Adds a random variation of up to 45 degrees to the left or right.
        float randomAngle = Random.Range(-45f, 45f);
        Vector3 deviatedDir = Quaternion.Euler(0f, randomAngle, 0f) * awayDir;

        Vector3 targetPosition = transform.position + (deviatedDir * fleeDistance);

        TrySetDestination(targetPosition, fleeDistance);
    }

    public void Chase(Transform targetTransform)
    {
        if (targetTransform == null || agent == null || !agent.enabled || !agent.isOnNavMesh)
            return;

        hasFaceTarget = false;
        agent.updateRotation = true;
        SetMovementSpeed(sprintSpeed);

        Vector3 targetPos = targetTransform.position;
        float targetDist = Vector3.Distance(targetPos, transform.position);
        if (targetDist >= 3f)
        {
            chaseTarget = null;
            chaseSide = 0;
            
            TrySetDestination(targetPos);
        }
        else 
        {
            if (chaseTarget != targetTransform || chaseSide == 0)
            {
                chaseTarget = targetTransform;
                chaseSide = Random.Range(0, 2) == 0 ? -1 : 1;
            }

            Vector3 targetRight = targetTransform.right;
            targetRight.y = 0f;
            if (targetRight.sqrMagnitude < 0.001f)
            {
                Vector3 directionToTarget = targetPos - transform.position;
                directionToTarget.y = 0f;
                targetRight = Vector3.Cross(Vector3.up, directionToTarget);
            }

            Vector3 flankPosition = targetPos + targetRight.normalized * chaseSide * chaseSideOffset;
            if (!TrySetDestination(flankPosition))
                TrySetDestination(targetPos);
        }
    }

    public void MoveToCuriosity(Vector3 curiosityPos)
    {
        Vector3 toRunic = transform.position - curiosityPos;
        toRunic.y = 0; // remains on the horizontal plane
        if (toRunic.sqrMagnitude < 0.01f)
        {
            toRunic = -runic.Player.transform.forward; // avoids dividing by zero
        }
        Vector3 targetPosition = curiosityPos + toRunic.normalized * unboundFollowDistance;
        MoveToPosition(targetPosition);
    }

    public void SetMovementSpeed(float speed)
    {
        if (agent != null)
            agent.speed = speed;
    }
}
