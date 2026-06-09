using UnityEngine;
using UnityEngine.InputSystem;

public class BasicFollowerBehavior : MonoBehaviour
{
    public PlayerController player; // referência ao player para acessar a posição do jogador
    public Transform playerPosition; // posição do jogador
    public float followerOffset = 2f; // Offset da posição do seguidor em relação ao jogador
    private int followMode = 1; // 0 = atrás, 1 = lado, 2 = qualquer posição próxima
    public int followSide = 1; // 0 = esquerda, 1 = direita
    public bool following = true; // se o seguidor deve seguir o jogador
    public bool moveToTarget = false; // se o seguidor deve se mover para a posição alvo
    public float followRange = 2f; // distância de seguimento
    public float minFollowDistance = 0.25f; // distância mínima em que para de seguir o jogador
    public float moveSpeed = 15f; // velocidade de movimento do seguidor
    public float timeToTeleport = 5f; // tempo em segundos para teleporte caso o seguidor fique muito longe do jogador
    private float teleportTimer = 0f; // timer para controlar o tempo de teleporte
    public float teleportDistance = 10f; // distância a partir da qual o seguidor irá se teletransportar para o jogador
    private bool teleportTimerStarted = false; // se o timer de teleporte foi iniciado
    public int updateFrequency = 15; // frequência de atualização da posição do seguidor (em frames)
    private int frameCounter = 0; // contador de frames para controlar a frequência de atualização
    private Quaternion lastMovingRotation;

    void Start()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerController>();
        }

        if (playerPosition == null && player != null)
        {
            playerPosition = player.transform;
        }
    }

    // Update is called once per frame
    void Update()
    {
        frameCounter++;
        Vector3 targetPosition;
        Vector3 right = lastMovingRotation * Vector3.right;
        Vector3 forward = lastMovingRotation * Vector3.forward;

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

        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            followSide = -followSide;
        }

        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            followMode = (followMode + 1) % 3; // alterna entre os modos de seguimento
        }

        if (frameCounter >= updateFrequency)
        {
            frameCounter = 0; // resetar o contador de frames

            float targetDistance = CalculateTargetDistance(targetPosition);
            float playerDistance = CalculateTargetDistance(playerPosition.position);

            if (following && targetDistance > followRange)
            {
                moveToTarget = true;
            }

            if (followMode != 2 && targetDistance <= minFollowDistance)
            {
                moveToTarget = false;
            } else if (followMode == 2 && targetDistance <= minFollowDistance * 7.5f)
            {
                moveToTarget = false;
            }

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

        if (moveToTarget)
        {
            // mover apenas na horizontal
            Vector3 moveDirection = (targetPosition - transform.position).normalized;
            moveDirection.y = 0f; // manter a altura atual
            transform.position += moveDirection * moveSpeed * Time.deltaTime;
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
