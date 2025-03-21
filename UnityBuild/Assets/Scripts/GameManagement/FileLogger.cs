using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using DataSystem;

namespace GameManagement
{
    public class LogEvent
    {
        public string eventType;
        public string timeStamp;
        public Dictionary<string, object> args;
        public LogType logType;

        public LogEvent(Constants.DataServerLogType eventType, Dictionary<string, object> args = null)
        {
            this.eventType = eventType.ToString();
            this.timeStamp = DateTime.UtcNow.ToString("o"); // ISO 8601 format
            this.args = args ?? new Dictionary<string, object>();
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }

    public static class FileLogger
    {   
        public static void Log(Constants.DataServerLogType eventType, Dictionary<string, object> args = null)
        {
            if (!Application.platform.Equals(RuntimePlatform.LinuxServer))
            {
                Debug.LogWarning("[FileLogger] 서버 모드에서 실행 중이 아닙니다.");
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
    }
}