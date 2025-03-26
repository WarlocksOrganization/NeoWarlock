using System;
using System.Collections.Generic;
using GameManagement;
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

        public static readonly int MaxGameEventTime = 15; // 오브젝트 이벤트 시간 15
        public static readonly int ScoreBoardTime = 15; // 스코어보드 보는 시간 12
        public static readonly int CardSelectionTime = 10; //카드 선택 시간 10
        public static readonly int CountTime = 5; // 카운트타임 5

        public enum RoomType
        {
            Solo = 0,
            Team = 1,
        }

        public enum PlayerState
        {
            NotReady = 0,
            Ready = 1,
            Start = 2,
            Counting = 3,
        }

        public enum GameState
        {
            NotStarted = 0,
            Counting = 1,
            Start = 2,
        }

        public enum CharacterClass
        {
            Mage,
            Archer,
            Warrior,
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
            
            ItemHP = 100,
            ItemSpeed = 101,
            ItemAttack = 102,
            ItemDefense = 103,
            ItemBomb = 104,
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
            
            ItemHP = 10001,
            ItemSpeed = 1002,
            ItemAttack = 1003,
            ItemDefense = 1004,
           
            ItemBomb = 1011,
            
            None = 100,
        }
        
        public enum SoundType
        {
            None,
            // BGM
            BGM_MainMenu = 1001,
            BGM_Lobby = 1002,
            BGM_SSAFY_CardSelect = 1101,
            BGM_SSAFY_GameStart = 1102,

            // SFX
            SFX_Click,
            SFX_Explosion,
            SFX_Heal,
            SFX_Swing
        }
        
        [System.Serializable]
        public class SoundData
        {
            public SoundType type;
            public AudioClip clip;
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
        public class SkillTypeMeshEntry
        {
            public Constants.SkillType skillType;
            public Mesh mesh;
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

            // 추가 필드
            public bool isDead = false;
            public int curHp = 0;
            public bool isMVP = false;

            public void Reset()
            {
                roundRanks.Clear();
                kills = 0;
                outKills = 0;
                damageDone = 0;
                totalScore = 0;
                isDead = false;
                curHp = 0;
                isMVP = false;
            }
        }
        
        [Serializable]
        public class RoundStats
        {
            public int kills;
            public int outKills;
            public int damageDone;
            public int rank;
        }

        public class PlayerRecord
        {
            public int playerId;
            public string nickname;
            public Constants.CharacterClass characterClass;
            public List<RoundStats> roundStatsList = new();

            public int GetScoreAtRound(int roundIndex)
            {
                if (roundIndex >= roundStatsList.Count) return 0;
                var r = roundStatsList[roundIndex];
                return r.kills * 200 + r.outKills * 300 + r.damageDone + GameManager.Instance.GetRankBonus(r.rank);
            }

            public int GetTotalScoreUpToRound(int roundInclusive)
            {
                int score = 0;
                for (int i = 0; i <= roundInclusive && i < roundStatsList.Count; i++)
                    score += GetScoreAtRound(i);
                return score;
            }
        }
    }
}