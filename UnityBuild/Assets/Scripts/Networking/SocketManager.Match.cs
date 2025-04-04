using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using DataSystem;
using GameManagement;
using IO.Swagger.Model;
using kcp2k;
using Mirror;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Player;
using Unity.Burst.CompilerServices;
using UnityEngine;

namespace Networking
{
    public partial class SocketManager : MonoBehaviour
    {
        private JToken _latestRoomData;
        private bool _hasPendingRoomUpdate = false;

        public struct RoomListChangedMessage : NetworkMessage
        {
            public string eventName;
        }

        public bool HasPendingRoomUpdate => _hasPendingRoomUpdate;

        public void ClearRoomUpdateFlag()
        {
            _hasPendingRoomUpdate = false;
        }

        public void RequestCreateRoom(string roomName, int maxPlayers)
        {
            // 방 생성 요청
            JToken roomData = new JObject();
            roomData["action"] = "createRoom";
            roomData["roomName"] = roomName;
            roomData["maxPlayers"] = maxPlayers;
            SendRequestToServer(roomData);
        }

        public void RequestListRooms()
        {
            // 방방 목록 요청
            JToken roomData = new JObject();
            roomData["action"] = "listRooms";
            SendRequestToServer(roomData);
        }

        public void RequestJoinRoom(int roomId, ushort port)
        {
            // 방 참가 요청
            try{
                var manager = RoomManager.singleton as RoomManager;
                KcpTransport transport = manager.GetComponent<KcpTransport>();
                transport.Port = port;

                JToken roomData = new JObject();
                roomData["action"] = "joinRoom";
                roomData["roomId"] = roomId;

                SendRequestToServer(roomData);
                Debug.Log($"[SocketManager] 방 참가: {roomId}");
            }
            catch (Exception ex){
                RequestExitRoom();
                var modal = ModalPopupUI.singleton as ModalPopupUI;
                if (modal != null)
                {
                    modal.ShowModalMessage("방 참가 실패\n서버와의 연결에 실패했습니다.");
                }
                Debug.Log($"[SocketManager] 방 참가 실패: {ex.Message}");
            }
        }

        public void RequestExitRoom()
        {
            // 방 탈퇴 요청
            JToken roomData = new JObject();
            roomData["action"] = "exitRoom";
            SendRequestToServer(roomData);
        }

        private void HandleCreateRoom(JToken data)
        {
            Debug.Log("[SocketManager] 방 생성됨");
            // 매치 생성 시 처리
            
            // 생성된 매치에 참가
            // 매치 참가 시 처리
            if (data.SelectToken("status").ToString() == "success")
            {   
                try{
                    var manager = RoomManager.singleton as RoomManager;
                    KcpTransport transport = manager.GetComponent<KcpTransport>();
                    transport.Port = data.SelectToken("port").ToObject<ushort>();

                    manager.StartClient();
                    Debug.Log($"[SocketManager] 방 생성 및 참가 완료: {data["roomId"].ToObject<int>()}");
//                    BroadcastRoomListChanged();
                }
                catch (Exception ex){
                    RequestExitRoom();
                    var modal = ModalPopupUI.singleton as ModalPopupUI;
                    if (modal != null)
                    {
                        modal.ShowModalMessage("방 생성 및 참가 실패\n서버와의 연결에 실패했습니다.");
                    }
                    Debug.Log($"[SocketManager] 방 생성 및 참가 실패: {ex.Message}");
                }
            }
            else
            {
                Debug.Log($"[SocketManager] 방 생성 및 참가 실패: {data["message"].ToString()}");
            }
        }

        private void HandleListRooms(JToken data)
        {
            Debug.Log("[SocketManager] 방 목록 받음");
            // 매치 목록 수신 시 처리
            FindRoomUI findRoomUI = FindFirstObjectByType<FindRoomUI>();
            findRoomUI.UpdateContainer(data);
            
            // 매치 목록 출력
            foreach (var room in data.SelectToken("rooms").ToArray())
            {
                Debug.Log(room);
            }
        }
        
        private void HandleJoinRoom(JToken data)
        {   
            // 매치 참가 시 처리
            if (data.SelectToken("status").ToString() == "success")
            {   
                var manager = RoomManager.singleton as RoomManager;
                manager.StartClient();
                Debug.Log($"[SocketManager] 방 참가 완료");
            }
            else
            {
                RequestExitRoom();
                var modal = ModalPopupUI.singleton as ModalPopupUI;
                if (modal != null)
                {
                    modal.ShowModalMessage("방 참가 실패\n서버와의 연결에 실패했습니다.");
                }
                Debug.Log($"[SocketManager] 방 참가 실패: {data.SelectToken("message").ToString()}");
            }
        }

        private void HandleExitRoom(JToken data)
        {
            if (data.SelectToken("status").ToString() == "success")
            {  
                var manager = RoomManager.singleton as RoomManager;
                manager.StopClient();
                Debug.Log("[SocketManager] 방 퇴장 성공");
            }
            else
            {
                var modal = ModalPopupUI.singleton as ModalPopupUI;
                if (modal != null)
                {
                    modal.ShowModalMessage("방 퇴장 실패\n서버와의 연결에 실패했습니다.");
                }
                Debug.Log($"[SocketManager] 방 퇴장 실패: {data.SelectToken("message").ToString()}");
            }
        }

        // SERVER ONLY
        [Server]
        public void RequestGameStart(int roomId = 0)
        {
            if (!UnityEngine.Application.platform.Equals(RuntimePlatform.LinuxServer))
            {
                Debug.LogWarning("[SocketManager] 게임 시작 요청은 서버에서만 가능");
                return;
            }
            // 매치 시작 요청
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

        [Server]
        public void RequestGameEnd(int roomId = 0, int gameId = 0)
        {
            if (!UnityEngine.Application.platform.Equals(RuntimePlatform.LinuxServer))
            {
                Debug.LogWarning("[SocketManager] 게임 종료 요청은 서버에서만 가능");
                return;
            }
            // 매치 종료 요청
            var manager = RoomManager.singleton as RoomManager;
            var roomData = manager.GetRoomData();

            roomId = roomData["roomId"] == null ? roomId : (int.TryParse(roomData["roomId"], out int roomIdInt) ? roomIdInt : 0);
            gameId = roomData["gameId"] == null ? gameId : (int.TryParse(roomData["gameId"], out int gameIdInt) ? gameIdInt : 0);
            
            JToken gameData = new JObject();
            gameData["action"] = "gameEnd";
            // gameData["roomId"] = roomId;
            gameData["gameId"] = gameId;

            
            SendRequestToServer(gameData);
        }

        [Server]
        private void HandleGameStart(JToken data)
        {
            Debug.Log("[SocketManager] 게임 시작됨");
            // 게임 시작 시 처리
            if (data.SelectToken("status").ToString() == "success")
            {
                try{
//                    BroadcastRoomListChanged();
                    var manager = RoomManager.singleton as RoomManager;
                    Dictionary<string, string> gameData = manager.GetRoomData();
                    gameData["gameId"] = data["gameId"].ToObject<int>().ToString();
                    manager.SetRoomData(gameData);

                    List<string> userIds = new List<string>();
                    foreach (var player in manager.roomSlots)
                    {
                        RoomPlayer roomPlayer = player.gameObject.GetComponent<RoomPlayer>();
                        userIds.Add(roomPlayer.UserId);
                    }

                    FileLogger.LogGameStart(manager.GetMapId(), userIds.Count(), userIds);
                }
                catch (Exception ex){
                    Debug.Log($"[SocketManager] 게임 시작 실패: {ex.Message}");
                }
            }
            else
            {
                Debug.Log($"[SocketManager] 게임 시작 실패: {data["message"].ToString()}");
            }
        }

        [Server]
        private void HandleGameEnd(JToken data)
        {
            Debug.Log("[SocketManager] 게임 종료됨");
            // 매치 종료 시 처리
            if (data.SelectToken("status").ToString() == "success")
            {
                try
                {
                    var manager = RoomManager.singleton as RoomManager;
                    manager.StopClient();
                    Debug.Log("[SocketManager] 게임 종료");
                }
                catch (Exception ex)
                {
                    Debug.Log($"[SocketManager] 게임 종료 실패: {ex.Message}");
                }
            }
            else
            {
                Debug.Log($"[SocketManager] 게임 종료 실패: {data["message"].ToString()}");
            }
        }

        [Server]
        private void HandleMatchError(JToken data)
        {
            Debug.LogWarning("[SocketManager] 매치 요청 오류");
            // 매치 오류 시 처리

            switch (data.SelectToken("error").ToString())
            {
                case "matchError":
                    Debug.LogWarning("[SocketManager] 매치 오류: " + data.SelectToken("message").ToString());
                    break;
                default:
                    Debug.LogWarning("[SocketManager] 알 수 없는 매치 오류: " + data.SelectToken("message").ToString());
                    break;
            }
        }

        [Server]
        private void HandleSetRoomDataServer(JToken data)
        {
            // 방 생성 시 처리
            Debug.Log("[SocketManager] 서버에 방 생섬됨");

            // 방 생성 처리
            var manager = RoomManager.singleton as RoomManager;
            
            try
            {
                Dictionary<string, string> roomData = new Dictionary<string, string>
                {
                    {"roomName", data.SelectToken("roomName").ToString()},
                    {"roomType", Constants.RoomType.Solo.ToString()},
                    {"maxPlayerCount", data.SelectToken("maxPlayers").ToString()},
                    {"gameId", null},
                    {"roomId", data.SelectToken("roomId").ToString()}
                };
                manager.SetRoomData(roomData);
                FileLogger.LogCreateRoom();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[SocketManager] 방 데이터 설정 오류: " + ex.Message);
            }
        }

        // 방 리스트 업데이트 broadcast
//        [Server]
//        private void BroadcastRoomListChanged()
//        {
//            string message = JsonConvert.SerializeObject(new {
//                eventName = "roomListUpdated"
//            });
//
//            foreach (var client in connectedClients) // 이 부분은 TCP 기반 클라이언트 목록이어야 함
//            {
//                try
//                {
//                    NetworkStream stream = client.GetStream();
//                    byte[] data = Encoding.UTF8.GetBytes(message);
//                    stream.Write(data, 0, data.Length);
//                }
//                catch (Exception ex)
//                {
//                    Debug.LogError($"[SocketManager] 클라이언트 전송 실패: {ex.Message}");
//                }
//            }
//        }
    }
}