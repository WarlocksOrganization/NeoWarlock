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
            private float cardSelectionTime = 10f;

            [SyncVar]
            public string PlayerNickname;

            public LobbyPlayerCharacter playerCharacter;

            [SerializeField] private GameObject gamePlayObject;

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
            public void RpcSendFinalScore(Constants.PlayerStats[] sortedStats)
            {
                if (gameplayUI == null)
                    gameplayUI = FindFirstObjectByType<GamePlayUI>();

                if (gameplayUI != null)
                {
                    gameplayUI.ShowFinalScoreBoard(sortedStats); // 점수판 띄우기

                    // ✅ 현재 라운드에 따라 다음 행동 결정
                    StartCoroutine(HandleRoundTransition());
                }
            }
            
            private IEnumerator HandleRoundTransition()
            {
                yield return new WaitForSeconds(15f); // 점수판 5초 보여줌

                int currentRound = GameManager.Instance.CurrentRound;

                if (currentRound < 3)
                {
                    if (isServer)
                    {
                        GameManager.Instance.NextRound();

                        // ✅ 동일 씬 다시 로드 (예: Gameplay 씬)
                        NetworkManager.singleton.ServerChangeScene(SceneManager.GetActiveScene().name);
                    }
                }
                else
                {
                    // ✅ 3라운드면 방으로 돌아가기 버튼 활성화
                    if (isServer)
                    {
                        ScoreBoardUI scoreBoardUI = FindFirstObjectByType<ScoreBoardUI>();
                        scoreBoardUI.ShowReturnToLobbyButton(); // → UI에 버튼 활성화 함수
                    }
                }
            }
            
            public void CheckGameOver()
            {
                var roundRanks = GameManager.Instance.GetCurrentRoundRanks();

                // ✅ 여기서 roundData 생성
                List<(int playerId, int kills, int outKills, int damageDone, int rank)> roundData = new();
                foreach (var (playerId, rank) in roundRanks)
                {
                    var stats = GameManager.Instance.GetPlayerStats(playerId); // PlayerStats 가져오기
                    roundData.Add((playerId, stats.kills, stats.outKills, stats.damageDone, rank));
                }


                // ✅ 반드시 이걸 호출해야 record.roundStatsList에 반영됨
                GameManager.Instance.AddRoundResult(roundData);

                var sortedStats = GameManager.Instance.GetSortedPlayerStats();
                RpcSendFinalScore(sortedStats);
            }

            
            [Command]
            public void CmdRequestReturnToLobby()
            {
                if (isServer)
                {
                    // 서버에서 전체 플레이어를 로비 씬으로 이동
                    NetworkManager.singleton.ServerChangeScene("GameRoom"); // 여기에 실제 로비 씬 이름
                }
            }
        }
    }
