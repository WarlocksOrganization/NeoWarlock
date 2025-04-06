using System;
using System.Collections.Generic;
using UnityEngine;

namespace DataSystem.Database
{
    public static partial class Database
    {
        public static readonly Dictionary<PlayerStatType, Sprite> iconDictionary = new Dictionary<PlayerStatType, Sprite>();
        public static readonly Dictionary<PlayerStatType, Sprite> statIconDictionary = new();


        private static void LoadCardIcons()
        {
            Sprite[] icons = Resources.LoadAll<Sprite>(Constants.CardIconPath);

            if (icons == null || icons.Length == 0)
            {
                Debug.LogError($"[PlayerCardIconManager] {Constants.CardIconPath} 경로에서 아이콘을 찾을 수 없습니다.");
                return;
            }

            foreach (var icon in icons)
            {
                string iconName = icon.name.Replace("_icon", "");

                if (iconName.EndsWith("Game")) // 예: HealthGame, SpeedGame 등
                {
                    string statName = iconName.Replace("Game", "");
                    if (Enum.TryParse(statName, out PlayerStatType statType))
                    {
                        statIconDictionary[statType] = icon;
                    }
                }
                else
                {
                    if (Enum.TryParse(iconName, out PlayerStatType statType))
                    {
                        iconDictionary[statType] = icon;
                    }
                }
            }

            Debug.Log($"✅ 카드 아이콘: {iconDictionary.Count}개, 스탯카드 아이콘: {statIconDictionary.Count}개 로드 완료");
        }


        public static Sprite GetCardIcon(PlayerStatType type)
        {
            return iconDictionary.TryGetValue(type, out var sprite) ? sprite : null;
        }
        
        public static Sprite GetBattleIcon(PlayerStatType type)
        {
            return statIconDictionary.TryGetValue(type, out var sprite) ? sprite : null;
        }
    }
}