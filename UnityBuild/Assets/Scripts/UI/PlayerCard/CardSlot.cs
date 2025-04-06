using DataSystem.Database;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image skillIcon;
    [SerializeField] private GameObject tooltip;
    [SerializeField] private TMP_Text tooltipText;

    public void Init(Database.PlayerCardData cardData)
    {
        if (cardData.StatType == PlayerStatType.Health || cardData.StatType == PlayerStatType.Speed ||
            cardData.StatType == PlayerStatType.AttackPower)
        {
            var skillData = Database.GetBattleIcon(cardData.StatType);
            if (skillData != null)
            {
                GetComponent<Image>().sprite = skillData;
            }
        }
        else if (cardData.StatType == PlayerStatType.Special)
        {
            var skillData = Database.GetAttackData(cardData.AppliedSkill+100);
            if (skillData != null)
            {
                GetComponent<Image>().sprite = skillData.Icon;
            }
        }
        else
        {
            var skillData = Database.GetAttackData(cardData.AppliedSkill);
            if (skillData != null)
            {
                GetComponent<Image>().sprite = skillData.Icon;
            }
        }
        
        skillIcon.sprite = Database.GetCardIcon(cardData.StatType);

        // 툴팁 텍스트 설정
        tooltipText.text = cardData.Name;
        tooltip.SetActive(false); // 기본은 숨김
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        tooltip.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        tooltip.SetActive(false);
    }
}