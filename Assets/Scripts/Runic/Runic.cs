using UnityEngine;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.AI;

public enum RunicState { Wild, Tamed, Fainted }
public enum BehaviorState { Idle, Wandering, Following, Fleeing, Hunting, Fighting }
public enum WildNature { Fearful, Friendly, Neutral, Territorial, Aggressive }

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
    [SerializeField] private float experience;
    [SerializeField] private float currentHP = 10;
    [SerializeField] private float maxHP = 10;
    [SerializeField] private float attack;
    [SerializeField] private float defense;
    [SerializeField] private float speed;
    [SerializeField] private float magic;
    [SerializeField] private float magicDefense;
    [SerializeField] private List<Element> elements;
    [SerializeField] private List<Skill> basicSkills;
    [SerializeField] private List<Skill> skills;

    [Header("Capture Settings (Wild Only)")]
    [SerializeField] private bool capturable = true;
    [SerializeField] private TargetMapAsset captureMap;
    [SerializeField] private float minCaptureChance = 0;
    [SerializeField] private float maxCaptureChance = 1;
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
    [SerializeField] private bool frozen = false;
    [SerializeField] private BehaviorState behaviorState = BehaviorState.Idle;
    
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

    [Header("References")]
    [SerializeField] private PlayerController player;
    [SerializeField] private PointTargetScript pointer;
    private NavMeshAgent agent;
    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private RunicDatabase runicDatabase;
    [SerializeField] private RunicStorageManager runicStorageManager;

    [Header("Follow Behavior")]
    [SerializeField] private float followerOffset = 2f;
    [Tooltip("Follow position mode: 0 = behind, 1 = side, 2 = any nearby position.")]
    private int followMode = 1;
    [SerializeField] private int followSide = 1;
    [SerializeField] private bool following = true;
    [SerializeField] private float followRange = 0.75f;
    [SerializeField] private float stopRange = 2.5f;

    [Header("Teleport Settings")]
    [SerializeField] private float timeToTeleport = 5f;
    private float teleportTimer = 0f;
    [SerializeField] private float teleportDistance = 10f;
    private bool teleportTimerStarted = false;

    [Header("Update Settings")]
    [SerializeField] private int updateFrequency = 15;
    private int frameCounter = 0;
    private Quaternion lastMovingRotation;

    public RunicState TameState => tameState;
    public string RunicInstanceId => runicInstanceId;
    public Sprite RunicIcon => runicIcon;
    public GameObject RunicModel => runicModel;
    public float CurrentHP => currentHP;
    public float MaxHP => maxHP;
    public float Attack => attack;
    public float Defense => defense;
    public float Speed => speed;
    public float Magic => magic;
    public float MagicDefense => magicDefense;
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

        agent = GetComponent<NavMeshAgent>();

        if (tameState == RunicState.Tamed || tameState == RunicState.Fainted) capturable = false;
        else following = false;
    }

    void Update()
    {
        frameCounter++;

        if (tameState == RunicState.Tamed) TameAI();
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
        runicIcon = species.SpeciesIcon;
        modelIndex = species.SpeciesModels != null && species.SpeciesModels.Count > 0
            ? Mathf.Clamp(data.modelIndex, 0, species.SpeciesModels.Count - 1)
            : 0;
        runicModel = species.SpeciesModels != null && species.SpeciesModels.Count > 0
            ? species.SpeciesModels[modelIndex]
            : null;
        level = data.level;
        experience = data.experience;
        currentHP = data.currentHP;
        maxHP = data.maxHP;
        attack = data.attack;
        defense = data.defense;
        speed = data.speed;
        magic = data.magic;
        magicDefense = data.magicDefense;
        basicSkills = runicDatabase.GetSkillsByIds(data.learnedBasicSkillIds);
        skills = runicDatabase.GetSkillsByIds(data.learnedSkillIds);
        
        tameState = RunicState.Tamed;
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
            modelIndex = species != null && species.SpeciesModels != null
                ? Mathf.Max(0, species.SpeciesModels.IndexOf(runicModel))
                : 0,
            level = level,
            experience = experience,
            currentHP = currentHP,
            maxHP = maxHP,
            attack = attack,
            defense = defense,
            speed = speed,
            magic = magic,
            magicDefense = magicDefense,
            elementIds = elemIds,
            learnedBasicSkillIds = bSkillIds,
            learnedSkillIds = sIds
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
        frozen = true;
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
        frozen = false;

        foreach (GameObject chain in activeChains)
        {
            if (chain != null)
                Destroy(chain);
        }

        activeChains.Clear();
    }

    public void TakeDamage(float damage)
    {
        if (frozen) return;

        currentHP -= damage;
        if (currentHP <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        // handle death logic (e.g., play animation, drop loot, etc.)
        Destroy(gameObject);
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

    private void TameAI()
    {
        Vector3 targetPosition;
        Vector3 right = lastMovingRotation * Vector3.right;
        Vector3 forward = lastMovingRotation * Vector3.forward;

        if (!pointer.FollowPoint)
        {
            switch (followMode)
            {
                case 0: // Atrás
                    targetPosition = player.transform.position - forward * followerOffset;
                    break;
                case 1: // Lado
                    targetPosition = player.transform.position + right * followSide * followerOffset;
                    break;
                case 2: // Qualquer posição próxima
                    targetPosition = player.transform.position;
                    break;
                default:
                    targetPosition = player.transform.position;
                    break;
            }            
        }
        else
        {
            targetPosition = pointer.IndicatedPosition;
        }

        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            followSide = -followSide;
        }
        
        bool interactableActive = playerInteraction != null && playerInteraction.InteractableTarget != null;

        if (!interactableActive && Keyboard.current.fKey.wasPressedThisFrame 
            && (!pointer.FollowPoint && player.CastingState != PlayerController.CastState.Channeling 
            || player.CastingState != PlayerController.CastState.Casting))
        {
            followMode = (followMode + 1) % 3;
        }

        if (frameCounter >= updateFrequency)
        {
            frameCounter = 0; // resetar o contador de frames

            float targetDistance = CalculateTargetDistance(targetPosition);
            float playerDistance = CalculateTargetDistance(player.transform.position);

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
                    TeleportToTarget(player.transform.position - forward * 5f);
                }
            } 
            else
            {
                teleportTimerStarted = false;
            }
            
            // atualiza a rotação de referência só quando o player se mover
            if (player.Moving)
            {
                lastMovingRotation = player.transform.rotation;
            }
        }
    }
}