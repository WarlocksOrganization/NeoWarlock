using System;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using DataSystem;
using Player;
using UI;
using UnityEngine.Rendering.LookDev;
using UnityEngine.SceneManagement;

namespace GameManagement
{
    public class GameRoomData : NetworkBehaviour
    {
        [SyncVar] public string roomName = "기본 방 이름"; // 방 이름 동기화
        [SyncVar] public Constants.RoomType roomType = Constants.RoomType.Solo; // 방 유형 동기화
        [SyncVar] public int maxPlayerCount = 4; // 최대 인원 동기화
        [SyncVar(hook = nameof(OnMapTypeChanged))] public Constants.RoomMapType roomMapType = Constants.RoomMapType.SSAFY;

        [SyncVar] public int gameId = 0;
        [SyncVar] public int roomId = 0;
        
        [SerializeField] private GameObject[] SSAFYObjects;
        [SerializeField] private GameObject[] LavaObjects;
        [SerializeField] private GameObject[] SpaceObjects;
        [SerializeField] private GameObject[] SeaObjects;
        
        [SerializeField] private List<GameObject> spawnedObjects = new();
        
        [SyncVar] public int currentRound = 0;
        
        private void Start()
        {
            GameLobbyUI gameLobbyUI = FindFirstObjectByType<GameLobbyUI>();
            if (gameLobbyUI != null && gameLobbyUI.RoomNameText != null)
            {
                gameLobbyUI.RoomNameText.text = roomName;
            }
        }
        
        public override void OnStartClient()
        {
            base.OnStartClient();

            // 로비 UI 찾기
            var ui = FindFirstObjectByType<GameLobbyUI>();
            if (ui != null)
            {
                ui.UpdateMapUI(roomMapType); // ✅ 현재 맵 타입으로 UI 갱신
                Debug.Log($"[GameRoomData] 클라이언트 초기화 - 맵 타입: {roomMapType}");
            }
        }

        [Server]
        public void SetRoomData(string name, Constants.RoomType type, int maxPlayers, int gId = 0, int rId = 0)
        {
            roomName = name;
            roomType = type;
            maxPlayerCount = maxPlayers;
            gameId = gId;
            roomId = rId;

            Debug.Log($"[GameRoomData] 설정 완료: {roomName}, 유형: {roomType}, 최대 인원: {maxPlayerCount}");
        }
        
        public void ChangeMap(bool next)
        {
            int current = (int)roomMapType;
            int totalMaps = 4; // 0~3까지 4개만 순환

            current += next ? 1 : -1;
            if (current < 0) current = totalMaps - 1;
            if (current >= totalMaps) current = 0;

            roomMapType = (Constants.RoomMapType)current;
        }

        private void OnMapTypeChanged(Constants.RoomMapType oldVal, Constants.RoomMapType newVal)
        {
            // 클라이언트 Hook: Lobby UI에 반영
            var lobby = FindFirstObjectByType<GameLobbyUI>();
            lobby?.UpdateMapUI(newVal);
        }
        
        [Server]
        public void SetRandomMapIfNeeded()
        {
            if (roomMapType == Constants.RoomMapType.Random)
            {
                // 1~3 사이 랜덤 맵으로 설정 (SSAFY, Lava, Space)
                int randomIndex = UnityEngine.Random.Range(1, 4); // 1, 2, 3
                roomMapType = (Constants.RoomMapType)randomIndex;

                Debug.Log($"[GameRoomData] 랜덤 맵 선택됨: {roomMapType}");
            }
        }
        
        [Server]
        public void SpawnGamePlayObjects()
        {
            ClearGamePlayObjects();

            GameObject[] targetPrefabs = roomMapType switch
            {
                Constants.RoomMapType.SSAFY => SSAFYObjects,
                Constants.RoomMapType.Lava => LavaObjects,
                Constants.RoomMapType.Space => SpaceObjects,
                Constants.RoomMapType.Sea => SeaObjects,
                _ => null
            };

            if (targetPrefabs == null) return;
            

            foreach (var prefab in targetPrefabs)
            {
                GameObject instance = Instantiate(prefab);
                NetworkServer.Spawn(instance);
                spawnedObjects.Add(instance);
            }
        }

        [Server]
        public void ClearGamePlayObjects()
        {
            foreach (var obj in spawnedObjects)
            {
                if (obj != null)
                    NetworkServer.Destroy(obj);
            }
            spawnedObjects.Clear();
        }
        
        [Server]
        public void StartNextRound()
        {
            Debug.Log("✅ [GameRoomData] StartNextRound 실행");

            currentRound++; // ⬅️ 라운드 인덱스 증가

            // 🔥 여기서 직접 GamePlayer.RpcStartCardSelection 호출 필요
            foreach (var conn in NetworkServer.connections.Values)
            {
                var player = conn.identity.GetComponent<GamePlayer>();
                if (player != null)
                {
                    player.RpcStartCardSelection(); // ✅ 각 플레이어에게 클라이언트 RPC 전송
                }
            }
        }

        [Server]
        private void RespawnAllPlayers()
        {
            foreach (var player in FindObjectsByType<PlayerCharacter>(FindObjectsSortMode.None))
            {
                player.State = Constants.PlayerState.NotReady;
            }
            
            foreach (var item in FindObjectsByType<SkillItemPickup>(sortMode: FindObjectsSortMode.None))
            {
                NetworkServer.Destroy(item.gameObject);
            }
            
            foreach (var item in FindObjectsByType<EnemyAI>(sortMode: FindObjectsSortMode.None))
            {
                NetworkServer.Destroy(item.gameObject);
            }

            RpcPlayerCharacterUIResurrect();
        }

        [ClientRpc]
        private void RpcPlayerCharacterUIResurrect()
        {
            FindFirstObjectByType<PlayerCharacterUI>().OnResurrectButtonClicked();
        }

        [Server]
        public void EndGame()
        {
            RpcShowReturnToLobbyButton();
        }

        [ClientRpc]
        private void RpcShowReturnToLobbyButton()
        {
            FindFirstObjectByType<ScoreBoardUI>()?.ShowReturnToLobbyButton();
        }
        
        [Server]
        public void PrepareNextRound()
        {
            Debug.Log("🔧 [GameRoomData] 다음 라운드 준비 중");

            // 라운드 증가 없이 선작업만
            GameManager.Instance.ResetRoundStateOnly();
            SpawnGamePlayObjects();      // 맵 오브젝트 미리 생성
            RespawnAllPlayers();         // 미리 부활
        }

    }
}