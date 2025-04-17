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

        // 현재 선택된 공격 인덱스를 서버와 클라이언트 간 동기화
        [SyncVar(hook = nameof(OnCurrentAttackChanged))]
        private int currentAttackIndex = -1;
        private IAttack currentAttack;

        // 플레이어가 선택 가능한 공격 스킬 배열 (기본 4개 + 아이템 스킬 1개)
        public IAttack[] availableAttacks = new IAttack[5];

        // 특정 상황에서만 사용하는 공격 모음
        private Dictionary<int, IAttack> certainAttacks = new();
        private Dictionary<int, AttackBase> activeAttacks = new();

        public readonly float BaseAttackPower = 1;
        
        // 공격력 수치를 동기화 (버프/디버프에 따라 변경 가능)
        [SyncVar(hook = nameof(OnAttackPowerChanged))] public float AttackPower = 1;
        public float BasePower => BaseAttackPower;
        public float CurrentAttackPower => AttackPower;

        // 아이템에 의해 획득한 스킬 ID 동기화
        [SyncVar(hook = nameof(OnItemSkillChanged))]
        public int itemSkillId = -1;

        // 공격 키 입력 처리 (Classic / AOS 방식 지원)
        private void UpdateAttack()
        {
            if (PlayerSetting.PlayerKeyType == Constants.KeyType.Classic)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1)) SetAttackType(1);
                if (Input.GetKeyDown(KeyCode.Alpha2)) SetAttackType(2);
                if (Input.GetKeyDown(KeyCode.Alpha3)) SetAttackType(3);
                if (Input.GetKeyDown(KeyCode.Alpha4)) SetAttackType(4);
            }
            else if (PlayerSetting.PlayerKeyType == Constants.KeyType.AOS)
            {
                if (Input.GetKeyDown(KeyCode.Q)) SetAttackType(1);
                if (Input.GetKeyDown(KeyCode.W)) SetAttackType(2);
                if (Input.GetKeyDown(KeyCode.E)) SetAttackType(3);
                if (Input.GetKeyDown(KeyCode.R)) SetAttackType(4);
            }

            if (Input.GetKeyDown(KeyCode.Escape)) SetAttackType(0); // 스킬 선택 해제

            if (currentAttack == null) return;

            // 마우스 클릭 시 공격 실행
            if (Input.GetMouseButtonDown(0) && currentAttack.IsReady())
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hitInfo, Mathf.Infinity, mouseTargetLayer))
                {
                    Attack(hitInfo.point);
                }
            }
        }

        private void OnAttackPowerChanged(float oldValue, float newValue) => NotifyStatChanged();

        private void OnCurrentAttackChanged(int oldIndex, int newIndex)
        {
            if (newIndex >= 0 && newIndex < availableAttacks.Length)
            {
                currentAttack = availableAttacks[newIndex];
            }
        }

        // 공격 스킬 선택
        private void SetAttackType(int index)
        {
            if (index == 0)
            {
                playerUI?.SelectSkill(index, false);
                currentAttackIndex = 0;
                currentAttack = null;
                playerProjector.CloseProjectile(); // 조준 이펙트 제거
                return;
            }

            if (index > 0 && index < availableAttacks.Length && availableAttacks[index]?.IsReady() == true)
            {
                currentAttackIndex = index;
                currentAttack = availableAttacks[index];
                CmdSetAttackType(index); // 서버에 선택 사실 전송
            }
        }

        // 아이템으로 인해 스킬이 추가되었을 때 실행
        private void OnItemSkillChanged(int _, int newSkillId)
        {
            if (newSkillId > 0)
            {
                SetAvailableAttack(4, newSkillId);
                if (isOwned)
                {
                    AudioManager.Instance.PlaySFX(Constants.SoundType.SFX_AcquireItem);
                }
            }
        }

        // 특정 인덱스에 스킬 할당
        public void SetAvailableAttack(int index, int skillId)
        {
            var data = Database.GetAttackData(skillId);
            if (data == null)
            {
                Debug.LogWarning($"[SetAvailableAttack] SkillID {skillId} data not found");
                return;
            }

            var clone = new Database.AttackData(data); // 공격 데이터 복사
            IAttack attack = CreateAttackInstance(clone);
            availableAttacks[index] = attack;

            if (isOwned)
            {
                playerUI ??= FindFirstObjectByType<PlayerCharacterUI>();
                playerUI?.SetQuickSlotData(index, clone.Icon, clone.Cooldown, clone.DisplayName, clone.Description);
            }

            // 서버에 정보 전달
            if (NetworkClient.active)
            {
                CmdSetAvailableAttack(index, skillId);
            }
        }

        // AttackConfig를 기반으로 공격 객체 생성
        private IAttack CreateAttackInstance(Database.AttackData data)
        {
            if (data.config == null)
            {
                Debug.LogError($"[CreateAttackInstance] {data.Name} has no config");
                return null;
            }

            GameObject go = new GameObject($"{data.Name} Attack");
            AttackBase attack = data.config.attackType switch
            {
                Constants.AttackType.Projectile => go.AddComponent<ProjectileAttack>(),
                Constants.AttackType.ProjectileSky => go.AddComponent<ProjectileSkyAttack>(),
                Constants.AttackType.Point => go.AddComponent<PointAttack>(),
                Constants.AttackType.Area => go.AddComponent<AreaAttack>(),
                Constants.AttackType.Melee => go.AddComponent<MeleeAttack>(),
                Constants.AttackType.Self => go.AddComponent<SelfAttack>(),
                Constants.AttackType.Beam => go.AddComponent<BeamAttack>(),
                Constants.AttackType.SpreadProjectile => go.AddComponent<SpreadProjectileAttack>(),
                _ => null
            };

            if (attack == null)
            {
                Debug.LogError($"[CreateAttackInstance] Unknown attack type: {data.config.attackType}");
                return null;
            }

            attack.transform.SetParent(transform);
            attack.projectilePrefab = data.config.Prefab;
            attack.Initialize(data);

            return attack;
        }

        // 서버에 스킬 설정 요청
        [Command(requiresAuthority = false)]
        public void CmdSetAvailableAttack(int index, int skillId)
        {
            var data = Database.GetAttackData(skillId);
            if (data == null) return;

            if (availableAttacks[index] == null)
                availableAttacks[index] = CreateAttackInstance(data);

            TargetUpdateAvailableAttack(connectionToClient, index, skillId);
        }

        // 클라이언트에 스킬 설정 동기화
        [TargetRpc]
        private void TargetUpdateAvailableAttack(NetworkConnectionToClient target, int index, int skillId)
        {
            if (availableAttacks[index] != null) return;

            var data = Database.GetAttackData(skillId);
            if (data == null) return;

            availableAttacks[index] = CreateAttackInstance(data);

            if (index == 4 && isOwned)
                PlayerSetting.ItemSkillID = skillId;
        }

        // 서버에서 선택된 스킬 인덱스 적용 후 전체 클라이언트에 전파
        [Command(requiresAuthority = false)]
        private void CmdSetAttackType(int index)
        {
            currentAttackIndex = index;
            currentAttack = availableAttacks[index];
            RpcSetAttackType(index);
        }

        // 클라이언트에서 UI, 투사체 조준기 등 처리
        [ClientRpc(includeOwner = true)]
        private void RpcSetAttackType(int index)
        {
            if (!isOwned) return;

            playerUI?.SelectSkill(index, true);
            playerProjector.SetDecalProjector(currentAttack, mouseTargetLayer, transform);
        }

        // 공격 실행 요청
        public void Attack(Vector3 target)
        {
            if (Time.time < attackLockTime) return;

            Vector3 dir = (target - transform.position).normalized;
            dir.y = 0;
            playerModel.transform.rotation = Quaternion.LookRotation(dir);

            float range = currentAttack.GetAttackData().Range;
            if (Vector3.Distance(transform.position, target) > range)
                target = transform.position + dir * range;

            playerUI?.UseSkill(currentAttackIndex, currentAttack.GetAttackData().Cooldown);

            float delay = currentAttack.GetAttackData().config.attackDelay;
            attackLockTime = currentAttack.GetAttackData().config.recoveryTime;
            if (attackLockTime > 0)
            {
                isMovingToTarget = false;
                _targetPosition = transform.position;
            }

            StartCoroutine(ExecuteAttack(target, delay));
        }

        // 공격 실행 전 애니메이션 및 이펙트 처리
        private IEnumerator ExecuteAttack(Vector3 target, float delay)
        {
            CmdTriggerAnimation(currentAttack.GetAttackData().config.animParameter);
            currentAttack.LastUsedTime = Time.time;
            int index = currentAttackIndex;

            CmdPlaySkillEffect(currentAttack.GetAttackData().config.skillType);
            SetAttackType(0); // 선택 해제

            yield return new WaitForSeconds(delay);

            int skillId = index == 4 ? PlayerSetting.ItemSkillID : PlayerSetting.AttackSkillIDs[index];
            CmdAttack(target, index, playerId, skillId);
        }

        [Command(requiresAuthority = false)]
        private void CmdPlaySkillEffect(Constants.SkillType type)
        {
            RpcPlaySkillEffect(type);
        }

        [ClientRpc]
        private void RpcPlaySkillEffect(Constants.SkillType type)
        {
            AudioManager.Instance.PlaySFX(type, gameObject);
            effectSystem?.PlaySkillEffect(type);
        }

        // 서버에서 실제 공격 처리
        [Command(requiresAuthority = false)]
        public void CmdAttack(Vector3 target, int index, int id, int skillId)
        {
            var serverSkillId = availableAttacks[index]?.GetAttackData()?.ID ?? -1;

            // 서버와 클라이언트 간 스킬 ID 불일치 시 보정
            if (serverSkillId != skillId)
            {
                Debug.LogWarning($"[CmdAttack] 서버와 클라이언트 스킬 ID 불일치! 서버: {serverSkillId}, 클라: {skillId}");
                var data = Database.GetAttackData(skillId);
                if (data != null)
                {
                    availableAttacks[index] = CreateAttackInstance(data);
                }
            }

            if (availableAttacks[index] == null)
            {
                Debug.LogError($"[CmdAttack] 서버에서 스킬ID {skillId} 데이터 찾을 수 없음");
                return;
            }

            Vector3 dir = (target - transform.position).normalized;
            Vector3 firePos = attackTransform.position + dir;
            availableAttacks[index].Execute(target, firePos, gameObject, id, skillId, AttackPower);

            // 아이템 스킬은 한 번 사용 후 제거
            if (index == 4)
            {
                itemSkillId = -1;
                if (availableAttacks[4] is MonoBehaviour oldAttack)
                    Destroy(oldAttack.gameObject);
                availableAttacks[4] = null;
            }
        }

        // 서버에서 아이템 스킬 설정
        [Server]
        public void ServerSetItemSkill(int skillId)
        {
            itemSkillId = skillId;
            SetAvailableAttack(4, skillId);
            TargetSetItemSkill(connectionToClient, skillId);
        }

        // 클라이언트에 아이템 스킬 ID 설정 전파
        [TargetRpc]
        private void TargetSetItemSkill(NetworkConnection target, int skillId)
        {
            PlayerSetting.ItemSkillID = skillId;
        }
    }
}
