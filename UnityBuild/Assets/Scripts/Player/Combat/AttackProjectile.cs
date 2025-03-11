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

        [SyncVar] private GameObject owner;

        public void SetProjectileData(float damage, float speed, float radius, float range, float lifeTime, float knockback, AttackConfig config, GameObject owner = null)
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

        protected void OnCollisionEnter(Collision col)
        {
            if (!isServer) return;
            if ((layerMask.value & (1 << col.gameObject.layer)) == 0) return;
            // ✅ 근접 공격은 공격자와 동일한 레이어에 속한 대상은 무시
            if (this.attackConfig.attackType is Constants.AttackType.Melee && col.gameObject == this.owner) return;
            // ✅ 근접 공격일 경우 플레이어에게만 피해를 줌
            if (this.attackConfig.attackType is Constants.AttackType.Melee && col.gameObject.GetComponent<CharacterController>() == null) return;
            Debug.Log("OnCollisionEnter");
            Explode();
        }

        protected void OnTriggerEnter(Collider col)
        {
            if (!isServer) return;
            Debug.Log("hit");
            if ((layerMask.value & (1 << col.gameObject.layer)) == 0) return;
            // ✅ 근접 공격은 공격자와 동일한 레이어에 속한 대상은 무시
            Debug.Log("hit2");
            if (this.attackConfig.attackType is Constants.AttackType.Melee && col.gameObject == this.owner) return;
            // ✅ 근접 공격일 경우 플레이어에게만 피해를 줌
            if (this.attackConfig.attackType is Constants.AttackType.Melee && col.gameObject.GetComponent<CharacterController>() == null) return;
            Debug.Log("OnTriggerEnter");
            Explode();
        }

        protected void Explode()
        {
            // ✅ 공격별 파티클 효과 적용
            if (attackConfig != null)
            {
                GameObject explosion = Instantiate(attackConfig.explosionEffectPrefab, transform.position, Quaternion.identity);
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
