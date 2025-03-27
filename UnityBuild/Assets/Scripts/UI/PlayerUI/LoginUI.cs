using System.Collections;
using Player.Combat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Text.RegularExpressions;
public class LoginUI : MonoBehaviour
{
    [SerializeField] private GameObject _userName;
    [SerializeField] private GameObject _password;
    [SerializeField] private GameObject _loginButton;

    [SerializeField] private GameObject _OnlineUI;

    void Start()
    {
        _loginButton.GetComponent<Button>().onClick.AddListener(OnClickLogin);
        if (PlayerPrefs.HasKey("sessionToken"))
        {
            TurnOnOnlineUI();
        }
    }

    private void OnClickLogin()
    {
        // 로그인 요청
        Networking.SocketManager.singleton.InitSocketConnection();
        StartCoroutine(LoginProcess());
    }

    private IEnumerator LoginProcess()
    {
        var SocketManager = Networking.SocketManager.singleton;
        if (SocketManager == null)
        {
            Debug.Log("[LoginUI] 소켓 매니저가 없습니다.");
            yield break;
        }

        while (!SocketManager.IsConnected())
        {
            yield return new WaitForSeconds(0.5f);
        }

        System.String _userNameText = _userName.GetComponent<TMP_InputField>().text.Trim();
        System.String _passwordText = _password.GetComponent<TMP_InputField>().text.Trim();

        _userNameText = Regex.Replace(_userNameText, @"[^\u0020-\u007E]", string.Empty);
        _passwordText = Regex.Replace(_passwordText, @"[^\u0020-\u007E]", string.Empty);

        Networking.SocketManager.singleton.RequestAuth(_userNameText, _passwordText);
        
        float elapsedTime = 0;
        while (PlayerPrefs.GetString("sessionToken") == "")
        {
            elapsedTime += Time.deltaTime;
            if (elapsedTime > 5)
            {
                Debug.Log("[LoginUI] 인증 실패");
                yield break;
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    public void TurnOnOnlineUI()
    {
        _OnlineUI.SetActive(true);
        gameObject.SetActive(false);
    }
}
