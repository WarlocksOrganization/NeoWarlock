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

        protected int playerid;
        protected int skillid;
        protected float radius;
        
        [SerializeField] protected GameObject explosionEffectPrefab; // ✅ 파티클 프리팹
        protected AttackConfig config;

        protected GameObject owner;

        public void Initialize(float damage, float radius, float knockback, AttackConfig config, GameObject owner, int playerid, int skillid)
        {
            explosionDamage = damage;
            explosionRadius = radius;
            knockbackForce = knockback * knockbackForceFactor;
            this.config = config;
            explosionDuration = config.attackDuration;
            explosionInterval = config.attackInterval;
            this.radius = radius;

            explosionEffectPrefab.transform.localScale = new Vector3(radius, radius, radius);

            this.owner = owner;
            
            this.playerid = playerid;
            this.skillid = skillid;
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

            // ✅ 원기둥(캡슐) 충돌 범위 설정
            Vector3 bottom = transform.position + Vector3.down * 0.5f;  // 바닥 위치
            Vector3 top = transform.position + Vector3.up * 2.0f;       // 원기둥 높이 조절
            float radius = explosionRadius; // 원기둥 반지름 (기존의 반지름 유지)

            Collider[] hitColliders = Physics.OverlapCapsule(bottom, top, radius);
    
            foreach (Collider hit in hitColliders)
            {
                IDamagable damagable = hit.transform.GetComponent<IDamagable>();
                if (damagable != null)
                {   
                    // ✅ 공격 타입에 따라 대상을 구분하여 데미지 적용
                    if (config.attackType == DataSystem.Constants.AttackType.Melee && hit.transform.gameObject == owner) continue;
                    if (config.attackType == DataSystem.Constants.AttackType.Self && hit.transform.gameObject != owner) continue;

                    damagable.takeDamage((int)explosionDamage, transform.position, knockbackForce, config, playerid, skillid);
                }
            }
        }

        protected virtual GameObject CreateParticleEffect()
        {
            // ✅ 서버에서 파티클 효과 생성
            if (explosionEffectPrefab != null)
            {
                GameObject effect = Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
                effect.transform.localScale = new Vector3(radius, radius, radius);  // ✅ 프리팹 인스턴스에서만 변경
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
