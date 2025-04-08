using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Text.RegularExpressions;
using GameManagement;
using DataSystem;
using Mirror;

namespace Networking
{ 
    public class LogManager : MonoBehaviour
    {
        public string logServerIP = "localhost";
        public ushort logServerPort = 8081;
        public ushort Port = 7777;
        private Thread _logThread;
        private bool _isRunning = false;

        private int _lastTimeLogSend = -998244353; // 마지막 로그 전송 시각
        [SerializeField]private int _logSendInterval = 300; // 로그 전송 간격 (초)
        private Queue<string> _logWriterQueue = new Queue<string>();

        public static LogManager singleton;

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

        private void Start()
        {
            if (!Application.platform.Equals(RuntimePlatform.LinuxServer))
            {
                Debug.LogWarning("[LogManager] 서버 모드에서 실행 중이 아닙니다.");
                #if !UNITY_EDITOR
                Application.logMessageReceived += HandleLog;
                Debug.unityLogger.logEnabled = false;
                #endif
                return;
            }

            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].StartsWith("-logServerIP="))
                {
                    logServerIP = args[i].Substring(13);
                    Debug.Log($"[LogManager] 로그 서버 IP 변경: {logServerIP}");
                }
                else if (args[i].StartsWith("-logServerPort="))
                {
                    if (ushort.TryParse(args[i].Substring(15), out ushort port))
                    {
                        logServerPort = port;
                        Debug.Log($"[LogManager] 로그 서버 포트 변경: {logServerPort}");
                    }
                    else
                    {
                        Debug.LogWarning($"[LogManager] 로그 서버 포트 변경 실패: {args[i].Substring(15)}");
                    }
                }
                else if (args[i].StartsWith("-logFilepath="))
                {
                    Constants.LogFilepath = args[i].Substring(14);
                    Debug.Log($"[LogManager] 로그 파일 경로 변경: {Constants.LogFilepath}");
                }
                else if (args[i].StartsWith("-port="))
                {
                    if (ushort.TryParse(args[i].Substring(6), out ushort port))
                    {
                        Port = port;
                        Debug.Log($"[LogManager] 포트 변경: {Port}");
                        Constants.LogFilename = $"log_{Port}.log";
                    }
                    else
                    {
                        Debug.LogWarning($"[LogManager] 포트 변경 실패: {args[i].Substring(6)}");
                    }
                }
            }
        }

        [Server]            
        public IEnumerator SendLogsToServer()
        {
            if (!Application.platform.Equals(RuntimePlatform.LinuxServer))
            {
                Debug.LogWarning("[LogManager] 서버 모드에서 실행 중이 아닙니다.");
                yield break;
            }

            // Port 번호에 따라 10분 단위로 대기
            // Port 번호의 마지막 자리와 현재 시간의 분을 비교하여 대기
            var roomManager = RoomManager.singleton as RoomManager;
            while ((DateTime.Now.Minute % 5) != (int)(Port % 5))
            {
                // 10분 단위로 대기
                yield return new WaitForSeconds(20);
            }

            // 로그 파일 읽기
            string logPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + Constants.LogFilepath + Constants.LogFilename;
            if (!File.Exists(logPath))
            {
                Debug.LogWarning("[LogManager] 로그 파일이 존재하지 않습니다.");
                yield break;
            }
            string logText = $"[{File.ReadAllText(logPath)}]";
            if (string.IsNullOrEmpty(logText) || logText == "[]")
            {
                Debug.LogWarning("[LogManager] 로그 파일이 비어있습니다.");
                yield break;
            }
            // 로그 파일 json 객체로 변환
            JObject logJson = new JObject();
            JArray logArray = JArray.Parse(logText);
            logJson["data"] = logArray;
            logJson["timestamp"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            
            string logJsonString = logJson.ToString();

            // 서버에 로그 전송
            UnityWebRequest request = new UnityWebRequest($"http://{logServerIP}:{logServerPort}/api/log", "POST");
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(logJsonString);
            request.uploadHandler = (UploadHandler)new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("[LogManager] 로그 전송 실패: " + request.error);
            }
            else
            {
                // 이전 로그 백업
                string backupPath = logPath + $".{DateTime.Now:yyyyMMdd_HHmmss}.bak";
                File.Copy(logPath, backupPath);
                File.WriteAllText(logPath, "");
                Debug.Log("[LogManager] 로그 전송 성공");
            }

            request.Dispose();
        }

        [Server]
        public void EnqueueLog(string log)
        {
            // 로그를 큐에 추가
            if (!Application.platform.Equals(RuntimePlatform.LinuxServer))
            {
                Debug.LogWarning("[LogManager] 서버 모드에서 실행 중이 아닙니다.");
                return;
            }

            _logWriterQueue.Enqueue(log);
        }

        private void Update()
        {
            if (!Application.platform.Equals(RuntimePlatform.LinuxServer))
            {
                // 로그 서버 모드가 아닐 경우 로그 전송을 하지 않음
                return;
            }

            // 로그 큐에 있는 로그를 서버에 전송
            if (_lastTimeLogSend + _logSendInterval < Time.time)
            {
                _lastTimeLogSend = (int)Time.time;
                StartCoroutine(SendLogsToServer());
            }

            // 로그 파일에 기록
            if (_logWriterQueue.Count > 0)
            {
                string log = _logWriterQueue.Dequeue();
                string logDirPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + Constants.LogFilepath;
                if (!Directory.Exists(logDirPath))
                {
                    Directory.CreateDirectory(logDirPath);
                }
                string logPath = logDirPath + Constants.LogFilename;
                // 로그 파일이 존재하지 않으면 생성
                if (!File.Exists(logPath))
                {
                    File.Create(logPath).Close();
                }
                // 로그 파일에 기록
                using (StreamWriter writer = new StreamWriter(logPath, true))
                {
                    writer.WriteLine(log + ",");
                }
            }
        }

        private void HandleLog(string logString, string stackTrace, LogType type)
        {
            // 일반 로그는 무시시
            if (type == LogType.Log)
            {
                return;
            }

            // 커스텀 클라이언트 로그 기록
            WriteCustomClientLog(logString, stackTrace);
        }

        private void WriteCustomClientLog(string logString, string stackTrace)
        {
            // 게임 디렉토리 내에 생성
            string logDirPath = Path.Combine(Application.dataPath, "../Logs");
            string logFilePath = Path.Combine(logDirPath, "client_log.txt");

            if (!Directory.Exists(logDirPath))
            {
                Directory.CreateDirectory(logDirPath);
            }
            if (!File.Exists(logFilePath))
            {
                File.Create(logFilePath).Close();
            }

            using (StreamWriter writer = new StreamWriter(logFilePath, true))
            {
                writer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {logString}");
                if (!string.IsNullOrEmpty(stackTrace))
                {
                    writer.WriteLine(stackTrace);
                }
            }
        }

        private void OnApplicationQuit()
        {
            // 애플리케이션이 종료될 때 로그 전송 중지
            if (_logThread != null)
            {
                _isRunning = false;
                _logThread.Join();
            }

            if (!Application.platform.Equals(RuntimePlatform.LinuxServer))
            {
                Application.logMessageReceived -= HandleLog;
            }
        }
    }
}