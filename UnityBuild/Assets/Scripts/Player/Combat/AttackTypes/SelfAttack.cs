using Mirror;
using UnityEngine;

namespace Player.Combat
{
    public class SelfAttack : AttackBase
    {
        public override void Execute(Vector3 mousePosition, Vector3 firePoint, GameObject owner, int playerid, int skillid, float attackPower)
        {
            // ✅ 공통 로직: 발사 위치 결정
            Vector3 spawnPosition = owner.transform.position;

            Vector3 direction = (mousePosition - spawnPosition).normalized;
            direction.y = 0f; // ✅ y축 제거하여 직선 발사

            GameObject projectile = Instantiate(projectilePrefab, spawnPosition, Quaternion.LookRotation(direction, Vector3.up));
            AttackProjectile bullet = projectile.GetComponent<AttackProjectile>();

            if (bullet != null)
            {
                bullet.SetProjectileData(
                    attackData.Damage*attackPower,
                    attackData.Speed,
                    attackData.Radius,
                    attackData.Range,
                    5f,
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
                Debug.LogError("ProjectileAttack: AttackProjectile 컴포넌트를 찾을 수 없습니다!");
            }
        }
    }
}