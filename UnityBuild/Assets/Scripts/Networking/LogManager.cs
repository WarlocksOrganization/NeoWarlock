using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
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
        public string logServerIP = "localhost";          // 로그 서버 IP
        public ushort logServerPort = 8081;               // 로그 서버 포트
        public ushort Port = 7777;                        // 게임 서버 포트
        private Thread _logThread;                        // (예비) 로그 쓰레드
        private bool _isRunning = false;

        private int _lastTimeLogSend = -998244353;        // 마지막 로그 전송 시각
        [SerializeField] private int _logSendInterval = 300; // 로그 전송 주기 (초 단위)
        private Queue<string> _logWriterQueue = new();    // 로그 저장 큐

        public static LogManager singleton;

        void Awake()
        {
            // 싱글톤 설정
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
            // 서버 환경에서만 동작
            if (!Application.platform.Equals(RuntimePlatform.LinuxServer))
            {
                Debug.LogWarning("[LogManager] 서버 모드에서 실행 중이 아닙니다.");
                return;
            }

            // 명령행 인자에 따라 환경 설정 변경
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
                        Constants.LogFilename = $"log_{Port}.log";
                        Debug.Log($"[LogManager] 포트 변경: {Port}");
                    }
                }
            }
        }

        [Server]
        public IEnumerator SendLogsToServer()
        {
            // 테스트 모드 혹은 클라이언트 환경에서는 비활성
            if (Constants.IsTestMode || !Application.platform.Equals(RuntimePlatform.LinuxServer))
                yield break;

            // 포트 기준으로 전송 주기 조정
            while ((DateTime.Now.Minute % 5) != (int)(Port % 5))
                yield return new WaitForSeconds(20);

            // 로그 파일 경로 설정
            string logPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + Constants.LogFilepath + Constants.LogFilename;
            if (!File.Exists(logPath))
                yield break;

            string logText = $"[{File.ReadAllText(logPath)}]";
            if (string.IsNullOrEmpty(logText) || logText == "[]")
                yield break;

            // 로그 JSON 포맷 구성
            JObject logJson = new JObject
            {
                ["data"] = JArray.Parse(logText),
                ["timestamp"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            };

            // 서버로 전송
            UnityWebRequest request = new UnityWebRequest($"http://{logServerIP}:{logServerPort}/api/log", "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(logJson.ToString());
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            // 결과 처리
            if (request.result == UnityWebRequest.Result.Success)
            {
                string backupPath = logPath + $".{DateTime.Now:yyyyMMdd_HHmmss}.bak";
                File.Copy(logPath, backupPath);
                File.WriteAllText(logPath, ""); // 원본 초기화
                Debug.Log("[LogManager] 로그 전송 성공");
            }
            else
            {
                Debug.LogWarning("[LogManager] 로그 전송 실패: " + request.error);
            }

            request.Dispose();
        }

        [Server]
        public void EnqueueLog(string log)
        {
            // 서버 환경에서 로그 큐에 추가
            if (!Application.platform.Equals(RuntimePlatform.LinuxServer)) return;
            _logWriterQueue.Enqueue(log);
        }

        private void Update()
        {
            // 클라이언트 환경에서는 미작동
            if (!Application.platform.Equals(RuntimePlatform.LinuxServer)) return;

            // 주기적 로그 전송
            if (_lastTimeLogSend + _logSendInterval < Time.time)
            {
                _lastTimeLogSend = (int)Time.time;
                StartCoroutine(SendLogsToServer());
            }

            // 로그 파일 저장 처리
            if (_logWriterQueue.Count > 0)
            {
                string log = _logWriterQueue.Dequeue();
                string logDirPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + Constants.LogFilepath;
                if (!Directory.Exists(logDirPath))
                    Directory.CreateDirectory(logDirPath);

                string logPath = logDirPath + Constants.LogFilename;
                if (!File.Exists(logPath))
                    File.Create(logPath).Close();

                using StreamWriter writer = new StreamWriter(logPath, true);
                writer.WriteLine(log + ",");
            }
        }

        private void HandleLog(string logString, string stackTrace, LogType type)
        {
            // 경고 및 에러만 별도 저장
            if (type == LogType.Log) return;
            WriteCustomClientLog(logString, stackTrace);
        }

        private void WriteCustomClientLog(string logString, string stackTrace)
        {
            string logDirPath = Path.Combine(Application.dataPath, "../Logs");
            string logFilePath = Path.Combine(logDirPath, "client_log.txt");

            if (!Directory.Exists(logDirPath))
                Directory.CreateDirectory(logDirPath);
            if (!File.Exists(logFilePath))
                File.Create(logFilePath).Close();

            using StreamWriter writer = new StreamWriter(logFilePath, true);
            writer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {logString}");
            if (!string.IsNullOrEmpty(stackTrace))
                writer.WriteLine(stackTrace);
        }

        private void OnApplicationQuit()
        {
            // 종료 시 스레드 정리 및 핸들러 제거
            if (_logThread != null)
            {
                _isRunning = false;
                _logThread.Join();
            }

            if (!Application.platform.Equals(RuntimePlatform.LinuxServer))
                Application.logMessageReceived -= HandleLog;
        }
    }
}
