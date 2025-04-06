using System.Collections.Generic;
using System.Linq;
using DataSystem;
using DataSystem.Database;
using GameManagement;
using Mirror;
using UI;
using UnityEngine;

namespace Player
{
    public partial class PlayerCharacter
    {
        // ✅ 카드 효과 적용 (클라이언트에서 먼저 호출 가능)
        public void ApplyCardBonuses(List<Database.PlayerCardData> cards)
        {
            if (!isOwned) return;
            
            var last3Cards = cards.Skip(Mathf.Max(0, cards.Count - 3)).ToList();
            
            foreach (var card in last3Cards)
            {
                if (IsBasicStat(card.StatType))
                {
                    CmdModifyPlayerStat(card.StatType, card.BonusStat);
                }
                else if (IsAttackStat(card.StatType))
                {
                    CmdModifyPlayerAttackStat(card.AppliedSkill, card.StatType, card.BonusStat);
                }
            }
            NotifyStatChanged();
            
            var ui = FindFirstObjectByType<PlayerCharacterUI>();
            
            UpdateCount();
        }

        // ✅ 기본 스탯 적용 (서버 전용)
        [Command]
        private void CmdModifyPlayerStat(PlayerStatType statType, float bonusPercent)
        {
            float multiplier = 1 + (bonusPercent / 100f);
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

            Debug.Log($"[서버] 기본 스탯 {statType} +{bonusPercent}% 적용됨");
        }

        // ✅ 공격 관련 스탯 강화 적용 (서버 전용)
        [Command]
        private void CmdModifyPlayerAttackStat(int skillId, PlayerStatType statType, float bonusPercent)
        {
            int skillIndex = System.Array.IndexOf(AttackSkills, skillId);
            if (skillIndex == -1 || skillIndex >= availableAttacks.Length || availableAttacks[skillIndex] == null)
            {
                Debug.LogWarning($"[CmdModifyPlayerAttackStat] 유효하지 않은 스킬 ID: {skillId}, {PlayerSetting.AttackSkillIDs}");
                return;
            }

            var attackData = availableAttacks[skillIndex].GetAttackData();
            if (attackData == null) return;

            float multiplier = 1 + (bonusPercent / 100f);
            float inverseMultiplier = 1 - (bonusPercent / 100f);

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
                default:
                    Debug.LogWarning($"[CmdModifyPlayerAttackStat] 처리되지 않은 공격 스탯: {statType}");
                    break;
            }

            Debug.Log($"[서버] 스킬 {attackData.Name} → {statType} +{bonusPercent}%");
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
