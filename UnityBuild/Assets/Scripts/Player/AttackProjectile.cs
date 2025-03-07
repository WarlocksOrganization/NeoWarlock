using Mirror;
using Player.Combat;
using UnityEngine;

namespace Player
{
    public class AttackProjectile : NetworkBehaviour
    {
        [SerializeField] private LayerMask layerMask;
        [SerializeField] private GameObject explosionPrefab;

        [SyncVar] private float damage;
        [SyncVar] private float speed;
        [SyncVar] private float radius;
        [SyncVar] private float range;
        [SyncVar] private float lifeTime;
        [SyncVar] private float knockbackForce;

        private Vector3 moveDirection;
        private Rigidbody rb;
        private AttackConfig attackConfig; // ✅ 공격별 설정값 저장

        public void SetProjectileData(float damage, float speed, float radius, float range, float lifeTime, float knockback, AttackConfig config)
        {
            this.damage = damage;
            this.speed = speed;
            this.radius = radius;
            this.range = range;
            this.lifeTime = lifeTime;
            this.knockbackForce = knockback;

            attackConfig = config; // ✅ 인스턴스 내에서 참조
            transform.localScale = new Vector3(radius, radius, radius);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            moveDirection = transform.forward;
            rb = GetComponent<Rigidbody>();

            if (rb != null)
            {
                StartCoroutine(MoveProjectile()); // ✅ MovePosition을 이용한 이동 처리
            }

            Invoke(nameof(EnsureCorrectPosition), 0.1f);
            Invoke(nameof(DestroySelf), lifeTime);
        }

        private System.Collections.IEnumerator MoveProjectile()
        {
            while (true)
            {
                rb.MovePosition(rb.position + moveDirection * speed * Time.fixedDeltaTime);
                yield return new WaitForFixedUpdate();
            }
        }

        private void EnsureCorrectPosition()
        {
            transform.position += Vector3.up * 0.1f; // ✅ 클라이언트 위치 보정
        }

        private void DestroySelf()
        {
            if (isServer)
            {
                NetworkServer.Destroy(gameObject);
            }
        }

        private void OnCollisionEnter(Collision col)
        {
            if (!isServer) return;

            if ((layerMask.value & (1 << col.gameObject.layer)) == 0) return;

            Explode();
        }

        private void OnTriggerEnter(Collider col)
        {
            if (!isServer) return;

            if ((layerMask.value & (1 << col.gameObject.layer)) == 0) return;

            Explode();
        }

        private void Explode()
        {
            // ✅ 공격별 파티클 효과 적용
            if (attackConfig != null && attackConfig.explosionEffectPrefab != null)
            {
                GameObject explosion = Instantiate(explosionPrefab, transform.position, Quaternion.identity);
                Explosion explosionComponent = explosion.GetComponent<Explosion>();

                if (explosionComponent != null)
                {
                    explosionComponent.Initialize(damage, radius, knockbackForce, attackConfig.explosionEffectPrefab, attackConfig);
                }

                NetworkServer.Spawn(explosion);
            }

            NetworkServer.Destroy(gameObject);
        }
    }
}
