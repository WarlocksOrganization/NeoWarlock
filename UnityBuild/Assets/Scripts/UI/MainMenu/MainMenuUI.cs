using DataSystem;
using System.Collections;
using GameManagement;
using kcp2k;
using Mirror;
using Networking;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

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
                    displayModeMessage = "창모드 : Alt+Enter";
                    break;

                case FullScreenMode.Windowed:
                    displayModeMessage = "전체화면 : Alt+Enter";
                    break;

                default:
                    displayModeMessage = "화면 모드를 감지할 수 없습니다.";
                    break;
            }
            displayModeText.text = displayModeMessage;
            
            AudioManager.Instance.PlayBGM(Constants.SoundType.BGM_MainMenu);
            StartCoroutine(TryFetchMatrixFromServer());
        }
        public void OnClickGameStartButtion()
        {
            AudioManager.Instance.PlaySFX(Constants.SoundType.SFX_Button);
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
            AudioManager.Instance.PlaySFX(Constants.SoundType.SFX_Button);
            PlayerSetting.Nickname = nicknameInputField.text;
            
            if (nicknameInputField.text == "")
            {
                PlayerSetting.Nickname = "Player" + Random.Range(1000, 9999);
            }
            lanUI.SetActive(true);
            gameObject.SetActive(false);
        }
        private IEnumerator TryFetchMatrixFromServer()
        {
            if (!MatrixUpdateTracker.ShouldUpdateAfterHours(1)) yield break;

            string url = "http://j12a509.p.ssafy.io:8081/hints";

            UnityWebRequest request = UnityWebRequest.Get(url);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                MatrixFileManager.SaveMatrixJson(json);
                MatrixUpdateTracker.SaveNow();
                // MatrixManager.Instance.LoadMatrixFromJson(json);
            }
            else
            {
                Debug.LogError("데이터 요청 실패: " + request.error);
            }
        }
    }
}
