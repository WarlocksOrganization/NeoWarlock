using Mirror;
using UnityEngine;
using Player;
using System.Collections.Generic;
using DataSystem;

public class SkillItemPickup : NetworkBehaviour
{

    private void OnTriggerEnter(Collider other)
    {
        if (!isServer) return;

        var player = other.GetComponent<PlayerCharacter>();
        if (player == null) return;
        
        int[] skillIds = { 1001, 1002, 1003, 1004 };
        int randomSkillId = skillIds[Random.Range(0, skillIds.Length)];
        
        player.itemSkillId = randomSkillId;

        NetworkServer.Destroy(gameObject);
    }

}