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
            args["roomId"] = int.TryParse(roomData["roomId"].ToString(), out int roomId) ? roomId : 0;
            args["gameId"] =  int.TryParse(roomData["gameId"].ToString(), out int gameId) ? gameId : 0;
            args["patchVersion"] = Application.version;
            LogEvent logEvent = new LogEvent(eventType, args);
            string json = logEvent.ToJson();

            var LogManager = Networking.LogManager.singleton;
            if (LogManager == null)
            {
                Debug.LogWarning("[FileLogger] LogManager가 존재하지 않습니다.");
                return;
            }
            LogManager.EnqueueLog(json);
        }

        // 임시로 CreateRoom, GameStart, GameEnd 로그를 남김
        // 추후에 필요에 따라 추가적인 로그를 남길 수 있음
        public static void LogCreateRoom()
        {
            Dictionary<string, object> args = new Dictionary<string, object>();
            args["gameId"] = 0; // Default gameId for room creation
            args["userId"] = 0; // Server as the creator
            Log(Constants.DataServerLogType.createRoom, args);
        }

        public static void LogJoinRoom(string userId)
        {
            Dictionary<string, object> args = new Dictionary<string, object>();
            args["userId"] = int.TryParse(userId, out int userIdInt) ? userIdInt : 0;
            Log(Constants.DataServerLogType.joinRoom, args);
        }

        public static void LogExitRoom(string userId)
        {
            Dictionary<string, object> args = new Dictionary<string, object>();
            args["userId"] = int.TryParse(userId, out int userIdInt) ? userIdInt : 0;
            Log(Constants.DataServerLogType.exitRoom, args);
        }

        public static void LogPlayerReady(string userId)
        {
            Dictionary<string, object> args = new Dictionary<string, object>();
            args["userId"] = int.TryParse(userId, out int userIdInt) ? userIdInt : 0;
            Log(Constants.DataServerLogType.playerReady, args);
        }

        public static void LogGameStart(int mapId, int playerCount, List<string> userIds)
        {
            Dictionary<string, object> args = new Dictionary<string, object>();
            args["mapId"] = mapId;
            args["playerCount"] = playerCount;
            List<int> userIdInts = new List<int>();
            foreach (string userId in userIds)
            {
                if (int.TryParse(userId, out int userIdInt))
                {
                    userIdInts.Add(userIdInt);
                }
            }
            args["userIds"] = userIdInts;
            Log(Constants.DataServerLogType.gameStart, args);
        }

        public static void LogSkillHit(string userId, string target, int damage, string skillId)
        {
            // Dictionary<string, object> args = new Dictionary<string, object>();
            // args["userId"] = int.TryParse(userId, out int userIdInt) ? userIdInt : 0;
            // args["target"] = int.TryParse(target, out int targetInt) ? targetInt : 0;
            // args["damage"] = damage;
            // args["skillId"] = int.TryParse(skillId, out int skillIdInt) ? skillIdInt : 0;
            // Log(Constants.DataServerLogType.skillHit, args);
        }

        public static void LogKill(string userId, string target, string killType, string skillId)
        {
            Dictionary<string, object> args = new Dictionary<string, object>();
            args["userId"] = int.TryParse(userId, out int userIdInt) ? userIdInt : 0;
            args["target"] = int.TryParse(target, out int targetInt) ? targetInt : 0;
            args["killType"] = killType;
            args["skillId"] = int.TryParse(skillId, out int skillIdInt) ? skillIdInt : 0;
            Log(Constants.DataServerLogType.kill, args);
        }

        public static void LogGameEnd(int mapId, int playerCount, List<Dictionary<string, object>> playerLogs)
        {
            Dictionary<string, object> args = new Dictionary<string, object>();
            args["mapId"] = mapId;
            args["playerCount"] = playerCount;
            args["playerLogs"] = playerLogs;
            Log(Constants.DataServerLogType.gameEnd, args);
        }
    }
}