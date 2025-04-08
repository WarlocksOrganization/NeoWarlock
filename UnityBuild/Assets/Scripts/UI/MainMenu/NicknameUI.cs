using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text.RegularExpressions;
using Player;
using GameManagement;
using UnityEditor;
using System.Net.Sockets;
using System.Net.Http;
public class NicknameUI : MonoBehaviour
{
    [SerializeField] private GameObject _nicknameErrorText;
    [SerializeField] private GameObject _nickNameShowText;
    [SerializeField] private GameObject _nicknameInputField;
    [SerializeField] private GameObject _nicknameConfirmButton;

    [SerializeField] private GameObject _onlineUI;

    public void OnClickConfirmButton()
    {
        // 닉네임 변경 요청
        var SocketManager = Networking.SocketManager.singleton;
        if (SocketManager == null)
        {
            Debug.Log("[NicknameUI] 소켓 매니저가 없습니다.");
            return;
        }

        System.String _nicknameText = _nicknameInputField.GetComponent<TMP_InputField>().text.Trim();
        
        //get nickname Length
        int byteLength = CalculateByteLength(_nicknameText);
        Debug.Log("Nickname Length: " + byteLength);
        // 4~16 자
        if (byteLength < 4 || byteLength > 16)
        {
            HandleUpdateNicknameError("닉네임은 영숫자 4~16자 한글 2~8자여야 합니다.");
            return;
        }
        // 4~16 자 한/영/숫자만 허용
        if (Regex.IsMatch(_nicknameText, @"[^a-zA-Z0-9가-힣]"))
        {
            HandleUpdateNicknameError("닉네임은 한글, 영문, 숫자만 허용됩니다.");
            return;
        }

        if (SocketManager.IsConnected())
        {
            SocketManager.RequestUpdateNickname(_nicknameText);
        }
        else
        {
            SyncLocalNickname();
            SyncNicknameShower();
            var modal = ModalPopupUI.singleton as ModalPopupUI;
            if (modal != null)
            {
                modal.ShowModalMessage("닉네임 변경 성공\n" + PlayerSetting.Nickname + " 님!");
            }
        }
    }

    public void SyncLocalNickname()
    {
        string _nicknameText = _nicknameInputField.GetComponent<TMP_InputField>().text.Trim();
        // PlayerPrefs.SetString("nickName", _nicknameText);
        Networking.SocketManager.singleton.nickName = _nicknameText;
    }

    public void SyncNicknameShower()
    {
        // 로컬 닉네임 동기화
        if (Networking.SocketManager.singleton != null)
        {
            PlayerSetting.Nickname = Networking.SocketManager.singleton.nickName;
        }
        _nickNameShowText.GetComponent<TextMeshProUGUI>().text = PlayerSetting.Nickname;
        TurnOffNicknameUI();
    }

    public void HandleUpdateNicknameError(string error)
    {
        // 닉네임 변경 실패 처리
        _nicknameErrorText.GetComponent<TextMeshProUGUI>().text = error;
    }

    public void TurnOnNicknameUI()
    {
        _onlineUI.SetActive(false);
        _nicknameErrorText.GetComponent<TextMeshProUGUI>().text = "한글 2~8자, 영문 4~16자\n특수문자 사용 불가";
        gameObject.SetActive(true);
    }
    public void TurnOffNicknameUI()
    {
        _onlineUI.SetActive(true);
        gameObject.SetActive(false);
    }

    private int CalculateByteLength(string input)
    {
        int byteLength = 0;
    
        foreach (char c in input)
        {
            // Check if the character is a Korean character (Hangul)
            if (c >= 0xAC00 && c <= 0xD7A3) // Unicode range for Hangul syllables
            {
                byteLength += 2; // Korean characters count as 2 bytes
            }
            else
            {
                byteLength += 1; // Other characters count as 1 byte
            }
        }
    
        return byteLength;
    }
}
