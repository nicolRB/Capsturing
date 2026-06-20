using UnityEngine;

public class EnemyScript : MonoBehaviour
{
    [Header("Capturing")]
    public bool capturable = true;
    public float minCaptureChance = 0; // chance de captura base assumindo que o player teve 0 pontos no minigame de captura
    public float maxCaptureChance = 1;  // chance de captura assumindo que o player teve foi perfeito no minigame
    public TargetMapAsset captureMap;
    public float perfectWeight = 2; // peso de acertos perfeitos no minigame (espera-se ser maior que o de acertos bons)
    public float goodWeight = 1; // peso de acertos bons no minigame (espera-se ser maior que o de erros)
    public float missWeight = 0; // peso de erros no minigame (maior que 0 significa q mesmo errando sempre vai ter algum aumento de chance minimo. menor que 0 significa que errar penaliza na chance de captura)
    public float CCModifier = 0; // modificador externo gerais de chance de captura (configurações de dificuldade/itens/situacional. talvez não seja usado)
    // mais modificadores de chance
    public float perfectBonus = 0;
    public float perfectMultiplier = 0;
    public float goodBonus = 0;
    public float goodMultiplier = 0;
    public float missBonus = 0;
    public float missMultiplier = 0;

    void Start()
    {
        
    }

    void Update()
    {
        
    }
}
