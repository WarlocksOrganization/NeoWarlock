using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using DataSystem;
using Mirror;

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
                return;
            }

            LogEvent logEvent = new LogEvent(eventType, args);
            string json = logEvent.ToJson();
            
            #if UNITY_EDITOR
            // 콘솔에 출력
            Debug.Log(json);
            #else
            // 로그 파일에 기록
            try{
                string logPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + Constants.LogFilepath;
                File.AppendAllText(logPath, json + "," + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Debug.LogError("[FileLogger] 로그 파일 기록 오류: " + ex.Message);
            }
            #endif
        }

        public static void LogKill(string killer, string victim, string killType, string skillId)
        {
            Dictionary<string, object> args = new Dictionary<string, object>();
            args["killer"] = killer;
            args["victim"] = victim;
            args["killType"] = killType;
            args["skillId"] = skillId;

            Log(Constants.DataServerLogType.kill, args);
        }

        public static void LogSkillHit(string attacker, string victim, int damage, string skillId)
        {
            Dictionary<string, object> args = new Dictionary<string, object>();
            args["attacker"] = attacker;
            args["victim"] = victim;
            args["damage"] = damage;
            args["skillId"] = skillId;

            Log(Constants.DataServerLogType.skillHit, args);
        }

        public static void LogSkillUse(string caster, string skillId)
        {
            Dictionary<string, object> args = new Dictionary<string, object>();
            args["caster"] = caster;
            args["skillId"] = skillId;

            Log(Constants.DataServerLogType.skillUse, args);
        }

        public static void LogGameStart(string gameId, string roomName, string roomType, int maxPlayerCount)
        {
            Dictionary<string, object> args = new Dictionary<string, object>();
            args["gameId"] = gameId;
            args["roomName"] = roomName;
            args["roomType"] = roomType;
            args["maxPlayerCount"] = maxPlayerCount;

            Log(Constants.DataServerLogType.gameStart, args);
        }
    }
}