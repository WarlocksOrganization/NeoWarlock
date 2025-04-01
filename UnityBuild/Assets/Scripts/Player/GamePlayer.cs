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

        [SerializeField] private GameObject[] SSAFYPlayObject;
        [SerializeField] private GameObject[] LavaPlayObject;
        [SerializeField] private GameObject[] SpacePlayObject;
        [SerializeField] private GameObject[] SeaPlayObject;

        private PlayerCardUI playerCardUI;
        private GamePlayUI gameplayUI;
        private static bool gameplayObjectSpawned = false;

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
                CmdSetUserId(PlayerSetting.UserId);
                playerCardUI = FindFirstObjectByType<PlayerCardUI>();
            }
        }

        [RuntimeInitializeOnLoadMethod]
        private static void OnLoad() => SceneManager.sceneLoaded += (_, _) => gameplayObjectSpawned = false;

        private void SpawnLobbyPlayerCharacter()
        {
            if (isServer && !gameplayObjectSpawned)
            {
                gameplayObjectSpawned = true;
                
                GameRoomData gameRoomData = FindFirstObjectByType<GameRoomData>();
                if (gameRoomData.roomMapType == Constants.RoomMapType.SSAFY)
                {
                    foreach (GameObject gameObject in SSAFYPlayObject)
                    {
                        NetworkServer.Spawn(Instantiate(gameObject), connectionToClient);
                    }
                }
                else if (gameRoomData.roomMapType == Constants.RoomMapType.Lava)
                {
                    foreach (GameObject gameObject in LavaPlayObject)
                    {
                        NetworkServer.Spawn(Instantiate(gameObject), connectionToClient);
                    }
                }
                else if (gameRoomData.roomMapType == Constants.RoomMapType.Space)
                {
                    foreach (GameObject gameObject in SpacePlayObject)
                    {
                        NetworkServer.Spawn(Instantiate(gameObject), connectionToClient);
                    }
                }
                else if (gameRoomData.roomMapType == Constants.RoomMapType.Sea)
                {
                    foreach (GameObject gameObject in SeaPlayObject)
                    {
                        NetworkServer.Spawn(Instantiate(gameObject), connectionToClient);
                    }
                }
            }

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
            yield return new WaitUntil(() => gameplayUI != null);
            gameplayUI.ShowGameOverTextAndScore(allRecords, roundIndex);
            StartCoroutine(HandleRoundTransition());
        }

        private IEnumerator HandleRoundTransition()
        {
            yield return new WaitForSeconds(Constants.ScoreBoardTime);

            int currentRound = GameManager.Instance.currentRound;

            if (currentRound < 3)
            {
                if (isServer)
                {
                    NetworkManager.singleton.ServerChangeScene(SceneManager.GetActiveScene().name);
                }
                else
                {
                    yield return new WaitUntil(() => NetworkClient.ready);
                    CmdRequestSceneReload();
                }
            }
            else
            {
                // ❗ 현재 isServer 조건 걸려있음
                // 이건 ClientRpc니까 서버가 아니라도 실행되도록 호출만 서버에서 하면 돼
                if (isServer)
                {
                    Debug.Log("🔔 최종 라운드 종료, 로비 버튼 표시");
                    // 모든 플레이어에게 로비 버튼 표시
                    RpcShowReturnToLobbyButton();
                }
                else
                {
                    FindFirstObjectByType<ScoreBoardUI>()?.ShowReturnToLobbyButton();
                }
            }
        }

        [Command(requiresAuthority = false)]
        public void CmdRequestSceneReload()
        {
            if (isServer)
                NetworkManager.singleton.ServerChangeScene(SceneManager.GetActiveScene().name);
        }

        [ClientRpc]
        public void RpcShowReturnToLobbyButton()
        {
            FindFirstObjectByType<ScoreBoardUI>()?.ShowReturnToLobbyButton();
        }

        private IEnumerator WaitForAllPlayersThenStartCardSelection()
        {
            yield return new WaitUntil(() => NetworkServer.connections.Count >= NetworkRoomManager.singleton.numPlayers);
            yield return new WaitUntil(() => FindObjectsByType<LobbyPlayerCharacter>(sortMode: FindObjectsSortMode.None).Length >= NetworkRoomManager.singleton.numPlayers);
            yield return new WaitForSeconds(0.5f);
            StartCoroutine(CardSelectionTimer());
        }

        private IEnumerator CardSelectionTimer()
        {
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

        [TargetRpc]
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

        public void OnCardSelectionConfirmed()
        {
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

            playerCharacter.CmdSetCharacterData(
                PlayerSetting.PlayerCharacterClass,
                PlayerSetting.MoveSkill,
                PlayerSetting.AttackSkillIDs
            );
            StartCoroutine(DelayedStatSetup());
            int[] selectedCardIds = PlayerSetting.PlayerCards.Select(card => card.ID).ToArray();
            CmdSetPlayerCards(UserId, selectedCardIds);
            CmdMarkPlayerReady(selectedCardIds);
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
            var gameManager = GameManagement.GameManager.Instance;
            if (gameManager == null)
            {
                Debug.LogError("[GamePlayer] GameManager is null.");
                return;
            }
            gameManager.SetPlayerCards(playerCharacter.userId, selectedCardIds);
        }

        [Command]
        public void CmdMarkPlayerReady(int[] selectedCardIds)
        {
            if (playerCharacter == null) return;

            playerCharacter.State = Constants.PlayerState.Ready;

            // 모든 플레이어 준비 여부 체크
            bool allReady = FindObjectsByType<LobbyPlayerCharacter>(sortMode: FindObjectsSortMode.None)
                .All(p => p.State == Constants.PlayerState.Ready);

            if (allReady)
            {
                RpcGameStart();
                var timer = FindFirstObjectByType<NetworkTimer>();
                timer?.StartGameFlow(Constants.CountTime, Constants.MaxGameEventTime);
            }
        }

        [ClientRpc]
        public void RpcGameStart()
        {
            if (isOwned)
                FindFirstObjectByType<GamePlayUI>()?.CallGameStart();
        }

        public void CheckGameOver()
        {
            if (!isServer) return;
            GameManager.Instance.TryCheckGameOver(); // ✅ 이제 여기서만 실행
        }


        [ClientRpc]
        public void RpcUpdateRound(int round)
        {
            GameManager.Instance.currentRound = round;
        }
        
        [RuntimeInitializeOnLoadMethod]
        private static void OnSceneLoaded()
        {
            SceneManager.sceneLoaded += (_, _) =>
            {
                if (GameManager.Instance != null)
                    GameManager.Instance.ResetRoundState();
            };
        }
    }
}
