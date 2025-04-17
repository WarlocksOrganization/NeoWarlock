using System;
using System.Collections.Generic;
using System.Linq;
using DataSystem;
using GameManagement;
using kcp2k;
using Mirror;
using Newtonsoft.Json.Linq;
using Player;
using UnityEngine;

namespace Networking
{
    public partial class SocketManager : MonoBehaviour
    {
        private JToken _latestRoomData;
        private bool _hasPendingRoomUpdate = false;

        // 네트워크 메시지 구조체 정의
        public struct RoomListChangedMessage : NetworkMessage
        {
            public string eventName;
        }

        // 외부에서 방 목록 갱신 여부 확인
        public bool HasPendingRoomUpdate => _hasPendingRoomUpdate;
        public void ClearRoomUpdateFlag() => _hasPendingRoomUpdate = false;

        // ✅ 방 생성 요청
        public void RequestCreateRoom(string roomName, int maxPlayers)
        {
            JToken roomData = new JObject();
            roomData["action"] = "createRoom";
            roomData["roomName"] = roomName;
            roomData["maxPlayers"] = maxPlayers;
            SendRequestToServer(roomData);
        }

        // ✅ 방 목록 요청
        public void RequestListRooms()
        {
            JToken roomData = new JObject();
            roomData["action"] = "listRooms";
            SendRequestToServer(roomData);
        }

        // ✅ 방 참가 요청
        public void RequestJoinRoom(int roomId, ushort port)
        {
            try
            {
                var manager = RoomManager.singleton as RoomManager;
                KcpTransport transport = manager.GetComponent<KcpTransport>();
                transport.Port = port;

                JToken roomData = new JObject();
                roomData["action"] = "joinRoom";
                roomData["roomId"] = roomId;

                SendRequestToServer(roomData);
                Debug.Log($"[SocketManager] 방 참가: {roomId}");
            }
            catch (Exception ex)
            {
                RequestExitRoom();
                ModalPopupUI.singleton?.ShowModalMessage("방 참가 실패\n서버와의 연결에 실패했습니다.");
                Debug.Log($"[SocketManager] 방 참가 실패: {ex.Message}");
            }
        }

        // ✅ 방 퇴장 요청
        public void RequestExitRoom()
        {
            JToken roomData = new JObject();
            roomData["action"] = "exitRoom";
            SendRequestToServer(roomData);
        }

        // ✅ 방 생성 응답 처리
        private void HandleCreateRoom(JToken data)
        {
            Debug.Log("[SocketManager] 방 생성됨");

            if (data.SelectToken("status").ToString() == "success")
            {
                try
                {
                    var manager = RoomManager.singleton as RoomManager;
                    KcpTransport transport = manager.GetComponent<KcpTransport>();
                    transport.Port = data["port"].ToObject<ushort>();
                    manager.StartClient();

                    Debug.Log($"[SocketManager] 방 생성 및 참가 완료: {data["roomId"].ToObject<int>()}");
                }
                catch (Exception ex)
                {
                    RequestExitRoom();
                    ModalPopupUI.singleton?.ShowModalMessage("방 생성 및 참가 실패\n서버와의 연결에 실패했습니다.");
                    Debug.Log($"[SocketManager] 방 생성 실패: {ex.Message}");
                }
            }
            else
            {
                Debug.Log($"[SocketManager] 방 생성 실패: {data["message"].ToString()}");
            }
        }

        // ✅ 방 목록 응답 처리
        private void HandleListRooms(JToken data)
        {
            Debug.Log("[SocketManager] 방 목록 받음");
            FindRoomUI findRoomUI = FindFirstObjectByType<FindRoomUI>();
            findRoomUI.UpdateContainer(data);

            foreach (var room in data["rooms"])
            {
                Debug.Log(room);
            }
        }

        // ✅ 방 참가 응답 처리
        private void HandleJoinRoom(JToken data)
        {
            if (data["status"].ToString() == "success")
            {
                RoomManager.singleton.StartClient();
                Debug.Log("[SocketManager] 방 참가 완료");
            }
            else
            {
                RequestExitRoom();
                ModalPopupUI.singleton?.ShowModalMessage("방 참가 실패\n서버와의 연결에 실패했습니다.");
                Debug.Log($"[SocketManager] 방 참가 실패: {data["message"]}");
            }
        }

        // ✅ 방 퇴장 응답 처리
        private void HandleExitRoom(JToken data)
        {
            if (data["status"].ToString() == "success")
            {
                RoomManager.singleton.StopClient();
                Debug.Log("[SocketManager] 방 퇴장 성공");
            }
            else
            {
                ModalPopupUI.singleton?.ShowModalMessage("방 퇴장 실패\n서버와의 연결에 실패했습니다.");
                Debug.Log($"[SocketManager] 방 퇴장 실패: {data["message"]}");
            }
        }

        // ✅ 서버에서 게임 시작 요청
        [Server]
        public void RequestGameStart(int roomId = 0)
        {
            if (!Application.platform.Equals(RuntimePlatform.LinuxServer))
            {
                Debug.LogWarning("[SocketManager] 게임 시작 요청은 서버에서만 가능");
                return;
            }

            if (roomId > 0)
            {
                var manager = RoomManager.singleton as RoomManager;

                JToken gameData = new JObject();
                gameData["action"] = "gameStart";
                gameData["roomId"] = roomId;
                gameData["mapId"] = manager.GetMapId();

                SendRequestToServer(gameData);
            }
            else
            {
                Debug.LogWarning("[SocketManager] 방 ID가 올바르지 않음");
            }
        }

        // ✅ 서버에서 게임 종료 요청
        [Server]
        public void RequestGameEnd(int roomId = 0, int gameId = 0)
        {
            if (!Application.platform.Equals(RuntimePlatform.LinuxServer))
            {
                Debug.LogWarning("[SocketManager] 게임 종료 요청은 서버에서만 가능");
                return;
            }

            var manager = RoomManager.singleton as RoomManager;
            var roomData = manager.GetRoomData();

            roomId = int.TryParse(roomData["roomId"], out var rId) ? rId : roomId;
            gameId = int.TryParse(roomData["gameId"], out var gId) ? gId : gameId;

            JToken gameData = new JObject();
            gameData["action"] = "gameEnd";
            gameData["gameId"] = gameId;

            SendRequestToServer(gameData);
        }

        // ✅ 서버에서 게임 시작 응답 처리
        [Server]
        private void HandleGameStart(JToken data)
        {
            Debug.Log("[SocketManager] 게임 시작됨");

            if (data["status"].ToString() == "success")
            {
                try
                {
                    var manager = RoomManager.singleton as RoomManager;
                    var roomData = manager.GetRoomData();
                    roomData["gameId"] = data["gameId"].ToString();
                    manager.SetRoomData(roomData);

                    List<string> userIds = manager.roomSlots
                        .Select(p => p.GetComponent<RoomPlayer>().UserId)
                        .ToList();

                    FileLogger.LogGameStart(manager.GetMapId(), userIds.Count, userIds);
                }
                catch (Exception ex)
                {
                    Debug.Log($"[SocketManager] 게임 시작 처리 중 예외 발생: {ex.Message}");
                }
            }
            else
            {
                Debug.Log($"[SocketManager] 게임 시작 실패: {data["message"]}");
            }
        }

        // ✅ 서버에서 게임 종료 응답 처리
        [Server]
        private void HandleGameEnd(JToken data)
        {
            Debug.Log("[SocketManager] 게임 종료됨");

            if (data["status"].ToString() == "success")
            {
                RoomManager.singleton.StopClient();
                Debug.Log("[SocketManager] 게임 정상 종료됨");
            }
            else
            {
                Debug.Log($"[SocketManager] 게임 종료 실패: {data["message"]}");
            }
        }

        // ✅ 매치 중 오류 응답 처리
        [Server]
        private void HandleMatchError(JToken data)
        {
            Debug.LogWarning("[SocketManager] 매치 요청 오류");

            switch (data["error"]?.ToString())
            {
                case "matchError":
                    Debug.LogWarning("[SocketManager] 매치 오류: " + data["message"]);
                    break;
                default:
                    Debug.LogWarning("[SocketManager] 알 수 없는 매치 오류: " + data["message"]);
                    break;
            }
        }

        // ✅ 서버에서 방 생성 데이터 수신 처리
        [Server]
        private void HandleSetRoomDataServer(JToken data)
        {
            Debug.Log("[SocketManager] 서버에 방 생성 요청 수신");

            try
            {
                var manager = RoomManager.singleton as RoomManager;
                string roomName = data["roomName"].ToString();

                Dictionary<string, string> roomData = new()
                {
                    {"roomName", roomName.TrimEnd('$')},
                    {"roomType", roomName.EndsWith("$") ? Constants.RoomType.Team.ToString() : Constants.RoomType.Solo.ToString()},
                    {"maxPlayerCount", data["maxPlayers"].ToString()},
                    {"gameId", null},
                    {"roomId", data["roomId"].ToString()}
                };

                manager.SetRoomData(roomData);
                FileLogger.LogCreateRoom();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[SocketManager] 방 데이터 설정 오류: " + ex.Message);
            }
        }
    }
}
