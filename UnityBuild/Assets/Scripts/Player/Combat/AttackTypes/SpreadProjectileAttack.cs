using System;
using Mirror;
using UnityEngine;

namespace Player.Combat
{
    public class SpreadProjectileAttack : ProjectileAttack
    {
        protected override Vector3 GetSpawnPosition(Vector3 firePoint)
        {
            return firePoint; // 정면 기본 위치 그대로
        }

        public override void Execute(Vector3 mousePosition, Vector3 firePoint, GameObject owner, int playerid, int skillid, float attackPower)
        {
            Vector3 spawnCenter = GetSpawnPosition(firePoint);
            Vector3 direction = (mousePosition - spawnCenter).normalized;
            direction.y = 0f;

            Quaternion lookRotation = Quaternion.LookRotation(direction, Vector3.up);

            // 왼쪽, 가운데, 오른쪽 3개 위치
            Vector3 right = Vector3.Cross(Vector3.up, direction).normalized;
            float offset = attackData.Radius*1.5f;

            Vector3[] positions =
            {
                spawnCenter - right * offset*2f,
                spawnCenter - right * offset,
                spawnCenter,
                spawnCenter + right * offset,
                spawnCenter + right * offset*2f
            };

            foreach (Vector3 pos in positions)
            {
                GameObject projectile = Instantiate(projectilePrefab, pos, lookRotation);
                AttackProjectile bullet = projectile.GetComponent<AttackProjectile>();

                if (bullet != null)
                {
                    bullet.SetProjectileData(
                        attackData.Damage * attackPower,
                        attackData.Speed,
                        attackData.Radius,
                        attackData.Range,
                        attackData.Range / Math.Abs(attackData.Speed),
                        attackData.KnockbackForce,
                        attackData.config,
                        owner,
                        playerid,
                        skillid
                    );

                    NetworkServer.Spawn(projectile);
                }
                else
                {
                    Debug.LogError("[SpreadProjectileAttack] AttackProjectile 컴포넌트 누락!");
                }
            }
        }
    }
}
