using GameManagement;
using Mirror;
using Networking;
using UnityEngine;

namespace Player
{
    public class RoomPlayer : NetworkRoomPlayer
    {
        private GameRoomData roomData;
    
        [SyncVar]
        public string PlayerNickname;

        public LobbyPlayerCharacter LobbyPlayer;

        public override void Start()
        {
            base.Start();

            if (isServer)
            {
                SpawnLobbyPlayerCharacter();
            }

            if (isOwned)
            {
                CmdSetNickname(PlayerSetting.Nickname);
            }
        }

        private void SpawnLobbyPlayerCharacter()
        {
            // 플레이어 스폰 위치 설정 및 생성
            Vector3 spawnPos = FindFirstObjectByType<SpawnPosition>().GetSpawnPosition();
        
            LobbyPlayer = Instantiate(RoomManager.singleton.spawnPrefabs[0], spawnPos, Quaternion.identity).GetComponent<LobbyPlayerCharacter>();
        
            NetworkServer.Spawn(LobbyPlayer.gameObject, connectionToClient);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

            // GameRoomData 자동 동기화 확인
            roomData = FindFirstObjectByType<GameRoomData>();

            if (roomData != null && isOwned)
            {
                Debug.Log($"방 입장, 방 이름: {roomData.roomName}, 방 유형: {roomData.roomType}, 최대 인원: {roomData.maxPlayerCount}");
            }
        }

        [Command]
        public void CmdSetNickname(string nickname)
        {
            PlayerNickname = nickname;
            LobbyPlayer.nickname = PlayerNickname;
        }
    }
}