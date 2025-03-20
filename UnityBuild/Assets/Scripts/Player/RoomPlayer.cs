using System.Collections.Generic;
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
                CmdSetPlayerNumber(); // ✅ 이제 서버에서 자동으로 playerId 할당
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
        public void CmdSetPlayerNumber()
        {
            if (!isServer) return; // 서버에서만 실행

            // ✅ 현재 존재하는 모든 RoomPlayer의 ID 목록 생성
            var allPlayers = FindObjectsByType<RoomPlayer>(FindObjectsSortMode.None);
            HashSet<int> assignedIds = new HashSet<int>();

            foreach (var player in allPlayers)
            {
                assignedIds.Add(player.LobbyPlayer.playerId);
            }

            // ✅ 중복되지 않는 가장 작은 ID 찾기
            int newId = 0;
            while (assignedIds.Contains(newId))
            {
                newId++;
            }

            // ✅ 중복되지 않는 ID 설정
            LobbyPlayer.playerId = newId;

            // ✅ 클라이언트의 PlayerSetting에도 적용
            RpcUpdatePlayerSetting(newId);
        }

        [ClientRpc]
        private void RpcUpdatePlayerSetting(int newId)
        {
            if (isOwned) // 본인 클라이언트만 적용
            {
                PlayerSetting.PlayerId = newId;
                Debug.Log($"[RoomPlayer] 내 PlayerNum이 {newId}로 설정됨");
            }
        }
        
        [Command]
        public void CmdSetClss(Constants.CharacterClass characterClass)
        {
            //LobbyPlayer.SetCharacterClass(characterClass);
        }
    }
}