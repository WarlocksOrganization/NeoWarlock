using DataSystem;
using GameManagement;
using Mirror;
using Networking;
using UnityEngine;

namespace Player
{
    public class RoomPlayer : NetworkRoomPlayer
    {
        protected GameRoomData roomData;

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
                CmdSetPlayerNumber(PlayerSetting.PlayerNum);
                CmdSetNickname(PlayerSetting.Nickname);
                if (PlayerSetting.PlayerCharacterClass != Constants.CharacterClass.None)
                {
                    CmdSetClss(PlayerSetting.PlayerCharacterClass);
                }
            }
        }

        protected void SpawnLobbyPlayerCharacter()
        {
            Vector3 spawnPos = FindFirstObjectByType<SpawnPosition>().GetSpawnPosition();
            LobbyPlayer = Instantiate((NetworkRoomManager.singleton as RoomManager).spawnPrefabs[0], spawnPos, Quaternion.identity).GetComponent<LobbyPlayerCharacter>();
            NetworkServer.Spawn(LobbyPlayer.gameObject, connectionToClient);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();

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
        
        [Command]
        public void CmdSetPlayerNumber(int PlayerNum)
        {
            LobbyPlayer.playerId = PlayerNum;
        }
        
        [Command]
        public void CmdSetClss(Constants.CharacterClass characterClass)
        {
            //LobbyPlayer.SetCharacterClass(characterClass);
        }
    }
}