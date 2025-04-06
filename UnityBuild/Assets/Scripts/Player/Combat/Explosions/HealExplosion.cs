using System.Collections;
using Interfaces;
using Mirror;
using Unity.Burst.CompilerServices;
using UnityEngine;

namespace Player.Combat
{
    public class HealExplosion : Explosion
    {
        protected override void Explode()
        {
            if (!isServer) return; // ✅ 서버에서만 실행

            // ✅ 원기둥(캡슐) 충돌 범위 설정
            Vector3 bottom = transform.position + Vector3.down * 0.5f;  // 바닥 위치
            Vector3 top = transform.position + Vector3.up * 2.0f;       // 원기둥 높이 조절
            float radius = explosionRadius; // 원기둥 반지름 (기존의 반지름 유지)

            Collider[] hitColliders = Physics.OverlapCapsule(bottom, top, radius);
            int totalDamageDealt = 0;
    
            foreach (Collider hit in hitColliders)
            {
                IDamagable damagable = hit.transform.GetComponent<IDamagable>();
                if (damagable != null)
                {
                    if (config != null)
                    {
                        if (config.attackType == DataSystem.Constants.AttackType.Melee && hit.transform.gameObject == owner) continue;
                        if (config.attackType == DataSystem.Constants.AttackType.Self && hit.transform.gameObject != owner) continue;
                    }

                    if (config != null && config.attackType != DataSystem.Constants.AttackType.Self &&
                        explosionDamage >= 0 && hit.transform.gameObject != owner)
                    {
                        // ✅ 같은 팀이면 무시
                        var hitPlayer = hit.GetComponent<PlayerCharacter>();
                        var ownerPlayer = owner != null ? owner.GetComponent<PlayerCharacter>() : null;

                        if (hitPlayer != null && ownerPlayer != null &&
                            hitPlayer.team != DataSystem.Constants.TeamType.None &&
                            hitPlayer.team == ownerPlayer.team)
                        {
                            continue; // 같은 팀이면 패스
                        }
                    }

                    int dealt = damagable.takeDamage((int)explosionDamage, transform.position, knockbackForce, config, playerid, skillid);
                    
                    if (hit.gameObject == owner)
                    {
                        continue;
                    }
                    
                    totalDamageDealt += dealt;
                }
            }

            if (owner != null && totalDamageDealt > 0)
            {
                if (owner.TryGetComponent<PlayerCharacter>(out var pc))
                {
                    // 피해의 절반만큼 회복 (마이너스 데미지로 힐)
                    pc.takeDamage(-totalDamageDealt / 2, transform.position, 0f, null, -1, -1);
                }
            }
        }
    }
}
