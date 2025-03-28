using System.Collections;
using Interfaces;
using Mirror;
using UnityEngine;

namespace Player.Combat
{
    public class ContinuousRandomExplosion : ContinuousExplosion
    {
        protected override IEnumerator ExplodeContinuously()
        {
            float elapsedTime = 0f;

            // ✅ 첫 번째 폭발을 즉시 실행 (한 프레임 대기 후)
            yield return null;
            ExplodeAt(transform.position);

            while (elapsedTime < explosionDuration)
            {
                yield return new WaitForSeconds(base.explosionInterval);

                Vector3 randomPosition = GetRandomPositionInCircle(transform.position, explosionRadius);
                ExplodeAt(randomPosition);

                elapsedTime += base.explosionInterval;
            }

            StartCoroutine(AutoDestroy());
        }

        private Vector3 GetRandomPositionInCircle(Vector3 center, float radius)
        {
            float randomAngle = Random.Range(0f, Mathf.PI * 2f); // 0 ~ 360도 범위
            float randomRadius = Random.Range(0f, radius*2); // ✅ explosionRadius만 사용

            float offsetX = Mathf.Cos(randomAngle) * randomRadius;
            float offsetZ = Mathf.Sin(randomAngle) * randomRadius;

            return new Vector3(center.x + offsetX, center.y, center.z + offsetZ);
        }

        private void ExplodeAt(Vector3 position)
        {
            if (!isServer) return; // ✅ 네트워크 서버 체크

            Collider[] hitColliders = Physics.OverlapSphere(position, explosionRadius);
            
            foreach (Collider hit in hitColliders)
            {
                IDamagable damagable = hit.transform.GetComponent<IDamagable>();
                if (damagable != null)
                {
                    // ✅ 공격 타입별 타겟 필터링
                    if (config.attackType == DataSystem.Constants.AttackType.Melee && hit.transform.gameObject == owner) continue;
                    if (config.attackType == DataSystem.Constants.AttackType.Self && hit.transform.gameObject != owner) continue;

                    damagable.takeDamage((int)explosionDamage, position, knockbackForce, config,playerid, skillid);
                }
            }

            // ✅ 폭발 이펙트 생성 및 네트워크 동기화
            if (explosionEffectPrefab != null)
            {
                GameObject effect = Instantiate(explosionEffectPrefab, position, Quaternion.identity);
                effect.GetComponent<AttackParticle>().SetAttackParticleData(config.skillType);
                NetworkServer.Spawn(effect);
            }
        }
    }
}
