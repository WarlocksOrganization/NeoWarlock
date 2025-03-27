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

        public string matchServerIP = "127.0.0.1"; // 서버 IP 주소
        public int socketServerPort = 8080; // 서버 포트 번호
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
                        matchServerIP = args[i].Substring(16);
                        Debug.Log($"[SocketManager] 소켓 서버 IP 변경: {matchServerIP}");
                    }
                    else if (args[i].StartsWith("-socketServerPort="))
                    {
                        if (int.TryParse(args[i].Substring(18), out int port))
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
            int retries = 0;
            // 최대 재시도 횟수만큼 반복
            while (_client == null || (!_client.Connected && retries < maxRetries))
            {
                try
                {
                    _client = new TcpClient();
                    _client.Connect(matchServerIP, socketServerPort);
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
                    Thread.Sleep(1000);
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
                SendMessageToServer("{\"connectionType\":\"client\"}");
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
                // 소켓 오류 시 연결 해제
                if (ex.Message.Contains("SocketException"))
                {
                    CloseConnection();
                    if (UnityEngine.Application.platform.Equals(RuntimePlatform.LinuxServer))
                    {
                        // 리눅스 서버에서 실행 중인 경우 재시도
                        _restart = true;
                    }
                    else
                    {
                        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
                    }
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

        public void CloseConnection()
        {
            // 연결 해제
            if (_stream != null)
                _stream.Close();

            if (_client != null)
                _client.Close();

            StopAllCoroutines();
            
            // 로컬 저장소 초기화
            if (PlayerPrefs.HasKey("sessionToken"))
            {
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

            Debug.Log("[SocketManager] 소켓 서버와 연결 해제.");
        }
        
        private void ClientResponseHandler(string message)
        {
            try
            {
                if (message.StartsWith("\"") && message.EndsWith("\""))
                {
                    message = message.Substring(1, message.Length - 2);
                    message = message.Replace("\\\"", "\"");
                }
                JToken data = JToken.Parse(message);

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
                }
            }
            catch (Exception ex)
            {
                var modal = ModalPopupUI.singleton as ModalPopupUI;
                if (modal != null)
                {
                    modal.ShowModalMessage("서버 응답 처리 중 오류가 발생했습니다.");
                }
                Debug.LogError("[SocketManager] 클라이언트 소켓 응답 처리 오류: " + ex.Message);
            }
        }

        private void ServerResponseHandler(string message)
        {
            if (!UnityEngine.Application.platform.Equals(RuntimePlatform.LinuxServer))
            {
                Debug.LogWarning("[SocketManager] 서버 응답 처리는 서버에서만 가능");
                return;
            }

            // 응답 처리 시도
            try
            {
                if (message.StartsWith("\"") && message.EndsWith("\""))
                {
                    message = message.Substring(1, message.Length - 2);
                    message = message.Replace("\\\"", "\"");
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
            catch (Exception ex)
            {
                Debug.LogError("[SocketManager] 서버 소켓 응답 처리 오류: " + ex.Message);
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
            if (_restart)
            {
                // 리눅스 서버에서 실행 중인 경우 재시도
                _restart = false;
                InitSocketConnection();
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