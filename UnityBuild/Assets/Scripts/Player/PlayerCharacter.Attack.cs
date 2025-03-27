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
        private Dictionary<int, IAttack> certainAttacks = new();
        private Dictionary<int, AttackBase> activeAttacks = new();

        [SyncVar] public float BaseAttackPower = 1;
        [SyncVar] public float AttackPower = 1;

        [SyncVar(hook = nameof(OnItemSkillChanged))]
        public int itemSkillId = -1;

        private void UpdateAttack()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) SetAttackType(1);
            if (Input.GetKeyDown(KeyCode.Alpha2)) SetAttackType(2);
            if (Input.GetKeyDown(KeyCode.Alpha3)) SetAttackType(3);
            if (Input.GetKeyDown(KeyCode.Alpha4)) SetAttackType(4);
            if (Input.GetKeyDown(KeyCode.Escape)) SetAttackType(0);

            if (currentAttack == null) return;

            if (Input.GetMouseButtonDown(0) && currentAttack.IsReady())
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hitInfo, Mathf.Infinity, mouseTargetLayer))
                {
                    Attack(hitInfo.point);
                }
            }
        }

        private void OnCurrentAttackChanged(int oldIndex, int newIndex)
        {
            if (newIndex >= 0 && newIndex < availableAttacks.Length)
            {
                currentAttack = availableAttacks[newIndex];
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

            if (index > 0 && index < availableAttacks.Length && availableAttacks[index]?.IsReady() == true)
            {
                currentAttackIndex = index;
                currentAttack = availableAttacks[index];
                CmdSetAttackType(index);
            }
        }

        private void OnItemSkillChanged(int _, int newSkillId)
        {
            if (newSkillId > 0)
                SetAvailableAttack(4, newSkillId);
        }

        public void SetAvailableAttack(int index, int skillId)
        {
            var data = Database.GetAttackData(skillId);
            if (data == null)
            {
                Debug.LogWarning($"[SetAvailableAttack] SkillID {skillId} data not found");
                return;
            }

            var clone = new Database.AttackData(data);
            IAttack attack = CreateAttackInstance(clone);
            availableAttacks[index] = attack;

            if (isOwned)
            {
                playerUI ??= FindFirstObjectByType<PlayerCharacterUI>();
                playerUI?.SetQuickSlotData(index, clone.Icon, clone.Cooldown, clone.DisplayName, clone.Description);
            }

            if (NetworkClient.active)
                CmdSetAvailableAttack(index, skillId);
        }

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

        [Command(requiresAuthority = false)]
        public void CmdSetAvailableAttack(int index, int skillId)
        {
            var data = Database.GetAttackData(skillId);
            if (data == null) return;

            if (availableAttacks[index] == null)
                availableAttacks[index] = CreateAttackInstance(data);

            TargetUpdateAvailableAttack(connectionToClient, index, skillId);
        }

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

        [Command(requiresAuthority = false)]
        private void CmdSetAttackType(int index)
        {
            currentAttackIndex = index;
            currentAttack = availableAttacks[index];
            RpcSetAttackType(index);
        }

        [ClientRpc(includeOwner = true)]
        private void RpcSetAttackType(int index)
        {
            if (!isOwned) return;

            playerUI?.SelectSkill(index, true);
            playerProjector.SetDecalProjector(currentAttack, mouseTargetLayer, transform);
        }

        public void Attack(Vector3 target)
        {
            if (Time.time < attackLockTime) return;

            Vector3 dir = (target - transform.position).normalized;
            playerModel.transform.rotation = Quaternion.LookRotation(dir);

            float range = currentAttack.GetAttackData().Range;
            if (Vector3.Distance(transform.position, target) > range)
                target = transform.position + dir * range;

            playerUI?.UseSkill(currentAttackIndex, currentAttack.GetAttackData().Cooldown);

            float delay = currentAttack.GetAttackData().config.attackDelay;
            attackLockTime = currentAttack.GetAttackData().config.recoveryTime;
            isMovingToTarget = false;
            _targetPosition = transform.position;

            playerProjector.CloseProjectile();
            StartCoroutine(ExecuteAttack(target, delay));
        }

        private IEnumerator ExecuteAttack(Vector3 target, float delay)
        {
            CmdTriggerAnimation(currentAttack.GetAttackData().config.animParameter);
            currentAttack.LastUsedTime = Time.time;
            int index = currentAttackIndex;

            CmdPlaySkillEffect(currentAttack.GetAttackData().config.skillType);
            SetAttackType(0);

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

        [Command(requiresAuthority = false)]
        public void CmdAttack(Vector3 target, int index, int id, int skillId)
        {
            Vector3 dir = (target - transform.position).normalized;
            Vector3 firePos = attackTransform.position + dir;
            availableAttacks[index]?.Execute(target, firePos, gameObject, id, skillId, AttackPower);

            if (index == 4) itemSkillId = -1;
        }

        [Command(requiresAuthority = false)]
        public void CmdCertainAttack(Vector3 target, int skillId, bool useOrigin)
        {
            if (!certainAttacks.ContainsKey(skillId))
            {
                var data = Database.GetAttackData(skillId);
                if (data != null)
                    certainAttacks[skillId] = CreateAttackInstance(data);
            }

            if (certainAttacks.TryGetValue(skillId, out var attack))
            {
                Vector3 firePos = useOrigin ? attackTransform.position : attackTransform.position + (target - transform.position).normalized;
                attack.Execute(target, firePos, gameObject, playerId, skillId, AttackPower);
            }
        }
    }
}