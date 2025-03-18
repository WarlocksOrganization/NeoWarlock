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
        [SerializeField] private GameObject lanUI;

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
            PlayerSetting.Nickname = nicknameInputField.text;
            
            if (nicknameInputField.text == "")
            {
                PlayerSetting.Nickname = "Player" + Random.Range(1000, 9999);
            }
            lanUI.SetActive(true);
            gameObject.SetActive(false);
        }
    }
}
