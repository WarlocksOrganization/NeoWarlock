using System.Collections;
using System.Collections.Generic;
using DataSystem;
using DataSystem.Database;
using GameManagement;
using Interfaces;
using kcp2k;
using Mirror;
using Player.Combat;
using UI;
using UnityEngine;

namespace Player
{
    public partial class PlayerCharacter : IAttackable
    {
        [Header("Attack")]
        [SerializeField] private PlayerProjector playerProjector;
        private Vector3 aimPosition;

        [SyncVar(hook = nameof(OnCurrentAttackChanged))]
        [SerializeField] private int currentAttackIndex = -1;
        private IAttack currentAttack;
        
        public IAttack[] availableAttacks = new IAttack[5];
        private Dictionary<int, IAttack> certainAttacks = new Dictionary<int, IAttack>();
        
        private Dictionary<int, AttackBase> activeAttacks = new Dictionary<int, AttackBase>(); // ✅ 캐싱용 딕셔너리

        
        private void UpdateAttack()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) SetAttackType(1);
            if (Input.GetKeyDown(KeyCode.Alpha2)) SetAttackType(2);
            if (Input.GetKeyDown(KeyCode.Alpha3)) SetAttackType(3);
            
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                SetAttackType(0);
            }

            if (currentAttack == null) return;

            if (Input.GetMouseButtonDown(0))
            {
                if (currentAttack.IsReady())
                {
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    if (Physics.Raycast(ray, out RaycastHit hitInfo, Mathf.Infinity, mouseTargetLayer))
                    {
                        Attack(hitInfo.point);
                    }
                }
                else
                {
                    float remainingTime = (currentAttack.LastUsedTime + currentAttack.CooldownTime) - Time.time;
                    playerProjector.CloseProjectile();
                    Debug.Log($"공격 준비 중! 남은 쿨타임: {remainingTime:F1}초");
                }
            }
        }
        
        private void OnCurrentAttackChanged(int oldIndex, int newIndex)
        {
            if (newIndex >= 0 && newIndex < availableAttacks.Length)
            {
                currentAttack = availableAttacks[newIndex];
                //Debug.Log($"공격 타입 동기화 완료: {currentAttack.GetType().Name}");
            }
        }

        private void SetAttackType(int index)
        {
            if (index == 0)
            {
                playerUI?.SelectSkill(index, false);
                currentAttackIndex = 0;
                currentAttack = null;
                playerProjector.SetDecalProjector(null, mouseTargetLayer, transform);
                return;
            }

            if (index > 0 && index < availableAttacks.Length && availableAttacks[index] != null &&
                availableAttacks[index].IsReady())
            {
                currentAttackIndex = index;
                currentAttack = availableAttacks[currentAttackIndex];

                CmdSetAttackType(index);
            }
        }
        
        public void SetAvailableAttack(int index, int skillId)
        {
            var originalAttackData = Database.GetAttackData(skillId);

            if (originalAttackData != null)
            {
                // ✅ 원본 AttackData를 복사하여 개별적인 인스턴스를 생성
                var playerAttackData = new Database.AttackData()
                {
                    ID = originalAttackData.ID,
                    Name = originalAttackData.Name,
                    DisplayName = originalAttackData.DisplayName,
                    Description = originalAttackData.Description,
                    Speed = originalAttackData.Speed,
                    Range = originalAttackData.Range,
                    Radius = originalAttackData.Radius,
                    Damage = originalAttackData.Damage,
                    KnockbackForce = originalAttackData.KnockbackForce,
                    Cooldown = originalAttackData.Cooldown,
                    config = originalAttackData.config,
                    Icon = originalAttackData.Icon
                };

                // ✅ 개별 AttackData를 기반으로 스킬 인스턴스 생성
                IAttack attackInstance = CreateAttackInstance(playerAttackData);
                availableAttacks[index] = attackInstance;

                if (isOwned && playerUI == null)
                {
                    playerUI = FindFirstObjectByType<PlayerCharacterUI>();
                }
                if (playerUI != null)
                {
                    playerUI.SetQuickSlotData(index, playerAttackData.Icon, playerAttackData.Cooldown, playerAttackData.DisplayName, playerAttackData.Description);
                }
            }
            else
            {
                Debug.LogWarning($"[SetAvailableAttack] 공격 데이터를 가져오지 못했습니다. (SkillID: {skillId})");
            }

            if (NetworkClient.active)
            {
                CmdSetAvailableAttack(index, skillId);
            }
        }


        
        private AttackBase CreateAttackInstance(Database.AttackData data)
        {
            if (data.config == null)
            {
                Debug.LogError($"AttackData {data.Name}에 설정된 AttackConfig가 없습니다!");
                return null;
            }

            AttackBase attackInstance = null;

            switch (data.config.attackType)
            {
                case Constants.AttackType.Projectile:
                    attackInstance = new GameObject($"{data.Name} Attack").AddComponent<ProjectileAttack>();
                    break;

                case Constants.AttackType.ProjectileSky:
                    attackInstance = new GameObject($"{data.Name} Attack").AddComponent<ProjectileSkyAttack>();
                    break;

                case Constants.AttackType.Point:
                    attackInstance = new GameObject($"{data.Name} Attack").AddComponent<PointAttack>();
                    break;

                case Constants.AttackType.Area:
                    attackInstance = new GameObject($"{data.Name} Attack").AddComponent<AreaAttack>();
                    break;

                case Constants.AttackType.Melee:
                    attackInstance = new GameObject($"{data.Name} Attack").AddComponent<MeleeAttack>();
                    break;

                case Constants.AttackType.Self:
                    attackInstance = new GameObject($"{data.Name} Attack").AddComponent<SelfAttack>();
                    break;

                default:
                    Debug.LogError($"알 수 없는 공격 타입: {data.config.attackType}");
                    return null;
            }
            
            attackInstance.transform.SetParent(transform);
            
            // ✅ 공통 속성 설정
            attackInstance.projectilePrefab = data.config.Prefab;
            attackInstance.Initialize(data);

            return attackInstance;
        }

        
        [Command]
        public void CmdSetAvailableAttack(int index, int skillId)
        {
            var attackData = Database.GetAttackData(skillId);

            if (attackData != null)
            {
                // 💡 이미 존재하는 인스턴스인지 확인
                if (availableAttacks[index] == null)
                {
                    IAttack attackInstance = CreateAttackInstance(attackData);
                    availableAttacks[index] = attackInstance;
                }

                // 클라이언트에게 데이터 동기화
                TargetUpdateAvailableAttack(connectionToClient, index, skillId);
            }
            else
            {
                Debug.LogWarning("서버에서 공격 데이터를 가져오지 못했습니다.");
            }
        }
        
        [TargetRpc]
        private void TargetUpdateAvailableAttack(NetworkConnectionToClient target, int index, int skillId)
        {
            var attackData = Database.GetAttackData(skillId);

            if (attackData != null)
            {
                // 💡 이미 존재하는 인스턴스인지 확인 후, 새로운 인스턴스를 생성하지 않음
                if (availableAttacks[index] == null)
                {
                    IAttack attackInstance = CreateAttackInstance(attackData);
                    availableAttacks[index] = attackInstance;
                }
            }
            else
            {
                Debug.LogWarning("클라이언트에서 공격 데이터를 가져오지 못했습니다.");
            }
        }

        [Command]
        private void CmdSetAttackType(int index)
        {
            currentAttackIndex = index;
            currentAttack = availableAttacks[currentAttackIndex];

            RpcSetAttackType(index);
        }

        [ClientRpc(includeOwner = true)]
        private void RpcSetAttackType(int index)
        {
            if (isOwned)
            {
                playerUI?.SelectSkill(index, true);
                playerProjector.SetDecalProjector(currentAttack, mouseTargetLayer, transform);
            }
        }
        
        public void Attack(Vector3 targetPosition)
        {
            if (Time.time < attackLockTime) return; // ✅ 공격 중일 때 중복 실행 방지

            // ✅ 현재 마우스 위치와 플레이어(또는 fireTransform) 위치 간 거리 계산
            float distance = Vector3.Distance(transform.position, targetPosition);

            Vector3 direction = (targetPosition - transform.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            playerModel.transform.rotation = targetRotation;
            
            // ✅ 거리가 공격 가능 거리(attackRange)보다 크면 제한
            if (distance > currentAttack.GetAttackData().Range)
            {
                targetPosition = transform.position + direction *(currentAttack.GetAttackData().Range);
            }

            playerUI?.UseSkill(currentAttackIndex, currentAttack.GetAttackData().Cooldown);

            float attackDelay = currentAttack.GetAttackData().config.attackDelay;
            float recoveryTime = currentAttack.GetAttackData().config.recoveryTime;

            attackLockTime = recoveryTime; // ✅ 행동 불가 시간 설정
            if (recoveryTime > 0)
            {
                isMovingToTarget = false;
                _targetPosition = transform.position;
            }
           
            playerProjector.CloseProjectile();
    
            StartCoroutine(ExecuteAttack(targetPosition, attackDelay));
        }

        private IEnumerator ExecuteAttack(Vector3 targetPosition, float attackDelay)
        {
            // ✅ 애니메이션 실행
            CmdTriggerAnimation(currentAttack.GetAttackData().config.animParameter);
            currentAttack.LastUsedTime = Time.time;
            int nextAttckIndex = currentAttackIndex;
            
            CmdPlaySkillEffect(currentAttack.GetAttackData().config.skillType);
            
            SetAttackType(0);

            yield return new WaitForSeconds(attackDelay); // ✅ 공격 딜레이 적용

            CmdAttack(targetPosition, nextAttckIndex, playerId, PlayerSetting.AttackSkillIDs[nextAttckIndex]);
        }
        
        [Command]
        private void CmdPlaySkillEffect(Constants.SkillType skillType)
        {
            RpcPlaySkillEffect(skillType);
        }

        [ClientRpc]
        private void RpcPlaySkillEffect(Constants.SkillType skillType)
        {
            if (effectSystem != null)
            {
                effectSystem.PlaySkillEffect(skillType);
            }
        }


        [Command]
        public void CmdAttack(Vector3 attackPosition, int nextAttckIndex, int id, int skillId)
        {
            Vector3 direction = (attackPosition - transform.position).normalized;
            
            availableAttacks[nextAttckIndex]?.Execute(attackPosition, 
                attackTransform.position + direction, gameObject, id, skillId);
        }

        [Command]
        public void CmdCertainAttack(Vector3 attackPosition, int skillId, bool originPosition)  
        {
            Vector3 direction = (attackPosition - transform.position).normalized;
            if (!certainAttacks.ContainsKey(skillId))
            {
                var attackData = Database.GetAttackData(skillId);
                if (attackData != null)
                {
                    IAttack attackInstance = CreateAttackInstance(attackData);
                    certainAttacks.Add(skillId, attackInstance);
                }
            }

            if (certainAttacks.ContainsKey(skillId))
            {
                Vector3 firePosition = originPosition ? attackTransform.position : attackTransform.position + direction;
                certainAttacks[skillId].Execute(attackPosition, 
                    firePosition, gameObject, playerId, skillId);
            }
        }
    }
}
