using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using DataSystem;
using TMPro;
using DataSystem.Database;

public class CharacterSelectionManager : MonoBehaviour
{
    public Transform skillIconContainer;

    public TextMeshProUGUI characterName;
    public TextMeshProUGUI characterLine;
    public TextMeshProUGUI characterAtk;
    public TextMeshProUGUI characterHp;
    public TextMeshProUGUI characterSpeed;
    public TextMeshProUGUI characterKnock;

    private SkillButton[] skillButtons;
    
    private void Awake()
    {
        InitializeSkillButtons();
    }

    private void InitializeSkillButtons()
    {
        skillButtons = new SkillButton[skillIconContainer.childCount];
        for (int i = 0; i < skillIconContainer.childCount; i++)
        {
            skillButtons[i] = skillIconContainer.GetChild(i).GetComponent<SkillButton>();
        }
    }

    public void SelectCharacter(Constants.CharacterClass characterClass)
    {
        Database.CharacterClassData detail = Database.GetCharacterClassData(characterClass);
        if (detail != null)
        {
            characterName.text = detail.CharacterName;
            characterLine.text = detail.CharacterDescription;
            characterAtk.text = new string('★', detail.CharacterAtk);
            characterHp.text = new string('★', detail.CharacterHp);
            characterSpeed.text = new string('★', detail.CharacterSpeed);
            characterKnock.text = new string('★', detail.CharacterKnock);
        }
        else
        {
            Debug.LogWarning($"⚠️ {characterClass}의 설명 데이터를 찾을 수 없습니다.");
            return;
        }
        
        ResetSkillIcons();
        LoadCharacterSkills(detail);
    }

    private void LoadCharacterSkills(Database.CharacterClassData characterData)
    {
        int index = 0;
        
        for (int i = 0; i < characterData.AttackSkillIds.Count && index < skillButtons.Length; i++, index++)
        {
            Database.AttackData attackData = Database.GetAttackData(characterData.AttackSkillIds[i]);
            if (attackData != null)
            {
                skillButtons[index].SetUp(attackData.DisplayName, attackData.Description, attackData.Icon);
                skillButtons[index].gameObject.SetActive(true);
            }
        }
        
        if (index < skillButtons.Length)
        {
            MovementSkillConfig movementSkill = Database.GetMovementSkillData(characterData.MovementSkillType);
            if (movementSkill != null)
            {
                skillButtons[index].SetUp(movementSkill.skillName, movementSkill.Description, movementSkill.skillIcon);
                skillButtons[index].gameObject.SetActive(true);
            }
        }
    }

    private void ResetSkillIcons()
    {   
        foreach (SkillButton button in skillButtons)
        {
            button.gameObject.SetActive(false);
        }
    }
}