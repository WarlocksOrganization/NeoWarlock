using DataSystem;
using GameManagement;
using kcp2k;
using Mirror;
using Networking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class LanUI : MonoBehaviour
    {
        [SerializeField] private TMP_InputField ipInputField;

        [SerializeField] private Button createRoomButton;
        [SerializeField] private Button enterRoomButton;
        
        public void OnCreateRoomButtion()
        {
            AudioManager.Instance.PlaySFX(Constants.SoundType.SFX_Button);
            NetworkManager.singleton.networkAddress = ipInputField.text;
            if (ipInputField.text == "")
            {
                NetworkManager.singleton.networkAddress = "127.0.0.1";
            }
                
            if (NetworkManager.singleton.transport is kcp2k.KcpTransport kcp)
            {
                kcp.Port = 7777; // ✅ KCP Transport의 포트 설정
            }
        }
        
        public void OnClickEnterRoomButtion()
        {
            AudioManager.Instance.PlaySFX(Constants.SoundType.SFX_Button);
            NetworkManager.singleton.networkAddress = ipInputField.text;
            if (ipInputField.text == "")
            {
                NetworkManager.singleton.networkAddress = "127.0.0.1";
            }
                
            if (NetworkManager.singleton.transport is kcp2k.KcpTransport kcp)
            {
                kcp.Port = 7777; // ✅ KCP Transport의 포트 설정
            }
            
            var manager = RoomManager.singleton as RoomManager;
            
            manager.StartClient();
        }
    }
}
