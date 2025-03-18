using System;
using System.Collections.Generic;
using UnityEngine;

namespace DataSystem.Database
{
    public static partial class Database
    {
        public static readonly Dictionary<PlayerStatType, Sprite> iconDictionary = new Dictionary<PlayerStatType, Sprite>();

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
                string iconName = icon.name.Replace("_icon", ""); // "_icon" 제거

                if (Enum.TryParse(iconName, out PlayerStatType statType))
                {
                    iconDictionary[statType] = icon;
                }
            }

            Debug.Log($"총 {iconDictionary.Count}개의 아이콘을 로드했습니다.");
        }

        public static Sprite GetCardIcon(PlayerStatType type)
        {
            return iconDictionary.TryGetValue(type, out var sprite) ? sprite : null;
        }
    }
}