using System.Collections.Generic;
using Player.Combat;
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
                return attack;
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
                
                AttackConfig attackConfig = Resources.Load<AttackConfig>(Constants.ConfigPath + columns[1] + "Config"); // ✅ ConfigName 사용

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
                    Speed = float.Parse(columns[2]),
                    Range = float.Parse(columns[3]),
                    Radius = float.Parse(columns[4]),
                    Damage = float.Parse(columns[5]),
                    KnockbackForce = float.Parse(columns[6]),
                    Cooldown = float.Parse(columns[7]),
                    config = attackConfig ,
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
            public float Speed;
            public float Range;
            public float Radius;
            public float Damage;
            public float KnockbackForce;
            public float Cooldown;

            public AttackConfig config; // ✅ 공격 설정을 ScriptableObject로 참조
            public Sprite Icon;
        }
    }
}
