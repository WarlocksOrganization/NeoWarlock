using System;
using Mirror;
using UnityEngine;
using DataSystem;

namespace GameManagement
{
    public class GameRoomData : NetworkBehaviour
    {
        [SyncVar] public string roomName = "기본 방 이름"; // 방 이름 동기화
        [SyncVar] public Constants.RoomType roomType = Constants.RoomType.Solo; // 방 유형 동기화
        [SyncVar] public int maxPlayerCount = 4; // 최대 인원 동기화
        [SyncVar] public int Round = 3;

        private void Start()
        {
            GameLobbyUI gameLobbyUI = FindFirstObjectByType<GameLobbyUI>();
            if (gameLobbyUI != null)
            {
                gameLobbyUI.RoomNameText.text = roomName;
            }
        }

        [Server]
        public void SetRoomData(string name, Constants.RoomType type, int maxPlayers)
        {
            roomName = name;
            roomType = type;
            maxPlayerCount = maxPlayers;

            Debug.Log($"[GameRoomData] 설정 완료: {roomName}, 유형: {roomType}, 최대 인원: {maxPlayerCount}");
        }
    }
}