using System;
using System.Collections;
using System.Collections.Generic;
using DataSystem;
using GameManagement;
using Mirror;
using Networking;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class OnlineUI : MonoBehaviour
    {
        [SerializeField] private GameObject createRoomUI;
        [SerializeField] private GameObject lobbyButton;
        [SerializeField] private GameObject refreshButton;
        [SerializeField] private GameObject quitButton;
        [SerializeField] private GameObject findRoomUI;
        [SerializeField] private GameObject nicknameUI;
        [SerializeField] private GameObject mainMenuUI;
        public CCUDisplay ccuDisplay;

        [Serializable]
        class CCUActionCheck
        {
            public string action;
        }

        private SocketManager socketManager;
        private FindRoomUI findRoomUIComponent;
        private Queue<string> deferredMessages = new Queue<string>();

        private void Start()
        {
            socketManager = SocketManager.singleton as SocketManager;
            findRoomUIComponent = findRoomUI.GetComponent<FindRoomUI>();
            lobbyButton.GetComponentInChildren<Button>().onClick.AddListener(socketManager.OnClickLogout);
            quitButton.GetComponentInChildren<Button>().onClick.AddListener(QuitGame);
            refreshButton.GetComponentInChildren<Button>().onClick.AddListener(findRoomUIComponent.TurnOnFindRoomUI);

            Debug.Log("[OnlineUI] Start 완료");
        }

        public void OnEnable()
        {
            Debug.Log("[OnlineUI] OnEnable 진입");
            
            GameManager.Instance.isLan = true;

            nicknameUI.GetComponent<NicknameUI>().SyncNicknameShower();

            if (socketManager != null)
            {
                socketManager.OnMessageReceived += OnMessageReceived;
                Debug.Log("[OnlineUI] SocketManager 메시지 수신 리스너 연결됨");
            }

            if (ccuDisplay == null)
            {
                Debug.Log("[OnlineUI] OnEnable 시점에 ccuDisplay가 null입니다!");
            }

            while (deferredMessages.Count > 0)
            {
                Debug.Log("[OnlineUI] 보류된 메시지 처리 중");
                HandleCCUMessage(deferredMessages.Dequeue());
            }
        }

        public void OnDisable()
        {
            Debug.Log("[OnlineUI] OnDisable 진입");
            if (socketManager != null)
            {
                socketManager.OnMessageReceived -= OnMessageReceived;
                Debug.Log("[OnlineUI] SocketManager 리스너 제거됨");
            }
        }

        public void OnClickEnterGameRoomButton()
        {
            AudioManager.Instance.PlaySFX(Constants.SoundType.SFX_Button);
            if (Networking.SocketManager.singleton.IsSessionValid())
            {
                findRoomUIComponent.TurnOnFindRoomUI();
            }
            else
            {
                var manager = RoomManager.singleton as RoomManager;
                manager.StartClient();

                Debug.Log($"방 참가 완료: {manager.roomName}, 유형: {manager.roomType}, 최대 인원: {manager.maxConnections}");
            }
        }

        public void OnClickCreateGameRoomButton()
        {
            AudioManager.Instance.PlaySFX(Constants.SoundType.SFX_Button);
            createRoomUI.SetActive(true);
            gameObject.SetActive(false);
        }

        public void switchToMainMenuUI()
        {
            mainMenuUI.SetActive(true);
            gameObject.SetActive(false);
        }

        public void OnMessageReceived(string jsonMessage)
        {
            Debug.Log($"[OnlineUI] 메시지 수신됨: {jsonMessage}");

            CCUActionCheck actionCheck;
            try
            {
                actionCheck = JsonUtility.FromJson<CCUActionCheck>(jsonMessage);
            }
            catch (Exception e)
            {
                Debug.Log($"[OnlineUI] JSON 파싱 실패: {e.Message}");
                return;
            }

            if (actionCheck.action == "CCUList")
            {
                Debug.Log("[OnlineUI] CCUList 메시지 감지됨");

                if (!gameObject.activeInHierarchy)
                {
                    Debug.Log("[OnlineUI] UI 비활성 상태 → 메시지 보류");
                    deferredMessages.Enqueue(jsonMessage);
                    return;
                }

                HandleCCUMessage(jsonMessage);
            }
            else
            {
                Debug.Log($"[OnlineUI] CCU 외 메시지 무시됨: {actionCheck.action}");
            }
        }

        private void HandleCCUMessage(string json)
        {
            Debug.Log($"[OnlineUI] HandleCCUMessage 진입");

            CCUMessage ccuList;
            try
            {
                ccuList = JsonUtility.FromJson<CCUMessage>(json);
            }
            catch (Exception e)
            {
                Debug.Log($"[HandleCCUMessage] 파싱 실패: {e.Message}");
                return;
            }

            if (ccuList == null || ccuList.users == null)
            {
                Debug.Log("[HandleCCUMessage] 유저 데이터가 null임");
                return;
            }

            Debug.Log($"[HandleCCUMessage] 유저 수: {ccuList.users.Count}");

            if (ccuDisplay == null)
            {
                Debug.Log("[HandleCCUMessage] ccuDisplay가 null임");
                return;
            }

            Debug.Log("[HandleCCUMessage] UI 렌더링 시작");
            ccuDisplay.UpdateCCUDisplay(ccuList.users);
        }

        public void QuitGame()
        {
            socketManager?.OnClickLogout();
            Application.Quit();
        }
    }
}
