using UnityEngine;
using UnityEngine.UI;

public class showMenuModal : MonoBehaviour
{
    public GameObject menuModal;
    //public GameObject clickDetectorPanel;
    public Button openCloseButton;

    void Start()
    {
        menuModal.SetActive(false);
        openCloseButton.onClick.AddListener(ToggleMenu);

        //clickDetectorPanel.SetActive(false);

        //Button clickButton = clickDetectorPanel.GetComponent<Button>();
        //clickButton.onClick.AddListener(ToggleMenu);
    }

    void ToggleMenu()
    {
        menuModal.SetActive(!menuModal.activeSelf);
        //clickDetectorPanel.SetActive(!clickDetectorPanel.activeSelf);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleMenu();
        }
    }
}
