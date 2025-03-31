using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using DataSystem;
using Mirror;
using Networking;

namespace GameManagement
{
    public class LogEvent
    {
        public Dictionary<string, object> data;

        public LogEvent(Constants.DataServerLogType eventType, Dictionary<string, object> args = null)
        {
            this.data = args ?? new Dictionary<string, object>();
            this.data["eventType"] = eventType.ToString();
            this.data["createdAt"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this.data, Formatting.Indented);
        }
    }

    public static class FileLogger
    {
        public static void Log(Constants.DataServerLogType eventType, Dictionary<string, object> args = null)
        {
            if (!Application.platform.Equals(RuntimePlatform.LinuxServer))
            {
                Debug.LogWarning("[FileLogger] 리눅스 서버 모드에서만 사용 가능합니다.");
                return;
            }

            var roomManager = RoomManager.singleton as RoomManager;
            if (roomManager == null)
            {
                Debug.LogWarning("[FileLogger] RoomManager가 존재하지 않습니다.");
                return;
            }

            Dictionary<string, string> roomData = roomManager.GetRoomData();
            args["roomId"] = roomData["roomId"] == null ? "0" : roomData["roomId"];
            args["gameId"] = roomData["gameId"] == null ? "0" : roomData["gameId"];
            args["patchVersion"] = "0.51"; // Add patch version to all logs
            LogEvent logEvent = new LogEvent(eventType, args);
            string json = logEvent.ToJson();

            // 로그 파일에 기록
            try
            {
                string logDirPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + Constants.LogFilepath;
                string logPath = logDirPath + Constants.LogFilename;
                // 로그 파일이 시스템 경로에 없으면 생성
                if (!Directory.Exists(logDirPath))
                {
                    Directory.CreateDirectory(logDirPath);
                }
                if (!File.Exists(logPath))
                {
                    File.Create(logPath).Close();
                }

                File.AppendAllText(logPath, json + "," + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Debug.LogError("[FileLogger] 로그 파일 기록 오류: " + ex.Message);
            }
        }

        public static void LogCreateRoom()
        {
            Dictionary<string, object> args = new Dictionary<string, object>();
            args["gameId"] = "0"; // Default gameId for room creation
            args["userId"] = "0"; // Server as the creator
            Log(Constants.DataServerLogType.createRoom, args);
        }

        public static void LogJoinRoom(string userId)
        {
            Dictionary<string, object> args = new Dictionary<string, object>();
            args["userId"] = userId;
            Log(Constants.DataServerLogType.joinRoom, args);
        }

        public static void LogExitRoom(string userId)
        {
            Dictionary<string, object> args = new Dictionary<string, object>();
            args["userId"] = userId;
            Log(Constants.DataServerLogType.exitRoom, args);
        }

        public static void LogPlayerReady(string userId)
        {
            Dictionary<string, object> args = new Dictionary<string, object>();
            args["userId"] = userId;
            Log(Constants.DataServerLogType.playerReady, args);
        }

        public static void LogGameStart(string mapId, int playerCount, List<string> userIds)
        {
            Dictionary<string, object> args = new Dictionary<string, object>();
            args["mapId"] = mapId;
            args["playerCount"] = playerCount;
            args["userId"] = userIds;
            Log(Constants.DataServerLogType.gameStart, args);
        }

        public static void LogSkillHit(string userId, string target, int damage, string skillId)
        {
            Dictionary<string, object> args = new Dictionary<string, object>();
            args["userId"] = userId;
            args["target"] = target;
            args["damage"] = damage;
            args["skillId"] = skillId;
            Log(Constants.DataServerLogType.skillHit, args);
        }

        public static void LogKill(string userId, string target, string killType, string skillId)
        {
            Dictionary<string, object> args = new Dictionary<string, object>();
            args["userId"] = userId;
            args["target"] = target;
            args["killType"] = killType;
            args["skillId"] = skillId;
            Log(Constants.DataServerLogType.kill, args);
        }

        public static void LogGameEnd(string mapId, int playerCount, List<Dictionary<string, object>> playerLogs)
        {
            Dictionary<string, object> args = new Dictionary<string, object>();
            args["mapId"] = mapId;
            args["playerCount"] = playerCount;
            args["playerLogs"] = playerLogs;
            Log(Constants.DataServerLogType.gameEnd, args);
        }
    }
}