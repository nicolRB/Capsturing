using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct HitboxMapping
{
    public SkillHitboxType hitboxType;
    public Collider targetCollider;
}

public class RunicHitboxRegistry : MonoBehaviour
{
    [SerializeField] private List<HitboxMapping> hitboxMappings = new List<HitboxMapping>();
    [SerializeField] private List<Collider> hurtboxes = new List<Collider>();
    private HashSet<Collider> hurtboxSet;

    private Dictionary<SkillHitboxType, Collider> hitboxDict;

    private void Awake()
    {
        hitboxDict = new Dictionary<SkillHitboxType, Collider>();
        foreach (var mapping in hitboxMappings)
        {
            if (mapping.targetCollider != null && !hitboxDict.ContainsKey(mapping.hitboxType))
            {
                hitboxDict.Add(mapping.hitboxType, mapping.targetCollider);
                mapping.targetCollider.enabled = false; // Hitboxes começam desativadas
            }
        }

        hurtboxSet = new HashSet<Collider>(hurtboxes);
    }

    public Collider GetCollider(SkillHitboxType type)
    {
        if (hitboxDict != null && hitboxDict.TryGetValue(type, out Collider col))
        {
            return col;
        }
        return null;
    }

    public bool IsHurtbox(Collider col)
    {
        return col != null && hurtboxSet.Contains(col);
    }

    public void ActivateHitbox(SkillHitboxType type)
    {
        Collider col = GetCollider(type);
        if (col != null)
        {
            col.enabled = true;
        }
    }

    public void ActivateHitbox(SkillHitboxType type, float sizeMultiplier, float power, float knockback, bool continuous)
    {
        Collider col = GetCollider(type);
        if (col != null)
        {
            col.enabled = true;
            col.transform.localScale = new Vector3(sizeMultiplier, sizeMultiplier, sizeMultiplier);

            HitboxItem hitboxItem = col.GetComponent<HitboxItem>();
            
            if (hitboxItem != null)
                hitboxItem.Setup(GetComponent<Runic>(), power, knockback, continuous);
        }
    }

    public void DeactivateHitbox(SkillHitboxType type)
    {
        Collider col = GetCollider(type);
        if (col != null)
        {
            col.enabled = false;
        }
    }

    public void DeactivateAllHitboxes()
    {
        foreach (var mapping in hitboxMappings)
        {
            if (mapping.targetCollider != null)
            {
                mapping.targetCollider.enabled = false;
            }
        }
    }

    public void ResetHitbox(SkillHitboxType type)
    {
        Collider col = GetCollider(type);
        if (col != null)
        {
            col.enabled = false;
            col.transform.localScale = new Vector3(1, 1, 1);
        }
    }
    
    public void ResetAllHitboxes()
    {
        foreach (var mapping in hitboxMappings)
        {
            if (mapping.targetCollider != null)
            {
                mapping.targetCollider.enabled = false;
                mapping.targetCollider.transform.localScale = new Vector3(1, 1, 1);
            }
        }
    }
}