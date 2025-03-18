using GameManagement;
using kcp2k;
using Mirror;
using Networking;
using TMPro;
using UnityEngine;

namespace UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private TMP_InputField nicknameInputField;
        [SerializeField] private GameObject onlineUI;

        public void OnClickGameStartButtion()
        {
            PlayerSetting.Nickname = nicknameInputField.text;
            
            if (nicknameInputField.text == "")
            {
                PlayerSetting.Nickname = "Player" + Random.Range(1000, 9999);
            }
            onlineUI.SetActive(true);
            gameObject.SetActive(false);
        }

        public void OnClickLANButtion()
        {
            NetworkManager.singleton.networkAddress = "localhost";
                
            if (NetworkManager.singleton.transport is kcp2k.KcpTransport kcp)
            {
                kcp.Port = 7777; // ✅ KCP Transport의 포트 설정
            }

            OnClickGameStartButtion();
        }
    }
}
