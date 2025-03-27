using System;
using System.Collections;
using System.Collections.Generic;
using DataSystem;
using DataSystem.Database;
using GameManagement;
using Mirror;
using Networking;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;

namespace Player
{
    public class GamePlayer : NetworkRoomPlayer
    {
        public PlayerGameStats stats;
        
        private PlayerCardUI playerCardUI;

        [SyncVar]
        public string PlayerNickname;

        [SyncVar]
        public string UserId;

        public LobbyPlayerCharacter playerCharacter;

        [SerializeField] private GameObject gamePlayObject;
        [SerializeField] private GameObject gamePlayHand;

        private GamePlayUI gameplayUI;
        private static bool gameplayObjectSpawned = false;
        
        private bool isRoundEnding = false;

        private void Awake()
        {
            gameplayUI = FindFirstObjectByType<GamePlayUI>();
        }

        public override void Start()
        {
            base.Start();

            if (isServer)
            {
                SpawnLobbyPlayerCharacter();
                StartCoroutine(WaitForAllPlayersThenStartCardSelection());
            }

            if (isOwned)
            {
                CmdSetNickname(PlayerSetting.Nickname);
                CmdSetPlayerNumber(PlayerSetting.PlayerId);
                playerCardUI = FindFirstObjectByType<PlayerCardUI>();
            }
        }
        
        [RuntimeInitializeOnLoadMethod]
        private static void OnLoad()
        {
            SceneManager.sceneLoaded += (scene, mode) =>
            {
                gameplayObjectSpawned = false;
            };
        }

        protected void SpawnLobbyPlayerCharacter()
        {
            if (isServer && !gameplayObjectSpawned)
            {
                GameObject gameObj = Instantiate(gamePlayObject, Vector3.zero, Quaternion.identity);
                NetworkServer.Spawn(gameObj);
    
                GameObject gamePlayobj = Instantiate(gamePlayHand, Vector3.zero, Quaternion.identity);
                NetworkServer.Spawn(gamePlayobj);

                gameplayObjectSpawned = true; // ✅ 중복 생성 방지
            }

            // 캐릭터는 여전히 각 플레이어별로 생성
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
        
        [Command]
        public void CmdSetUserId(string userId)
        {
            UserId = userId;
            playerCharacter.userId = userId;
        }

        [Command]
        public void CmdSetPlayerNumber(int playerNum)
        {
            playerCharacter.playerId = playerNum;
        }
        
        private IEnumerator CardSelectionTimer()
        {
            int cardSelectionTime = Constants.CardSelectionTime;
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
                if (!isOwned) return;

                if (playerCharacter == null)
                {
                    LobbyPlayerCharacter[] pc = FindObjectsByType<LobbyPlayerCharacter>(sortMode: FindObjectsSortMode.None);
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
                        int skillIdToUpgrade = slotData.AppliedSkill;

                        for (int i = 1; i < PlayerSetting.AttackSkillIDs.Length; i++)
                        {
                            if (PlayerSetting.AttackSkillIDs[i] == skillIdToUpgrade)
                            {
                                PlayerSetting.AttackSkillIDs[i] += 100;
                                break;
                            }
                        }
                    }
                }

                playerCharacter.CmdSetCharacterData(
                    PlayerSetting.PlayerCharacterClass,
                    PlayerSetting.MoveSkill,
                    PlayerSetting.AttackSkillIDs
                );
            }
        }

        private void OnDestroy()
        {
            GameLobbyUI gameLobbyUI = FindFirstObjectByType<GameLobbyUI>();

            gameLobbyUI?.UpdatePlayerInRoon();
        }
        
        [ClientRpc]
        public void RpcSendFinalScore(Constants.PlayerRecord[] allRecords, int roundIndex)
        {
            StartCoroutine(ShowFinalScoreAndNextRound(allRecords, roundIndex));
        }

        private IEnumerator ShowFinalScoreAndNextRound(Constants.PlayerRecord[] allRecords, int roundIndex)
        {
            float timeout = 1f;
            while (gameplayUI == null && timeout > 0f)
            {
                gameplayUI = FindFirstObjectByType<GamePlayUI>();
                timeout -= Time.deltaTime;
                yield return null;
            }

            if (gameplayUI != null)
            {
                gameplayUI.ShowGameOverTextAndScore(allRecords, roundIndex);
            }
            else
            {
                Debug.LogWarning("[GamePlayer] gameplayUI를 찾지 못했습니다. 점수판은 생략하고 다음 라운드로 넘어갑니다.");
            }
            
            StartCoroutine(HandleRoundTransition()); // ✅ UI 없더라도 다음 라운드 진행
        }
        
        private IEnumerator HandleRoundTransition()
        {
            yield return new WaitForSeconds(Constants.ScoreBoardTime); // 점수판 보여줌

            int currentRound = GameManager.Instance.currentRound;

            if (currentRound < 3)
            {
                if (isServer)
                {
                    // ✅ 동일 씬 다시 로드 (예: Gameplay 씬)
                    NetworkManager.singleton.ServerChangeScene(SceneManager.GetActiveScene().name);
                    //gameObject.SetActive(false);
                }
                else
                {
                    // ✅ 클라이언트라면 서버에 씬 전환 요청
                    CmdRequestSceneReload();
                }
            }
            else
            {
                ShowReturnToLobbyButton();
            }
        }
        
        [Command]
        public void CmdRequestSceneReload()
        {
            Debug.Log("[GamePlayer] CmdRequestSceneReload called. Reloading scene on server.");
            NetworkManager.singleton.ServerChangeScene(SceneManager.GetActiveScene().name);
        }
        
        public void ShowReturnToLobbyButton()
        {
            ScoreBoardUI scoreBoardUI = FindFirstObjectByType<ScoreBoardUI>();
            scoreBoardUI.ShowReturnToLobbyButton();
        }
        
        public void CheckGameOver()
        {
            if (isRoundEnding)
                return;
            
            isRoundEnding = true;
            
            var alivePlayers = GameManager.Instance.GetAlivePlayers();
            if (alivePlayers.Count > 1)
                return;
            
            var roundRanks = GameManager.Instance.GetCurrentRoundRanks();

            // ✅ roundData 생성
            List<(int playerId, int kills, int outKills, int damageDone, int rank)> roundData = new();
            foreach (var (playerId, rank) in roundRanks)
            {
                var stats = GameManager.Instance.GetPlayerStats(playerId);
                roundData.Add((playerId, stats.kills, stats.outKills, stats.damageDone, rank));
            }

            GameManager.Instance.AddRoundResult(roundData);
            
            RpcUpdateRound(GameManager.Instance.currentRound);

            // ✅ 올바른 타입의 데이터 전송
            var allRecords = GameManager.Instance.GetAllPlayerRecords(); // ← 이게 핵심
            RpcSendFinalScore(allRecords, GameManager.Instance.currentRound - 1);
        }
        
        IEnumerator WaitForAllPlayersThenStartCardSelection()
        {
            // 모든 플레이어가 로비에 들어올 때까지 대기
            while (NetworkServer.connections.Count < NetworkRoomManager.singleton.numPlayers)
            {
                yield return null;
            }

            // 모든 플레이어의 캐릭터가 생성되었는지도 확인
            while (FindObjectsOfType<LobbyPlayerCharacter>().Length < NetworkRoomManager.singleton.numPlayers)
            {
                yield return null;
            }

            yield return new WaitForSeconds(0.5f); // 안정성 확보용

            StartCoroutine(CardSelectionTimer());
        }
        
        public void OnCardSelectionConfirmed()
        {
            if (playerCharacter == null)
            {
                playerCharacter = FindObjectsOfType<LobbyPlayerCharacter>()
                    .FirstOrDefault(pc => pc.playerId == PlayerSetting.PlayerId);
            }

            if (playerCharacter == null)
            {
                Debug.LogWarning("[GamePlayer] playerCharacter를 여전히 찾지 못했습니다.");
                return;
            }

            playerCharacter.State = Constants.PlayerState.Ready;

            playerCharacter.CmdSetCharacterData(
                PlayerSetting.PlayerCharacterClass,
                PlayerSetting.MoveSkill,
                PlayerSetting.AttackSkillIDs
            );

            Debug.Log("[GamePlayer] 카드 선택 완료 및 캐릭터 정보 서버 전송 완료");
        }

        
        [Command]
        public void CmdConfirmCardSelected()
        {
            if (playerCharacter == null)
            {
                playerCharacter = FindObjectsOfType<LobbyPlayerCharacter>()
                    .FirstOrDefault(pc => pc.playerId == PlayerSetting.PlayerId);
            }
            
            playerCharacter.State = Constants.PlayerState.Ready;

            var allReady = FindObjectsOfType<LobbyPlayerCharacter>()
                .All(p => p.State == Constants.PlayerState.Ready);

            if (allReady)
            {
                RpcGameStart(); // 기존 UI 호출
                // ✅ 여기에 타이머 시작 추가
                var timer = FindFirstObjectByType<NetworkTimer>();
                if (timer != null)
                {
                    timer.StartGameFlow(Constants.CountTime, Constants.MaxGameEventTime);
                }
            }
        }
        
        [ClientRpc]
        public void RpcGameStart()
        {
            GamePlayUI ui = FindFirstObjectByType<GamePlayUI>();
            if (ui != null)
            {
                ui.CallGameStart(); // 👈 GameStart를 public 메서드로 분리
            }
        }
        
        [ClientRpc]
        public void RpcUpdateRound(int round)
        {
            GameManager.Instance.currentRound = round;
            Debug.Log($"[Client] currentRound updated: {round}");
        }


    }
}