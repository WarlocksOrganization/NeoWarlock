using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RuleModalUI : MonoBehaviour
{
    public GameObject ruleModal; // 모달 패널
    public TextMeshProUGUI ruleText; // 규칙 텍스트 표시
    public Button prevButton; // ◀ 이전 버튼
    public Button nextButton; // ▶ 다음 버튼
    public Button closeButton; // X 닫기 버튼
    public Transform pageIndicatorContainer; // 페이지 인디케이터 부모 오브젝트
    public Image ruleImage; // 변경될 이미지
    public Sprite[] ruleImages; // 페이지별 이미지 배열
    public GameObject pageDotPrefab; // 페이지 표시 점(● ○)

    private string[] rules = {
        "각 라운드마다 상대방의 체력을 0으로 만들거나\n스테이지 밖으로 밀쳐내세요!",
        "라운드 시작 전, 세 가지 강화 효과를 얻을 수 있습니다.\n각 효과마다 한 번씩 새로고침을 할 수 있습니다!",
        "맵마다 고유의 기믹이나 오브젝트가 존재합니다.\n상대보다 유리한 상황이라고 방심하면 안됩니다!",
        "체력이 0이 되더라도 끝난게 아닙니다!\n다음 라운드를 기약하며 게임에 변수를 만들어보세요!"
    };

    private int currentPage = 0;
    private GameObject[] pageDots;

    void Start()
    {
        ruleModal.SetActive(false);
        closeButton.onClick.AddListener(CloseModal);
        prevButton.onClick.AddListener(PrevPage);
        nextButton.onClick.AddListener(NextPage);
        InitPageIndicators();
        UpdateUI();
    }

    public void OpenModal()
    {
        ruleModal.SetActive(true);
        currentPage = 0;
        UpdateUI();
    }

    void CloseModal()
    {
        ruleModal.SetActive(false);
    }

    void PrevPage()
    {
        if (currentPage > 0)
        {
            currentPage--;
            UpdateUI();
        }
    }

    void NextPage()
    {
        if (currentPage < rules.Length - 1)
        {
            currentPage++;
            UpdateUI();
        }
    }

    void UpdateUI()
    {
        ruleText.text = rules[currentPage];

        // 페이지 인디케이터 업데이트
        for (int i = 0; i < pageDots.Length; i++)
        {
            pageDots[i].GetComponent<Image>().color = (i == currentPage) ? Color.white : Color.gray;
        }

        // 첫 페이지와 마지막 페이지에서 버튼 상태 변경
        prevButton.interactable = (currentPage > 0);
        nextButton.interactable = (currentPage < rules.Length - 1);

        // 이미지 변경
        if (ruleImages != null && ruleImages.Length > currentPage)
        {
            ruleImage.sprite = ruleImages[currentPage];
        }
    }

    void InitPageIndicators()
    {
        pageDots = new GameObject[rules.Length];

        for (int i = 0; i < rules.Length; i++)
        {
            GameObject dot = Instantiate(pageDotPrefab, pageIndicatorContainer);
            pageDots[i] = dot;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseModal();
        }
    }
}
