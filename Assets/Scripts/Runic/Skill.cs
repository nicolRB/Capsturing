using UnityEngine;

public enum SkillMovementType
{
    None,
    DashForward,
    LeapTowardsTarget,
    TeleportToTarget
}

public enum SkillKeyStat
{
    Attack,
    Magic,
    Defense,
    MagicDefense,
    Hp,
    Speed,
    None
}

public enum SkillHitboxType
{
    None,
    MeleeGenericStrike,
    MeleeGenericSlam,
    MeleeGenericSweep,
    MeleeGenericUppercut,
    MeleeClaw,
    MeleeBite,
    MeleeTail,
    Charge,
    JumpImpactAOE
}

public enum SkillAttackType
{
    Melee,
    Movement,
    Ranged
}

[System.Serializable]
public struct SkillStep
{
    [Tooltip("Time in seconds relative to the start of the start of the skill in which this step may happen")]
    [SerializeField] private float delay; 
    
    [Tooltip("Specific type of movement for this step")]
    [SerializeField] private SkillMovementType movementType;
    
    [Tooltip("Force or speed applied by this step")]
    [SerializeField] private float force;

    [Tooltip("If it is required to recalculate the direction of the target for this step")]
    [SerializeField] private bool reAimAtTarget;
    
    [Header("Step Hitbox Configuration")]
    [Tooltip("What kind of hitbox is generated at the time of the step")]
    [SerializeField] private SkillHitboxType hitboxType;
    
    [Tooltip("The size of the generated hitbox, which is multiplied by the size of the runic")]
    [SerializeField] private float hitboxSize;

    [Tooltip("If the damage coused is constant. Only applies damage once if not.")]
    [SerializeField] private bool continuous;

    public float Delay => delay;
    public SkillMovementType MovementType => movementType;
    public float Force => force;
    public bool ReAimAtTarget => reAimAtTarget;
    public SkillHitboxType HitboxType => hitboxType;
    public float HitboxSize => hitboxSize;
    public bool Continuous => continuous;
}

[CreateAssetMenu(fileName = "NewSkill", menuName = "Runic/Skill")]
public class Skill : ScriptableObject
{
    [Header("Basic Info")]
    [SerializeField] private string skillId;
    [SerializeField] private string skillName;
    [SerializeField] private SkillAttackType skillType;
    [SerializeField] private Element skillElement;

    [Header("Combat Attributes (Damage/Effect)")]
    [SerializeField] private SkillKeyStat keyStat = SkillKeyStat.Attack;
    [Tooltip("Base power of the skill multiplied by the runic's stats in the damage formula")]
    [SerializeField] private float power = 100f;
    [SerializeField] private float knockbackValue = 2f;
    
    [SerializeField] private float duration = 1f;
    [SerializeField] private float cooldown = 5f;
    [SerializeField] private float range = 1f;
    
    [Header("Movement Sequence / Actions")]
    [SerializeField] private SkillStep[] steps;

    public string SkillId => skillId;
    public string SkillName => skillName;
    public float Duration => duration;
    public float Cooldown => cooldown;
    public float Range => range;
    public Element SkillElement => skillElement;
    public SkillKeyStat KeyStat => keyStat;
    public float Power => power;
    public float KnockBackValue => knockbackValue;
    public SkillStep[] Steps => steps;
    public SkillAttackType SkillType => skillType;
}