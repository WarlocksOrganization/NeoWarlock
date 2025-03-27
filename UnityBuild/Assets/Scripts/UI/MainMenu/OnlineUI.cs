using System.Collections;
using Mirror;
using Networking;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class OnlineUI : MonoBehaviour
    {
        [SerializeField] private GameObject createRoomUI;
        [SerializeField] private GameObject findRoomUI;
        [SerializeField] private GameObject nickNameUI;
        [SerializeField] private GameObject logoutButton;

        private void Start()
        {
            var socketManager = SocketManager.singleton as SocketManager;
            logoutButton.GetComponent<Button>().onClick.AddListener(socketManager.CloseConnection);
        }
        public void OnClickEnterGameRoomButton()
        {
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
            createRoomUI.SetActive(true);
            gameObject.SetActive(false);
        }

        public void OnEnable()
        {
            nickNameUI.GetComponent<NicknameUI>().SyncNicknameShower();
        }
    }
}
