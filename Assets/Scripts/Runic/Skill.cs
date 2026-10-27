using System.Collections.Generic;
using UnityEngine;

public enum SkillMovementType
{
    None,
    DashToTarget,
    ChargeForward,
    LeapTowardsTarget,
    HighLeap,
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

public enum SkillDamageType 
{ 
    Physical, 
    Magical, 
    True 
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

public static class SkillDisplay
{
    public static string GetShortKeyStatName(SkillKeyStat keyStat)
    {
        return keyStat switch
        {
            SkillKeyStat.Attack => "atk",
            SkillKeyStat.Magic => "mag",
            SkillKeyStat.Defense => "def",
            SkillKeyStat.MagicDefense => "mdef",
            SkillKeyStat.Hp => "hp",
            SkillKeyStat.Speed => "spd",
            SkillKeyStat.None => "var",
            _ => "var"
        };
    }

    public static string GetSkillTypeLabel(Skill skill)
    {
        if (skill == null)
            return "Unknown";

        string typeName = skill.SkillType switch
        {
            SkillAttackType.Melee => "Melee",
            SkillAttackType.Ranged => "Ranged",
            SkillAttackType.Movement => "Movement",
            _ => skill.SkillType.ToString()
        };

        string keyStatName = GetShortKeyStatName(skill.KeyStat);

        if (skill.KeyStat == SkillKeyStat.None)
            return typeName;

        return $"{typeName}({keyStatName})";
    }
}

// Projectiles and VFX
[System.Serializable]
public struct SkillEmission {
    [SerializeField] private GameObject emissionPrefab;
    [SerializeField] private float speed;
    [SerializeField] private bool continuousEmission ;

    [Tooltip("Number of projectiles per shot (ignored if continuousEmission)")]
    [SerializeField, Min(1)] private int amount; // not used if continuousEmission  is true
    [SerializeField] private float multiFireDelay; // not used if continuousEmission is false and amount is 1
    [SerializeField] private Vector3 spreadAngles; // not used if continuousEmission is false and amount is 1
    [SerializeField] private float spreadRandomness; // not used if continuousEmission is false and amount is 1
    [SerializeField] private SkillHitboxType origin; // if it's a blast coming out of the landing hitbox or spit coming out of the bite hitbox

    [Tooltip("If checked, rotates the emitted object towards the target instead of using the prefab rotation.")]
    [SerializeField] private bool rotateToFaceTarget;
    [SerializeField] private Vector3 customRotation;
    [SerializeField] private float emissionScale;

    public GameObject EmissionPrefab => emissionPrefab;
    public float Speed => speed;
    public bool ContinuousEmission => continuousEmission;
    public int Amount => amount;
    public float MultiFireDelay => multiFireDelay;
    public Vector3 SpreadAngles => spreadAngles;
    public float SpreadRandomness => spreadRandomness;
    public SkillHitboxType Origin => origin;
    public bool RotateToFaceTarget => rotateToFaceTarget;
    public Vector3 CustomRotation => customRotation;
    public float EmissionScale => emissionScale; 
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

    [Tooltip("DashToTarget: folga entre o rúnico e a superfície do alvo ao parar")]
    [SerializeField] private float stopDistance;

    [Tooltip("If it is required to recalculate the direction of the target for this step")]
    [SerializeField] private bool reAimAtTarget;
    
    [Header("Step Hitbox Configuration")]
    [Tooltip("What kind of hitbox is generated at the time of the step")]
    [SerializeField] private SkillHitboxType hitboxType;
    
    [Tooltip("The size of the generated hitbox, which is multiplied by the size of the runic")]
    [SerializeField] private float hitboxSize;

    [Tooltip("If the damage coused is constant. Only applies damage once if not.")]
    [SerializeField] private bool continuous;

    [Tooltip("Projectiles and VFX")]
    [SerializeField] private List<SkillEmission> emissions;

    public float Delay => delay;
    public SkillMovementType MovementType => movementType;
    public float Force => force;
    public float StopDistance => stopDistance;
    public bool ReAimAtTarget => reAimAtTarget;
    public SkillHitboxType HitboxType => hitboxType;
    public float HitboxSize => hitboxSize;
    public bool Continuous => continuous;
    public List<SkillEmission> Emissions => emissions;
}

[CreateAssetMenu(fileName = "NewSkill", menuName = "Runic/Skill")]
public class Skill : ScriptableObject
{
    [Header("Basic Info")]
    [SerializeField] private string skillId;
    [SerializeField] private string skillName;
    [SerializeField] private Sprite skillSprite;
    [SerializeField] private SkillAttackType skillType;
    [SerializeField] private Element skillElement;

    [Header("Combat Attributes (Damage/Effect)")]
    [SerializeField] private SkillKeyStat keyStat = SkillKeyStat.Attack;
    [SerializeField] private SkillDamageType damageType = SkillDamageType.Physical;

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
    public Sprite SkillSprite => skillSprite;
    public float Duration => duration;
    public float Cooldown => cooldown;
    public float Range => range;
    public Element SkillElement => skillElement;
    public SkillKeyStat KeyStat => keyStat;
    public float Power => power;
    public float KnockBackValue => knockbackValue;
    public SkillStep[] Steps => steps;
    public SkillAttackType SkillType => skillType;
    public SkillDamageType DamageType => damageType;
}