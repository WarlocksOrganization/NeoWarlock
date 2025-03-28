using System;
using Mirror;
using UnityEngine;
using DataSystem;
using UnityEngine.Rendering.LookDev;

namespace GameManagement
{
    public class GameRoomData : NetworkBehaviour
    {
        [SyncVar] public string roomName = "기본 방 이름"; // 방 이름 동기화
        [SyncVar] public Constants.RoomType roomType = Constants.RoomType.Solo; // 방 유형 동기화
        [SyncVar] public int maxPlayerCount = 4; // 최대 인원 동기화
        [SyncVar] public int Round = 3;
        [SyncVar] public Constants.RoomMapType roomMapType = Constants.RoomMapType.SSAFY;

        [SyncVar] public string gameId = null;
        [SyncVar] public string roomId = null;

        private void Start()
        {
            GameLobbyUI gameLobbyUI = FindFirstObjectByType<GameLobbyUI>();
            if (gameLobbyUI != null && gameLobbyUI.RoomNameText != null)
            {
                gameLobbyUI.RoomNameText.text = roomName;
            }
        }

        [Server]
        public void SetRoomData(string name, Constants.RoomType type, int maxPlayers, string gId = null, string rId = null)
        {
            roomName = name;
            roomType = type;
            maxPlayerCount = maxPlayers;
            gameId = gId;
            roomId = rId;

            Debug.Log($"[GameRoomData] 설정 완료: {roomName}, 유형: {roomType}, 최대 인원: {maxPlayerCount}");
        }
    }
}