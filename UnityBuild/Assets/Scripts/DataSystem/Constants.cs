using System;
using UnityEngine;

namespace DataSystem
{
    public static class Constants
    {
        public static string CSVFileName = "Data/AttacksData";
        public static string ConfigPath = "Configs/AttackConfigs/";
        
        public enum RoomType
        {
            Solo = 0,           // 개인전
            Team = 1,           // 팀전
        }
        
        public enum CharacterClass
        {
            Mage,   // 마법사
            Archer, // 궁수
            Warrior // 전사
        }
        
        public enum AttackType
        {
            Projectile,
            ProjectileSky,
            Point,
            Area,
            Melee
        }
        
        public enum BuffType
        {
            None,
            SpeedBoost,
            DamageBoost,
            DefenseBoost,
            Slow,
            Poison,
        }

        public enum SkillType
        {
            Fire,
            Thunder,
            Ice,
            Meteor,
            TelePort,
        }
        
        [Serializable]
        public class BuffEffectEntry
        {
            public Constants.BuffType buffType;
            public ParticleSystem effect;
        }
        
        [Serializable]
        public class SkillEffectEntry
        {
            public Constants.SkillType skillType;
            public ParticleSystem effect;
        }
    }
}

