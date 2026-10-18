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
    private readonly Dictionary<Collider, bool> hitboxEnabledBeforePause = new Dictionary<Collider, bool>();
    private bool paused;

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

    public void SetPaused(bool state)
    {
        if (paused == state)
            return;

        paused = state;
        if (paused)
        {
            hitboxEnabledBeforePause.Clear();
            foreach (Collider hitbox in hitboxDict.Values)
            {
                if (hitbox == null) continue;
                hitboxEnabledBeforePause[hitbox] = hitbox.enabled;
                hitbox.enabled = false;
            }
            return;
        }

        foreach (KeyValuePair<Collider, bool> entry in hitboxEnabledBeforePause)
        {
            if (entry.Key != null)
                entry.Key.enabled = entry.Value;
        }
        hitboxEnabledBeforePause.Clear();
    }

    private void SetHitboxEnabled(Collider hitbox, bool state)
    {
        if (hitbox == null)
            return;

        if (paused)
        {
            hitboxEnabledBeforePause[hitbox] = state;
            hitbox.enabled = false;
        }
        else
        {
            hitbox.enabled = state;
        }
    }

    public void ActivateHitbox(SkillHitboxType type)
    {
        Collider col = GetCollider(type);
        if (col != null)
        {
            SetHitboxEnabled(col, true);
        }
    }

    public void ActivateHitbox(SkillHitboxType type, float sizeMultiplier, Skill skill, float power, bool continuous)
    {
        Collider col = GetCollider(type);
        if (col == null) return;

        SetHitboxEnabled(col, true);
        col.transform.localScale = Vector3.one * sizeMultiplier;

        HitboxItem hitboxItem = col.GetComponent<HitboxItem>();
        if (hitboxItem != null)
            hitboxItem.Setup(GetComponent<Runic>(), skill, power, continuous);
    }

    public void DeactivateHitbox(SkillHitboxType type)
    {
        Collider col = GetCollider(type);
        if (col != null)
        {
            SetHitboxEnabled(col, false);
        }
    }

    public void DeactivateAllHitboxes()
    {
        foreach (var mapping in hitboxMappings)
        {
            if (mapping.targetCollider != null)
            {
                SetHitboxEnabled(mapping.targetCollider, false);
            }
        }
    }

    public void ResetHitbox(SkillHitboxType type)
    {
        Collider col = GetCollider(type);
        if (col != null)
        {
            SetHitboxEnabled(col, false);
            col.transform.localScale = new Vector3(1, 1, 1);
        }
    }
    
    public void ResetAllHitboxes()
    {
        foreach (var mapping in hitboxMappings)
        {
            if (mapping.targetCollider != null)
            {
                SetHitboxEnabled(mapping.targetCollider, false);
                mapping.targetCollider.transform.localScale = new Vector3(1, 1, 1);
            }
        }
    }
}