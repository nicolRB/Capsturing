using UnityEngine;
using System.Collections.Generic;

public enum RunicState { Wild, Tamed, Dead }

public enum BehaviorState { 
    Idle, 
    Wandering,
    Investigating,
    Eating,
    Scouting, 
    Patrolling, 
    Following, 
    CommandedMove, 
    CommandedAttack, 
    Threatening, 
    Fleeing, 
    Hunting, 
    Fighting 
}

public enum WildNature { 
    Neutral, 
    Fearful, 
    FearfulFriendly, 
    FearfulNeutral, 
    FearfulProtective, 
    FearfulNeutralProtective, 
    FearfulFriendlyProtective, 
    Friendly, 
    Territorial, 
    Hostile 
}

public enum CombatStyle { Skittish, Ranger, FaceOn, Brutish, Regal }
public enum CommandedMoveMode { Stand, Scout, Patrol }
public enum CombatMode { Passive, Escort, Aggressive }

public class Runic : MonoBehaviour
{
    [Header("Tame State")]
    [SerializeField] private RunicState tameState = RunicState.Wild;

    [Header("Species Template")]
    [SerializeField] private RunicSpecies species;

    [Header("Visuals")]
    [SerializeField] private Sprite runicIcon;
    [SerializeField] private GameObject runicModel;
    [SerializeField] private int modelIndex;

    [Header("Runtime Stats")]
    [SerializeField] private string runicInstanceId;
    [SerializeField] private string nickname;
    [SerializeField] private int level = 1;
    [SerializeField] private int experience;
    [SerializeField] private int currentHP = 10;
    [SerializeField] private int maxHP = 10;
    [SerializeField] private float attack = 1;
    [SerializeField] private float defense = 1;
    [SerializeField] private float weight = 1;
    [SerializeField] private float knockbackResistance;
    [SerializeField, Range(0f, 1f)] private float knockbackUpwardBias = 0.75f;
    [SerializeField] private float speed = 4;
    [SerializeField] private float walkSpeed = 2;
    [SerializeField] private float sprintSpeed = 6;
    [SerializeField] private float magic = 1;
    [SerializeField] private float magicDefense = 1;
    [SerializeField] private List<Element> elements;
    [SerializeField] private List<Skill> basicSkills;
    [SerializeField] private List<Skill> skills;

    [Header("Unique Features (Doesn't Rely On Species)")]
    [SerializeField] private string uniqueSpritePath;
    [SerializeField] private string uniqueModelAssetPath;

    [Header("Capture Settings (Wild Only)")]
    [SerializeField] private bool capturable = true;
    [SerializeField] private TargetMapAsset captureMap;
    [SerializeField] private float targetPerfectWeight = 2;
    [SerializeField] private float targetGoodWeight = 1;
    [SerializeField] private float targetMissWeight = 0;
    [Tooltip("External additive capture-chance modifier for difficulty, items, or situational effects.")]
    [SerializeField] private float CCModifier = 0;
    [Tooltip("External multiplicative capture-chance modifier for difficulty, items, or situational effects.")]
    [SerializeField] private float CCMultiplier = 0;

    [Header("Capture Bonuses")]
    [SerializeField] private float perfectBonus = 0;
    [SerializeField] private float perfectMultiplier = 0;
    [SerializeField] private float goodBonus = 0;
    [SerializeField] private float goodMultiplier = 0;
    [SerializeField] private float missBonus = 0;
    [SerializeField] private float missMultiplier = 0;

    [Header("Behavior")]
    [SerializeField] private bool frozen;
    [SerializeField] private WildNature nature = WildNature.Neutral;
    [SerializeField] private BehaviorState actState = BehaviorState.Idle;
    [SerializeField] private CommandedMoveMode commandedMovementMode = CommandedMoveMode.Stand;
    [SerializeField] private CombatMode combatCommand = CombatMode.Escort; 
    
    [Header("Chains")]
    [SerializeField] private GameObject captureChainsPrefab;
    private List<GameObject> activeChains = new List<GameObject>();
    [SerializeField] private float chainYAngleMinVariance = 60f;
    [SerializeField] private float chainYAngleMaxVariance = 60f;
    [SerializeField] private float chainMinZAngle = 20f;
    [SerializeField] private float chainMaxZAngle = 95f;
    [SerializeField] private int minChainNumber = 6;
    [SerializeField] private int maxChainNumber = 8;
    [SerializeField] private float chainSpawnInterval = 0f;

    [Header("Other VFX")]
    [SerializeField] private GameObject damagePopupPrefab;

    [Header("Sizes")]
    [SerializeField] private float width = 1f;
    [SerializeField] private float height = 1f;

    public float Width => width;
    public float Height => height;

    [Header("References")]
    [SerializeField] private PlayerController player;
    [SerializeField] private PointTargetScript pointer;
    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private RunicDatabase runicDatabase;
    [SerializeField] private RunicStorageManager runicStorageManager;
    [SerializeField] private HPBar hpBar;
    private RunicCombat combat;
    private RunicMovement movement;
    private RunicWildAI runicWildAI;
    private Rigidbody rb;
    
    [Header("Combat Behavior")]
    [SerializeField] private CombatStyle combatStyle;
    [SerializeField] private float recovery = 2;
    [SerializeField] private float specialRecovery = 10;
    [SerializeField] private float aggression = 0.5f;
    [SerializeField] private float specialAggression = 0.2f;
    [SerializeField] private float iframes = 0.2f;
    [SerializeField] private float invencibilityTime;

    [Header("Relations")]
    [SerializeField] private List<Runic> allies = new List<Runic>();
    [SerializeField] private List<Runic> engagedEnemies = new List<Runic>();
    [SerializeField] private List<Runic> threats = new List<Runic>();

    private bool chained;

    private bool movementPauseApplied;

    public bool PlayerChanneling => player != null && player.CastingState == CastState.Channeling;
    public bool Chained => chained;
    public bool IsPaused => frozen || PlayerChanneling || chained;

    public RunicState TameState => tameState;
    public WildNature Nature => nature;
    public BehaviorState ActState => actState;
    public CommandedMoveMode CommandedMovementMode => commandedMovementMode;
    public CombatMode CombatCommand => combatCommand;
    public RunicSpecies Species => species;
    public string RunicInstanceId => runicInstanceId;
    public Sprite RunicIcon => runicIcon;
    public GameObject RunicModel => runicModel;
    public HPBar HealthBar => hpBar;
    public float CurrentHP => currentHP;
    public float MaxHP => maxHP;
    public float Attack => attack;
    public float Defense => defense;
    public float Weight => weight;
    public float KnockbackResistance => knockbackResistance;
    public float Speed => speed;
    public float WalkSpeed => walkSpeed;
    public float SprintSpeed => sprintSpeed;
    public float Magic => magic;
    public float MagicDefense => magicDefense;
    public float Recovery => recovery;
    public float SpecialRecovery => specialRecovery;
    public float Aggression => aggression;
    public float SpecialAggression => specialAggression;
    public bool Capturable => capturable;
    public TargetMapAsset CaptureMap => captureMap;
    public float TargetPerfectWeight => targetPerfectWeight;
    public float TargetGoodWeight => targetGoodWeight;
    public float TargetMissWeight => targetMissWeight;
    public float CaptureChanceModifier => CCModifier;
    public float CaptureChanceMultiplier => CCMultiplier;
    public float PerfectBonus => perfectBonus;
    public float PerfectMultiplier => perfectMultiplier;
    public float GoodBonus => goodBonus;
    public float GoodMultiplier => goodMultiplier;
    public float MissBonus => missBonus;
    public float MissMultiplier => missMultiplier;
    public bool IsFrozen => frozen;
    public RunicDatabase RunicDatabase => runicDatabase;
    public PointTargetScript Pointer => pointer;
    public PlayerController Player => player;
    public PlayerInteraction PlayerInteraction => playerInteraction;
    public List<Skill> BasicSkills => basicSkills;
    public List<Skill> Skills => skills;
    public float InvencibilityTime => invencibilityTime;
    public List<Runic> Allies => allies;
    public List<Runic> EngagedEnemies => engagedEnemies;
    public List<Runic> Threats => threats;
    public RunicCombat Combat => combat;
    public RunicMovement Movement => movement;
    public Rigidbody Rb => rb;
    public List<Element> Elements => elements;

    public void PruneDestroyedRelations()
    {
        allies?.RemoveAll(ally => ally == null);
        engagedEnemies?.RemoveAll(enemy => enemy == null);
        threats?.RemoveAll(threat => threat == null);
    }

    public void SetRunicDatabase(RunicDatabase database)
    {
        runicDatabase = database;
    }

    void Start()
    {
        if (player == null) player = FindFirstObjectByType<PlayerController>();

        if (pointer == null) pointer = FindFirstObjectByType<PointTargetScript>();

        if (runicDatabase == null) runicDatabase = FindFirstObjectByType<RunicDatabase>();

        if (runicStorageManager == null) runicStorageManager = FindFirstObjectByType<RunicStorageManager>();

        if (hpBar == null) hpBar = GetComponentInChildren<HPBar>();
 
        rb = GetComponent<Rigidbody>();

        combat = GetComponent<RunicCombat>();
        
        movement = GetComponent<RunicMovement>();
        
        runicWildAI = GetComponent<RunicWildAI>();

        combat.SetCombatStyle(combatStyle);

        if (tameState == RunicState.Tamed)
        {
            capturable = false;
            actState = BehaviorState.Following;
        }

        if (rb != null)
            rb.mass = weight;

        RefreshMovementPause();
    }

    void Update()
    {
        RefreshMovementPause();
        if (IsPaused)
            return;

        if (invencibilityTime > 0)
            invencibilityTime -= Time.deltaTime;

        if (tameState == RunicState.Tamed)
        {
            if (player.CommandedTarget != null)
                actState = BehaviorState.CommandedAttack;
            else if (pointer.FollowPoint)
                actState = BehaviorState.CommandedMove;
            else actState = BehaviorState.Following;
        }

    }

    private void RefreshMovementPause()
    {
        bool shouldPauseMovement = IsPaused;
        if (movementPauseApplied == shouldPauseMovement)
            return;

        movementPauseApplied = shouldPauseMovement;
        movement?.SetPaused(shouldPauseMovement);
        combat?.SetPaused(shouldPauseMovement);
    }

    // Initializes creature state from a save file entry
    public void InitializeFromData(RunicSaveData data)
    {
        runicInstanceId = data.runicInstanceId;
        species = runicDatabase != null ? runicDatabase.GetSpeciesById(data.speciesId) : null;
        if (species == null)
        {
            Debug.LogError($"Could not load Runic species with ID '{data.speciesId}'.", this);
            return;
        }

        nickname = string.IsNullOrEmpty(data.nickname) ? species.SpeciesName : data.nickname;
        runicIcon = data.runtimeIcon != null 
        ? data.runtimeIcon : species.SpeciesIcon;
        modelIndex = species.SpeciesModels != null && species.SpeciesModels.Count > 0
            ? Mathf.Clamp(data.modelIndex, 0, species.SpeciesModels.Count - 1)
            : 0;
        runicModel = data.runtimeModel != null
            ? data.runtimeModel
            : species.SpeciesModels != null && species.SpeciesModels.Count > 0
            ? species.SpeciesModels[modelIndex]
            : null;
        width = data.width;
        height = data.height;
        level = data.level;
        experience = data.experience;
        currentHP = data.currentHP;
        maxHP = data.maxHP;
        attack = data.attack;
        defense = data.defense;
        weight = data.weight;
        knockbackResistance = data.knockbackResistance;
        speed = data.speed;
        walkSpeed = data.walkSpeed;
        sprintSpeed = data.sprintSpeed;
        magic = data.magic;
        magicDefense = data.magicDefense;
        recovery = data.recovery;
        specialRecovery = data.specialRecovery;
        aggression = data.aggression;
        specialAggression = data.specialAggression;
        basicSkills = runicDatabase.GetSkillsByIds(data.learnedBasicSkillIds);
        skills = runicDatabase.GetSkillsByIds(data.learnedSkillIds);
        elements = runicDatabase.GetElementsByIds(data.elementIds);
        combatStyle = data.combatStyle;
        
        tameState = RunicState.Tamed;
        
        if (rb != null)
            rb.mass = weight;
    }

    public RunicSaveData ExportToSaveData()
    {
        List<string> elemIds = new List<string>();
        if (elements != null)
        {
            foreach (var elem in elements)
            {
                if (elem != null) elemIds.Add(elem.ElementId);
            }
        }

        List<string> bSkillIds = new List<string>();
        if (basicSkills != null)
        {
            foreach (var skill in basicSkills)
            {
                if (skill != null) bSkillIds.Add(skill.SkillId);
            }
        }

        List<string> sIds = new List<string>();
        if (skills != null)
        {
            foreach (var skill in skills)
            {
                if (skill != null) sIds.Add(skill.SkillId);
            }
        }

        return new RunicSaveData
        {
            runicInstanceId = System.Guid.NewGuid().ToString(),
            speciesId = species != null ? species.SpeciesId : "",
            nickname = string.IsNullOrEmpty(nickname) && species != null ? species.SpeciesName : nickname,
            runtimeIcon = runicIcon,
            runtimeModel = runicModel,
            modelIndex = species != null && species.SpeciesModels != null
                ? Mathf.Max(0, species.SpeciesModels.IndexOf(runicModel))
                : 0,
            width = width,
            height = height,
            level = level,
            experience = experience,
            currentHP = currentHP,
            maxHP = maxHP,
            attack = attack,
            defense = defense,
            weight = weight,
            knockbackResistance = knockbackResistance,
            speed = speed,
            walkSpeed = walkSpeed,
            sprintSpeed = sprintSpeed,
            magic = magic,
            magicDefense = magicDefense,
            recovery = recovery,
            specialRecovery = SpecialRecovery,
            aggression = aggression,
            specialAggression = specialAggression,
            elementIds = elemIds,
            learnedBasicSkillIds = bSkillIds,
            learnedSkillIds = sIds,
            combatStyle = combatStyle
        };
    }

    public void Capture()
    {
        if (tameState != RunicState.Wild) return;

        RunicSaveData capturedData = ExportToSaveData();

        if (runicStorageManager != null)
            runicStorageManager.AddCapturedRunic(capturedData);
        else
            Debug.LogError("Runic.Capture: RunicStorageManager não encontrado na cena.", this);

        Destroy(gameObject);
    }

    public void Chain()
    {
        chained = true;
        RefreshMovementPause();
        int instances = Random.Range(minChainNumber, maxChainNumber+1);
        float YAxis = Random.Range(transform.rotation.eulerAngles.y + chainYAngleMinVariance,
                                    transform.rotation.eulerAngles.y + chainYAngleMaxVariance);

        for (int i = 0; i < instances; i++)
        {
            float ZAxis = Random.Range(chainMinZAngle, chainMaxZAngle);
            Quaternion chainRotation = Quaternion.Euler(0, YAxis, ZAxis);

            GameObject chain = Instantiate(captureChainsPrefab, transform.position, chainRotation);
            activeChains.Add(chain);

            YAxis = Random.Range(YAxis + chainYAngleMinVariance, YAxis + chainYAngleMaxVariance);
        }
    }

    public void Unchain()
    {
        chained = false;
        RefreshMovementPause();

        foreach (GameObject chain in activeChains)
        {
            if (chain != null)
                Destroy(chain);
        }

        activeChains.Clear();
    }

    public void Dodge()
    {
        invencibilityTime = iframes;
    }

    public void TakeDamage(int damage, Runic attacker = null, bool playerAttacker = false, 
        Transform ambientSource = null, Color damageColor = default(Color), float damageScale = 1)
    {
        if (frozen) return;

        currentHP -= damage;
        Debug.Log(gameObject + " has taken " + damage + " damage.");

        // damage popup
        GameObject popup = Instantiate(damagePopupPrefab, 
            transform.position + Vector3.up * 2f, Quaternion.identity);
        Color popupColor = damageColor.a > 0f ? damageColor : Color.white;
        popup.GetComponent<DamagePopup>().Setup(damage, popupColor, damageScale);

        if (tameState == RunicState.Tamed && runicStorageManager != null)
            runicStorageManager.UpdateRunicInBox(this);

        if (currentHP <= 0)
        {
            if (currentHP < 0)
                currentHP = 0;

            Die();
            return;
        }

        // Ambient damage
        if (attacker == null && !playerAttacker)
        {
            actState = BehaviorState.Fleeing;
            if (runicWildAI != null)
                runicWildAI.SetEnvironmentalThreat(ambientSource);

            hpBar.UpdateBarColor();
            return;
        }

        // If the attacker is the player
        if (playerAttacker)
        {
            actState = BehaviorState.Fighting;
            runicWildAI.SetFightPlayer(true);
            hpBar.UpdateBarColor();
            return;
        }

        bool isSameSpecies = attacker != null && attacker.Species == species;
        bool isPredator = CheckAttackerRelation(attacker) && !isSameSpecies;

        if (!IsFearful() && !isPredator)
        {
            actState = BehaviorState.Fighting;
            if (!engagedEnemies.Contains(attacker))
                engagedEnemies.Add(attacker);
                
            if (attacker.TameState == RunicState.Tamed)
                runicWildAI.SetFightPlayer(true);
            hpBar.UpdateBarColor();
        }
        else 
        {
            actState = BehaviorState.Fleeing;
            threats.Add(attacker);
        }
    }

    public int CalculateDamage(Skill skill, Runic attacker, float totalPower)
    {
        return CalculateDamage(totalPower, DamageProfile.FromSkill(skill, attacker));
    }

    public int CalculateDamage(float totalPower, DamageProfile profile)
    {
        return DamageCalculator.Calculate(totalPower, profile, defense, magicDefense, elements);
    }

    public float GetKeyStatValue(SkillKeyStat keyStat) => keyStat switch
    {
        SkillKeyStat.Attack => attack,
        SkillKeyStat.Magic => magic,
        SkillKeyStat.Defense => defense,
        SkillKeyStat.MagicDefense => magicDefense,
        SkillKeyStat.Hp => currentHP,
        SkillKeyStat.Speed => speed,
        _ => 100f
    };

    public bool IsFearful()
    {
        return nature == WildNature.Fearful || nature == WildNature.FearfulFriendly
            || nature == WildNature.FearfulNeutral || nature == WildNature.FearfulProtective
            || nature == WildNature.FearfulNeutralProtective 
            || nature == WildNature.FearfulFriendlyProtective;
    }

    public bool IsProtective()
    {
        return nature == WildNature.FearfulProtective 
            || nature == WildNature.FearfulNeutralProtective 
            || nature == WildNature.FearfulFriendlyProtective;
    }

    public bool IsFriendly()
    {
        return nature == WildNature.Friendly 
            || nature == WildNature.FearfulFriendly 
            || nature == WildNature.FearfulFriendlyProtective;
    }

    bool CheckAttackerRelation(Runic attacker)
    {
        if (runicDatabase == null || attacker.species == null || species == null)
            return false;

        return runicDatabase.TryGetRelation(
            attacker.species,
            species,
            out SpeciesRelation relation)
            && relation == SpeciesRelation.Predator;
    }

    public void ApplyKnockback(Runic attacker, float knockbackValue)
    {
        if (frozen)
        {
            Debug.Log("Knockback negated by frozen runic.");
            return;
        }
        
        if (attacker == null || movement == null || knockbackValue <= 0f)
            return;

        Vector3 away = transform.position - attacker.transform.position;
        away.y = 0f;

        if (away.sqrMagnitude < 0.001f)
        {
            away = attacker.transform.forward;
            away.y = 0f;
        }

        if (away.sqrMagnitude < 0.001f)
            away = Vector3.forward;

        away.Normalize();

        Vector3 direction = (away + Vector3.up * Mathf.Max(0f, knockbackUpwardBias)).normalized;
        float resistance = Mathf.Clamp01(knockbackResistance / 100f);
        float weightFactor = attacker.Weight / Mathf.Max(attacker.Weight + Weight, 0.01f);
        float impulse = knockbackValue * weightFactor * (1f - resistance);

        Debug.Log("Knockback applied: " + impulse);

        if (impulse > 0f)
            movement.ApplyKnockback(direction * impulse);
    }

    public void SetBehaviorState(BehaviorState state)
    {
        actState = state;
        hpBar.UpdateBarColor();
    }

    public void AddThreat(Runic threat){
        if (!threats.Contains(threat))
        threats.Add(threat);
    }
    
    public void AddEnemy(Runic enemy){
        if (!engagedEnemies.Contains(enemy))
        engagedEnemies.Add(enemy);
    }

    public void AddAlly(Runic ally)
    {
        if (!allies.Contains(ally))
        {
            allies.Add(ally);
        }
    }

    public void Die()
    {
        Debug.Log(gameObject + " has died");

        if (player.CommandedTarget == this)
            player.SetCommandedTarget(null);

        if (player != null && player.RunicUI != null && tameState == RunicState.Tamed)
            player.RunicUI.SetRunic(null);
            player.RunicUI.gameObject.SetActive(false);

        // handle death logic (e.g., play animation, drop loot, etc.)
        Destroy(gameObject);
    }

    public void SetFreeze(bool state)
    {
        frozen = state;
        RefreshMovementPause();
    }

    public void ResetSkillCooldowns()
    {
        combat.ResetSkillCooldowns();
    }
}