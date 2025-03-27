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
        private int backupCount = 0;

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

            _logThread = new Thread(StartLogRoutine);
            _isRunning = true;
            _logThread.Start();
        }

        [Server]
        private void StartLogRoutine()
        {
            while (_isRunning)
            {
                // 5분에 한 번씩 로그 전송
                StartCoroutine(SendLogsToServer());
                Thread.Sleep(60000);
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

            // 10분 단위로 로그 전송
            var roomManager = RoomManager.singleton as RoomManager;
            // while (DateTime.Now.Minute % 10 != Port % 10)
            // {
            //     yield return new WaitForSeconds(30);
            // }

            // 로그 파일 읽기
            string logPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + Constants.LogFilepath + Constants.LogFilename;
            if (!File.Exists(logPath))
            {
                Debug.LogWarning("[LogManager] 로그 파일이 존재하지 않습니다.");
                yield break;
            }
            string logText = $"[{File.ReadAllText(logPath)}]";
            // 로그 파일 json 객체로 변환
            JObject logJson = new JObject();
            JArray logArray = JArray.Parse(logText);
            logJson["data"] = logArray;
            logJson["timestamp"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            
            string logJsonString = logJson.ToString();
            Debug.Log(logJsonString);

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
                string backupPath = logPath + $".{++backupCount}";
                File.Copy(logPath, backupPath);
                File.WriteAllText(logPath, "");
                Debug.Log("[LogManager] 로그 전송 성공");
            }

            request.Dispose();
        }

        private void OnApplicationQuit()
        {
            // 애플리케이션이 종료될 때 로그 전송 중지
            if (_logThread != null)
            {
                _isRunning = false;
                _logThread.Join();
            }
        }
    }
}