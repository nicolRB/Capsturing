using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class SaveDataContainer
{
    [Header("Player State")]
    public float[] playerPosition = new float[3];
    public float playerRotation;
    public int playerMaxHP;
    public int playerHP;
    public int lastSceneIndex;
    public List<float> spellCooldownList = new List<float>();
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
        lastSceneIndex = 0;
        spellCooldownList = new List<float>();
        boxStorage = new List<RunicSaveData>();
        partyIds = new List<string>();
    }
}