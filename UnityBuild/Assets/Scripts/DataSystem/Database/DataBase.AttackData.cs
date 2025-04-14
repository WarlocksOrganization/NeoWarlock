using System.Collections.Generic;
using Mirror;
using Unity.VisualScripting;
using UnityEngine;

namespace DataSystem.Database
{
    public static partial class Database
    {
        private static readonly Dictionary<int, AttackData > attackDataDictionary = new();
        
        public static AttackData  GetAttackData(int id)
        {
            if (attackDataDictionary.TryGetValue(id, out var attack))
            {
                return new AttackData(attack); // ✅ 깊은 복사
            }

            Debug.LogWarning($"[Database] GetAttackData(): No AttackData found for id {id}.");
            return null;
        }

        public static void LoadAttackData()
        {
            string csvFileName = Constants.CsvFileName;
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

                AttackConfig attackConfig = Resources.Load<AttackConfig>(Constants.ConfigPath + columns[1] + "Config");

                if (attackConfig == null)
                {
                    Debug.LogError($"공격 설정을 찾을 수 없습니다. Config: {columns[1]}");
                    continue;
                }

                Sprite attackIcon = Resources.Load<Sprite>(Constants.IconPath + columns[1] + "_icon");
                if (attackIcon == null)
                {
                    Debug.LogError($"아이콘을 찾을 수 없습니다: {columns[1]}");
                    continue;
                }

                AttackData data = new()
                {
                    ID = int.Parse(columns[0]),
                    Name = columns[1],
                    DisplayName = columns[2], // ✅ 추가된 필드 반영
                    Description = columns[3], // ✅ 추가된 필드 반영
                    Speed = float.Parse(columns[4]),
                    Range = float.Parse(columns[5]),
                    Radius = float.Parse(columns[6]),
                    Damage = float.Parse(columns[7]),
                    KnockbackForce = float.Parse(columns[8]),
                    Cooldown = float.Parse(columns[9]),
                    config = attackConfig,
                    Icon = attackIcon
                };

                attackDataDictionary[data.ID] = data;
            }

            Debug.Log($"총 {attackDataDictionary.Count}개의 공격 데이터를 로드했습니다.");
        }


        public class AttackData
        {
            public int ID;
            public string Name;
            public string DisplayName; // ✅ 스킬의 한글 이름
            public string Description; // ✅ 스킬 설명
            [SyncVar]  public float Speed;
            [SyncVar]  public float Range;
            [SyncVar]  public float Radius;
            [SyncVar]  public float Damage;
            [SyncVar]  public float KnockbackForce;
            [SyncVar]  public float Cooldown;
    
            public AttackConfig config; // ✅ 공격 설정을 ScriptableObject로 참조
            public Sprite Icon;
            
            public AttackData(AttackData other)
            {
                ID = other.ID;
                Name = other.Name;
                DisplayName = other.DisplayName;
                Description = other.Description;
                Speed = other.Speed;
                Range = other.Range;
                Radius = other.Radius;
                Damage = other.Damage;
                KnockbackForce = other.KnockbackForce;
                Cooldown = other.Cooldown;
                config = other.config;
                Icon = other.Icon;
            }

            // 기본 생성자도 명시해주면 좋음
            public AttackData() { }
        }
    }
}
