using System.Collections;
using Interfaces;
using Mirror;
using UnityEngine;

namespace Player.Combat
{
    public class ContinuousExplosion : Explosion
    {
        private float explosionDuration = 3f; // 지속 시간
        private float explosionInterval = 0.5f; // 폭발 간격

        public override void OnStartServer()
        {
            CreateParticleEffect();
            StartCoroutine(ExplodeContinuously());
        }

        private IEnumerator ExplodeContinuously()
        {
            float elapsedTime = 0f;

            while (elapsedTime < explosionDuration)
            {
                yield return new WaitForSeconds(explosionInterval);
                Explode(); // 기존 Explosion의 피해 로직 실행
                elapsedTime += explosionInterval;
            }

            StartCoroutine(AutoDestroy());
        }
    }
}
