using UnityEngine;

[CreateAssetMenu(fileName = "NovaHabilidade", menuName = "Runico/Habilidade")]
public class Skill : ScriptableObject
{
    [SerializeField] private string skillId;
    [SerializeField] private string skillName;
    [SerializeField] private float cooldown;
    [SerializeField] private Element skillElement;

    public string SkillId => skillId;
    public string SkillName => skillName;
    public float Cooldown => cooldown;
    public Element SkillElement => skillElement;
}