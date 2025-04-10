using System.Collections;
using Player.Combat;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Text.RegularExpressions;
using UnityEngine.EventSystems;

public class LoginUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField _userName;
    [SerializeField] private TMP_InputField _password;
    [SerializeField] private GameObject _loginButton;

    [SerializeField] private GameObject _OnlineUI;

    void Start()
    {
        _loginButton.GetComponent<Button>().onClick.AddListener(OnClickLogin);
        GameManagement.PlayerSetting.PlayerId = -1;
        if (Networking.SocketManager.singleton.IsSessionValid())
        {
            TurnOnOnlineUI();
        }
        
        _userName.onSubmit.AddListener(_ => OnClickLogin());
        _password.onSubmit.AddListener(_ => OnClickLogin());
    }
    
    void Update()
    {
        // 현재 ID 입력 필드가 선택되어 있고 Tab 키를 누른 경우
        if (_userName.isFocused && Input.GetKeyDown(KeyCode.Tab))
        {
            // Tab 기본 기능 차단
            EventSystem.current.SetSelectedGameObject(null);

            // 포커스를 Password Input으로 넘김
            _password.Select();
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

        System.String _userNameText = _userName.text.Trim();
        System.String _passwordText = _password.text.Trim();

        _userNameText = Regex.Replace(_userNameText, @"[^\u0020-\u007E]", string.Empty);
        _passwordText = Regex.Replace(_passwordText, @"[^\u0020-\u007E]", string.Empty);

        Networking.SocketManager.singleton.RequestAuth(_userNameText, _passwordText);
        
        float elapsedTime = 0;
        while (!Networking.SocketManager.singleton.IsSessionValid())
        {
            elapsedTime += Time.deltaTime;
            if (elapsedTime > 5)
            {
                Debug.Log("[LoginUI] 인증 실패");
                yield break;
            }
            yield return new WaitForSeconds(0.5f);
        }

        TurnOnOnlineUI();
    }

    public void TurnOnOnlineUI()
    {
        _OnlineUI.SetActive(true);
        gameObject.SetActive(false);
        FindFirstObjectByType<FindRoomUI>().TurnOnFindRoomUI();
    }
}
