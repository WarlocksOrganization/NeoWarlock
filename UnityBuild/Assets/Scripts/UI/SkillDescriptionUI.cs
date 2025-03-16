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
}