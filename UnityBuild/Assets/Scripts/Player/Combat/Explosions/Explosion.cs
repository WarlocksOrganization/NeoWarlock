using System.Collections;
using Interfaces;
using Mirror;
using Unity.Burst.CompilerServices;
using UnityEngine;

namespace Player.Combat
{
    public class Explosion : NetworkBehaviour
    {
        protected float explosionDamage;
        protected float explosionRadius;
        protected float knockbackForce;
        protected float knockbackForceFactor = 20f;
        [SerializeField] protected GameObject explosionEffectPrefab; // ✅ 파티클 프리팹
        protected AttackConfig config;

        protected GameObject owner;

        public void Initialize(float damage, float radius, float knockback, AttackConfig config, GameObject owner = null)
        {
            explosionDamage = damage;
            explosionRadius = radius;
            knockbackForce = knockback * knockbackForceFactor;
            this.config = config;

            explosionEffectPrefab.transform.localScale = new Vector3(radius, radius, radius);

            this.owner = owner;
        }

        public override void OnStartServer()
        {
            CreateParticleEffect();
            Explode();
            StartCoroutine(AutoDestroy());
        }

        protected void Explode()
        {
            if (!isServer) return; // ✅ 서버에서만 실행
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosionRadius);

            foreach (Collider hit in hitColliders)
            {
                IDamagable damagable = hit.transform.GetComponent<IDamagable>();
                if (damagable != null && (config.attackType != DataSystem.Constants.AttackType.Melee || hit.transform.gameObject != this.owner))
                {
                    damagable.takeDamage((int)explosionDamage, transform.position, knockbackForce, config);
                }
            }
        }

        protected void CreateParticleEffect()
        {
            // ✅ 서버에서 파티클 효과 생성
            if (explosionEffectPrefab != null)
            {
                GameObject effect = Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
                effect.GetComponent<AttackParticle>().SetAttackParticleData(config.skillType);
                NetworkServer.Spawn(effect); // ✅ 네트워크에 동기화
            }
        }

        protected IEnumerator AutoDestroy()
        {
            yield return new WaitForSeconds(5f);

            if (isServer)
            {
                NetworkServer.Destroy(gameObject); // ✅ 서버에서만 제거
            }
            else
            {
                Destroy(gameObject); // ✅ 클라이언트에서도 안전하게 제거
            }
        }
    }
}
