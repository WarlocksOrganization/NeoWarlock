using System.Collections;
using DataSystem;
using GameManagement;
using Mirror;
using Networking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class CreateRoomUI : MonoBehaviour
    {
        private GameRoomData roomData;
        [SerializeField] private TMP_InputField roomNameInput;
        [SerializeField] private Button[] roomTypeButtons;
        [SerializeField] private Button[] maxPlayerCountButtons;

        private const int MIN_PLAYER = 2;
        Color blackWithAlpha = new Color(0f, 0f, 0f, 200f / 255f);
        Color whiteWithAlpha = new Color(1f, 1f, 1f, 200f / 255f);
        void Start()
        {
            // GameRoomData를 새로운 GameObject에 추가
            GameObject roomDataObject = new GameObject("GameRoomData");
            roomData = roomDataObject.AddComponent<GameRoomData>();

            // 초기화 설정
            string defaultRoomName = !string.IsNullOrEmpty(PlayerSetting.Nickname) 
                ? PlayerSetting.Nickname + "님의 방" 
                : "새로운 방";

            roomData.roomName = defaultRoomName;
            roomData.roomType = Constants.RoomType.Solo;
            roomData.maxPlayerCount = 6;

            roomNameInput.text = roomData.roomName;
        }


        public void OnClickRoomTypeButton(int index)
        {
            AudioManager.Instance.PlaySFX(Constants.SoundType.SFX_Button);
            for (int i = 0; i < roomTypeButtons.Length; i++)
            {
                roomTypeButtons[i].gameObject.GetComponent<Image>().color = (i == index) ? blackWithAlpha : whiteWithAlpha;
            }

            roomData.roomType = (Constants.RoomType)index;
        }

        public void OnClickMaxPlayerCountButton(int index)
        {
            AudioManager.Instance.PlaySFX(Constants.SoundType.SFX_Button);
            for (int i = 0; i < maxPlayerCountButtons.Length; i++)
            {
                maxPlayerCountButtons[i].gameObject.GetComponent<Image>().color = (i == index) ? blackWithAlpha : whiteWithAlpha;
            }

            roomData.maxPlayerCount = index + MIN_PLAYER;
        }

        public void OnClickConfirmButton()
        {
            AudioManager.Instance.PlaySFX(Constants.SoundType.SFX_Button);
            if (string.IsNullOrEmpty(roomNameInput.text))
            {
                Debug.LogWarning("방 이름을 입력하세요!");
                return;
            }

            roomData.roomName = roomNameInput.text;

            var manager = RoomManager.singleton as RoomManager;

            if (PlayerPrefs.HasKey("sessionToken"))
            {
                // 로그인 상태에서 방 생성
                Networking.SocketManager.singleton.RequestCreateRoom(roomData.roomName, roomData.maxPlayerCount);
                return;
            }

            if (manager.isNetworkActive)
            {
                StartCoroutine(RestartHostWithDelay(manager));
            }
            else
            {
                StartHost(manager);
            }
        }

        private IEnumerator RestartHostWithDelay(RoomManager manager)
        {
            if (NetworkServer.active)
            {
                // 연결된 클라이언트 강제 종료 전에 대기
                NetworkServer.DisconnectAll(); // 연결 끊기
                yield return new WaitForSeconds(0.3f);
            }

            if (NetworkClient.isConnected)
            {
                NetworkClient.Disconnect();
                yield return new WaitForSeconds(0.3f);
            }

            manager.StopHost();
            yield return new WaitForSeconds(0.5f);

            StartHost(manager);
        }

        private void StartHost(RoomManager manager)
        {
            manager.roomName = roomData.roomName;
            manager.roomType = roomData.roomType;
            manager.maxPlayerCount = roomData.maxPlayerCount;

            //manager.StartHost();
            manager.StartServer(); //Debug
            Debug.Log($"방 생성 완료: {roomData.roomName}, 유형: {roomData.roomType}, 최대 인원: {roomData.maxPlayerCount}");
        }
    }
}
