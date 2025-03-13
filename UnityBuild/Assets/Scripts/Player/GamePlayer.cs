using System.Collections;
using DataSystem;
using GameManagement;
using Mirror;
using Networking;
using UnityEngine;

namespace Player
{
    public class GamePlayer : NetworkRoomPlayer
    {
        public PlayerGameStats stats;
        
        private PlayerCardUI playerCardUI;
        private float cardSelectionTime = 10f;

        [SyncVar]
        public string PlayerNickname;

        public LobbyPlayerCharacter playerCharacter;

        public override void Start()
        {
            base.Start();

            if (isServer)
            {
                SpawnLobbyPlayerCharacter();
                StartCoroutine(CardSelectionTimer());
            }

            if (isOwned)
            {
                CmdSetNickname(PlayerSetting.Nickname);
                playerCardUI = FindFirstObjectByType<PlayerCardUI>();
            }
        }

        protected void SpawnLobbyPlayerCharacter()
        {
            Vector3 spawnPos = FindFirstObjectByType<SpawnPosition>().GetSpawnPosition();
            playerCharacter = Instantiate((NetworkRoomManager.singleton as RoomManager).spawnPrefabs[0], spawnPos, Quaternion.identity).GetComponent<LobbyPlayerCharacter>();
            NetworkServer.Spawn(playerCharacter.gameObject, connectionToClient);
        }

        [Command]
        public void CmdSetNickname(string nickname)
        {
            PlayerNickname = nickname;
            playerCharacter.nickname = PlayerNickname;
        }
        
        private IEnumerator CardSelectionTimer()
        {
            while (cardSelectionTime > 0)
            {
                yield return new WaitForSeconds(1f);
                cardSelectionTime--;
                RpcUpdateTimer(cardSelectionTime);
            }

            RpcUpdateTimer(0); // ✅ 0초일 때 최종 업데이트
            playerCharacter.CmdSetCharacterData(
                PlayerSetting.PlayerCharacterClass,
                PlayerSetting.MoveSkill,
                PlayerSetting.AttackSkillIDs
            );
            playerCharacter.SetIsDead(false);
        }

        // ✅ 서버 -> 클라이언트 타이머 동기화
        [ClientRpc]
        private void RpcUpdateTimer(float time)
        {
            if (playerCardUI != null)
            {
                playerCardUI.UpdateTimer(time);
            }
        }
    }
}
