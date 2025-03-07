using DataSystem;
using GameManagement;
using Mirror;
using UnityEngine;

namespace Networking
{
    public class RoomManager : NetworkRoomManager
    {
        [SerializeField] private GameRoomData gameRoomDataPrefab;

        private GameRoomData roomDataInstance;

        public string roomName;
        public Constants.RoomType roomType;
        public int maxPlayerCount = 4;

        public override void OnRoomStartHost()
        {
            base.OnRoomStartHost();

            if (roomDataInstance == null && gameRoomDataPrefab != null)
            {
                roomDataInstance = Instantiate(gameRoomDataPrefab);
                NetworkServer.Spawn(roomDataInstance.gameObject);

                // 방 데이터 초기화
                roomDataInstance.SetRoomData(roomName, roomType, maxPlayerCount);
                maxConnections = maxPlayerCount;

                Debug.Log($"[RoomManager] 방 생성: {roomName}, 유형: {roomType}, 최대 인원: {maxConnections}");
            }
        }
    }
}