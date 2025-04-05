using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Unity.Mathematics;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using System.Linq;
using IO.Swagger.Model;
using kcp2k;
using System.Buffers.Text;
using UI;
using Mirror;

namespace Networking
{
    public partial class SocketManager : MonoBehaviour
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private Thread _clientThread;
        private byte[] _buffer;
        private Dictionary<string, bool> _pendingRequests = new Dictionary<string, bool>(); 
        private Queue<string> _messageQueue = new Queue<string>();
        private bool _restart = false;
        private bool _closeConnection = false;
        private bool _calledFromClient = false;
        private bool _serverTerminated = false;
        private int _lastAlivePingTime = 0;

        public string socketServerIP = "127.0.0.1"; // 서버 IP 주소
        public ushort socketServerPort = 8080; // 서버 포트 번호
        public int bufferSize = 8192; // 버퍼 크기
        public int maxRetries = 5; // 최대 재시도 횟수
        public static SocketManager singleton;

        void Awake()
        {
            if (singleton != null)
            {
                Destroy(gameObject);
                return;
            }
            singleton = this;
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            // 서버에 연결
            // 백그라운드 스레드로 연결
            if (PlayerPrefs.HasKey("sessionToken"))
            {
                PlayerPrefs.DeleteKey("sessionToken");
            }
            if (PlayerPrefs.HasKey("userId"))
            {
                PlayerPrefs.DeleteKey("userId");
            }

            if (UnityEngine.Application.platform.Equals(RuntimePlatform.LinuxServer))
            {
                // 리눅스 서버에서 실행 중인 경우 명령행 인수로 소켓 서버 IP 및 포트 변경
                var args = System.Environment.GetCommandLineArgs();
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i].StartsWith("-socketServerIP="))
                    {
                        socketServerIP = args[i].Substring(16);
                        Debug.Log($"[SocketManager] 소켓 서버 IP 변경: {socketServerIP}");
                    }
                    else if (args[i].StartsWith("-socketServerPort="))
                    {
                        ushort port;
                        if (ushort.TryParse(args[i].Substring(18), out port))
                        {
                            socketServerPort = port;
                            Debug.Log($"[SocketManager] 소켓 서버 포트 변경: {socketServerPort}");
                        }
                        else
                        {
                            Debug.LogWarning($"[SocketManager] 소켓 서버 포트 변경 실패: {args[i].Substring(18)}");
                        }
                    }
                    else if (args[i].StartsWith("-socketBufferSize="))
                    {
                        if (int.TryParse(args[i].Substring(18), out int size))
                        {
                            bufferSize = size;
                            Debug.Log($"[SocketManager] 소켓 버퍼 크기 변경: {bufferSize}");
                        }
                        else
                        {
                            Debug.LogWarning($"[SocketManager] 소켓 버퍼 크기 변경 실패: {args[i].Substring(18)}");
                        }
                    }
                    else if (args[i].StartsWith("-socketMaxRetries="))
                    {
                        if (int.TryParse(args[i].Substring(19), out int retries))
                        {
                            maxRetries = retries;
                            Debug.Log($"[SocketManager] 소켓 최대 재시도 횟수 변경: {maxRetries}");
                        }
                        else
                        {
                            Debug.LogWarning($"[SocketManager] 소켓 최대 재시도 횟수 변경 실패: {args[i].Substring(19)}");
                        }
                    }
                }

                InitSocketConnection();
            }

            // TEST
            // InitSocketConnection();
            // StartCoroutine(TestResister());
        }

        public void InitSocketConnection()
        {
            // 소켓 연결 초기화
            Debug.Log("[SocketManager] 소켓 서버 연결 시도...");

            _clientThread = new Thread(() => ConnectToServer());
            _clientThread.IsBackground = true;
            _clientThread.Start();
        }

        void ConnectToServer()
        {
            // 서버에 연결 시도
            if (UnityEngine.Application.platform.Equals(RuntimePlatform.LinuxServer))
            {
                // 리눅스 서버에서 실행 중인 경우 재시도
                int retries = 0;
                while (_client == null || (!_client.Connected && retries < maxRetries))
                {
                    try
                    {
                        _client = new TcpClient();
                        _client.Connect(socketServerIP, socketServerPort);
                        _stream = _client.GetStream();
                        Debug.Log("[SocketManager] 소켓 서버 연결 성공!");
                        InitialMessage();
                        // 데이터 수신 시작
                        ReceiveData();
                        break;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("[SocketManager] 소켓 서버 연결 오류: " + ex.Message);
                        retries++;
                        Thread.Sleep(1000); // 1초 대기 후 재시도
                    }
                }

                if (retries == maxRetries)
                {
                    Debug.LogError("[SocketManager] 소켓 서버 연결 재시도 횟수 초과");
                    // 서버 연결 실패 시 메인 스레드에서 종료
                    _serverTerminated = true;
                    return;
                }
            }
            else
            {
                // 클라이언트 모드에서 실행 중인 경우 연결 시도        
                try
                {
                    _client = new TcpClient();
                    _client.Connect(socketServerIP, socketServerPort);
                    _stream = _client.GetStream();
                    Debug.Log("[SocketManager] 소켓 서버 연결 성공!");
                    InitialMessage();
                    // 데이터 수신 시작
                    ReceiveData();
                }
                catch (Exception ex)
                {
                    Debug.LogError("[SocketManager] 소켓 서버 연결 오류: " + ex.Message);

                    var modalPopup = ModalPopupUI.singleton;
                    if (modalPopup != null)
                    {
                        modalPopup.EnqueueModalMessage("서버와의 연결에 실패했습니다.\n잠시 후 다시 시도해주세요.");
                    }
                }
            }
        }

        void InitialMessage()
        {
            // 서버에 초기 메시지 전송
            if (UnityEngine.Application.platform.Equals(RuntimePlatform.LinuxServer))
            {
                var roomManager = RoomManager.singleton as RoomManager;
                int port = roomManager.GetComponent<KcpTransport>().Port;
                SendMessageToServer("{\"connectionType\":\"mirror\",\"port\":" + port + "}");
            }
            else
            {
                SendMessageToServer("{\"connectionType\":\"client\",\"version\":\"" + UnityEngine.Application.version + "\"}");
            }
        }

        void ReceiveData()
        {
            // 서버로부터 데이터 수신
            try
            {
                bool isServer = UnityEngine.Application.platform.Equals(RuntimePlatform.LinuxServer);
                while (_client.Connected)
                {
                    _buffer = new byte[bufferSize];

                    int bytesRead = _stream.Read(_buffer, 0, _buffer.Length);
                    if (bytesRead > 0)
                    {
                        string receivedMessage = Encoding.UTF8.GetString(_buffer, 0, bytesRead);
                        // 서버 명령 처리
                        _messageQueue.Enqueue(receivedMessage);
                        Debug.Log("[MatchManager] 매칭 서버로부터 수신: " + receivedMessage);
                    }
                }
            }
            catch(Exception ex)
            {
                Debug.Log("[SocketManager] 소켓 통신 오류: " + ex.Message);
                if (ex.Message.Contains("Unable to read data from the transport connection:"))
                {
                    Debug.Log("[SocketManager] 소켓 연결이 중단되었습니다.");
                    if (ex.Message.Contains("WSACancelBlockingCall"))
                    {
                        _calledFromClient = true;
                    }
                    EnqueueCloseConnection();
                }
                else
                {
                    Debug.Log("[SocketManager] 소켓 통신 오류: " + ex.Message);
                }

                if (UnityEngine.Application.platform.Equals(RuntimePlatform.LinuxServer))
                {
                    // 리눅스 서버에서 실행 중인 경우 재시도
                    EnqueueCloseConnection();
                    _restart = true;
                }
            }
        }

        public void SendMessageToServer(string message)
        {
            // 서버로 메시지 전송
            if (_client != null && _client.Connected && _stream != null)
            {
                // 최대 버퍼 크기만큼 쓰기
                byte[] data = Encoding.UTF8.GetBytes(message);
                _stream.Write(data, 0, math.min(data.Length, bufferSize));
                Debug.Log("[SocketManager] 소켓 서버로 메시지 보냄: " + message);
            }
        }

        public void SendRequestToServer(JToken data)
        {
            // 서버에 요청식으로 메시지 전송
            
            string requestType = data["action"].ToString();
            _pendingRequests[requestType] = true;

            SendMessageToServer(data.ToString());
        }
        
        public void OnClickLogout()
        {
            // 로그아웃 버튼 클릭 시 호출
            var modalPopup = ModalPopupUI.singleton;
            if (modalPopup != null && PlayerPrefs.HasKey("sessionToken"))
            {
                modalPopup.ShowModalMessage("로그아웃 되었습니다.");
            }
            
            _calledFromClient = true;
            EnqueueCloseConnection();
        }

        public void EnqueueCloseConnection()
        {
            _closeConnection = true;
        }

        public void CloseConnection(bool calledFromClient = false)
        {
            // 로컬 저장소 초기화
            if (PlayerPrefs.HasKey("sessionToken"))
            {
                if (_client != null &&_client.Connected)
                {
                    RequestLogout();
                }
                PlayerPrefs.DeleteKey("sessionToken");
            }
            if (PlayerPrefs.HasKey("userId"))
            {
                PlayerPrefs.DeleteKey("userId");
            }
            if (PlayerPrefs.HasKey("nickName"))
            {
                PlayerPrefs.DeleteKey("nickName");
                PlayerPrefs.SetString("nickName", "Player" + UnityEngine.Random.Range(1000, 9999));
            }

            // 연결 해제
            if (_stream != null)
                _stream.Close();

            if (_client != null)
                _client.Close();

            StopAllCoroutines();

            if (!UnityEngine.Application.platform.Equals(RuntimePlatform.LinuxServer) && !calledFromClient)
            {
                // 클라이언트 모드에서 실행 중인 경우 씬 전환
                var modalPopup = ModalPopupUI.singleton;
                if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "MainMenu")
                {
                    UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
                }

                OnlineUI onlineUI = FindFirstObjectByType<OnlineUI>();
                if (onlineUI != null)
                {
                    onlineUI.switchToMainMenuUI();
                }
                
                if (modalPopup != null)
                {
                    modalPopup.ShowModalMessage("서버와의 연결이 끊어졌습니다.");
                }
            }

            Debug.Log("[SocketManager] 소켓 서버와 연결 해제.");
        }
        
        private void ClientResponseHandler(string message)
        {
            if (message.StartsWith("\"") && message.EndsWith("\""))
            {
                message = message.Substring(1, message.Length - 2);
                message = JToken.Parse(message).ToString();
            }
            JToken data = JToken.Parse(message);

            if (data.SelectToken("status") != null && data.SelectToken("status").ToString() == "error")
            {
                // 서버에서 에러 응답 처리
                Debug.LogWarning("[SocketManager] 서버 에러 응답: " + message);
                var modalPopup = ModalPopupUI.singleton as ModalPopupUI;
                if (modalPopup != null)
                {
                    string modalMessage = "서버에서 에러가 발생했습니다.\n잠시 후 다시 시도해주세요.";
                    if (data.SelectToken("message") != null)
                    {
                        modalMessage = data.SelectToken("message").ToString();
                    }
                    // string modalMessage = "서버에서 에러가 발생했습니다.\n잠시 후 다시 시도해주세요.";
                    // if (message.Contains("Invalid password") || message.Contains("Invalid username"))
                    // {
                    //     modalMessage = "아이디 또는 비밀번호가\n잘못되었습니다.\n다시 시도해주세요.";
                    // }
                    // else if (message.Contains("Invalid session token"))
                    // {
                    //     modalMessage = "세션이 만료되었습니다.\n다시 로그인해주세요.";
                    // }
                    
                    modalPopup.EnqueueModalMessage(modalMessage);
                }
                return;
            }

            if (data.SelectToken("action") != null)
            {
                // 요청식 응답 처리
                if (!_pendingRequests.ContainsKey(data.SelectToken("action").ToString()))
                {
                    Debug.LogWarning("[SocketManager] 요청되지 않은 클라이언트 액션: " + message);
                    return;
                }

                _pendingRequests.Remove(data["action"].ToString());
                Debug.Log("[SocketManager] 요청식 응답 처리: " + data.ToString());
                // 응답 처리
                
                switch (data.SelectToken("action").ToString())
                {

                    case "login":
                        HandleAuth(data);
                        break;

                    case "register":
                        HandleRegister(data);
                        break;

                    case "refreshSession":
                        HandleRefreshSession(data);
                        break;

                    case "listRooms":
                        HandleListRooms(data);
                        break;

                    case "createRoom":
                        HandleCreateRoom(data);
                        break;

                    case "joinRoom":
                        HandleJoinRoom(data);
                        break;

                    case "exitRoom":
                        HandleExitRoom(data);
                        break;

                    case "updateNickName":
                        HandleUpdateNickname(data);
                        break;

                    default:
                        Debug.LogWarning("[SocketManager] 알 수 없는 클라이언트 액션: " + message);
                        break;
                }
                // 방 목록 업데이트 알림 이벤트
//                if (data.SelectToken("eventName") != null)
//                {
//                    string eventType = data["eventName"].ToString();
//                    switch (eventType)
//                    {
//                        case "roomListUpdated":
//                            Debug.Log("[SocketManager] 실시간 방 목록 업데이트 수신");
//                            _hasPendingRoomUpdate = true;
//                            FindRoomUI findRoomUI = FindFirstObjectByType<FindRoomUI>();
//                            findRoomUI?.ShowRefreshButton(true);
//                            break;
//                    }
//                    return;
//                }
            }
        
        }

        private void ServerResponseHandler(string message)
        {
            if (!UnityEngine.Application.platform.Equals(RuntimePlatform.LinuxServer))
            {
                Debug.LogWarning("[SocketManager] 서버 응답 처리는 서버에서만 가능");
                return;
            }

            
            if (message.StartsWith("\"") && message.EndsWith("\""))
            {
                message = message.Substring(1, message.Length - 2);
                message = JToken.Parse(message).ToString();
            }
            JToken data = JToken.Parse(message);
            
            if (data.SelectToken("action") != null)
            {
                // 중앙 서버 역할 시의 서버 메시지 처리
                switch (data.SelectToken("action").ToString())
                {
                    case "setRoom":
                        HandleSetRoomDataServer(data);
                        break;
                    
                    case "gameStart":
                        HandleGameStart(data);
                        break;

                    case "gameEnd":
                        HandleGameEnd(data);
                        break;

                    default:
                        Debug.LogWarning("[SocketManager] 알 수 없는 서버 액션: " + message);
                        break;
                }
            }
        }

        // TEST
        public IEnumerator TestResister()
        {   
            yield return new WaitForSeconds(1);
            RequestRegister("gogogo", "testpassword");
            yield return new WaitForSeconds(1);
            RequestRegister("burnyouwithlight", "a509test");
            yield return new WaitForSeconds(1);
            RequestRegister("emperornecro", "a509test");
            yield return new WaitForSeconds(1);
            RequestRegister("goinmul", "a509test");
            yield return new WaitForSeconds(1);
            RequestRegister("powerwarrior", "a509test");
            yield return new WaitForSeconds(1);
            RequestRegister("zizonmage", "a509test");
            yield return new WaitForSeconds(1);
            RequestRegister("geniusarcher", "a509test");
        }

        private void Update()
        {
            if (UnityEngine.Application.platform.Equals(RuntimePlatform.LinuxServer))
            {
                if (_serverTerminated)
                {
                    // 서버 연결이 종료된 경우
                    Debug.Log("[SocketManager] 서버 연결 종료됨. 스레드 종료.");
                    _clientThread.Abort();
                    UnityEngine.Application.Quit();
                    return;
                }
                if (_restart)
                {
                    // 리눅스 서버에서 실행 중인 경우 재시도
                    _restart = false;
                    InitSocketConnection();
                }
            }
            
            // 소켓 연결 상태 확인 및 메시지 처리
            if (_closeConnection)
            {
                // 연결 해제 요청 시 연결 해제
                CloseConnection(_calledFromClient);
                _closeConnection = false;
                _calledFromClient = false;
            }            

            if (_messageQueue.Count > 0)
            {
                string message = _messageQueue.Dequeue();
                if (UnityEngine.Application.platform.Equals(RuntimePlatform.LinuxServer))
                {
                    ServerResponseHandler(message);
                }
                else
                {
                    ClientResponseHandler(message);
                }
            }

            if (_lastAlivePingTime > 0 && _lastAlivePingTime + 10 < Time.time)
            {
                // 10초마다 서버에 AlivePing 요청
                _lastAlivePingTime = (int)Time.time;
                AlivePing();
            }
        }

        
        private void OnApplicationQuit()
        {   
            // 애플리케이션 종료 시 연결 해제
            if (UnityEngine.Application.platform.Equals(RuntimePlatform.LinuxServer))
            {
                var manager = RoomManager.singleton as RoomManager;
                var roomData = manager.GetRoomData();
                if (roomData["roomId"] != null && roomData["gameId"] != null)
                {
                    RequestGameEnd();
                }
            }
            else if (_client.Connected)
            {
                RequestExitRoom();
            }
            // 연결 해제 후 스레드 종료
            CloseConnection();
            _clientThread.Join();
        }

        public bool IsConnected()
        {
            return _client != null && _client.Connected;
        }
    }
}