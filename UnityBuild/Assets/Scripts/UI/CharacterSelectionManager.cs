using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class CharacterSelectionManager : MonoBehaviour
{
    public Transform skillIconContainer;
    public GameObject skillButtonPrefab;
    public SkillDescriptionUI skillDescriptionUI;
    public CharacterDetailLoader characterDetailLoader;
    public SkillDetailLoader skillLoader;

    public TextMeshProUGUI characterName;
    public TextMeshProUGUI characterLine;
    public TextMeshProUGUI characterAtk;
    public TextMeshProUGUI characterHp;
    public TextMeshProUGUI characterSpeed;
    public TextMeshProUGUI characterKnock;

    private List<GameObject> currentSkillIcons = new List<GameObject>();
    public void SelectCharacter(string characterClass)
    {
        if (skillLoader == null)
        {
            Debug.LogError("❌ skillLoader가 null입니다! CharacterSelectionManager의 Inspector에서 SkillLoader를 연결하세요.");
            return;
        }

        // ✅ 기존 스킬 UI 제거 대신 버튼 재사용 (Clone 생성 방지)
        ResetSkillIcons();

        CharacterDetail detail = characterDetailLoader.GetCharacterDetail(characterClass);
        if (detail != null)
        {
            characterName.text = detail.characterName;
            characterLine.text = detail.characterLine;
            characterAtk.text = detail.characterAtk;
            characterHp.text = detail.characterHp;
            characterSpeed.text = detail.characterSpeed;
            characterKnock.text = detail.characterKnock;
        }
        else
        {
            Debug.LogWarning($"⚠️ {characterClass}의 설명 데이터를 찾을 수 없습니다.");
        }

        List<SkillData> skills = skillLoader.GetSkillsForCharacter(characterClass);
        if (skills == null || skills.Count == 0)
        {
            Debug.LogWarning($"⚠️ {characterClass}의 스킬 데이터를 찾을 수 없습니다.");
            return;
        }

        // ✅ 기존에 존재하는 버튼을 재사용
    for (int i = 0; i < skills.Count; i++)
    {
        if (i < skillIconContainer.childCount)
        {
            GameObject skillButton = skillIconContainer.GetChild(i).gameObject;
            skillButton.SetActive(true);
            skillButton.GetComponent<Image>().sprite = skills[i].skillIcon;

            // ✅ index 값을 고정하여 버튼 이벤트가 올바르게 작동하도록 함
            int index = i;
            skillButton.GetComponent<Button>().onClick.RemoveAllListeners();
            skillButton.GetComponent<Button>().onClick.AddListener(() =>
                skillDescriptionUI.ShowSkillDetail(skills[index].skillName, skills[index].skillDescription, skillButton.transform.position)
            );
        }
    }
    }

    private void ResetSkillIcons()
    {   
        foreach (Transform child in skillIconContainer)
        {
            child.gameObject.SetActive(false); // ✅ 기존 버튼들을 숨김
        }
    }

}
