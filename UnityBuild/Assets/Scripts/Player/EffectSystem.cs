using System.Collections.Generic;
using DataSystem;
using UnityEngine;

public class EffectSystem : MonoBehaviour
{
    [Header("Buff Effects")]
    [SerializeField] private List<Constants.BuffEffectEntry> buffEffectList = new List<Constants.BuffEffectEntry>();
    private Dictionary<Constants.BuffType, ParticleSystem> buffEffects;

    [Header("Skill Effects")]
    [SerializeField] private List<Constants.SkillEffectEntry> skilleffectList = new List<Constants.SkillEffectEntry>();
    private Dictionary<Constants.SkillType, ParticleSystem> skillEffects;

    private void Awake()
    {
        InitializeBuffEffects();
        InitializeSkillEffects();
    }

    private void InitializeBuffEffects()
    {
        buffEffects = new Dictionary<Constants.BuffType, ParticleSystem>();

        foreach (var entry in buffEffectList)
        {
            if (!buffEffects.ContainsKey(entry.buffType) && entry.effect != null)
            {
                buffEffects.Add(entry.buffType, entry.effect);
            }
        }
    }

    private void InitializeSkillEffects()
    {
        skillEffects = new Dictionary<Constants.SkillType, ParticleSystem>();

        foreach (var entry in skilleffectList)
        {
            if (!skillEffects.ContainsKey(entry.skillType) && entry.effect != null)
            {
                skillEffects.Add(entry.skillType, entry.effect);
            }
        }
    }
    
    public void PlayBuffEffect(Constants.BuffType type, bool isActive)
    {
        if (buffEffects.TryGetValue(type, out ParticleSystem effect))
        {
            if (isActive)
            {
                effect.Play();
            }
            else
            {
                effect.Stop();
            }
        }
        else
        {
            Debug.LogWarning($"BuffType {type}에 대한 이펙트가 등록되지 않았습니다!");
        }
    }
    
    public void PlaySkillEffect(Constants.SkillType type)
    {
        if (skillEffects.TryGetValue(type, out ParticleSystem effect))
        {
            effect.Play();
        }
        else
        {
            Debug.LogWarning($"SkillType {type}에 대한 이펙트가 등록되지 않았습니다!");
        }
    }

}
