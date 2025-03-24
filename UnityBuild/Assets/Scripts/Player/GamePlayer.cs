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

            public LobbyPlayerCharacter playerCharacter;

            [SerializeField] private GameObject gamePlayObject;
            [SerializeField] private GameObject gamePlayHand;

            private GamePlayUI gameplayUI;

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
                GameObject gameObj = Instantiate(gamePlayObject, Vector3.zero, Quaternion.identity);
                NetworkServer.Spawn(gameObj, connectionToClient);
                
                GameObject gamePlayobj = Instantiate(gamePlayHand, Vector3.zero, Quaternion.identity);
                NetworkServer.Spawn(gamePlayobj, connectionToClient);
                
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

            private void OnDestroy()
            {
                GameLobbyUI gameLobbyUI = FindFirstObjectByType<GameLobbyUI>();

                gameLobbyUI?.UpdatePlayerInRoon();
            }
            
            [ClientRpc]
            public void RpcSendFinalScore(Constants.PlayerRecord[] allRecords, int roundIndex)
            {
                if (gameplayUI == null)
                    gameplayUI = FindFirstObjectByType<GamePlayUI>();

                if (gameplayUI != null)
                {
                    gameplayUI.ShowFinalScoreBoard(allRecords, roundIndex);

                    // ✅ 점수표 보여준 후 다음 라운드로 넘어가는 흐름
                    StartCoroutine(HandleRoundTransition()); // ← 여기가 빠져있었음
                }
            }
            
            private IEnumerator HandleRoundTransition()
            {
                yield return new WaitForSeconds(Constants.ScoreBoardTime); // 점수판 5초 보여줌

                int currentRound = GameManager.Instance.CurrentRound;

                if (currentRound < 3)
                {
                    if (isServer)
                    {
                        // ✅ 동일 씬 다시 로드 (예: Gameplay 씬)
                        NetworkManager.singleton.ServerChangeScene(SceneManager.GetActiveScene().name);
                        //gameObject.SetActive(false);
                    }
                }
                else
                {
                    ScoreBoardUI scoreBoardUI = FindFirstObjectByType<ScoreBoardUI>();
                    scoreBoardUI.ShowReturnToLobbyButton(); // → UI에 버튼 활성화 함수
                }
            }
            
            public void CheckGameOver()
            {
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

                // ✅ 올바른 타입의 데이터 전송
                var allRecords = GameManager.Instance.GetAllPlayerRecords(); // ← 이게 핵심
                RpcSendFinalScore(allRecords, GameManager.Instance.CurrentRound - 1);
            }

        }
    }
