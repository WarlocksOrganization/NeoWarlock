using System.Collections;
using DataSystem;
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
        [SerializeField] private GameObject quitButton;
        [SerializeField] private GameObject findRoomUI;
        [SerializeField] private GameObject nicknameUI;
        [SerializeField] private GameObject mainMenuUI;

        private SocketManager socketManager;

        private void Start()
        {
            socketManager = SocketManager.singleton as SocketManager;
            lobbyButton.GetComponentInChildren<Button>().onClick.AddListener(socketManager.OnClickLogout);
            quitButton.GetComponentInChildren<Button>().onClick.AddListener(QuitGame);
        }
        public void OnClickEnterGameRoomButton()
        {
            AudioManager.Instance.PlaySFX(Constants.SoundType.SFX_Button);
            if (PlayerPrefs.HasKey("sessionToken"))
            {
                findRoomUI.GetComponent<FindRoomUI>().TurnOnFindRoomUI();
            }
            else
            {
                var manger = RoomManager.singleton as RoomManager;
                manger.StartClient();
            
                Debug.Log($"방 참가 완료: {manger.roomName}, 유형: {manger.roomType}, 최대 인원: {manger.maxConnections}");
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

        public void OnEnable()
        {
            // UI 활성화 시 닉네임 동기화
            nicknameUI.GetComponent<NicknameUI>().SyncNicknameShower();
        }

        public void QuitGame()
        {
            socketManager?.OnClickLogout();
            Application.Quit();
        }
    }
}
