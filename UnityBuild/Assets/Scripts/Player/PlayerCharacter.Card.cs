using System;
using System.Collections.Generic;
using System.Linq;
using DataSystem.Database;
using Mirror;
using UI;
using UnityEngine;

namespace Player
{
    public partial class PlayerCharacter
    {
        // 카드 보너스 효과를 적용하는 메서드 (클라이언트에서 호출됨)
        public void ApplyCardBonuses(List<Database.PlayerCardData> cards)
        {
            // 자신이 조작하는 캐릭터가 아닐 경우 무시
            if (!isOwned) return;

            // 가장 마지막에 선택한 3장의 카드만 효과 적용
            var last3Cards = cards.Skip(Mathf.Max(0, cards.Count - 3)).ToList();

            // 각 카드에 대해 효과 적용
            foreach (var card in last3Cards)
            {
                if (IsBasicStat(card.StatType))
                {
                    // 체력, 방어력 등의 기본 스탯 보너스는 직접 캐릭터에 적용
                    CmdModifyPlayerStat(card.StatType, card.BonusStat);
                }
                else if (IsAttackStat(card.StatType))
                {
                    // 스킬 데미지/쿨다운 등 공격 관련 스탯일 경우
                    CmdModifyPlayerAttackStat(card.AppliedSkill, card.StatType, card.BonusStat);

                    // 강화 카드로 인해 스킬 ID가 변경된 경우 (ex. 기본 101 -> 강화 201)
                    var upgradedSkillId = card.AppliedSkill + 100;
                    var index = Array.IndexOf(AttackSkills, card.AppliedSkill);

                    // 기존 스킬 자리에 강화된 스킬 ID 등록 (서버에서도 적용)
                    if (index != -1)
                        CmdSetAvailableAttack(index, upgradedSkillId);
                }
            }

            // UI나 다른 시스템에 스탯이 변경되었음을 알림
            NotifyStatChanged();

            // 플레이어 UI 참조 (추후 UI 업데이트용)
            var ui = FindFirstObjectByType<PlayerCharacterUI>();
        }
        
        // ✅ 기본 스탯 적용 (서버 전용)
        [Command]
        private void CmdModifyPlayerStat(PlayerStatType statType, float bonusPercent)
        {
            var multiplier = 1 + bonusPercent / 100f;
            switch (statType)
            {
                case PlayerStatType.Health:
                    maxHp = (int)(maxHp * multiplier);
                    curHp = maxHp;
                    break;
                case PlayerStatType.Speed:
                    MaxSpeed *= multiplier;
                    MoveSpeed = MaxSpeed;
                    break;
                case PlayerStatType.AttackPower:
                    AttackPower *= multiplier;
                    break;
                default:
                    Debug.LogWarning($"[CmdModifyPlayerStat] 처리되지 않은 기본 스탯: {statType}");
                    break;
            }

            Debug.Log($"[서버] id : {playerId} 기본 스탯 {statType} +{bonusPercent}% 적용됨");
        }

        // ✅ 공격 관련 스탯 강화 적용 (서버 전용)
        [Command]
        private void CmdModifyPlayerAttackStat(int skillId, PlayerStatType statType, float bonusPercent)
        {
            var skillIndex = Array.IndexOf(AttackSkills, skillId);
            if (skillIndex == -1 || availableAttacks[skillIndex] == null) return;

            var attackData = availableAttacks[skillIndex].GetAttackData();
            if (attackData == null) return;

            var multiplier = 1 + bonusPercent / 100f;
            var inverseMultiplier = 1 - bonusPercent / 100f;

            switch (statType)
            {
                case PlayerStatType.AttackSpeed:
                    attackData.Speed *= multiplier;
                    break;
                case PlayerStatType.Range:
                    attackData.Range *= multiplier;
                    break;
                case PlayerStatType.Radius:
                    attackData.Radius *= multiplier;
                    break;
                case PlayerStatType.Damage:
                    attackData.Damage *= multiplier;
                    break;
                case PlayerStatType.KnockbackForce:
                    attackData.KnockbackForce *= multiplier;
                    break;
                case PlayerStatType.Cooldown:
                    attackData.Cooldown = Mathf.Max(0.5f, attackData.Cooldown * inverseMultiplier);
                    break;
            }

            Debug.Log($"[서버] id : {playerId} 스킬 {attackData.Name} → {statType} +{bonusPercent}%");

            // 🔁 클라이언트에도 똑같이 반영
            TargetModifyPlayerAttackStat(connectionToClient, skillId, statType, bonusPercent);
        }

        [TargetRpc]
        private void TargetModifyPlayerAttackStat(NetworkConnection target, int skillId, PlayerStatType statType,
            float bonusPercent)
        {
            var skillIndex = Array.IndexOf(AttackSkills, skillId);
            if (skillIndex == -1 || availableAttacks[skillIndex] == null) return;

            var attackData = availableAttacks[skillIndex].GetAttackData();
            if (attackData == null) return;

            var multiplier = 1 + bonusPercent / 100f;
            var inverseMultiplier = 1 - bonusPercent / 100f;

            switch (statType)
            {
                case PlayerStatType.AttackSpeed:
                    attackData.Speed *= multiplier;
                    break;
                case PlayerStatType.Range:
                    attackData.Range *= multiplier;
                    break;
                case PlayerStatType.Radius:
                    attackData.Radius *= multiplier;
                    break;
                case PlayerStatType.Damage:
                    attackData.Damage *= multiplier;
                    break;
                case PlayerStatType.KnockbackForce:
                    attackData.KnockbackForce *= multiplier;
                    break;
                case PlayerStatType.Cooldown:
                    attackData.Cooldown = Mathf.Max(0.5f, attackData.Cooldown * inverseMultiplier);
                    break;
            }

            Debug.Log($"[클라] id : {playerId} 스킬 {attackData.Name} → {statType} +{bonusPercent}%");
        }

        // ✅ 스탯 타입 판별 유틸
        private bool IsBasicStat(PlayerStatType stat)
        {
            return stat is PlayerStatType.Health or PlayerStatType.Speed or PlayerStatType.AttackPower;
        }

        private bool IsAttackStat(PlayerStatType stat)
        {
            return stat is PlayerStatType.AttackSpeed or PlayerStatType.Range or PlayerStatType.Radius
                or PlayerStatType.Damage or PlayerStatType.KnockbackForce or PlayerStatType.Cooldown;
        }
    }
}