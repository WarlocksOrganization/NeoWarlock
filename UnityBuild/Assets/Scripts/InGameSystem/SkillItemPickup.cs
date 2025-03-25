using Mirror;
using UnityEngine;
using Player;
using System.Collections.Generic;
using DataSystem;

public class SkillItemPickup : NetworkBehaviour
{
    [Header("Skill Setup")]
    public int skillId; // 📦 부여할 스킬 ID

    [SerializeField] private MeshFilter meshFilter; // ← 아이템의 MeshFilter 참조
    [SerializeField] private List<Constants.SkillTypeMeshEntry> skillMeshList;

    private Dictionary<Constants.SkillType, Mesh> skillMeshDictionary;

    private void Awake()
    {
        // Dictionary 변환
        skillMeshDictionary = new Dictionary<Constants.SkillType, Mesh>();
        foreach (var entry in skillMeshList)
        {
            if (!skillMeshDictionary.ContainsKey(entry.skillType) && entry.mesh != null)
            {
                skillMeshDictionary.Add(entry.skillType, entry.mesh);
            }
        }
    }

    private void Start()
    {
        // 아이템 생성 시, skillId에 맞는 Mesh 적용
        var attackData = DataSystem.Database.Database.GetAttackData(skillId);
        if (attackData != null)
        {
            var skillType = attackData.config.skillType;
            if (skillMeshDictionary.TryGetValue(skillType, out var mesh))
            {
                meshFilter.mesh = mesh;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isServer) return;

        var player = other.GetComponent<PlayerCharacter>();
        if (player == null) return;

        player.SetAvailableAttack(4, skillId); // 서버에서 안전하게 동기화

        NetworkServer.Destroy(gameObject); // 아이템 제거
    }
}