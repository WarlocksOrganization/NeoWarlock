using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillDescriptionUI : MonoBehaviour
{
    public GameObject skillDetailPanel;  // ������ �г�
    public TextMeshProUGUI skillNameText; // ��ų �̸� ǥ��
    public TextMeshProUGUI skillDescriptionText; // ��ų ���� ǥ��

    void Start()
    {
        skillDetailPanel.SetActive(false);
    }

    public void ShowSkillDetail(string skillName, string skillDescription, Vector3 buttonPosition)
    {
        skillNameText.text = skillName;
        skillDescriptionText.text = skillDescription;

        skillDetailPanel.transform.position = buttonPosition + new Vector3(480, 0, 0);

        skillDetailPanel.SetActive(true);
    }

    public void CloseSkillDetail()
    {
        skillDetailPanel.SetActive(false);
    }

    //void Update()
    //{
    //    // ESC Ű�� ������ �г� �ݱ�
    //    if (isPanelOpen && Input.GetKeyDown(KeyCode.Escape))
    //    {
    //        CloseSkillDetail();
    //    }
    //}
}


//using UnityEngine;
//using UnityEngine.UI;
//using TMPro;

//public class SkillDescriptionUI : MonoBehaviour
//{
//    public GameObject skillDetailPanel;  // ������ �г�
//    public GameObject clickDetectorPanel; // Ŭ������ �г�
//    public TextMeshProUGUI skillNameText; // ��ų �̸� ǥ��
//    public TextMeshProUGUI skillDescriptionText; // ��ų ���� ǥ��
//    //private RectTransform detailPanelRect; // ������ �г� RectTransform

//    private bool isPanelOpen = false;


//    void Start()
//    {
//        skillDetailPanel.SetActive(false);
//        clickDetectorPanel.SetActive(false); // ó���� ����

//        // ��ư Ŭ�� �� �г� �ݱ�
//        Button clickButton = clickDetectorPanel.GetComponent<Button>();
//        clickButton.onClick.AddListener(CloseSkillDetail);
//    }

//    public void ShowSkillDetail(string skillName, string skillDescription, Vector3 buttonPosition)
//    {
//        skillNameText.text = skillName;
//        skillDescriptionText.text = skillDescription;

//        skillDetailPanel.transform.position = buttonPosition + new Vector3(480, 0, 0);

//        skillDetailPanel.SetActive(true);
//        clickDetectorPanel.SetActive(true); // Ŭ�� ���� �г� Ȱ��ȭ
//        isPanelOpen = true;
//    }

//    public void CloseSkillDetail()
//    {
//        skillDetailPanel.SetActive(false);
//        clickDetectorPanel.SetActive(false); // ���� �гε� ����
//        isPanelOpen = false;
//    }

//    void Update()
//    {
//        // ESC Ű�� ������ �г� �ݱ�
//        if (isPanelOpen && Input.GetKeyDown(KeyCode.Escape))
//        {
//            CloseSkillDetail();
//        }
//    }
//}
