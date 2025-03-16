using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SkillButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private string skillName;
    private string skillDescription;
    private SkillDescriptionUI uiManager;

    void Awake()
    {
        uiManager = FindFirstObjectByType<SkillDescriptionUI>();
    }

    public void SetUp(string name, string description, Sprite icon)
    {
        skillName = name;
        skillDescription = description;
        GetComponent<Image>().sprite = icon;
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (uiManager != null)
        {
            uiManager.ShowSkillDetail(skillName, skillDescription, transform.position);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (uiManager != null)
        {
            uiManager.CloseSkillDetail();
        }
    }
}