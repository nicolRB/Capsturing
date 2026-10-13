using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class RunicCommands : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [SerializeField] private Runic summonedRunic;

    [Header("Skill Command")]
    [SerializeField] private Image skillCommandIcon;
    [SerializeField] private TextMeshProUGUI skillTimerText;
    [SerializeField] private float skillCommandCooldown;
    [SerializeField] private float skillTimer;

    [Header("Dodge Command")]
    [SerializeField] private Image dodgeCommandIcon;
    [SerializeField] private TextMeshProUGUI dodgeTimerText;
    [SerializeField] private Key runicDodgeKey = Key.T;
    [SerializeField] private float dodgeForce = 5f;
    [SerializeField] private float runicDodgeCooldown;
    [SerializeField] private float dodgeTimer;

    [Header("Cooldown Visuals")]
    [SerializeField] private Color cooldownTint = new Color(0.25f, 0.25f, 0.25f, 1f);

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        player = FindFirstObjectByType<PlayerController>();    
    }

    // Update is called once per frame
    void Update()
    {
        if (dodgeTimer > 0)
        {
            dodgeTimer -= Time.deltaTime;
            dodgeCommandIcon.color = cooldownTint;
        }
        else
            dodgeCommandIcon.color = Color.white;

        if (skillTimer > 0)
        {
            skillTimer -= Time.deltaTime;
            skillCommandIcon.color = cooldownTint;
        }
        else    
            skillCommandIcon.color = Color.white;

        if (Keyboard.current[runicDodgeKey].wasPressedThisFrame && dodgeTimer <= 0)
        {
            dodgeTimer = runicDodgeCooldown;
            CommandRunicDodge();
        }
    }

    public void CommandRunicDodge()
    {
        Vector3 dodgeDirection = MouseDodgeDirection();

        if (summonedRunic == null)
        {
            Runic[] allRunics = FindObjectsByType<Runic>(FindObjectsSortMode.None);

            foreach (var runic in allRunics)
            {
                if (runic.TameState == RunicState.Tamed)
                    ExecuteRunicDodge(runic, dodgeDirection);
            }
        }
        else
            ExecuteRunicDodge(summonedRunic, dodgeDirection);
    }

    private void ExecuteRunicDodge(Runic runic, Vector3 direction)
    {
        Debug.Log(runic.name + " dodging in the " + direction + " direction.");
        
        // Exemplo de como aplicar o movimento usando o RunicMovement (se acessível)
        // Ou você pode chamar uma função pública de esquiva dentro do script Runic / RunicMovement.
        RunicMovement movement = runic.GetComponent<RunicMovement>();
        if (movement != null)
        {
            // Força o rúnico a olhar para a direção da esquiva e aplica o impulso
            movement.FaceTarget(runic.transform.position + direction);
            movement.ApplyKnockback(direction * dodgeForce); // Reaproveitando a lógica de impulso que você já tem!
        }
    }

    private Vector3 MouseDodgeDirection()
    {
        // Grab the mouse delta at the moment of the command.
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        // If the mouse barely moves, apply a standard (or random) dodge to the player's back.
        if (mouseDelta.sqrMagnitude < 10f)
        {
            return -player.transform.forward;
        }

        // Find out if the predominant movement was horizontal (X) or vertical (Y).
        if (Mathf.Abs(mouseDelta.x) > Mathf.Abs(mouseDelta.y))
        {
            // Horizontal movement
            if (mouseDelta.x > 0)
            {
                // Right -> Right relative to the player
                return player.transform.right;
            }
            else
            {
                // Left -> Left relative to the player
                return -player.transform.right;
            }
        }
        else
        {
            // Vertical Movement
            if (mouseDelta.y > 0)
            {
                // Up -> Away from the player
                return -player.transform.forward;
            }
            else
            {
                // Downw -> Towards the player (close in/cover)
                return player.transform.forward;
            }
        }
    }
}