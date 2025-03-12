using System.Linq;
using DataSystem;
using GameManagement;
using Mirror;
using Player;
using UnityEngine;

namespace Networking
{
    public class RoomManager : NetworkRoomManager
    {
        [SerializeField] private GameRoomData gameRoomDataPrefab;

        private GameRoomData roomDataInstance;

        public string roomName;
        public Constants.RoomType roomType;
        public int maxPlayerCount = 6;

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
        
        public void StartGame()
        {
            if (roomSlots.Count < 1)
            {
                Debug.LogWarning("[RoomManager] 최소 1명의 플레이어가 필요합니다.");
                return;
            }

            foreach (var player in roomSlots)
            {
                player.CmdChangeReadyState(true);
            }

            Debug.Log("[RoomManager] 모든 플레이어가 준비되었습니다. 게임을 시작합니다!");
            ServerChangeScene(GameplayScene);
        }
    }
}