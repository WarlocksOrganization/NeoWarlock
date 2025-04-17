using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DataSystem;
using DataSystem.Database;
using GameManagement;
using kcp2k;
using Mirror;
using Networking;
using Newtonsoft.Json.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Player
{
    public class GamePlayer : NetworkRoomPlayer
    {
        [SyncVar] public string PlayerNickname;
        public PlayerGameStats stats;

        [SyncVar]
        public string UserId;
        
        public LobbyPlayerCharacter playerCharacter;

        private PlayerCardUI playerCardUI;
        private GamePlayUI gameplayUI;
        public bool isPlayerSpawned = false;
        
        public override void Start()
        {
            base.Start();

            if (isServer)
            {
                GameManager.Instance.ResetRoundState();
                SpawnLobbyPlayerCharacter();
                StartCoroutine(WaitForAllPlayersThenStartCardSelection());
            }

            if (isOwned)
            {
                CmdSetNickname(PlayerSetting.Nickname);
                CmdSetPlayerNumber(PlayerSetting.PlayerId);
                CmdSetUserId(PlayerSetting.UserId);
                playerCardUI = FindFirstObjectByType<PlayerCardUI>();
                gameplayUI = FindFirstObjectByType<GamePlayUI>();
            }
        }
        
        private void SpawnLobbyPlayerCharacter()
        {
            if (!isServer || isPlayerSpawned) return;
            isPlayerSpawned = true;

            // ✅ 이미 connectionToClient로 캐릭터 생성됐는지 확인
            bool alreadySpawned = FindObjectsByType<LobbyPlayerCharacter>(FindObjectsSortMode.None)
                .Any(p => p.connectionToClient == this.connectionToClient);

            if (alreadySpawned)
            {
                Debug.LogWarning($"[GamePlayer] 캐릭터 중복 생성 방지 - connId: {connectionToClient.connectionId}");
                return;
            }

            // ✅ 스폰
            Vector3 spawnPos = FindFirstObjectByType<SpawnPosition>().GetSpawnPosition();
            GameObject pcObj = Instantiate((NetworkRoomManager.singleton as RoomManager).spawnPrefabs[0], spawnPos, Quaternion.identity);
            playerCharacter = pcObj.GetComponent<LobbyPlayerCharacter>();
            NetworkServer.Spawn(pcObj, connectionToClient);
        }

        [Command]
        public void CmdSetNickname(string nickname)
        {
            PlayerNickname = nickname;
            if (playerCharacter != null)
                playerCharacter.nickname = nickname;
        }

        [Command]
        public void CmdSetUserId(string userId)
        {
            UserId = userId;
            if (playerCharacter != null)
                playerCharacter.userId = userId;
        }

        [Command]
        public void CmdSetPlayerNumber(int playerNum)
        {
            if (playerCharacter != null)
                playerCharacter.playerId = playerNum;
        }

        [ClientRpc]
        public void RpcSendFinalScore(Constants.PlayerRecord[] allRecords, int roundIndex)
        {
            StartCoroutine(ShowFinalScoreAndNextRound(allRecords, roundIndex));
        }

        private IEnumerator ShowFinalScoreAndNextRound(Constants.PlayerRecord[] allRecords, int roundIndex)
        {
            gameplayUI = FindFirstObjectByType<GamePlayUI>();
            yield return new WaitUntil(() => gameplayUI != null);

            // ⬇️ 스코어보드 표시 + 끝나면 카드 선택 트리거
            gameplayUI.ShowGameOverTextAndScore(allRecords, roundIndex, () =>
            {
                Debug.Log("[GamePlayer] 스코어보드 끝! 다음 라운드 준비 요청");
            });
        }
        
        [ClientRpc]
        public void RpcStartCardSelection()
        {
            var scoreBoardUIui = FindFirstObjectByType<ScoreBoardUI>();
            if (scoreBoardUIui != null)
                scoreBoardUIui.gameObject.SetActive(false);
            
            playerCardUI?.gameObject.SetActive(true);
        }

        public IEnumerator WaitForAllPlayersThenStartCardSelection()
        {
            yield return new WaitUntil(() => NetworkServer.connections.Count >= NetworkRoomManager.singleton.numPlayers);
            yield return new WaitUntil(() => FindObjectsByType<LobbyPlayerCharacter>(sortMode: FindObjectsSortMode.None).Length >= NetworkRoomManager.singleton.numPlayers);
            yield return new WaitForSeconds(0.5f);

            if (isServer)
            {
                // ✅ 첫 라운드 시작 전에도 맵 오브젝트 생성
                FindFirstObjectByType<GameRoomData>()?.SpawnGamePlayObjects();

                // ✅ 서버에서 단 한 번만 타이머 실행
                if (GameManager.Instance.isCardSelectionStarted == false)
                {
                    GameManager.Instance.isCardSelectionStarted = true;
                    StartCoroutine(CardSelectionTimer());
                }
            }
        }

        private IEnumerator CardSelectionTimer()
        {
            Debug.Log("CardSelectionTimer");
            int time = Constants.CardSelectionTime;
            while (time > 0)
            {
                RpcUpdateTimer(time);
                yield return new WaitForSeconds(1);
                time--;
            }

            RpcUpdateTimer(0);

            // ✅ 모든 플레이어를 강제로 Ready 상태로
            var allPlayers = FindObjectsByType<LobbyPlayerCharacter>(FindObjectsSortMode.None);
            foreach (var player in allPlayers)
            {
                player.State = Constants.PlayerState.Ready;
            }

            // ✅ 카운트다운 시작
            RpcGameStart();

            var timer = FindFirstObjectByType<NetworkTimer>();
            timer?.StartGameFlow(Constants.CountTime, Constants.MaxGameEventTime);
        }

        [ClientRpc]
        private void RpcUpdateTimer(float time)
        {
            if (playerCardUI == null)
                playerCardUI = FindFirstObjectByType<PlayerCardUI>();

            if (playerCardUI != null && playerCardUI.gameObject != null)
            {
                playerCardUI.UpdateTimer(time);
            }
            else
            {
                Debug.LogWarning("[GamePlayer] RpcUpdateTimer() - PlayerCardUI is missing or destroyed.");
            }
        }

        // 카드 선택 완료 후 호출되는 함수 (클라이언트에서 실행)
        public void OnCardSelectionConfirmed()
        {
            // 현재 플레이어의 캐릭터 객체를 찾음
            if (playerCharacter == null)
            {
                playerCharacter = FindObjectsByType<LobbyPlayerCharacter>(FindObjectsSortMode.None)
                    .FirstOrDefault(pc => pc.playerId == PlayerSetting.PlayerId);
            }

            if (playerCharacter == null)
            {
                Debug.LogWarning("[GamePlayer] playerCharacter를 찾을 수 없습니다.");
                return;
            }

            // 선택된 강화 카드들의 ID 배열로 추출
            int[] selectedCardIds = PlayerSetting.PlayerCards.Select(card => card.ID).ToArray();

            // 서버에 캐릭터 설정 요청 (직업, 이동기, 공격 스킬, 팀, 카드 ID)
            CmdSetCharacterDataOnServer(
                PlayerSetting.PlayerCharacterClass,
                PlayerSetting.MoveSkill,
                PlayerSetting.AttackSkillIDs,
                PlayerSetting.TeamType,
                selectedCardIds
            );

            // 스탯 적용을 지연 실행 (UI 업데이트를 위해)
            StartCoroutine(DelayedStatSetup());

            // 선택한 카드 ID를 서버에 저장
            CmdSetPlayerCards(UserId, selectedCardIds);

            // 서버에서 캐릭터 상태를 Ready로 설정하고 강화 효과 적용
            CmdMarkPlayerReady(selectedCardIds);
        }

        [Command]
        public void CmdSetCharacterDataOnServer(
            Constants.CharacterClass cls,
            Constants.SkillType moveSkill,
            int[] attackSkills,
            Constants.TeamType team,
            int[] cardIDs)
        {
            // 현재 클라이언트와 연결된 캐릭터를 서버에서 탐색
            var target = FindObjectsByType<LobbyPlayerCharacter>(FindObjectsSortMode.None)
                .FirstOrDefault(pc => pc.connectionToClient == connectionToClient);

            if (target == null)
            {
                Debug.LogWarning("[CmdSetCharacterDataOnServer] 해당 connection의 캐릭터를 찾을 수 없습니다.");
                return;
            }

            // 캐릭터 클래스 및 스킬, 팀 정보 설정
            target.SetCharacterData(cls, moveSkill, attackSkills);
            target.team = team;

            Debug.Log($"[CmdSetCharacterDataOnServer] 캐릭터 설정 완료: {cls}, {moveSkill}, {string.Join(",", attackSkills)}");
        }


        private IEnumerator DelayedStatSetup()
        {
            yield return new WaitUntil(() => playerCharacter.PLayerCharacterClass != Constants.CharacterClass.None);

            var statUI = FindFirstObjectByType<PlayerStatUI>();
            statUI?.Setup(playerCharacter);
            playerCharacter.NotifyStatChanged();
        }

        [Command]
        public void CmdSetPlayerCards(string userId, int[] selectedCardIds)
        {
            if (string.IsNullOrEmpty(userId)) return;

            GameManager.Instance.SetPlayerCards(userId, selectedCardIds);
        }

        [Command]
        public void CmdMarkPlayerReady(int[] selectedCardIds)
        {
            if (playerCharacter == null) return;

            // ✅ 서버에서도 카드 강화 적용
            var cardList = selectedCardIds.Select(id => Database.GetPlayerCardData(id)).ToList();
            playerCharacter.ApplyCardBonuses(cardList);

            playerCharacter.State = Constants.PlayerState.Ready;

            // 모든 플레이어 준비 여부 체크
            bool allReady = FindObjectsByType<LobbyPlayerCharacter>(sortMode: FindObjectsSortMode.None)
                .All(p => p.State == Constants.PlayerState.Ready);

            if (allReady)
            {
                RpcGameStart();
                var timer = FindFirstObjectByType<NetworkTimer>();
                timer?.StartGameFlow(Constants.CountTime, Constants.MaxGameEventTime);

                GameHand.Instance?.SwitchTarget();
                
                FindFirstObjectByType<DragonAI>()?.Init();
            }
        }

        [ClientRpc]
        public void RpcGameStart()
        {
            if (isOwned)
            {
                StartCoroutine(EnsureCharacterSetupThenStart());
            }
        }
        
        private IEnumerator EnsureCharacterSetupThenStart()
        {
            yield return new WaitUntil(() =>
                playerCharacter != null &&
                playerCharacter.PLayerCharacterClass != Constants.CharacterClass.None);

            FindFirstObjectByType<GamePlayUI>()?.CallGameStart();
            FindFirstObjectByType<GamePlayUI>()?.ShowCards(PlayerSetting.PlayerCards);
        }

        public void CheckGameOver()
        {
            if (!isServer) return;
            Debug.Log("CheckGameOver");
            GameManager.Instance.TryCheckGameOver(); // ✅ 이제 여기서만 실행
        }
        
        
        void OnDestroy()
        {
            StopAllCoroutines(); // or Stop specific coroutine
            GameManager.Instance.isCardSelectionStarted =false;
        }
        
        [ClientRpc]
        public void RpcPrepareScoreBoard()
        {
            var sbUI = FindFirstObjectByType<ScoreBoardUI>();
            if (sbUI != null)
            {
                sbUI.gameObject.SetActive(true); // ✅ 먼저 켜두기
                Debug.Log("[GamePlayer] 스코어보드 미리 활성화됨");
            }
        }

    }
}
