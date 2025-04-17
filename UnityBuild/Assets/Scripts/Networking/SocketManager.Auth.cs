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
        // 🟡 주기적으로 서버에 접속 상태 확인 메시지를 전송
        private void AlivePing()
        {
            JToken aliveData = new JObject();
            aliveData["action"] = "alivePing";

            _pendingRequests["refreshSession"] = true;
            _lastAlivePingTime = (int)Time.time;

            SendMessageToServer(aliveData.ToString());
        }

        // 🟢 OAuth2 인증 요청 (SSAFY 학사 시스템 인증 흐름 예시)
        public void RequestOauth(string id)
        {
            JToken oauthData = new JObject();
            oauthData["action"] = "SSAFYlogin";
            oauthData["userName"] = id;
            oauthData["password"] = id;

            _pendingRequests["login"] = true;
            SendMessageToServer(oauthData.ToString());
        }

        // 🟢 일반 회원가입 요청
        public void RequestRegister(string user_name, string password)
        {
            JToken registerData = new JObject();
            registerData["action"] = "register";
            registerData["userName"] = user_name;
            registerData["password"] = password;

            SendRequestToServer(registerData);
        }

        // 🟢 일반 로그인 요청
        public void RequestAuth(string user_name, string password)
        {
            JToken authData = new JObject();
            authData["action"] = "login";
            authData["userName"] = user_name;
            authData["password"] = password;

            SendRequestToServer(authData);
        }

        // 🔴 로그아웃 요청 (세션 제거)
        public void RequestLogout()
        {
            JToken authData = new JObject();
            authData["action"] = "logout";

            SendMessageToServer(authData.ToString());
        }

        // 🟡 닉네임 변경 요청
        public void RequestUpdateNickname(string nickname)
        {
            JToken changeNicknameData = new JObject();
            changeNicknameData["action"] = "updateNickName";
            changeNicknameData["nickName"] = nickname;

            SendRequestToServer(changeNicknameData);
        }

        // 🟢 로그인 결과 처리
        private void HandleAuth(JToken data)
        {
            if (data.SelectToken("status").ToString() == "success")
            {
                // 로그인 성공 시, 유저 정보 저장 및 UI 갱신
                sessionToken = data.SelectToken("sessionToken").ToString();
                userId = data.SelectToken("userId").ToString();
                nickName = data.SelectToken("nickName").ToString();

                PlayerSetting.Nickname = nickName;
                PlayerSetting.UserId = userId;

                // UI 전환
                LoginUI loginUI = FindFirstObjectByType<LoginUI>();
                loginUI?.TurnOnOnlineUI();

                // 성공 메시지 표시
                ModalPopupUI.singleton?.ShowModalMessage("로그인 성공\n환영합니다, " + nickName + " 님!");

                // 서버 AlivePing 시작
                AlivePing();
            }
            else
            {
                ModalPopupUI.singleton?.ShowModalMessage("로그인 실패\n아이디와 비밀번호를 확인해주세요.");
                Debug.LogWarning("[SocketManager] 인증 실패");
            }
        }

        // 🟢 회원가입 결과 처리
        private void HandleRegister(JToken data)
        {
            if (data.SelectToken("status").ToString() == "success")
            {
                Debug.Log("[SocketManager] 회원가입 성공");
            }
            else
            {
                Debug.LogWarning("[SocketManager] 회원가입 실패");
            }
        }

        // 🟢 세션 갱신 처리
        private void HandleRefreshSession(JToken data)
        {
            if (data.SelectToken("status").ToString() == "success")
            {
                Debug.Log("[SocketManager] 세션 갱신 성공");
            }
            else
            {
                Debug.LogWarning("[SocketManager] 세션 갱신 실패");
            }
        }

        // 🟡 닉네임 변경 응답 처리
        private void HandleUpdateNickname(JToken data)
        {
            if (data.SelectToken("status").ToString() == "success")
            {
                NicknameUI nicknameUI = FindFirstObjectByType<NicknameUI>();
                var modal = ModalPopupUI.singleton;

                if (nicknameUI == null || modal == null)
                {
                    Debug.LogWarning("[SocketManager] 닉네임 변경 UI 또는 모달이 없습니다.");
                    modal?.ShowModalMessage("닉네임 변경 실패\nUI 또는 모달이 없습니다.");
                    return;
                }

                // 로컬 닉네임 및 UI 갱신
                nicknameUI.SyncLocalNickname();
                nicknameUI.SyncNicknameShower();

                modal.ShowModalMessage("닉네임 변경 성공\n" + PlayerSetting.Nickname + " 님!");
            }
            else
            {
                Debug.LogWarning("[SocketManager] 닉네임 변경 실패");

                // 실패 원인을 사용자에게 표시
                NicknameUI nicknameUI = FindFirstObjectByType<NicknameUI>();
                nicknameUI?.HandleUpdateNicknameError(data.SelectToken("message").ToString());
            }
        }
    }
}
