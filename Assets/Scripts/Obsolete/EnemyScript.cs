using UnityEngine;
using Random = UnityEngine.Random;
using System.Collections.Generic;

public class EnemyScript : MonoBehaviour
{
    [Header("Capturing")]
    public bool capturable = true;
    public TargetMapAsset captureMap;
    public float minCaptureChance = 0; // chance de captura base assumindo que o player teve 0 pontos no minigame de captura
    public float maxCaptureChance = 1;  // chance de captura assumindo que o player teve foi perfeito no minigame sem contar modificadores externos (como dificuldade, itens, etc)
    public float targetPerfectWeight = 2; // peso de acertos perfeitos no minigame (espera-se ser maior que o de acertos bons)
    public float targetGoodWeight = 1; // peso de acertos bons no minigame (espera-se ser maior que o de erros)
    public float targetMissWeight = 0; // peso de erros no minigame (maior que 0 significa q mesmo errando sempre vai ter algum aumento de chance minimo. menor que 0 significa que errar penaliza na chance de captura)
    public float CCModifier = 0; // modificador externo gerais de chance de captura (configurações de dificuldade/itens/situacional. talvez não seja usado)
    public float CCMultiplier = 0; // multiplicador externo geral de chance de captura (configurações de dificuldade/itens/situacional. talvez não seja usado)

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

    [Header("Capture Bonuses")]
    public float perfectBonus = 0;
    public float perfectMultiplier = 0;
    public float goodBonus = 0;
    public float goodMultiplier = 0;
    public float missBonus = 0;
    public float missMultiplier = 0;

    [Header("Combat Behavior")]
    public bool frozen = false;

    [Header("Combat Stats")]
    public float maxHealth = 100;
    public float currentHealth = 100;

    void Start()
    {
        frozen = false;
    }

    void Update()
    {
        
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

    public void Capture()
    {
        // deletes the enemy from the scene and adds it to the player's collection
        Destroy(gameObject);
    }

    public void TakeDamage(float damage)
    {
        if (frozen) return;

        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        // handle death logic (e.g., play animation, drop loot, etc.)
        Destroy(gameObject);
    }
}