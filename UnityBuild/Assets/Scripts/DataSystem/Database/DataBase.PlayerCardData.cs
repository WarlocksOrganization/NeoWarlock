using System.Collections.Generic;
using UnityEngine;

namespace DataSystem.Database
{
    public static partial class Database
    {
        private static readonly Dictionary<int, PlayerCardData> playerCardDictionary = new();

        public static PlayerCardData GetPlayerCardData(int id)
        {
            if (playerCardDictionary.TryGetValue(id, out var card))
            {
                return card;
            }

            Debug.LogWarning($"[Database] GetPlayerCardData(): No PlayerCardData found for id {id}.");
            return null;
        }

        public static void LoadPlayerCardData()
        {
            string csvFileName = "Data/PlayerCardsData"; // ✅ CSV 파일 경로
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

                // ✅ 아이콘을 카드 이름을 기반으로 로드
                Sprite cardIcon = Resources.Load<Sprite>($"Sprites/PlayerCards/{columns[1]}_icon");
                if (cardIcon == null)
                {
                    Debug.LogWarning($"[Database] 아이콘을 찾을 수 없습니다: {columns[1]}");
                    continue;
                }

                PlayerCardData data = new()
                {
                    ID = int.Parse(columns[0]),
                    Name = columns[1],
                    ApplyClass = (Constants.CharacterClass)int.Parse(columns[2]), // 적용 클래스
                    StatType = (PlayerStatType)int.Parse(columns[3]), // 증가 능력치 타입
                    BonusStat = float.Parse(columns[4]), // 증가 능력치 값
                    AppliedSkillIndex = int.Parse(columns[5]), // ✅ 강화되는 스킬 번호 (0,1,2,3)
                };

                playerCardDictionary[data.ID] = data;
            }

            Debug.Log($"총 {playerCardDictionary.Count}개의 플레이어 카드 데이터를 로드했습니다.");
        }

        public class PlayerCardData
        {
            public int ID;
            public string Name;
            public Constants.CharacterClass ApplyClass; // ✅ 적용할 클래스
            public PlayerStatType StatType; // ✅ 증가하는 능력치 타입
            public float BonusStat; // ✅ 증가하는 수치
            public int AppliedSkillIndex; // ✅ 강화되는 스킬 번호 (0,1,2,3)
        }
    }

    // ✅ 스탯 종류 (어떤 능력치를 증가시킬 것인지)
    public enum PlayerStatType
    {
        None,
        Health, // 플레이어 체력
        Speed, //플레이어스피드
        
        
        AttackSpeed = 10,  // 스킬 이동 속도 증가
        Range = 11, // 공격거리
        Radius = 12, // 범위
        Damage = 13, // 공격력 증가
        KnockbackForce = 14, // 넉백거리
        Cooldown = 15, // 쿨타임 감소
        
        Special = 20, //스페셜
        
        
    }
}
