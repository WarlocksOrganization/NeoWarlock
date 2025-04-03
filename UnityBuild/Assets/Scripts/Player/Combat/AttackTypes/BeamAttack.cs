using UnityEngine;
using Mirror;
using DataSystem.Database;
using Interfaces;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Player.Combat
{
    public class BeamAttack : AttackBase
    {
        public override void Execute(Vector3 mousePosition, Vector3 firePoint, GameObject owner, int playerid, int skillid, float attackPower)
        {
            Vector3 origin = owner.transform.position;
            Vector3 dir = (mousePosition - origin).normalized;
            dir.y = 0; // 평면 기준

            float range = attackData.Range;
            float radius = attackData.Radius;

            // ✅ 빔 범위 내 모든 적 검출 (OverlapBox 등)
            Vector3 center = origin + dir * range / 2f;
            Quaternion rotation = Quaternion.LookRotation(dir);
            Vector3 halfExtents = new Vector3(radius, 1f, range / 2f);

            Collider[] hits = Physics.OverlapBox(center, halfExtents, rotation, LayerMask.GetMask("Player")); // 적 레이어로 교체

            foreach (var hit in hits)
            {
                if (hit.gameObject == owner)
                {
                    continue;
                }
                IDamagable damagable = hit.transform.GetComponent<IDamagable>();
                if (damagable != null)
                {
                    damagable.takeDamage((int)attackData.Damage, owner.transform.position, attackData.KnockbackForce, attackData.config, playerid, skillid);
                    
                    if (attackData.config.particlePrefab != null)
                    {
                        Vector3 hitPoint = hit.ClosestPoint(owner.transform.position);
                        Quaternion hitRot = Quaternion.LookRotation((hitPoint - owner.transform.position).normalized);

                        GameObject beamFX = Instantiate(
                            attackData.config.particlePrefab,
                            hitPoint,
                            hitRot
                        );
                        
                        beamFX.GetComponent<AttackParticle>().SetAttackParticleData(attackData.config.skillType);
                        beamFX.transform.localScale = new Vector3(radius, radius, range);
                        NetworkServer.Spawn(beamFX);
                    }
                }
            }

            // ✅ 이펙트 생성
            if (attackData.config.particlePrefab2 != null)
            {
                GameObject beamFX = Instantiate(
                    attackData.config.particlePrefab2,
                    center + Vector3.up * 2f,
                    rotation
                );
                beamFX.GetComponent<AttackParticle>().SetAttackParticleData(attackData.config.skillType);
                beamFX.transform.localScale = new Vector3(radius, radius, range);
                NetworkServer.Spawn(beamFX);
            }

            Debug.Log($"[BeamAttack] 빔 공격 발동 - 타겟 수: {hits.Length}");
        }
        
        private void OnDrawGizmosSelected()
        {
#if UNITY_EDITOR
            if (attackData == null || !EditorApplication.isPlaying) return;

            Vector3 origin = transform.position;
            Vector3 dir = transform.forward;
            dir.y = 0;

            float range = attackData.Range;
            float radius = attackData.Radius;

            Vector3 center = origin + dir * range / 2f;
            Quaternion rotation = Quaternion.LookRotation(dir);
            Vector3 halfExtents = new Vector3(radius, 1f, range / 2f);

            // ✅ 빔 영역 박스 그리기
            Gizmos.color = new Color(1f, 0f, 0f, 0.4f);
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(center, rotation, Vector3.one);
            Gizmos.DrawCube(Vector3.zero, halfExtents * 2f);
            Gizmos.matrix = oldMatrix;
#endif
        }

    }
}