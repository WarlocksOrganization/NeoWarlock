using System.Collections;
using DataSystem;
using DataSystem.Database;
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

        [SerializeField] private GameObject gamePlayObject;

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
                CmdSetPlayerNumber(PlayerSetting.PlayerId);
                playerCardUI = FindFirstObjectByType<PlayerCardUI>();
            }
        }

        protected void SpawnLobbyPlayerCharacter()
        {
            Vector3 spawnPos = FindFirstObjectByType<SpawnPosition>().GetSpawnPosition();
            playerCharacter = Instantiate((NetworkRoomManager.singleton as RoomManager).spawnPrefabs[0], spawnPos, Quaternion.identity).GetComponent<LobbyPlayerCharacter>();
            NetworkServer.Spawn(playerCharacter.gameObject, connectionToClient);

            if (isServer)
            {
                GameObject gameObj = Instantiate(gamePlayObject, Vector3.zero, Quaternion.identity);
                NetworkServer.Spawn(gameObj, connectionToClient);
            }
        }

        [Command]
        public void CmdSetNickname(string nickname)
        {
            PlayerNickname = nickname;
            playerCharacter.nickname = PlayerNickname;
        }
        
        [Command]
        public void CmdSetPlayerNumber(int playerNum)
        {
            playerCharacter.playerId = playerNum;
        }
        
        private IEnumerator CardSelectionTimer()
        {
            while (cardSelectionTime > 0)
            {
                RpcUpdateTimer(cardSelectionTime);
                yield return new WaitForSeconds(1f);
                cardSelectionTime--;
            }

            RpcUpdateTimer(0); // ✅ 0초일 때 최종 업데이트
        }

        // ✅ 서버 -> 클라이언트 타이머 동기화
        [ClientRpc]
        private void RpcUpdateTimer(float time)
        {
            if (playerCardUI != null)
            {
                playerCardUI.UpdateTimer(time);
            }

            if (time <= 0)
            {
                if (!isOwned)
                {
                    return;
                }
                if (playerCharacter == null)
                {
                    LobbyPlayerCharacter[] pc =
                        FindObjectsByType<LobbyPlayerCharacter>(sortMode: FindObjectsSortMode.None);
                    foreach (var pcharacter in pc)
                    {
                        if (pcharacter.playerId == PlayerSetting.PlayerId)
                        {
                            playerCharacter = pcharacter;
                            break;
                        }
                    }
                }
                
                playerCharacter.State = Constants.PlayerState.Ready;
                
                foreach (var slot in playerCardUI.slots)
                {
                    var slotData = slot.GetCurrentCard();
                    if (slotData.StatType == PlayerStatType.Special)
                    {
                        PlayerSetting.AttackSkillIDs[slotData.AppliedSkillIndex] += 100; // ✅ 스킬 업그레이드
                    }
                }
                
                playerCharacter.CmdSetCharacterData(
                    PlayerSetting.PlayerCharacterClass,
                    PlayerSetting.MoveSkill,
                    PlayerSetting.AttackSkillIDs
                );
            }
        }
    }
}
