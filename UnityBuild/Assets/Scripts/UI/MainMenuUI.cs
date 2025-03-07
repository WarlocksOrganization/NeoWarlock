using GameManagement;
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
    }
}
