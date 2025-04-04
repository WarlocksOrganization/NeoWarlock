using System.Collections;
using UnityEngine;
using Mirror;
using DataSystem.Database;
using Interfaces;

namespace Player.Combat
{
    public class BeamAttack : AttackBase
    {
        public override void Execute(Vector3 mousePosition, Vector3 firePoint, GameObject owner, int playerid, int skillid, float attackPower)
        { 
            owner.GetComponent<MonoBehaviour>().StartCoroutine(BeamAttackRoutine(mousePosition, owner, playerid, skillid, attackPower));
        }

        private IEnumerator BeamAttackRoutine(Vector3 mousePosition, GameObject owner, int playerid, int skillid, float attackPower)
        {
            Vector3 origin = owner.transform.position;
            Vector3 dir = (mousePosition - origin).normalized;
            dir.y = 0;

            float range = attackData.Range;
            float radius = attackData.Radius;
            float duration = attackData.config.attackDuration;
            float interval = attackData.config.attackInterval;

            Quaternion rotation = Quaternion.LookRotation(dir);
            Vector3 halfExtents = new Vector3(radius, 1f, range / 2f);
            Vector3 center = origin + dir * range / 2f;

            float elapsed = 0f;
            
            if (attackData.config.particlePrefab2 != null)
            {
                GameObject beamFX = Instantiate(attackData.config.particlePrefab2, center + Vector3.up * 2f, rotation);
                beamFX.GetComponent<AttackParticle>().SetAttackParticleData(attackData.config.skillType);
                beamFX.transform.localScale = new Vector3(radius, radius, range);
                NetworkServer.Spawn(beamFX);
            }

            // 지속시간 동안 반복 공격
            while (elapsed < duration)
            {
                Collider[] hits = Physics.OverlapBox(center, halfExtents, rotation, LayerMask.GetMask("Player"));

                foreach (var hit in hits)
                {
                    if (hit.gameObject == owner) continue;

                    IDamagable damagable = hit.transform.GetComponent<IDamagable>();
                    if (damagable != null)
                    {
                        damagable.takeDamage((int)attackData.Damage, owner.transform.position, attackData.KnockbackForce, attackData.config, playerid, skillid);

                        // 개별 피격 이펙트
                        if (attackData.config.particlePrefab != null)
                        {
                            Vector3 hitPoint = hit.ClosestPoint(owner.transform.position);
                            Quaternion hitRot = Quaternion.LookRotation((hitPoint - owner.transform.position).normalized);

                            GameObject beamFX = Instantiate(attackData.config.particlePrefab, hitPoint, hitRot);
                            beamFX.GetComponent<AttackParticle>().SetAttackParticleData(attackData.config.skillType);
                            beamFX.transform.localScale = Vector3.one;
                            NetworkServer.Spawn(beamFX);
                        }
                    }
                }
                
                yield return new WaitForSeconds(interval);
                elapsed += interval;
            }

            Debug.Log($"[BeamAttack] 빔 공격 종료 (총 지속 시간: {duration}초)");
        }
    }
}
