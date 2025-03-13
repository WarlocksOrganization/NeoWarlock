using System.Collections.Generic;
using UnityEngine;

namespace DataSystem.Database
{
    public static partial class Database
    {
        public static readonly Dictionary<int, PlayerCardData> playerCardDictionary = new();

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

                PlayerCardData data = new()
                {
                    ID = int.Parse(columns[0]),
                    Name = columns[1],
                    StatType = (PlayerStatType)int.Parse(columns[2]), // 증가 능력치 타입
                    BonusStat = float.Parse(columns[3]), // 증가 능력치 값
                };

                playerCardDictionary[data.ID] = data;
            }

            Debug.Log($"총 {playerCardDictionary.Count}개의 플레이어 카드 데이터를 로드했습니다.");
        }

        public class PlayerCardData
        {
            public int ID;
            public string Name;
            public PlayerStatType StatType; // ✅ 증가하는 능력치 타입
            public float BonusStat; // ✅ 증가하는 수치
        }
    }

    // ✅ 스탯 종류 (어떤 능력치를 증가시킬 것인지)
    public enum PlayerStatType
    {
        None,
        Health, // 플레이어 체력
        Speed, //플레이어스피드
        
        
        AttackSpeed = 10,  // 스킬 이동 속도 증가
        Range = 11, // 스킬 공격거리
        Radius = 12, // 스킬 범위
        Damage = 13, // 스킬 공격력 증가
        KnockbackForce = 14, // 스킬 넉백거리
        Cooldown = 15, // 스킬 쿨타임 감소
        
        Special = 20, //스킬 업그레이들
        
        
    }
}
