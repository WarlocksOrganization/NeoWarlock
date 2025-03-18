using System.Collections.Generic;
using DataSystem;
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
            if (attackConfig != null)
            {
                Vector3 explosionPosition = transform.position + Vector3.up * 2f; 
                if (Physics.Raycast(transform.position + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 10f, layerMask))
                {
                    explosionPosition = hit.point + Vector3.up * 0.1f;
                }

                GameObject explosion = Instantiate(attackConfig.explosionEffectPrefab, explosionPosition, Quaternion.identity);
                Explosion explosionComponent = explosion.GetComponent<Explosion>();

                if (explosionComponent != null)
                {
                    explosionComponent.Initialize(damage, radius, knockbackForce, attackConfig, this.owner, playerid, skillid);
                }

                NetworkServer.Spawn(explosion);
            }

            // ✅ 폭발 후에도 사라지지 않도록 유지 (Destroy 제거)
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
            }

            // ✅ 중복 폭발 방지
            if (!explodedTargets.Contains(col))
            {
                explodedTargets.Add(col); // 이미 폭발한 대상 저장
                Explode();
            }
        }
    }
}