using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SaveDataContainer
{
    [Header("Player State")]
    public float[] playerPosition = new float[3];
    public float playerRotation;
    public int playerHP;
    public string lastSceneName;
    public List<RunicSaveData> boxStorage = new List<RunicSaveData>();
    public List<string> partyIds = new List<string>();

    // Initialize a new save with the default player and world state.
    public SaveDataContainer()
    {
        playerPosition[0] = 0f;
        playerPosition[1] = 1f;
        playerPosition[2] = 0f;
        playerRotation = 0f;
        playerHP = 100;
        lastSceneName = "TutorialScene";
        boxStorage = new List<RunicSaveData>();
        partyIds = new List<string>();
    }
}