using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RuleModalUI : MonoBehaviour
{
    public GameObject ruleModal; // ��� �г�
    public TextMeshProUGUI ruleText; // ��Ģ �ؽ�Ʈ ǥ��
    public Button prevButton; // �� ���� ��ư
    public Button nextButton; // �� ���� ��ư
    public Button closeButton; // X �ݱ� ��ư
    public Transform pageIndicatorContainer; // ������ �ε������� �θ� ������Ʈ
    public Image ruleImage; // ����� �̹���
    public Sprite[] ruleImages; // �������� �̹��� �迭
    public GameObject pageDotPrefab; // ������ ǥ�� ��(�� ��)

    private string[] rules = {
        "�� ���帶�� ������ ü���� 0���� ����ų�\n�������� ������ ���ĳ�����!",
        "���� ���� ��, �� ���� ��ȭ ȿ���� ���� �� �ֽ��ϴ�.\n�� ȿ������ �� ���� ���ΰ�ħ�� �� �� �ֽ��ϴ�!",
        "�ʸ��� ������ ����̳� ������Ʈ�� �����մϴ�.\n��뺸�� ������ ��Ȳ�̶�� ����ϸ� �ȵ˴ϴ�!",
        "ü���� 0�� �Ǵ��� ������ �ƴմϴ�!\n���� ���带 ����ϸ� ���ӿ� ������ ��������!"
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

        // ������ �ε������� ������Ʈ
        for (int i = 0; i < pageDots.Length; i++)
        {
            pageDots[i].GetComponent<Image>().color = (i == currentPage) ? Color.white : Color.gray;
        }

        // ù �������� ������ ���������� ��ư ���� ����
        prevButton.interactable = (currentPage > 0);
        nextButton.interactable = (currentPage < rules.Length - 1);

        // �̹��� ����
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
