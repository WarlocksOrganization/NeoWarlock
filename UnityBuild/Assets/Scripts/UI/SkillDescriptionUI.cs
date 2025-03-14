using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillDescriptionUI : MonoBehaviour
{
    public GameObject skillDetailPanel;  // 상세정보 패널
    public TextMeshProUGUI skillNameText; // 스킬 이름 표시
    public TextMeshProUGUI skillDescriptionText; // 스킬 설명 표시

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
    //    // ESC 키를 누르면 패널 닫기
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
//    public GameObject skillDetailPanel;  // 상세정보 패널
//    public GameObject clickDetectorPanel; // 클릭감시 패널
//    public TextMeshProUGUI skillNameText; // 스킬 이름 표시
//    public TextMeshProUGUI skillDescriptionText; // 스킬 설명 표시
//    //private RectTransform detailPanelRect; // 상세정보 패널 RectTransform

//    private bool isPanelOpen = false;


//    void Start()
//    {
//        skillDetailPanel.SetActive(false);
//        clickDetectorPanel.SetActive(false); // 처음엔 숨김

//        // 버튼 클릭 시 패널 닫기
//        Button clickButton = clickDetectorPanel.GetComponent<Button>();
//        clickButton.onClick.AddListener(CloseSkillDetail);
//    }

//    public void ShowSkillDetail(string skillName, string skillDescription, Vector3 buttonPosition)
//    {
//        skillNameText.text = skillName;
//        skillDescriptionText.text = skillDescription;

//        skillDetailPanel.transform.position = buttonPosition + new Vector3(480, 0, 0);

//        skillDetailPanel.SetActive(true);
//        clickDetectorPanel.SetActive(true); // 클릭 감지 패널 활성화
//        isPanelOpen = true;
//    }

//    public void CloseSkillDetail()
//    {
//        skillDetailPanel.SetActive(false);
//        clickDetectorPanel.SetActive(false); // 감지 패널도 숨김
//        isPanelOpen = false;
//    }

//    void Update()
//    {
//        // ESC 키를 누르면 패널 닫기
//        if (isPanelOpen && Input.GetKeyDown(KeyCode.Escape))
//        {
//            CloseSkillDetail();
//        }
//    }
//}
