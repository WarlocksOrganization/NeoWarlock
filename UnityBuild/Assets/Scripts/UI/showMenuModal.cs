using UnityEngine;
using UnityEngine.UI;

public class showMenuModal : MonoBehaviour
{
    public GameObject menuModal;
    public Button openCloseButton;

    void Start()
    {
        menuModal.SetActive(false);
        openCloseButton.onClick.AddListener(ToggleMenu);
    }

    void ToggleMenu()
    {
        menuModal.SetActive(!menuModal.activeSelf);
    }

    public void OnClickExitGameButton()
    {
        Application.Quit();
    }
}
