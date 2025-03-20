using System;
using System.Linq;
using DataSystem;
using GameManagement;
using kcp2k;
using Mirror;
using Player;
using Unity.VisualScripting;
using UnityEngine;

namespace Networking
{
    public class RoomManager : NetworkRoomManager
    {
        [SerializeField] private GameRoomData gameRoomDataPrefab;

        private GameRoomData roomDataInstance;

        public string roomName;
        public Constants.RoomType roomType;
        public int maxPlayerCount = 6;

        public string matchID = null;

        public override void Start()
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].StartsWith("-port="))
                {
                    ushort port;
                    if (ushort.TryParse(args[i].Substring(6), out port))
                    {
                        Debug.Log($"[RoomManager] 포트 변경: {port}");
                        KcpTransport transport = GetComponent<KcpTransport>();
                        transport.Port = port;
                    }
                    else
                    {
                        Debug.LogWarning($"[RoomManager] 포트 변경 실패: {args[i].Substring(6)}");
                    }
                }
                else if (args[i].StartsWith("-roomName="))
                {
                    roomName = args[i].Substring(10);
                    Debug.Log($"[RoomManager] 방 이름 변경: {roomName}");
                }
                else if (args[i].StartsWith("-roomType="))
                {
                    if (Enum.TryParse(args[i].Substring(10), out Constants.RoomType type))
                    {
                        roomType = type;
                        Debug.Log($"[RoomManager] 방 유형 변경: {roomType}");
                    }
                    else
                    {
                        Debug.LogWarning($"[RoomManager] 방 유형 변경 실패: {args[i].Substring(10)}");
                    }
                }
                else if (args[i].StartsWith("-maxPlayerCount="))
                {
                    if (int.TryParse(args[i].Substring(16), out int count))
                    {
                        maxPlayerCount = count;
                        Debug.Log($"[RoomManager] 최대 인원 변경: {maxPlayerCount}");
                    }
                    else
                    {
                        Debug.LogWarning($"[RoomManager] 최대 인원 변경 실패: {args[i].Substring(16)}");
                    }
                }
            }

            base.Start();
            if (Application.platform.Equals(RuntimePlatform.LinuxServer))
            {
                StartServer();
            }
        }

        public override void OnRoomStartHost()
        {
            base.OnRoomStartHost();

            if (roomDataInstance == null && gameRoomDataPrefab != null)
            {
                roomDataInstance = Instantiate(gameRoomDataPrefab);
                NetworkServer.Spawn(roomDataInstance.gameObject);

                // 방 데이터 초기화
                roomDataInstance.SetRoomData(roomName, roomType, maxPlayerCount);
                maxConnections = maxPlayerCount;

                Debug.Log($"[RoomManager] 방 생성: {roomName}, 유형: {roomType}, 최대 인원: {maxConnections}");
            }
        }

        public override void OnRoomStartServer()
        {
            base.OnRoomStartServer();

            if (NetworkClient.active)
            {
                return; // 호스트 모드라면 종료
            }

            if (roomDataInstance == null && gameRoomDataPrefab != null)
            {
                roomDataInstance = Instantiate(gameRoomDataPrefab);
                NetworkServer.Spawn(roomDataInstance.gameObject);
                DontDestroyOnLoad(roomDataInstance.gameObject);
                
                // 방 데이터 초기화
                roomDataInstance.SetRoomData(roomName, roomType, maxPlayerCount);
                maxConnections = maxPlayerCount;

                Debug.Log($"[RoomManager] 방 생성: {roomName}, 유형: {roomType}, 최대 인원: {maxConnections}");
            }
        }

        public override void OnServerConnect(NetworkConnectionToClient conn)
        {
            if (roomDataInstance.maxPlayerCount <= roomSlots.Count)
            {
                Debug.LogWarning("[RoomManager] 방이 가득 찼습니다.");
                conn.Disconnect();
                return;
            }

            base.OnServerConnect(conn);
        }

        public void StartGame()
        {
            if (matchID != null)
            {
                Debug.LogWarning("[RoomManager] 이미 게임이 시작되었습니다.");
                return;
            }

            matchID = Guid.NewGuid().ToString();

            if (roomSlots.Count < 1)
            {
                Debug.LogWarning("[RoomManager] 최소 1명의 플레이어가 필요합니다.");
                return;
            }

            foreach (var player in roomSlots)
            {
                player.CmdChangeReadyState(true);
            }

            Debug.Log("[RoomManager] 모든 플레이어가 준비되었습니다. 게임을 시작합니다! Match ID: " + matchID);
            if (roomSlots.Count > 1)
            {
                ServerChangeScene(GameplayScene);
            }
        }

    public override void OnServerChangeScene(string newSceneName)
        {
            base.OnServerChangeScene(newSceneName);
            if (newSceneName == GameplayScene)
            {
                // 게임 시작
                Debug.Log("[RoomManager] 게임 시작");
                GameLobbyUI gameLobbyUI = FindFirstObjectByType<GameLobbyUI>();
                if (gameLobbyUI != null)
                {
                    gameLobbyUI.gameObject.SetActive(false);
                }
            }
            else if (newSceneName == RoomScene)
            {
                // 게임 종료
                if (matchID == null)
                {
                    Debug.LogWarning("[RoomManager] 게임이 시작되지 않았습니다.");
                    return;
                }
                Debug.Log("[RoomManager] 게임 종료");
                matchID = null;
                foreach (var player in roomSlots)
                {
                    player.CmdChangeReadyState(false);
                }
            }
        }
    }
}