using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using DataSystem;
using GameManagement;
using kcp2k;
using Mirror;
using Newtonsoft.Json.Linq;
using Player;
using UnityEngine;

namespace Networking
{
    public partial class SocketManager : MonoBehaviour
    {   
        private void AlivePing()
        {
            // 서버에 AlivePing 요청
            JToken aliveData = new JObject();
            aliveData["action"] = "alivePing";
            _pendingRequests["refreshSession"] = true;
            _lastAlivePingTime = (int)Time.time;
            SendMessageToServer(aliveData.ToString());
        }

        public void RequestOauth(string id)
        {
            // OAuth2 인증 요청
            JToken oauthData = new JObject();
            oauthData["action"] = "SSAFYlogin";
            oauthData["userName"] = id;
            oauthData["password"] = id;

            _pendingRequests["login"] = true;
            SendMessageToServer(oauthData.ToString());
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

        public void RequestLogout()
        {
            // 인증 요청
            JToken authData = new JObject();
            authData["action"] = "logout";

            SendMessageToServer(authData.ToString());
        }
        public void RequestUpdateNickname(string nickname)
        {
            // 닉네임 변경 요청
            JToken changeNicknameData = new JObject();
            changeNicknameData["action"] = "updateNickName";
            changeNicknameData["nickName"] = nickname;

            SendRequestToServer(changeNicknameData);
        }

        private void HandleAuth(JToken data)
        {
            // 인증 처리
            if (data.SelectToken("status").ToString() == "success")
            {
                Debug.Log("[SocketManager] 인증 성공");
                // 인증 성공 시 처리
                sessionToken = data.SelectToken("sessionToken").ToString();
                userId = data.SelectToken("userId").ToString();
                nickName = data.SelectToken("nickName").ToString();
                // PlayerPrefs.SetString("sessionToken", data.SelectToken("sessionToken").ToString());
                // PlayerPrefs.SetString("userId", data.SelectToken("userId").ToString());
                // PlayerPrefs.SetString("nickName", data.SelectToken("nickName").ToString());

                PlayerSetting.Nickname = data.SelectToken("nickName").ToString();
                PlayerSetting.UserId = data.SelectToken("userId").ToString();

                LoginUI loginUI = FindFirstObjectByType<LoginUI>();
                if (loginUI != null)
                {
                    loginUI.TurnOnOnlineUI();
                }
                var modal = ModalPopupUI.singleton as ModalPopupUI;
                if (modal != null)
                {
                    modal.ShowModalMessage("로그인 성공\n환영합니다, " + PlayerSetting.Nickname + " 님!");
                }
                // AlivePing 요청을 주기적으로 보내는 코루틴
                AlivePing();
            }
            else
            {
                var modal = ModalPopupUI.singleton as ModalPopupUI;
                if (modal != null)
                {
                    modal.ShowModalMessage("로그인 실패\n아이디와 비밀번호를 확인해주세요.");
                }
                Debug.LogWarning("[SocketManager] 인증 실패");
                // 인증 실패 시 처리
                // PlayerPrefs.DeleteKey("sessionToken");
                // PlayerPrefs.DeleteKey("userId");
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

        private void HandleUpdateNickname(JToken data)
        {
            // 닉네임 변경 처리
            if (data.SelectToken("status").ToString() == "success")
            {
                NicknameUI nicknameUI = FindFirstObjectByType<NicknameUI>();
                var modal = ModalPopupUI.singleton as ModalPopupUI;

                if (nicknameUI == null || modal == null)
                {
                    Debug.LogWarning("[SocketManager] 닉네임 변경 UI 또는 모달이 없습니다.");
                    if (modal != null)
                    {
                        modal.ShowModalMessage("닉네임 변경 실패\nUI 또는 모달이 없습니다.");
                    }
                    return;
                }

                nicknameUI.SyncLocalNickname();
                nicknameUI.SyncNicknameShower();

                Debug.Log("[SocketManager] 닉네임 변경 성공");
                modal.ShowModalMessage("닉네임 변경 성공\n" + PlayerSetting.Nickname + " 님!");
            }
            else
            {
                Debug.LogWarning("[SocketManager] 닉네임 변경 실패");
                NicknameUI nicknameUI = FindFirstObjectByType<NicknameUI>();
                if (nicknameUI != null)
                {
                    nicknameUI.HandleUpdateNicknameError(data.SelectToken("message").ToString());
                }
            }
        }
    }
}