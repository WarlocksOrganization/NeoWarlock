using System.Collections.Generic;
using DataSystem;
using Mirror;
using Player.Combat;
using Unity.VisualScripting;
using UnityEngine;

namespace Player
{
    public class AttackProjectile : NetworkBehaviour
    {
        [SerializeField] protected LayerMask layerMask;

        [SyncVar] protected float damage;
        [SyncVar] protected float speed;
        [SyncVar] protected float radius;
        [SyncVar] protected float range;
        [SyncVar] protected float lifeTime;
        [SyncVar] protected float knockbackForce;
        [SyncVar(hook = nameof(OnSkillEffectChanged))] 
        protected Constants.SkillType skillType = Constants.SkillType.None;

        protected Vector3 moveDirection;
        protected Rigidbody rb;
        protected AttackConfig attackConfig; // ✅ 공격별 설정값 저장
        
        [Header("Skill Effects")]
        [SerializeField] private List<Constants.SkillEffectGameObjectEntry> skilleffectList = new List<Constants.SkillEffectGameObjectEntry>();
        private Dictionary<Constants.SkillType, GameObject> skillEffects;

        [SyncVar] protected GameObject owner;
        private bool isExplode = false;

        public void SetProjectileData(float damage, float speed, float radius, float range, float lifeTime, float knockback, AttackConfig config, GameObject owner)
        {
            this.damage = damage;
            this.speed = speed;
            this.radius = radius;
            this.range = range;
            this.lifeTime = lifeTime;
            this.knockbackForce = knockback;

            attackConfig = config; // ✅ 인스턴스 내에서 참조
            transform.localScale = new Vector3(radius, radius, radius);

            skillType = config.skillType;

            this.owner = owner;
        }

        void Awake()
        {
            InitializeSkillEffects();
        }
        
        private void InitializeSkillEffects()
        {
            skillEffects = new Dictionary<Constants.SkillType, GameObject>();

            foreach (var entry in skilleffectList)
            {
                if (!skillEffects.ContainsKey(entry.skillType) && entry.gObject != null)
                {
                    skillEffects.Add(entry.skillType, entry.gObject);
                }
            }
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
            
            Invoke(nameof(DestroySelf), lifeTime);
        }

        protected System.Collections.IEnumerator MoveProjectile()
        {
            while (true)
            {
                rb.MovePosition(rb.position + moveDirection * speed * Time.fixedDeltaTime);
                if (rb.transform.position.y <= 0)
                {
                    transform.position = new Vector3(transform.position.x, 0, transform.position.z);
                    Explode();
                }
                yield return new WaitForFixedUpdate();
            }
        }

        protected void DestroySelf()
        {
            if (isServer)
            {
                NetworkServer.Destroy(gameObject);
            }
        }

        protected virtual void OnTriggerEnter(Collider col)
        {
            if (!isServer) return;
            
            if (this.attackConfig.attackType is Constants.AttackType.Self)
            {
                if (col.gameObject != this.owner) return;
            }
            else
            {
                if ((layerMask & (1 << col.gameObject.layer)) == 0) return;
                if(col.gameObject == this.owner) return;
            }
            if (!isExplode)
            {
                isExplode = true;
                Explode();
            }
        }

        protected virtual void Explode()
        {
            // ✅ 공격별 파티클 효과 적용
            if (attackConfig != null)
            {
                // 1️⃣ 레이캐스트를 이용하여 지형이나 충돌 가능한 오브젝트 위에서 폭발 위치 설정
                Vector3 explosionPosition = transform.position + Vector3.up * 2f; // 기본적으로 살짝 위에서 시작
                if (Physics.Raycast(transform.position + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 10f, layerMask))
                {
                    explosionPosition = hit.point + Vector3.up * 0.1f; // 레이캐스트 충돌 지점 바로 위에서 생성
                }

                // 2️⃣ 파티클 이펙트 생성
                GameObject explosion = Instantiate(attackConfig.explosionEffectPrefab, explosionPosition, Quaternion.identity);
                Explosion explosionComponent = explosion.GetComponent<Explosion>();

                if (explosionComponent != null)
                {
                    explosionComponent.Initialize(damage, radius, knockbackForce, attackConfig, this.owner);
                }

                NetworkServer.Spawn(explosion);
            }

            NetworkServer.Destroy(gameObject);
        }


        private void OnSkillEffectChanged(Constants.SkillType oldValue, Constants.SkillType newValue)
        {
            foreach (var skill in skillEffects)
            {
                skill.Value.SetActive(false);
            }
            if (skillEffects.ContainsKey(newValue))
            {
                skillEffects[newValue].SetActive(true);
            }
        }
    }
}
