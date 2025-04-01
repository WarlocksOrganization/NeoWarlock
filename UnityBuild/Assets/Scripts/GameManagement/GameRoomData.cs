using System;
using Mirror;
using UnityEngine;
using DataSystem;
using UnityEngine.Rendering.LookDev;
using UnityEngine.SceneManagement;

namespace GameManagement
{
    public class GameRoomData : NetworkBehaviour
    {
        [SyncVar] public string roomName = "기본 방 이름"; // 방 이름 동기화
        [SyncVar] public Constants.RoomType roomType = Constants.RoomType.Solo; // 방 유형 동기화
        [SyncVar] public int maxPlayerCount = 4; // 최대 인원 동기화
        [SyncVar] public int Round = 3;
        [SyncVar(hook = nameof(OnMapTypeChanged))] public Constants.RoomMapType roomMapType = Constants.RoomMapType.SSAFY;
        
        [SerializeField] private GameObject[] SSAFYMapObjects;
        [SerializeField] private GameObject[] LavaMapObjects;
        [SerializeField] private GameObject[] SpaceMapObjects;
        [SerializeField] private GameObject[] SeaMapObjects;

        [SyncVar] public int gameId = 0;
        [SyncVar] public int roomId = 0;
        
        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
        
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!isServer) return;
            if (scene.name != "GamePlay") return; // 게임 씬 이름에 맞게 수정

            SpawnMapObjects();
        }
        
        private void SpawnMapObjects()
        {
            GameObject[] selectedMapSet = roomMapType switch
            {
                Constants.RoomMapType.SSAFY => SSAFYMapObjects,
                Constants.RoomMapType.Lava => LavaMapObjects,
                Constants.RoomMapType.Space => SpaceMapObjects,
                Constants.RoomMapType.Sea => SeaMapObjects,
                _ => null
            };

            if (selectedMapSet == null)
            {
                Debug.LogWarning("[GameRoomData] 맵 오브젝트 배열이 null입니다.");
                return;
            }

            foreach (var prefab in selectedMapSet)
            {
                GameObject go = Instantiate(prefab);
                NetworkServer.Spawn(go);
            }

            Debug.Log($"[GameRoomData] {roomMapType} 맵 오브젝트 생성 완료");
        }
        
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
    }
}