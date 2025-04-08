using System.Collections.Generic;
using UnityEngine;
using DataSystem;

namespace DataSystem.Database
{
    public static partial class Database
    {
        private static readonly Dictionary<Constants.RoomMapType, MapConfig> mapConfigDictionary = new();

        // 📌 MapConfig 데이터 로드
        public static void LoadMapConfigs()
        {
            MapConfig[] mapConfigs = Resources.LoadAll<MapConfig>("Configs/MapConfigs");

            if (mapConfigs == null || mapConfigs.Length == 0)
            {
                Debug.LogError("[Database] LoadMapConfigs() - MapConfig 리소스를 찾을 수 없습니다.");
                return;
            }

            foreach (var config in mapConfigs)
            {
                if (!mapConfigDictionary.ContainsKey(config.mapType))
                {
                    mapConfigDictionary.Add(config.mapType, config);
                }
                else
                {
                    Debug.LogWarning($"[Database] 중복된 MapConfig: {config.mapType}");
                }
            }

            Debug.Log($"[Database] 총 {mapConfigDictionary.Count}개의 MapConfig 로드 완료");
        }

        // 📌 MapConfig 조회
        public static MapConfig GetMapConfig(Constants.RoomMapType mapType)
        {
            if (mapConfigDictionary.TryGetValue(mapType, out var config))
            {
                return config;
            }

            Debug.LogWarning($"[Database] MapConfig을 찾을 수 없습니다: {mapType}");
            return null;
        }
    }
}