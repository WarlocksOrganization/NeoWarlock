using System;
using System.Collections;
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

        [SyncVar] protected int playerid;
        [SyncVar] protected int skillid;

        protected Vector3 moveDirection;
        protected Rigidbody rb;
        protected AttackConfig attackConfig; // ✅ 공격별 설정값 저장
        
        [Header("Skill Effects")]
        [SerializeField] private List<Constants.SkillEffectGameObjectEntry> skilleffectList = new List<Constants.SkillEffectGameObjectEntry>();
        private Dictionary<Constants.SkillType, GameObject> skillEffects;

        [SyncVar] protected GameObject owner;
        private bool isExplode = false;
        
        [SerializeField] private GameObject rangeDecalPrefab;
        private GameObject rangeDecalInstance;
        
        [SerializeField] private Transform colliderTransform;


        public void SetProjectileData(float damage, float speed, float radius, float range, float lifeTime, float knockback, AttackConfig config, GameObject owner, int playerid, int skillid)
        {
            this.damage = damage;
            this.speed = speed;
            this.radius = radius;
            this.range = range;
            this.lifeTime = lifeTime;
            this.knockbackForce = knockback;
            
            this.playerid = playerid;
            this.skillid = skillid;

            attackConfig = config; // ✅ 인스턴스 내에서 참조
            transform.localScale = new Vector3(radius, radius, radius);

            skillType = config.skillType;

            if (config.attackType == Constants.AttackType.ProjectileSky && colliderTransform)
            {
                colliderTransform.localScale = new Vector3(1f /radius,
                    1f / radius,
                    1f / radius);
            }

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
            
            ShowExplosionRange();
            
            Invoke(nameof(DestroySelf), lifeTime);
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
    
            if (NetworkClient.active)
            {
                return; // 호스트 모드라면 종료
            }

            moveDirection = transform.forward;
            rb = GetComponent<Rigidbody>();

            if (rb != null)
            {
                StartCoroutine(MoveProjectile()); // ✅ MovePosition을 이용한 이동 처리
            }
            
            ShowExplosionRange();
            
            Invoke(nameof(DestroySelf), lifeTime);
        } 

        protected virtual IEnumerator MoveProjectile()
        {
            while (true)
            {
                rb.MovePosition(rb.position + moveDirection * speed * Time.fixedDeltaTime);
                if (rb.transform.position.y <= 0)
                {
                    transform.position = new Vector3(transform.position.x, 0, transform.position.z);
                    Debug.Log("MoveProjectile : " + transform.position);
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
                
                var colPlayer = col.GetComponent<PlayerCharacter>();
                var ownerPlayer = owner != null ? owner.GetComponent<PlayerCharacter>() : null;

                if (colPlayer != null && ownerPlayer != null &&
                    colPlayer.team != Constants.TeamType.None &&
                    colPlayer.team == ownerPlayer.team)
                {
                    return; // 같은 팀이므로 무시
                }
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
                if (attackConfig.attackType == Constants.AttackType.Self || attackConfig.attackType == Constants.AttackType.Melee)
                {
                    explosionPosition.y -= 2f; // 근접 및 자기 공격은 발사 지점 고수
                }
                else if (Physics.Raycast(transform.position + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 10f, layerMask))
                {
                    explosionPosition = hit.point + Vector3.up * 0.1f; // 레이캐스트 충돌 지점 바로 위에서 생성
                }

                // 2️⃣ 파티클 이펙트 생성
                GameObject explosion = Instantiate(attackConfig.explosionEffectPrefab, explosionPosition, Quaternion.identity);
                Explosion explosionComponent = explosion.GetComponent<Explosion>();

                if (explosionComponent != null)
                {
                    explosionComponent.Initialize(damage, radius, knockbackForce, attackConfig, this.owner, playerid, skillid);
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

        private void OnDestroy()
        {
            if (rangeDecalInstance != null)
            {
                Destroy(rangeDecalInstance);
            }
        }

        private void ShowExplosionRange()
        {
            if (rangeDecalPrefab == null) return;

            Vector3 predictedPosition = transform.position;

            if (Physics.Raycast(transform.position + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 999f, layerMask))
            {
                predictedPosition = hit.point + Vector3.up * 0.05f; // 약간 띄워서 그리기
            }

            rangeDecalInstance = Instantiate(rangeDecalPrefab, predictedPosition, Quaternion.Euler(90, 0, 0));
            rangeDecalInstance.transform.localScale = Vector3.one;
    
            // Decal Projector의 Size를 반지름에 맞게 설정
            var projector = rangeDecalInstance.GetComponent<UnityEngine.Rendering.Universal.DecalProjector>();
            if (projector != null)
            {
                projector.size = new Vector3(radius * 2f, radius * 2f, 20f);
            }
        }

    }
}
