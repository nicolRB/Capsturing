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
    [NonSerialized] public Sprite runtimeIcon;
    [NonSerialized] public GameObject runtimeModel;
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