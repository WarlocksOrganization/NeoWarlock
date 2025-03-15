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

            while (elapsedTime < explosionDuration)
            {
                yield return new WaitForSeconds(base.explosionInterval);
                
                // 원형 범위 내 랜덤 위치 계산 (y축은 원래 위치 유지)
                Vector3 randomPosition = GetRandomPositionInCircle(transform.position, explosionRadius);

                // 폭발을 해당 위치에서 실행
                ExplodeAt(randomPosition);
                
                elapsedTime += base.explosionInterval;
            }

            StartCoroutine(AutoDestroy());
        }

        private Vector3 GetRandomPositionInCircle(Vector3 center, float radius)
        {
            float randomAngle = Random.Range(0f, 2f * Mathf.PI); // 0 ~ 360도 랜덤 각도
            float randomRadius = Random.Range(0f, 10); // 반지름 내 랜덤 거리

            float offsetX = Mathf.Cos(randomAngle) * randomRadius;
            float offsetZ = Mathf.Sin(randomAngle) * randomRadius;

            return new Vector3(center.x + offsetX, center.y, center.z + offsetZ); // ✅ y축은 기존과 동일
        }

        private void ExplodeAt(Vector3 position)
        {
            if (!isServer) return;

            Collider[] hitColliders = Physics.OverlapSphere(position, explosionRadius);
            
            foreach (Collider hit in hitColliders)
            {
                IDamagable damagable = hit.transform.GetComponent<IDamagable>();
                if (damagable != null)
                {
                    // ✅ 공격 타입에 따라 대상을 구분하여 데미지 적용
                    if (config.attackType == DataSystem.Constants.AttackType.Melee && hit.transform.gameObject == owner) continue;
                    if (config.attackType == DataSystem.Constants.AttackType.Self && hit.transform.gameObject != owner) continue;

                    damagable.takeDamage((int)explosionDamage, position, knockbackForce, config);
                }
            }

            // ✅ 해당 위치에서 파티클 효과 생성
            if (explosionEffectPrefab != null)
            {
                GameObject effect = Instantiate(explosionEffectPrefab, position, Quaternion.identity);
                effect.GetComponent<AttackParticle>().SetAttackParticleData(config.skillType);
                NetworkServer.Spawn(effect); // ✅ 네트워크 동기화
            }
        }
    }
}
