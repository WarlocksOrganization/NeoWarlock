using System;
using System.Collections.Generic;
using System.Linq;
using DataSystem;
using GameManagement;
using kcp2k;
using Mirror;
using Newtonsoft.Json.Linq;
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
        public bool isRoomOccupied => roomSlots.Count > 0;

        public override void Start()
        {
            if (Application.platform.Equals(RuntimePlatform.LinuxServer))
            {
                Debug.Log("[RoomManager] 리눅스 서버 모드로 실행 중");
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
            }
            
            base.Start();
            if (Application.platform.Equals(RuntimePlatform.LinuxServer))
            {
                StartServer();
            }
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
    
            if (roomDataInstance == null && gameRoomDataPrefab != null)
            {
                roomDataInstance = Instantiate(gameRoomDataPrefab);
                NetworkServer.Spawn(roomDataInstance.gameObject);
                DontDestroyOnLoad(roomDataInstance.gameObject);

                roomDataInstance.SetRoomData(roomName, roomType, maxPlayerCount);
                maxConnections = maxPlayerCount;

                Debug.Log("[RoomManager] OnStartServer()에서 방 생성 완료");
            }
        }

        public override void OnRoomStartServer()
        {
            base.OnRoomStartServer();

            if (!UnityEngine.Application.platform.Equals(RuntimePlatform.LinuxServer))
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
            if (roomDataInstance == null)
            {
                Debug.LogWarning("[RoomManager] roomDataInstance가 아직 생성되지 않았습니다. 연결을 종료합니다.");
                conn.Disconnect();
                return;
            }

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
            if (roomDataInstance.gameId != null)
            {
                Debug.LogWarning("[RoomManager] 이미 게임이 시작되었습니다.");
                return;
            }

            var players = FindObjectsOfType<PlayerCharacter>();
            bool allReady = players.All(p => p.State == Constants.PlayerState.Start);

            if (!allReady)
            {
                Debug.LogWarning("[RoomManager] 아직 준비되지 않은 플레이어가 있어 게임을 시작할 수 없습니다.");
                // return;
            }

            Debug.Log("[RoomManager] 모든 플레이어가 준비되었습니다. 게임을 시작합니다!");
            ServerChangeScene(GameplayScene);
        }
        public Dictionary<string, string> GetRoomData()
        {
            Dictionary<string, string> data = new Dictionary<string, string>();
            data["roomName"] = roomDataInstance.roomName;
            data["roomType"] = roomDataInstance.roomType.ToString();
            data["maxPlayerCount"] = roomDataInstance.maxPlayerCount.ToString();
            data["gameId"] = roomDataInstance.gameId;
            data["roomId"] = roomDataInstance.roomId;
            return data;
        }

        public void SetRoomData(Dictionary<string, string> data)
        {
            roomDataInstance.SetRoomData(data["roomName"], Constants.RoomType.Solo, int.Parse(data["maxPlayerCount"]), data["gameId"], data["roomId"]);
        }
        
        public override void OnClientDisconnect()
        {
            base.OnClientDisconnect();
            var modal = ModalPopupUI.singleton as ModalPopupUI;
            if (modal != null)
            {
                modal.ShowModalMessage("서버와의 연결이 끊겼습니다.");
            }
            SocketManager.singleton.RequestExitRoom();
        }
    }
}