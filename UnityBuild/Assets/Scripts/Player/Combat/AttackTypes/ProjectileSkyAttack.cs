using Mirror;
using UnityEngine;

namespace Player.Combat
{
    public class ProjectileSkyAttack : ProjectileAttack
    {
        // ✅ 공중에서 생성되도록 오버라이드
        protected override Vector3 GetSpawnPosition(Vector3 firePoint)
        {
            return firePoint + attackData.config.attackTrans; // ✅ 기본 위치에서 20m 위에서 발사
        }

        // ✅ lifeTime을 5초로 고정
        public override void Execute(Vector3 mousePosition, Vector3 firePoint, GameObject owner, int playerid, int skillid, float attackPower)
        {
            Vector3 spawnPosition = GetSpawnPosition(firePoint);
            Vector3 direction = (mousePosition - spawnPosition).normalized;

            GameObject projectile = Instantiate(projectilePrefab, spawnPosition, Quaternion.LookRotation(direction));
            AttackProjectile bullet = projectile.GetComponent<AttackProjectile>();

            if (bullet != null)
            {
                bullet.SetProjectileData(
                    attackData.Damage*attackPower,
                    attackData.Speed,
                    attackData.Radius,
                    attackData.Range,
                    5f, // ✅ 항상 5초로 고정
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
                Debug.LogError("ProjectileSkyAttack: AttackProjectile 컴포넌트를 찾을 수 없습니다!");
            }
        }
    }
}