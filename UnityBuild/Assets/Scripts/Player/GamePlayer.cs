using DataSystem;
using GameManagement;
using Mirror;
using Networking;
using UnityEngine;

namespace Player
{
    public class GamePlayer : NetworkRoomPlayer
    {
        protected GameRoomData roomData;
        public PlayerGameStats stats;

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

        [Command]
        public void CmdSetNickname(string nickname)
        {
            PlayerNickname = nickname;
            LobbyPlayer.nickname = PlayerNickname;
        }
        
        [Command]
        public void CmdSetClss(Constants.CharacterClass characterClass)
        {
            LobbyPlayer.SetCharacterClass(characterClass);
        }
    }
}
