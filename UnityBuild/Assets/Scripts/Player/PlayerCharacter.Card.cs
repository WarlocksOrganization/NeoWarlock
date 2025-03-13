using System.Collections.Generic;
using DataSystem.Database;
using Mirror;
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
                        CmdModifyPlayerStat(cardData.StatType, cardData.BonusStat);
                        break;

                    // ✅ 특정 스킬 강화 (개별 AttackData 수정)
                    case PlayerStatType.AttackSpeed:
                    case PlayerStatType.Range:
                    case PlayerStatType.Radius:
                    case PlayerStatType.Damage:
                    case PlayerStatType.KnockbackForce:
                    case PlayerStatType.Cooldown:
                        CmdModifyPlayerAttackStat(cardData.AppliedSkillIndex, cardData.StatType, cardData.BonusStat);
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

                default:
                    Debug.LogError($"[CmdModifyPlayerStat] 알 수 없는 스탯: {statType}");
                    return;
            }

            Debug.Log($"[서버] {statType} 강화 적용: {bonusPercentage}% 증가");
        }


       [Command]
       private void CmdModifyPlayerAttackStat(int skillIndex, PlayerStatType statType, float bonusPercentage)
       {
           if (skillIndex < 0 || skillIndex >= availableAttacks.Length) return;
           if (availableAttacks[skillIndex] == null) return;

           var attackData = availableAttacks[skillIndex].GetAttackData();
           if (attackData == null) return;

           float multiplier = 1 + (bonusPercentage / 100f); // ✅ 퍼센트 증가 반영

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
                   float cooldownMultiplier = 1 - (bonusPercentage / 100f); // ✅ 정확한 감소량 적용
                   attackData.Cooldown *= cooldownMultiplier; 
    
                   if (attackData.Cooldown < 0.5f) attackData.Cooldown = 0.5f; // ✅ 최소 쿨다운 제한
                   break;

               default:
                   Debug.LogError($"[CmdModifyPlayerAttackStat] 알 수 없는 스탯: {statType}");
                   return;
           }

           Debug.Log($"[서버] {attackData.Name} - {statType} 강화 적용: {bonusPercentage}% 증가");
       }
    }
}
