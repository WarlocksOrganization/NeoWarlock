using UnityEngine;
using UnityEngine.UI;
using Networking;
using Newtonsoft.Json.Linq;
using UnityEngine.Windows.Speech;
using System.Collections.Generic;
using System.Collections;
using TMPro;
using Player.Combat;

public class FindRoomUI : MonoBehaviour
{
    [SerializeField] private GameObject _roomContainerPrefab;
    // [SerializeField] private GameObject onlineUI;
    [SerializeField] private GameObject _contentParent;
    [SerializeField] private Button _refreshButton;

    private Dictionary<int, ushort> _roomPortDict = new Dictionary<int, ushort>();


    public void TurnOnFindRoomUI()
    {
        // 방 목록 요청
        Debug.Log("방 목록 요청");

        // gameObject.SetActive(true);
        // onlineUI.SetActive(false);
        Networking.SocketManager.singleton.RequestListRooms();
        // _refreshButton.onClick.AddListener(OnClickRefresh);
        // ShowRefreshButton(false);
    }

    public void ShowRefreshButton(bool show)
    {
        _refreshButton.gameObject.SetActive(show);
    }

    public void UpdateContainer(JToken data)
    {
        Debug.Log("방 목록 업데이트");
        // 방 목록을 받아와서 화면에 표시
        foreach (Transform child in _contentParent.transform)
        {
            Destroy(child.gameObject);
        }

        foreach (JToken room in data["rooms"])
        {
            GameObject roomContainer = Instantiate(_roomContainerPrefab, _contentParent.transform);
            int roomId = int.TryParse(room.SelectToken("roomId").ToString(), out int result) ? result : 0;
            string roomName = room.SelectToken("roomName").ToString();
            roomContainer.transform.Find("RoomId").GetComponent<TextMeshProUGUI>().text = roomId.ToString();
            roomContainer.transform.Find("RoomType").GetComponent<TextMeshProUGUI>().text = roomName.EndsWith("$") ? "팀전" : "개인전";
            roomContainer.transform.Find("RoomName").GetComponent<TextMeshProUGUI>().text = roomName.TrimEnd('$');
            roomContainer.transform.Find("RoomCount").GetComponent<TextMeshProUGUI>().text = room.SelectToken("currentPlayers").ToString() + " / " + room.SelectToken("maxPlayers").ToString();
            _roomPortDict[roomId] = room.SelectToken("port").ToObject<ushort>();
            Button roomButton = roomContainer.GetComponentInChildren<Button>();
            roomButton.onClick.RemoveAllListeners(); // 기존 리스너 제거
            if (room.SelectToken("status").ToString() == "WAITING")
            {
                roomButton.onClick.AddListener(() => OnClickRoom(roomId));
                roomButton.onClick.AddListener(() => GameObject.Find("ButtonDisabler").GetComponent<ButtonDisabler>().ButtonDisable(roomButton));
            }
            else
            {
                roomButton.GetComponentInChildren<TextMeshProUGUI>().text = "게임중";
                roomButton.interactable = false; // 게임 중인 방은 클릭 불가능
            }
        }
    }

    private void OnClickRoom(int roomId)
    {
        Networking.SocketManager.singleton.RequestJoinRoom(roomId, _roomPortDict[roomId]);
    }

    private void OnClickRefresh()
        {
//            var socket = SocketManager.singleton;
//            if (socket != null && socket.HasPendingRoomUpdate)
//            {
//                socket.RequestListRooms();
//                socket.ClearRoomUpdateFlag();
//                ShowRefreshButton(false);
//            }
        }
    // public void OnClickCloseFindRoom()
    // {
    //     gameObject.SetActive(false);
    //     onlineUI.SetActive(true);
    // }
}
