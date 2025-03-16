using System.Collections.Generic;
using UnityEngine;

namespace DataSystem.Database
{
    public static partial class Database
    {
        private static readonly Dictionary<Constants.CharacterClass, CharacterClassData> characterClassDictionary = new();
        
        public static CharacterClassData GetCharacterClassData(Constants.CharacterClass characterClass)
        {
            if (characterClassDictionary.TryGetValue(characterClass, out var characterData))
            {
                return characterData;
            }
            
            Debug.LogWarning($"[Database] GetCharacterClassData(): No CharacterClassData found for characterClass {characterClass}.");
            return null;
        }

        public static void LoadCharacterClassData()
        {
            string csvFileName = "Data/CharacterClassData";
            TextAsset csvFile = Resources.Load<TextAsset>(csvFileName);

            if (csvFile == null)
            {
                Debug.LogError($"CSV 파일을 찾을 수 없습니다: {csvFileName}");
                return;
            }

            string[] dataRows = csvFile.text.Split('\n');
            
            for (int i = 1; i < dataRows.Length; i++)
            {
                string row = dataRows[i].Trim();
                if (string.IsNullOrEmpty(row)) continue;

                string[] columns = row.Split(',');
                
                Constants.CharacterClass characterClass = (Constants.CharacterClass)System.Enum.Parse(typeof(Constants.CharacterClass), columns[0]);
                int skill1 = int.Parse(columns[1]);
                int skill2 = int.Parse(columns[2]);
                int skill3 = int.Parse(columns[3]);
                Constants.SkillType movementSkill = (Constants.SkillType)System.Enum.Parse(typeof(Constants.SkillType), columns[4]);
                string characterName = columns[5];
                string characterDescription = columns[6];
                int characterAtk = int.Parse(columns[7]);
                int characterHp =int.Parse(columns[8]);
                int characterSpeed = int.Parse(columns[9]);
                int characterKnock = int.Parse(columns[10]);

                CharacterClassData data = new()
                {
                    CharacterClass = characterClass,
                    AttackSkillIds = new List<int> { skill1, skill2, skill3 },
                    MovementSkillType = movementSkill,
                    CharacterName = characterName,
                    CharacterDescription = characterDescription,
                    CharacterAtk = characterAtk,
                    CharacterHp = characterHp,
                    CharacterSpeed = characterSpeed,
                    CharacterKnock = characterKnock
                };

                characterClassDictionary[data.CharacterClass] = data;
            }

            Debug.Log($"총 {characterClassDictionary.Count}개의 캐릭터 클래스를 로드했습니다.");
        }

        public class CharacterClassData
        {
            public Constants.CharacterClass CharacterClass;
            public List<int> AttackSkillIds = new();
            public Constants.SkillType MovementSkillType;
            public string CharacterName;
            public string CharacterDescription;
            public int CharacterAtk;
            public int CharacterHp;
            public int CharacterSpeed;
            public int CharacterKnock;
        }
    }
}