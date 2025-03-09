using System.Collections;
using DataSystem;
using DataSystem.Database;
using Interfaces;
using kcp2k;
using Mirror;
using Player.Combat;
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

        private void UpdateAttack()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) SetAttackType(1);
            if (Input.GetKeyDown(KeyCode.Alpha2)) SetAttackType(2);
            if (Input.GetKeyDown(KeyCode.Alpha3)) SetAttackType(3);
            if (Input.GetKeyDown(KeyCode.Alpha4)) SetAttackType(4);
            
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
            var attackData = Database.GetAttackData(skillId);

            if (attackData != null)
            {
                IAttack attackInstance = CreateAttackInstance(attackData);
                availableAttacks[index] = attackInstance;

                if (playerUI != null)
                {
                    // ✅ ScriptableObject 데이터 활용
                    playerUI.SetQuickSlotData(index, attackData.Icon, attackData.Cooldown);
                }
            }
            else
            {
                Debug.LogWarning("공격 데이터를 가져오지 못했습니다.");
            }

            CmdSetAvailableAttack(index, skillId);
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

                default:
                    Debug.LogError($"알 수 없는 공격 타입: {data.config.attackType}");
                    return null;
            }

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
                    attackInstance.LastUsedTime = Time.time - attackInstance.CooldownTime;
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

            playerUI?.UseSkill(currentAttackIndex);

            float attackDelay = currentAttack.GetAttackData().config.attackDelay;
            float recoveryTime = currentAttack.GetAttackData().config.recoveryTime;

            attackLockTime = recoveryTime; // ✅ 행동 불가 시간 설정
            playerProjector.CloseProjectile();
    
            StartCoroutine(ExecuteAttack(targetPosition, attackDelay));
        }

        private IEnumerator ExecuteAttack(Vector3 targetPosition, float attackDelay)
        {
            // ✅ 애니메이션 실행
            switch (currentAttack.GetAttackData().config.attackType)
            {
                case Constants.AttackType.Projectile:
                case Constants.AttackType.ProjectileSky:
                case Constants.AttackType.Point:
                case Constants.AttackType.Area:
                    CmdTriggerAnimation(currentAttack.GetAttackData().config.animParameter);
                    break;
                default:
                    Debug.LogError($"알 수 없는 공격 타입: {currentAttack.GetAttackData().config.attackType}");
                    break;
            }
            currentAttack.LastUsedTime = Time.time;
            int nextAttckIndex = currentAttackIndex;
            
            CmdPlaySkillEffect(currentAttack.GetAttackData().config.skillType);
            
            SetAttackType(0);

            yield return new WaitForSeconds(attackDelay); // ✅ 공격 딜레이 적용

            CmdAttack(targetPosition, nextAttckIndex);
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
        public void CmdAttack(Vector3 attackPosition, int nextAttckIndex)
        {
            Vector3 direction = (attackPosition - transform.position).normalized;
            
            availableAttacks[nextAttckIndex]?.Execute(attackPosition, 
                attackTransform.position + availableAttacks[nextAttckIndex].GetAttackData().Radius* 1f*direction, gameObject);
        }
    }
}
