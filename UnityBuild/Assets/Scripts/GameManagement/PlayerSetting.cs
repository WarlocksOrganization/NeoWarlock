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
        public static int SelectedClassCode => (int)PlayerCharacterClass;
        public static Constants.SkillType MoveSkill = Constants.SkillType.None;
        public static Constants.TeamType TeamType = Constants.TeamType.None;
        public static int[] AttackSkillIDs = {0,1,2,3,0};
        public static int ItemSkillID = 0;

        public static int PlayerId;
        public static string UserId;
        
        public static List<Database.PlayerCardData> PlayerCards = new List<Database.PlayerCardData>();

        // 평가 로직에서 사용하기 위한 매트릭스 원본
        public static string MatrixJsonText = "";  // 서버에서 주기적으로 받아온 텍스트 저장

        // 카드 ID만 추려낸 덱 (옵션)
        public static List<int> DeckCardIDs = new();
    }
}