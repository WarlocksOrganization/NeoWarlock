using DataSystem.Database;
using Mirror;
using UnityEngine;

namespace Player.Combat
{
    public class AreaAttack : AttackBase
    {
        public override void Execute(Vector3 mousePosition, Vector3 firePoint, GameObject owner, int playerid, int skillid)
        {
            // ✅ 공통 로직: 발사 위치 결정
            Vector3 spawnPosition = mousePosition;

            Vector3 direction = (mousePosition - spawnPosition).normalized;
            direction.y = 0f; // ✅ y축 제거하여 직선 발사

            GameObject projectile = Instantiate(projectilePrefab, spawnPosition, Quaternion.identity);
            AttackProjectile bullet = projectile.GetComponent<AttackProjectile>();

            if (bullet != null)
            {
                // ✅ 서버에서 직접 `SyncVar` 데이터 설정 후 Spawn
                bullet.SetProjectileData(
                    attackData.Damage,
                    attackData.Speed,
                    attackData.Radius,
                    attackData.Range,
                    attackData.Range / attackData.Speed,
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
