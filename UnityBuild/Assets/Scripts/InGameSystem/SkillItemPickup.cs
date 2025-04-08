using Mirror;
using UnityEngine;
using Player;
using System.Collections.Generic;
using DataSystem;
using DataSystem.Database;

public class SkillItemPickup : NetworkBehaviour
{
    [SerializeField] private GameObject floatingTextPrefab;

    private void OnTriggerEnter(Collider other)
    {
        if (!isServer) return;

        var player = other.GetComponent<PlayerCharacter>();
        // SkillItemPickup.cs
        if (player != null)
        {
            int[] skillIds = { 1001, 1002, 1003, 1004, 1005 };
            int randomSkillId = skillIds[Random.Range(0, skillIds.Length)];

            // ❌ 잘못된 방법: player.CmdSetItemSkill(randomSkillId); ← 클라 없으니까 에러
            // ✅ 올바른 방법:
            player.ServerSetItemSkill(randomSkillId);

            // 보여주기용 텍스트는 그대로
            var skillData = Database.GetAttackData(randomSkillId);
            if (skillData != null)
            {
                RpcShowFloatingText(player.netIdentity, skillData.DisplayName);
            }

            NetworkServer.Destroy(gameObject);
        }
    }

    [ClientRpc]
    void RpcShowFloatingText(NetworkIdentity playerId, string displayName)
    {
        if (playerId == null) return;

        var player = playerId.GetComponent<PlayerCharacter>();
        if (player == null) return;

        if (floatingTextPrefab != null)
        {
            GameObject go = Instantiate(floatingTextPrefab, player.transform.position + Vector3.up * 2f, Quaternion.identity, player.transform);
            FloatingDamageText fdt = go.GetComponent<FloatingDamageText>();
            if (fdt != null)
                fdt.SetText(displayName);
        }
    }
}