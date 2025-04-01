using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json.Linq;
using UnityEngine.Windows.Speech;
using System.Collections.Generic;
using System.Collections;
using TMPro;
using Player.Combat;

public class FindRoomUI : MonoBehaviour
{
    [SerializeField] private GameObject _roomContainerPrefab;
    [SerializeField] private GameObject onlineUI;
    [SerializeField] private GameObject _contentParent;
    private Dictionary<int, ushort> _roomPortDict = new Dictionary<int, ushort>();


    public void TurnOnFindRoomUI()
    {
        // 방 목록 요청
        Debug.Log("방 목록 요청");

        gameObject.SetActive(true);
        onlineUI.SetActive(false);
        Networking.SocketManager.singleton.RequestListRooms();
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
            roomContainer.transform.Find("RoomId").GetComponent<TextMeshProUGUI>().text = roomId.ToString();
            roomContainer.transform.Find("RoomType").GetComponent<TextMeshProUGUI>().text = "개인전";
            roomContainer.transform.Find("RoomName").GetComponent<TextMeshProUGUI>().text = room.SelectToken("roomName").ToString();
            roomContainer.transform.Find("RoomCount").GetComponent<TextMeshProUGUI>().text = room.SelectToken("currentPlayers").ToString() + " / " + room.SelectToken("maxPlayers").ToString();
            _roomPortDict[roomId] = room.SelectToken("port").ToObject<ushort>();
            roomContainer.GetComponentInChildren<Button>().onClick.AddListener(() => OnClickRoom(roomId));
        }
    }

    private void OnClickRoom(int roomId)
    {
        // 방 입장 요청
        GetComponentInChildren<Button>().interactable = false;
        StartCoroutine(ButtonDisableCoroutine());
        Networking.SocketManager.singleton.RequestJoinRoom(roomId, _roomPortDict[roomId]);
    }

    private IEnumerator ButtonDisableCoroutine()
    {
        // 버튼 클릭 방지 코루틴
        yield return new WaitForSeconds(1);
        GetComponentInChildren<Button>().interactable = true;
    }

    public void OnClickCloseFindRoom()
    {
        gameObject.SetActive(false);
        onlineUI.SetActive(true);
    }
}
