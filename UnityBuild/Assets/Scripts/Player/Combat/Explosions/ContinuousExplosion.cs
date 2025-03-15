using System.Collections;
using Interfaces;
using Mirror;
using UnityEngine;

namespace Player.Combat
{
    public class ContinuousExplosion : Explosion
    {
        public override void OnStartServer()
        {
            CreateParticleEffect();
            StartCoroutine(ExplodeContinuously());
        }

        protected virtual IEnumerator ExplodeContinuously()
        {
            float elapsedTime = 0f;

            while (elapsedTime < explosionDuration)
            {
                yield return new WaitForSeconds(base.explosionInterval);
                Explode(); // 기존 Explosion의 피해 로직 실행
                elapsedTime += explosionInterval;
            }

            StartCoroutine(AutoDestroy());
        }
    }
}
