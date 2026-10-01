using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RunicSaveData
{
    public string runicInstanceId;
    public string speciesId;
    public string nickname;
    public int modelIndex;
    public string uniqueModelPath;
    public Sprite runtimeIcon;
    public string uniqueSpritePath;
    public GameObject runtimeModel;
    public float width;
    public float height;
    public int level;
    public int experience;
    public int currentHP;
    public int maxHP;
    public float attack;
    public float defense;
    public float speed;
    public float magic;
    public float magicDefense;
    public List<string> elementIds;
    public List<string> learnedBasicSkillIds = new List<string>();
    public List<string> learnedSkillIds = new List<string>();
}