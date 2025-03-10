using System.Collections;
using System.Collections.Generic;
using DataSystem;
using Mirror;
using UnityEngine;

public class AttackParticle : NetworkBehaviour
{
    [SerializeField] private float destroyTime = 5f;
    
    [SyncVar(hook = nameof(OnSkillEffectChanged))] 
    protected Constants.SkillType skillType = Constants.SkillType.None;
    
    [Header("Skill Effects")]
    [SerializeField] private List<Constants.SkillEffectGameObjectEntry> skilleffectList = new List<Constants.SkillEffectGameObjectEntry>();
    private Dictionary<Constants.SkillType, GameObject> skillEffects;
    
    void Awake()
    {
        InitializeSkillEffects();
    }
    
    public override void OnStartServer()
    {
        StartCoroutine(AutoDestroy());
    }
    
    public void SetAttackParticleData(Constants.SkillType skillType)
    {
       this.skillType = skillType;
    }
    
    private void InitializeSkillEffects()
    {
        skillEffects = new Dictionary<Constants.SkillType, GameObject>();

        foreach (var entry in skilleffectList)
        {
            if (!skillEffects.ContainsKey(entry.skillType) && entry.gObject != null)
            {
                skillEffects.Add(entry.skillType, entry.gObject);
            }
        }
    }
    
    private IEnumerator AutoDestroy()
    {
        yield return new WaitForSeconds(destroyTime);

        if (isServer)
        {
            NetworkServer.Destroy(gameObject); // ✅ 서버에서만 제거
        }
        else
        {
            Destroy(gameObject); // ✅ 클라이언트에서도 안전하게 제거
        }
    }
    
    private void OnSkillEffectChanged(Constants.SkillType oldValue, Constants.SkillType newValue)
    {
        foreach (var skill in skillEffects)
        {
            skill.Value.SetActive(false);
        }
        if (skillEffects.ContainsKey(newValue))
        {
            skillEffects[newValue].SetActive(true);
        }
    }
}
