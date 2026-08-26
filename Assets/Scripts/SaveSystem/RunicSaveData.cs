using System;
using System.Collections.Generic;

[Serializable]
public class RunicSaveData
{
    public string runicInstanceId; // Unique identifier for this specific creature instance
    public string speciesId;       // References RunicSpecies.speciesId
    public string nickname;
    public int level;
    public float experience;
    public float currentHP;
    public float maxHP;
    public float attack;
    public float defense;
    public float speed;
    public float magic;
    public float magicDefense;
    public List<string> elementIds;
    public List<string> learnedBasicSkillIds = new List<string>();
    public List<string> learnedSkillIds = new List<string>();
}

[Serializable]
public class SaveDataContainer
{
    public List<RunicSaveData> party = new List<RunicSaveData>();
    public List<RunicSaveData> boxStorage = new List<RunicSaveData>();
}