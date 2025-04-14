using System.Collections.Generic;
using DataSystem;
using DataSystem.Database;
using UnityEngine;

namespace GameManagement
{
    public class PlayerSetting : MonoBehaviour
    {
        public static Constants.KeyType PlayerKeyType = Constants.KeyType.Classic;
        
        public static string Nickname = "";
        public static Constants.CharacterClass PlayerCharacterClass = Constants.CharacterClass.None;
        public static Constants.SkillType MoveSkill = Constants.SkillType.None;
        public static Constants.TeamType TeamType = Constants.TeamType.None;
        public static int[] AttackSkillIDs = {0,1,2,3,0};
        public static int ItemSkillID = 0;
        
        public static int PlayerId = -1;
        public static string UserId;
        
        public static List<Database.PlayerCardData> PlayerCards = new List<Database.PlayerCardData>();
    }
}