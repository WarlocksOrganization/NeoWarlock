using System.Collections.Generic;
using DataSystem;
using Interfaces;
using Mirror;
using Player.Combat;
using UnityEngine;

namespace Player
{
    public class PersistentAttackProjectile : AttackProjectile
    {
        private HashSet<Collider> explodedTargets = new HashSet<Collider>(); // ✅ 중복 폭발 방지

        protected override void Explode()
        {

        }

        protected override void OnTriggerEnter(Collider col)
        {
            if (!isServer) return;
            
            if (this.attackConfig.attackType is Constants.AttackType.Self)
            {
                if (col.gameObject != this.owner) return;
            }
            else
            {
                if ((layerMask & (1 << col.gameObject.layer)) == 0) return;
                if (col.gameObject == this.owner) return;
                
                var colPlayer = col.GetComponent<PlayerCharacter>();
                var ownerPlayer = owner != null ? owner.GetComponent<PlayerCharacter>() : null;

                if (colPlayer != null && ownerPlayer != null &&
                    colPlayer.team != Constants.TeamType.None &&
                    colPlayer.team == ownerPlayer.team)
                {
                    return; // 같은 팀이므로 무시
                }
            }

            // ✅ 중복 폭발 방지
            if (!explodedTargets.Contains(col))
            {
                explodedTargets.Add(col); // 이미 폭발한 대상 저장
                Explode();
            }
            
            IDamagable damagable = col.transform.GetComponent<IDamagable>();
            if (damagable != null)
            {
                damagable.takeDamage((int)damage, transform.position, knockbackForce, attackConfig, playerid, skillid);
            }
            
            if (attackConfig != null && attackConfig.particlePrefab != null)
            {
                Vector3 hitPoint = col.ClosestPoint(owner.transform.position);
                Quaternion hitRot = Quaternion.LookRotation((hitPoint - transform.position).normalized);
                
                GameObject effect = Instantiate(attackConfig.particlePrefab, hitPoint, hitRot);
                effect.transform.localScale = new Vector3(1f, 1f, 1f);  // ✅ 프리팹 인스턴스에서만 변경
                effect.GetComponent<AttackParticle>().SetAttackParticleData(attackConfig.skillType);
                NetworkServer.Spawn(effect); // ✅ 네트워크에 동기화
            }
        }
    }
}