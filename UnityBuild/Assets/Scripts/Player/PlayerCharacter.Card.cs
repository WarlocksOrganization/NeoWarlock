using System.Collections.Generic;
using DataSystem.Database;
using GameManagement;
using Mirror;
using UI;
using UnityEngine;

namespace Player
{
    public partial class PlayerCharacter
    {
        private void ApplyCardBonuses() 
        {
            foreach (var cardData in GameManagement.PlayerSetting.PlayerCards)
            {
                switch (cardData.StatType)
                {
                    // ✅ 기본 스탯 강화 (체력, 이동속도)
                    case PlayerStatType.Health:
                    case PlayerStatType.Speed:
                    case PlayerStatType.AttackPower:
                        CmdModifyPlayerStat(cardData.StatType, cardData.BonusStat);
                        break;

                    // ✅ 특정 스킬 강화 (개별 AttackData 수정)
                    case PlayerStatType.AttackSpeed:
                    case PlayerStatType.Range:
                    case PlayerStatType.Radius:
                    case PlayerStatType.Damage:
                    case PlayerStatType.KnockbackForce:
                    case PlayerStatType.Cooldown:
                        CmdModifyPlayerAttackStat(cardData.AppliedSkill, cardData.StatType, cardData.BonusStat);
                        break;

                    case PlayerStatType.Special:
                        break;

                    default:
                        Debug.LogError($"[ApplyCardBonuses] 알 수 없는 StatType: {cardData.StatType}");
                        break;
                }
            } 
        }
        
        [Command]
        private void CmdModifyPlayerStat(PlayerStatType statType, float bonusPercentage)
        {
            float multiplier = 1 + (bonusPercentage / 100f); // ✅ 퍼센트 증가 반영

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
                    Debug.LogError($"[CmdModifyPlayerStat] 알 수 없는 스탯: {statType}");
                    return;
            }

            Debug.Log($"[서버] {statType} 강화 적용: {bonusPercentage}% 증가");
        }


        [Command]
        private void CmdModifyPlayerAttackStat(int skillId, PlayerStatType statType, float bonusPercentage)
        {
            // skillId → index 매핑 (플레이어가 들고 있는 스킬 중에서)
            int skillIndex = -1;
            for (int i = 0; i < PlayerSetting.AttackSkillIDs.Length; i++)
            {
                if (PlayerSetting.AttackSkillIDs[i] == skillId)
                {
                    skillIndex = i;
                    break;
                }
            }

            if (skillIndex == -1)
            {
                Debug.LogWarning($"[강화 무시] 플레이어가 보유하지 않은 스킬 ID: {skillId}");
                return;
            }

            if (skillIndex >= availableAttacks.Length || availableAttacks[skillIndex] == null)
                return;

            var attackData = availableAttacks[skillIndex].GetAttackData();
            if (attackData == null) return;

            float multiplier = 1 + (bonusPercentage / 100f);

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
                    float cooldownMultiplier = 1 - (bonusPercentage / 100f);
                    attackData.Cooldown = Mathf.Max(0.5f, attackData.Cooldown * cooldownMultiplier);
                    break;
                default:
                    Debug.LogError($"[CmdModifyPlayerAttackStat] 알 수 없는 스탯: {statType}");
                    return;
            }

            Debug.Log($"[서버] {attackData.Name} 강화: {statType} +{bonusPercentage}%");
        }
    }
}
