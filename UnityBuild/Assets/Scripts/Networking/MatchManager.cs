using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Unity.Mathematics;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using Unity.Android.Gradle;

namespace Networking
{
    public class MatchManager : MonoBehaviour
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private Thread _clientThread;
        private bool _isConnected = false;

        public string matchServerIP = "127.0.0.1"; // 서버 IP 주소
        public int matchServerPort = 8080; // 서버 포트 번호
        public int bufferSize = 8192; // 버퍼 크기
        public int maxRetries = 5; // 최대 재시도 횟수
        public bool isDemoMode = false; // 데모 모드

        void Start()
        {
            // (Debug) 데모 모드라면 연결 시도하지 않음
            if (isDemoMode)
            {
                Debug.LogWarning("[MatchManager] 데모 모드로 실행 중입니다.");
                return;
            }

            // 서버에 연결
            // 백그라운드 스레드로 연결
            // 온라인 씬에서도 유지되도록 설정
            DontDestroyOnLoad(gameObject);

            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].StartsWith("-matchServerIP="))
                {
                    matchServerIP = args[i].Substring(16);
                    Debug.Log($"[MatchManager] 소켓 서버 IP 변경: {matchServerIP}");
                }
                else if (args[i].StartsWith("-matchServerPort="))
                {
                    if (int.TryParse(args[i].Substring(18), out int port))
                    {
                        matchServerPort = port;
                        Debug.Log($"[MatchManager] 소켓 서버 포트 변경: {matchServerPort}");
                    }
                    else
                    {
                        Debug.LogWarning($"[MatchManager] 소켓 서버 포트 변경 실패: {args[i].Substring(18)}");
                    }
                }
            }

            Debug.Log("[MatchManager] 소켓 서버 연결 시도...");

            _clientThread = new Thread(() => ConnectToServer(Application.platform.Equals(RuntimePlatform.LinuxServer)));
            _clientThread.IsBackground = true;
            _clientThread.Start();
        }

        void ConnectToServer(bool isServer)
        {
            // 서버에 연결 시도
            int retries = 0;
            // 최대 재시도 횟수만큼 반복
            while (!_isConnected && retries < maxRetries)
            {
                try
                {
                    _client = new TcpClient();
                    _client.Connect(matchServerIP, matchServerPort);
                    _stream = _client.GetStream();
                    _isConnected = true;
                    Debug.Log("[MatchManager] 매칭 서버 연결 성공!");
                    // 연결 성공 시 샘플 회원가입 요청
                    SampleRegister();
                    // 데이터 수신 시작
                    ReceiveData();
                    break;
                }
                catch (Exception ex)
                {
                    Debug.LogError("[MatchManager] 매칭 서버 소켓 오류: " + ex.Message);
                    retries++;
                    Thread.Sleep(1000);
                }
            }
        }

        void ReceiveData()
        {
            // 메시지 수신
            byte[] buffer = new byte[bufferSize];

            // 서버로부터 데이터 수신
            try
            {
                while (_isConnected)
                {
                    int bytesRead = _stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string receivedMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        // 서버 명령 처리
                        ServerResponseHandler(receivedMessage);
                        Debug.Log("[MatchManager] 매칭 서버로부터 수신: " + receivedMessage);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[MatchManager] 매칭 서버 수신 오류: " + ex.Message);
            }
        }

        public void SendMessageToServer(string message)
        {
            // 서버로 메시지 전송
            if (_isConnected && _stream != null)
            {
                // 최대 버퍼 크기만큼 쓰기

                byte[] data = Encoding.UTF8.GetBytes(message);
                _stream.Write(data, 0, math.min(data.Length, bufferSize));
                Debug.Log("[MatchManager] 매칭 서버로 메시지 보냄: " + message);
            }
        }

        void OnApplicationQuit()
        {
            // 애플리케이션이 종료될 때 연결 해제
            CloseConnection();
            // 클라이언트 소켓이 닫힐때까지 대기
            _clientThread.Join();
        }

        void CloseConnection()
        {
            // 연결 해제
            _isConnected = false;

            if (_stream != null)
                _stream.Close();

            if (_client != null)
                _client.Close();

            Debug.Log("[MatchManager] 매칭 서버와 소켓 연결 해제.");
        }

        private void ServerResponseHandler(string message)
        {
            // 서버 응답 처리

            // 응답 처리 시도
            try
            {
                JObject parsedJSON = JObject.Parse(message);


            }
            catch (Exception ex)
            {
                Debug.Log("[MatchManager] 매칭 서버 응답 처리 오류: " + ex.Message);
            }
        }

        private void SampleRegister()
        {
            // 샘플 회원가입 요청
            JObject registerData = new JObject();
            registerData["action"] = "register";
            registerData["user_name"] = "testuser";
            registerData["password"] = "testpassword";
            SendMessageToServer(registerData.ToString());
        }
    }
}