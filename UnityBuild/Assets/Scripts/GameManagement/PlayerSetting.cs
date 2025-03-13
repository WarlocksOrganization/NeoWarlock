using DataSystem;
using DataSystem.Database;
using UnityEngine;

namespace GameManagement
{
    public class PlayerSetting : MonoBehaviour
    {
        public static string Nickname = "";
        public static Constants.CharacterClass PlayerCharacterClass = Constants.CharacterClass.None;
        public static Constants.SkillType MoveSkill = Constants.SkillType.None;
        public static int[] AttackSkillIDs = new int[4];
        
        public static int PlayerNum;
        
        public static Database.PlayerCardData[] PlayerCardss;
    }
}