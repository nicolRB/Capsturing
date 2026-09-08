using System.Collections.Generic;

[System.Serializable]
public class SaveDataContainer
{
    public float[] playerPosition = new float[3];
    public float playerRotation;
    public int playerHP;
    public string lastSceneName;
    public List<RunicSaveData> boxStorage = new List<RunicSaveData>();
    public List<string> partyIds = new List<string>();

    // Construtor com valores padrão (para um Novo Jogo)
    public SaveDataContainer()
    {
        playerPosition[0] = 0f; // X
        playerPosition[1] = 1f; // Y
        playerPosition[2] = 0f; // Z
        playerRotation = 0f; // Yaw
        playerHP = 100;
        lastSceneName = "TutorialScene";
        boxStorage = new List<RunicSaveData>();
        partyIds = new List<string>();
    }
}