using UnityEngine;
using UnityEngine.UI;

public class SkillButton : MonoBehaviour
{
    public string skillName; // ��ų �̸�
    public string skillDescription; // ��ų ����
    private SkillDescriptionUI uiManager; // UI �Ŵ���

void Start()
{
    uiManager = FindFirstObjectByType<SkillDescriptionUI>(); // ✅ 새로운 방식 적용

    if (uiManager == null)
    {
        Debug.LogError("❌ SkillDescriptionUI를 찾을 수 없습니다! Scene에 추가되었는지 확인하세요.");
    }

    skillName = "캐릭터 스킬";
    skillDescription = "캐릭터 스킬 설명";

    // 버튼 클릭 시 상세 정보 띄우기
    GetComponent<Button>().onClick.AddListener(() =>
        uiManager.ShowSkillDetail(skillName, skillDescription, transform.position));
}
}
