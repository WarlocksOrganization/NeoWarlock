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
        protected float explosionDuration = 3f; // 지속 시간
        protected float explosionInterval = 0.5f; // 폭발 간격
        
        [SerializeField] protected GameObject explosionEffectPrefab; // ✅ 파티클 프리팹
        protected AttackConfig config;

        protected GameObject owner;

        public void Initialize(float damage, float radius, float knockback, AttackConfig config, GameObject owner = null)
        {
            explosionDamage = damage;
            explosionRadius = radius;
            knockbackForce = knockback * knockbackForceFactor;
            this.config = config;
            explosionDuration = config.attackDuration;
            explosionInterval = config.attackInterval;

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
            // Debug.Log($"Explosion in {transform.position.x}, {transform.position.z} owner is on {owner.transform.position.x}, {owner.transform.position.z}");
            foreach (Collider hit in hitColliders)
            {
                IDamagable damagable = hit.transform.GetComponent<IDamagable>();
                if (damagable != null)
                {   
                    // ✅ 공격 타입에 따라 대상을 구분하여 데미지 적용
                    if (config.attackType == DataSystem.Constants.AttackType.Melee && hit.transform.gameObject == owner) continue;
                    if (config.attackType == DataSystem.Constants.AttackType.Self && hit.transform.gameObject != owner) continue;
                    // Debug.Log($"Damaging {hit.transform.gameObject.name}");
                    damagable.takeDamage((int)explosionDamage, transform.position, knockbackForce, config);
                }
            }
        }

        protected virtual GameObject CreateParticleEffect()
        {
            // ✅ 서버에서 파티클 효과 생성
            if (explosionEffectPrefab != null)
            {
                GameObject effect = Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
                effect.GetComponent<AttackParticle>().SetAttackParticleData(config.skillType);
                NetworkServer.Spawn(effect); // ✅ 네트워크에 동기화
                return effect;
            }

            return null;
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
