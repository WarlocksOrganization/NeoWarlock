using UnityEngine;
using System;

namespace DataSystem
{
    [CreateAssetMenu(fileName = "MapConfig", menuName = "GameData/MapConfig", order = 1)]
    public class MapConfig : ScriptableObject
    {
        public Constants.RoomMapType mapType;
        public string mapName;
        public Constants.SoundType bgmType;
        public Material skyboxMaterial;
    }
}
