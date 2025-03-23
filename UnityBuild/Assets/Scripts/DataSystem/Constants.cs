using System;
using System.Collections.Generic;
using UnityEngine;

namespace DataSystem
{
    public static class Constants
    {
        public static readonly string CsvFileName = "Data/AttacksData";
        public static readonly string ConfigPath = "Configs/AttackConfigs/";
        public static readonly string MovementConfigPath = "Configs/MoveSkillConfigs";
        public static readonly string IconPath = "Sprites/AttackIcons/";
        public static readonly string ClassIconPath = "Sprites/ClassIcons/";
        public static readonly string CardIconPath = "Sprites/CardIcons/";
        
        public enum RoomType
        {
            Solo = 0,           // 개인전
            Team = 1,           // 팀전
        }

        public enum PlayerState
        {
            NotReady = 0,
            Ready = 1,
            Start = 2,
        }

        public enum GameState
        {
            NotStarted = 0,
            Counting = 1,
            Start = 2,
        }
        
        public enum CharacterClass
        {
            Mage,   // 마법사
            Archer, // 궁수
            Warrior, // 전사
            Necromancer,
            Priest,
            None = 100,
        }
        
        public enum AttackType
        {
            Projectile,
            ProjectileSky,
            Point,
            Area,
            Melee,
            Self,
        }
        
        public enum BuffType
        {
            None,
            SpeedBoost,
            DamageBoost,
            DefenseBoost,
            Slow,
            Poison,
            Charge,
            PowerBody,
            HolyShhield,
            PowerPowerBody,
            PowerCharge,
        }

        public enum SkillType
        {
            Fire,
            Thunder,
            Ice,
            Meteor,
            TelePort,
            Arrow,
            PoisonArrow,
            ExplosionArrow,
            Roll,
            PhantomAttack,
            SoulVortex,
            InfernalFlask,
            PhantomStep,
            Slash,
            PowerBody,
            Charge,
            ThunderStorm,
            FreezeArea,
            IceArrow,
            PoisonSpore,
            Starfall,
            Dash,
            HollyAttack,
            HollyShild,
            HolyRecovery,
            HolyTeleport,
            PowerSlash,
            PowerPowerBody,
            PowerCharge,
            PhantomSmart,
            SoulSwamp,
            InfernalPoison,
            
            None = 100,
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
        
        [Serializable]
        public class SkillEffectGameObjectEntry
        {
            public Constants.SkillType skillType;
            public GameObject gObject;
        }
        
        [Serializable]
        public class PlayerStats
        {
            public int playerId;
            public Constants.CharacterClass characterClass;
            public string nickname;

            public List<int> roundRanks = new List<int>();
            public int kills = 0;
            public int outKills = 0;
            public int damageDone = 0;
            public int totalScore = 0;
        }
        
    }
}

