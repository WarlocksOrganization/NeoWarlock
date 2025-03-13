using System.Collections.Generic;
using Player.Combat;
using UnityEngine;

namespace DataSystem.Database
{
    public static partial class Database
    {
        private static readonly Dictionary<Constants.SkillType, MovementSkillConfig> movementSkillDictionary = new();

        
        // 📌 이동 스킬 데이터 로드
        public static MovementSkillConfig GetMovementSkillData(Constants.SkillType skillType)
        {
            if (movementSkillDictionary.TryGetValue(skillType, out var skill))
            {
                return skill;
            }

            Debug.LogWarning($"[Database] GetMovementSkillData(): No MovementSkillData found for skillType {skillType}.");
            return null;
        }

        public static void LoadMovementSkillData()
        {
            MovementSkillConfig[] skillAssets = Resources.LoadAll<MovementSkillConfig>(Constants.MovementConfigPath);

            if (skillAssets == null || skillAssets.Length == 0)
            {
                Debug.LogError("이동 스킬 데이터를 찾을 수 없습니다.");
                return;
            }

            foreach (var skill in skillAssets)
            {
                if (!movementSkillDictionary.ContainsKey(skill.skillType))
                {
                    movementSkillDictionary.Add(skill.skillType, skill);
                }
            }

            Debug.Log($"총 {movementSkillDictionary.Count}개의 이동 스킬 데이터를 로드했습니다.");
        }
    }
}
