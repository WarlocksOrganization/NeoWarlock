using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SkillButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private string skillName;
    private string skillDescription;
    [SerializeField] private SkillDescriptionUI skillDescriptionUI;

    public void SetUp(string name, string description, Sprite icon)
    {
        skillName = name;
        skillDescription = description;
        GetComponent<Image>().sprite = icon;
        skillDescriptionUI.Setup(icon, skillName, skillDescription);
    }
    
    public void OnPointerEnter(PointerEventData eventData)
    {
        skillDescriptionUI.gameObject.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (skillDescriptionUI != null)
        {
            skillDescriptionUI.gameObject.SetActive(false);
        }
    }
}