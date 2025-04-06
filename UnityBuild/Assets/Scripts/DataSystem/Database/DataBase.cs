using System.Collections.Generic;
using UnityEngine;

namespace DataSystem.Database
{
    public static partial class Database
    {
        public static void LoadDataBase()
        {
            LoadAttackData();
            LoadMovementSkillData();
            LoadPlayerCardData();
            LoadCardIcons();
            LoadCharacterClassData();
            LoadBuffData();
        }
        
        public static Dictionary<string, BuffData> buffDictionary = new();

        public static void LoadBuffData()
        {
            var buffs = Resources.LoadAll<BuffData>(Constants.BuffConfigPath);
            foreach (var buff in buffs)
            {
                if (!buffDictionary.ContainsKey(buff.buffName))
                    buffDictionary.Add(buff.buffName, buff);
            }
        }
    }
}