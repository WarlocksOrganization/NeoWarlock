using System.Collections;
// using Mono.Cecil;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ModalPopupUI : MonoBehaviour
{
    [SerializeField] private GameObject modalPrefab;
    private GameObject _modalObject;
    private GameObject _modalMessage;
    private GameObject _modalConfirm;

    public static ModalPopupUI singleton;

    private void Awake()
    {
        if (singleton != null)
        {
            Destroy(gameObject);
            return;
        }
        singleton = this;
        DontDestroyOnLoad(gameObject);
    }

    private IEnumerator InitiateModal(string message)
    {
        // 모달 팝업을 초기화하는 함수
        GameObject canvas = GameObject.Find("Canvas");
        _modalObject = Instantiate(modalPrefab, canvas.transform);
        while (_modalObject.transform.Find("BackGround") == null)
        {
            yield return null;
        }
        _modalMessage = _modalObject.transform.Find("BackGround").Find("ModalTextArea").Find("ModalText").gameObject;
        _modalConfirm = _modalObject.transform.Find("BackGround").Find("ModalConfirm").gameObject;

        _modalMessage.GetComponent<TextMeshProUGUI>().text = message;
        _modalConfirm.GetComponent<Button>().onClick.AddListener(CloseModalMessage);
    }

    public void ShowModalMessage(string message)
    {
        // 모달 팝업을 띄우는 함수
        if (_modalObject == null)
        {
            StartCoroutine(InitiateModal(message));
        }
        else
        {
            _modalMessage.GetComponent<TextMeshProUGUI>().text = message;
            _modalObject.SetActive(true);
        }
    }

    public void CloseModalMessage()
    {
        // 모달 팝업을 닫는 함수
        _modalObject.SetActive(false);
    }
}
