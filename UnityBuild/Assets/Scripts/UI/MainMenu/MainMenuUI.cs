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
        [SerializeField] private TMP_Text displayModeText;
    private void Start()
        {
            string displayModeMessage;

            switch (Screen.fullScreenMode)
            {
                case FullScreenMode.FullScreenWindow:
                case FullScreenMode.ExclusiveFullScreen:
                    displayModeMessage = "전체화면 : Alt+Enter";
                    break;

                case FullScreenMode.Windowed:
                    displayModeMessage = "창모드 : Alt+Enter";
                    break;

                default:
                    displayModeMessage = "화면 모드를 감지할 수 없습니다.";
                    break;
            }
            displayModeText.text = displayModeMessage;
        }
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
