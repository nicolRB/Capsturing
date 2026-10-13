using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

public class RunicCommands : MonoBehaviour
{
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

    [Header("References")]
    [SerializeField] private PlayerController player;
    [SerializeField] private Runic summonedRunic;

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
            if (dodgeCommandIcon != null)
                dodgeCommandIcon.color = cooldownTint;
        }
        else if (dodgeCommandIcon != null)
            dodgeCommandIcon.color = Color.white;

        if (skillTimer > 0)
        {
            skillTimer -= Time.deltaTime;
            if (skillCommandIcon != null)
                skillCommandIcon.color = cooldownTint;
        }
        else if (skillCommandIcon != null)
            skillCommandIcon.color = Color.white;

        if (Keyboard.current[runicDodgeKey].wasPressedThisFrame && dodgeTimer <= 0)
        {
            dodgeTimer = runicDodgeCooldown;
            CommandRunicDodge();
        }

        int skillCommand = Keyboard.current.digit1Key.wasPressedThisFrame ? 0
            : Keyboard.current.digit2Key.wasPressedThisFrame ? 1
            : Keyboard.current.digit3Key.wasPressedThisFrame ? 2
            : -1;

        if (skillCommand >= 0 && skillTimer <= 0 && summonedRunic != null 
            && summonedRunic.Combat.IsSpecialSkillIndexReady(skillCommand))
        {
            CommandRunicSkill(skillCommand);
        }
    }

    void CommandRunicSkill(int skillNumber)
    {
        Skill skill = summonedRunic.Skills[skillNumber];
        if (skill != null)
        {
            skillTimer = skillCommandCooldown;

            if (summonedRunic.Combat.IsBusy())
            {
                summonedRunic.Combat.QueueUpSkill(summonedRunic.Skills[skillNumber]);
            }
            else
            {
                summonedRunic.Movement.FaceTarget(summonedRunic.transform.position + player.transform.forward);
                summonedRunic.Combat.ReadySkill(summonedRunic.Skills[skillNumber]);
            }
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
        
        RunicMovement movement = runic.GetComponent<RunicMovement>();
        if (movement != null)
        {
            movement.ApplyKnockback(direction * dodgeForce);
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

    public void SetSummonedRunic(Runic runic)
    {
        if (summonedRunic != null)
            summonedRunic = runic; 
    }

    public void RemoveSummonedRunic()
    {
        summonedRunic = null; 
    }
}