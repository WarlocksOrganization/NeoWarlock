using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using DataSystem;
using GameManagement;
using Mirror;
using Newtonsoft.Json.Linq;
using Player;
using UnityEngine;

namespace Networking
{
    public partial class SocketManager : MonoBehaviour
    {   
        private IEnumerator AlivePingSender()
        {
            // AlivePing 요청을 주기적으로 보내는 코루틴
            while (_client == null || !_client.Connected)
            {
                yield return new WaitForSeconds(1);
            }

            while (_client.Connected)
            {
                AlivePing();
                yield return new WaitForSeconds(60);
            }
        }
        private void AlivePing()
        {
            // 서버에 AlivePing 요청
            JToken aliveData = new JObject();
            aliveData["action"] = "alivePing";
            _pendingRequests["refreshSession"] = true;
            SendMessageToServer(aliveData.ToString());
        }
        public void RequestRegister(string user_name, string password)
        {
            // 회원가입 요청
            JToken registerData = new JObject();
            registerData["action"] = "register";
            registerData["userName"] = user_name;
            registerData["password"] = password;

            SendRequestToServer(registerData);
        }

        public void RequestAuth(string user_name, string password)
        {
            // 인증 요청
            JToken authData = new JObject();
            authData["action"] = "login";
            authData["userName"] = user_name;
            authData["password"] = password;

            SendRequestToServer(authData);
        }

        public void RequestRefreshSession()
        {
            // 세션 갱신 요청
            JToken refreshData = new JObject();
            refreshData["action"] = "refreshSession";
            refreshData["sessionId"] = PlayerPrefs.GetString("sessionId");

            SendRequestToServer(refreshData);
        }

        private void HandleAuth(JToken data)
        {
            // 인증 처리
            if (data.SelectToken("status").ToString() == "success")
            {
                Debug.Log("[SocketManager] 인증 성공");
                // 인증 성공 시 처리
                PlayerPrefs.SetString("sessionToken", data.SelectToken("sessionToken").ToString());
                PlayerPrefs.SetString("userName", data.SelectToken("userName").ToString());

                PlayerSetting.Nickname = data.SelectToken("userName").ToString();

                // AlivePing 요청을 주기적으로 보내는 코루틴
                StartCoroutine(AlivePingSender());
            }
            else
            {
                Debug.LogWarning("[SocketManager] 인증 실패");
                // 인증 실패 시 처리
                PlayerPrefs.DeleteKey("sessionToken");
                PlayerPrefs.DeleteKey("userName");
            }
        }

        private void HandleRegister(JToken data)
        {
            if (data.SelectToken("status").ToString() == "success")
            {
                Debug.Log("[SocketManager] 회원가입 성공");
            }
            else {
                Debug.LogWarning("[SocketManager] 회원가입 실패");
            }
        }

        private void HandleRefreshSession(JToken data)
        {
            // 세션 갱신 처리
            if (data.SelectToken("status").ToString() == "success")
            {
                Debug.Log("[SocketManager] 세션 갱신 성공");
            }
            else
            {
                Debug.LogWarning("[SocketManager] 세션 갱신 실패");
            }
        }
    }
}