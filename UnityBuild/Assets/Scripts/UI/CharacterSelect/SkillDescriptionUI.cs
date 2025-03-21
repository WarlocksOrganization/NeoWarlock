using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillDescriptionUI : MonoBehaviour
{
    public TextMeshProUGUI skillNameText;
    public TextMeshProUGUI skillDescriptionText; 
    public Image skillIcon;

    public void Setup(Sprite icon, string skillName, string skillDescription)
    {
        skillIcon.sprite = icon;
        skillNameText.text = skillName;
        skillDescriptionText.text = skillDescription;
    }

    public void ShowSkillDetail()
    {
        //transform.position = buttonPosition;
        if (skillNameText.text != "") {
            gameObject.SetActive(true);
        }
    }

    public void CloseSkillDetail()
    {
        gameObject.SetActive(false);
    }
}