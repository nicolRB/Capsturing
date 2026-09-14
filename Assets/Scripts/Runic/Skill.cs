using UnityEngine;

[CreateAssetMenu(fileName = "NovaHabilidade", menuName = "Runico/Habilidade")]
public class Skill : ScriptableObject
{
    public string skillName;
    public string skillId;
    public float Cooldown;
}