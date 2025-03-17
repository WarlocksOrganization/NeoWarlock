using DataSystem;
using GameManagement;
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
            for (int i = 0; i < roomTypeButtons.Length; i++)
            {
                roomTypeButtons[i].gameObject.GetComponent<Image>().color = (i == index) ? Color.black : Color.white;
            }

            roomData.roomType = (Constants.RoomType)index;
        }

        public void OnClickMaxPlayerCountButton(int index)
        {
            for (int i = 0; i < maxPlayerCountButtons.Length; i++)
            {
                maxPlayerCountButtons[i].gameObject.GetComponent<Image>().color = (i == index) ? Color.black : Color.white;
            }

            roomData.maxPlayerCount = index + MIN_PLAYER;
        }

        public void OnClickConfirmButton()
        {
            if (string.IsNullOrEmpty(roomNameInput.text))
            {
                Debug.LogWarning("방 이름을 입력하세요!");
                return;
            }

            roomData.roomName = roomNameInput.text; // UI 입력값을 roomData에 저장

            var manager = RoomManager.singleton as RoomManager;

            if (manager.isNetworkActive)
            {
                manager.StopHost(); // 기존 연결 초기화
            }

            // 방 데이터를 RoomManager에 설정
            manager.roomName = roomData.roomName;
            manager.roomType = roomData.roomType;
            manager.maxPlayerCount = roomData.maxPlayerCount;

            manager.StartHost(); // 새로운 방 생성
            // manager.StartServer(); // (Debug) 새로운 서버 생성
    
            Debug.Log($"방 생성 완료: {roomData.roomName}, 유형: {roomData.roomType}, 최대 인원: {roomData.maxPlayerCount}");
        }

    }
}
