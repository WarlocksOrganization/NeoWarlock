using System.Collections;
using Interfaces;
using Mirror;
using UnityEngine;

namespace Player.Combat
{
    public class ContinuousGrowExplosion : ContinuousExplosion
    {
        private GameObject explosionEffectInstance; // 생성된 이펙트 인스턴스 저장

        public override void OnStartServer()
        {
            StartCoroutine(ExplodeContinuously());
        }

        protected override IEnumerator ExplodeContinuously()
        {
            float startTime = Time.time;
            float initialRadius = explosionRadius; // 초기 반경 저장
            float maxRadius = initialRadius * 2f; // 최대 반경 (2배 커짐)

            // ✅ 초기 이펙트 생성 (여기서 인스턴스를 저장해두고 이후 크기 변경)
            explosionEffectInstance = CreateParticleEffect();

            while (Time.time - startTime < explosionDuration)
            {
                yield return new WaitForSeconds(base.explosionInterval);

                // 폭발 반경이 점점 커지게
                float progress = (Time.time - startTime) / explosionDuration; // 진행 비율 (0~1)
                explosionRadius = Mathf.Lerp(initialRadius, maxRadius, progress); // 반경 증가

                // ✅ 이펙트 크기를 부드럽게 증가시키는 코루틴 실행
                if (explosionEffectInstance != null)
                {
                    StartCoroutine(SmoothScaleEffect(explosionEffectInstance, explosionRadius));
                }

                Explode(); // ✅ 현재 커진 반경으로 폭발 실행
            }

            StartCoroutine(AutoDestroy());
        }

        protected override GameObject CreateParticleEffect()
        {
            // ✅ 서버에서 파티클 효과 생성
            if (explosionEffectPrefab != null)
            {
                GameObject effect = Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
                effect.GetComponent<AttackParticle>().SetAttackParticleData(config.skillType);

                // ✅ 초기 크기 설정 (작게 시작)
                effect.transform.localScale = Vector3.zero;

                NetworkServer.Spawn(effect); // ✅ 네트워크 동기화
                return effect;
            }

            return null;
        }

        private IEnumerator SmoothScaleEffect(GameObject effect, float targetRadius)
        {
            float duration = explosionInterval; // 이펙트 크기 증가 시간
            float elapsedTime = 0f;
            Vector3 startScale = effect.transform.localScale;
            Vector3 targetScale = new Vector3(targetRadius, targetRadius, targetRadius);

            while (elapsedTime < duration)
            {
                effect.transform.localScale = Vector3.Lerp(startScale, targetScale, elapsedTime / duration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            effect.transform.localScale = targetScale; // 최종 크기 보정
        }
    }
}