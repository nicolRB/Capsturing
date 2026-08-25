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
    public RunicState tameState = RunicState.Wild;

    [Header("Species Template")]
    public RunicSpecies species; // Loaded from ScriptableObject asset

    [Header("Runtime Stats")]
    public string nickname;
    public int level = 1;
    public float experience;
    public float currentHP = 10;
    public float maxHP = 10;
    public float attack;
    public float defense;
    public float speed;
    public float magic;
    public float magicDefense;
    public List<Element> elements;
    public List<Skill> basicSkills;
    public List<Skill> skills;

    [Header("Capture Settings (Wild Only)")]
    public bool capturable = true;
    public TargetMapAsset captureMap;
    public float minCaptureChance = 0; // chance de captura base assumindo que o player teve 0 pontos no minigame de captura
    public float maxCaptureChance = 1;  // chance de captura assumindo que o player teve foi perfeito no minigame sem contar 
    // modificadores externos (como dificuldade, itens, etc)
    public float targetPerfectWeight = 2; // peso de acertos perfeitos no minigame (espera-se ser maior que o de acertos bons)
    public float targetGoodWeight = 1; // peso de acertos bons no minigame (espera-se ser maior que o de erros)
    public float targetMissWeight = 0; // peso de erros no minigame (maior que 0 significa q mesmo errando sempre vai ter algum 
    // aumento de chance minimo. menor que 0 significa que errar penaliza na chance de captura)
    public float CCModifier = 0; // modificador externo gerais de chance de captura (configurações de dificuldade/itens/situacional. 
    // talvez não seja usado)
    public float CCMultiplier = 0; // multiplicador externo geral de chance de captura (configurações de 
    // dificuldade/itens/situacional. talvez não seja usado)

    [Header("Capture Bonuses")]
    public float perfectBonus = 0;
    public float perfectMultiplier = 0;
    public float goodBonus = 0;
    public float goodMultiplier = 0;
    public float missBonus = 0;
    public float missMultiplier = 0;

    [Header("Behavior")]
    public bool frozen = false;
    public BehaviorState behaviorState = BehaviorState.Idle;
    
    [Header("Chains")]
    public GameObject captureChainsPrefab;
    private List<GameObject> activeChains = new List<GameObject>();
    public float chainYAngleMinVariance = 60f; // minimum angle variance for the chains on the X axis
    public float chainYAngleMaxVariance = 60f; // maximum angle variance for the chains on the X axis
    public float chainMinZAngle = 20f;
    public float chainMaxZAngle = 95f;
    public int minChainNumber = 6;
    public int maxChainNumber = 8;
    public float chainSpawnInterval = 0f; // for later

    [Header("References")]
    public PlayerController player;
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

        if (pointer == null) pointer = FindFirstObjectByType<PointTargetScript>();

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
    public void InitializeFromData(RunicSaveData data, RunicSpecies speciesSO)
    {
        species = speciesSO;
        nickname = string.IsNullOrEmpty(data.nickname) ? speciesSO.speciesName : data.nickname;
        level = data.level;
        experience = data.experience;
        currentHP = data.currentHP;
        maxHP = data.maxHP;
        attack = data.attack;
        defense = data.defense;
        speed = data.speed;
        magic = data.magic;
        magicDefense = data.magicDefense;
        
        tameState = RunicState.Tamed;
    }

    public RunicSaveData ExportToSaveData()
    {
        List<string> elemIds = new List<string>();
        if (elements != null)
        {
            foreach (var elem in elements)
            {
                if (elem != null) elemIds.Add(elem.elementId);
            }
        }

        List<string> bSkillIds = new List<string>();
        if (basicSkills != null)
        {
            foreach (var skill in basicSkills)
            {
                if (skill != null) bSkillIds.Add(skill.skillId);
            }
        }

        List<string> sIds = new List<string>();
        if (skills != null)
        {
            foreach (var skill in skills)
            {
                if (skill != null) sIds.Add(skill.skillId);
            }
        }

        return new RunicSaveData
        {
            runicInstanceId = System.Guid.NewGuid().ToString(),
            speciesId = species != null ? species.speciesId : "",
            nickname = string.IsNullOrEmpty(nickname) && species != null ? species.speciesName : nickname,
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

        // 1. Convert active creature to saveable C# data structure
        RunicSaveData capturedData = ExportToSaveData();

        // 2. Load existing save, append new creature to party/storage, and save back to disk
        SaveManager saveManager = FindFirstObjectByType<SaveManager>();
        if (saveManager != null)
        {
            SaveDataContainer currentSave = saveManager.LoadGame();
            
            // Add to party if room, otherwise send to box storage
            if (currentSave.party.Count < 6)
            {
                currentSave.party.Add(capturedData);
            }
            else
            {
                currentSave.boxStorage.Add(capturedData);
            }

            saveManager.SaveGame(currentSave);
        }

        // 3. Remove wild unit from the active scene
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

        if (!pointer.followPoint)
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
            if (player.moving)
            {
                lastMovingRotation = player.transform.rotation;
            }
        }
    }
}