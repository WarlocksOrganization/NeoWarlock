using System.Collections;
using DataSystem;
using Mirror;
using Networking;
using UnityEngine;

namespace UI
{
    public class OnlineUI : MonoBehaviour
    {
        [SerializeField] private GameObject createRoomUI;
        [SerializeField] private GameObject findRoomUI;
    
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
    }
}
